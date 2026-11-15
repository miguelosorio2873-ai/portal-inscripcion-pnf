/**
 * Migra una sola vez los datos del archivo SQLite historico
 * (data/inscripciones.db) a la base MySQL/MariaDB del portal.
 *
 * Uso:
 *   npm run db:migrar
 *
 * La base MySQL debe estar vacia de estudiantes/inscripciones. Las carreras y
 * asignaturas iniciales se crean automaticamente. El archivo SQLite no se
 * modifica ni se elimina.
 */
import { DatabaseSync } from 'node:sqlite';
import { existsSync } from 'node:fs';
import path from 'node:path';
import { DB_NAME, ROOT_DIR, pool } from '../src/db.js';

const rutaSqlite = process.env.SQLITE_PATH
  ? path.resolve(process.env.SQLITE_PATH)
  : path.join(ROOT_DIR, 'data', 'inscripciones.db');

let sqlite;
let conexion;

try {
  if (!existsSync(rutaSqlite)) {
    throw new Error(`No se encontro el archivo SQLite: ${rutaSqlite}`);
  }

  sqlite = new DatabaseSync(rutaSqlite, { readOnly: true });
  conexion = await pool.getConnection();

  const [filasConteo] = await conexion.query('SELECT COUNT(*) AS total FROM estudiantes');
  if (Number(filasConteo[0].total) > 0) {
    throw new Error(
      `La base MySQL ${DB_NAME} ya tiene estudiantes. ` +
        'Para migrar desde cero ejecuta primero "npm run db:reset" y luego "npm run db:migrar".'
    );
  }

  const carrerasSqlite = sqlite.prepare('SELECT * FROM carreras ORDER BY id').all();
  const asignaturasSqlite = sqlite.prepare('SELECT * FROM asignaturas ORDER BY id').all();
  const estudiantesSqlite = sqlite.prepare('SELECT * FROM estudiantes ORDER BY id').all();
  const inscripcionesSqlite = sqlite.prepare('SELECT * FROM inscripciones ORDER BY id').all();
  const asignacionesSqlite = sqlite
    .prepare('SELECT inscripcion_id, asignatura_id FROM inscripcion_asignaturas')
    .all();

  const [carrerasMysql] = await conexion.query('SELECT id, codigo FROM carreras');
  const mapaCarreras = new Map(carrerasMysql.map((c) => [c.codigo, Number(c.id)]));

  const [asignaturasMysql] = await conexion.query(
    'SELECT id, carrera_id, codigo FROM asignaturas'
  );
  const mapaAsignaturasMysql = new Map(
    asignaturasMysql.map((a) => [`${Number(a.carrera_id)}|${a.codigo}`, Number(a.id)])
  );

  const codigoCarreraSqlite = new Map(carrerasSqlite.map((c) => [Number(c.id), c.codigo]));
  const mapaAsignaturas = new Map();
  for (const asignatura of asignaturasSqlite) {
    const carreraCodigo = codigoCarreraSqlite.get(Number(asignatura.carrera_id));
    const carreraMysqlId = mapaCarreras.get(carreraCodigo);
    const clave = `${carreraMysqlId}|${asignatura.codigo}`;
    const asignaturaMysqlId = mapaAsignaturasMysql.get(clave);
    if (!asignaturaMysqlId) {
      throw new Error(`No se encontro la asignatura ${clave} en MySQL.`);
    }
    mapaAsignaturas.set(Number(asignatura.id), asignaturaMysqlId);
  }

  await conexion.beginTransaction();

  for (const estudiante of estudiantesSqlite) {
    await conexion.execute(
      `INSERT INTO estudiantes
         (id, cedula, nombres, apellidos, email, telefono, fecha_nacimiento,
          sexo, direccion, password_hash, creado_en)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, COALESCE(?, CURRENT_TIMESTAMP))`,
      [
        Number(estudiante.id),
        estudiante.cedula,
        estudiante.nombres,
        estudiante.apellidos,
        estudiante.email,
        estudiante.telefono || null,
        estudiante.fecha_nacimiento || null,
        estudiante.sexo || null,
        estudiante.direccion || null,
        estudiante.password_hash,
        estudiante.creado_en || null,
      ]
    );
  }

  for (const inscripcion of inscripcionesSqlite) {
    const carreraCodigo = codigoCarreraSqlite.get(Number(inscripcion.carrera_id));
    const carreraMysqlId = mapaCarreras.get(carreraCodigo);
    if (!carreraMysqlId) {
      throw new Error(`No se encontro la carrera ${carreraCodigo} en MySQL.`);
    }

    await conexion.execute(
      `INSERT INTO inscripciones
         (id, codigo, estudiante_id, carrera_id, semestre, turno,
          fecha_inscripcion, estado, observaciones, impreso_en, impresiones)
       VALUES (?, ?, ?, ?, ?, ?, COALESCE(?, CURRENT_TIMESTAMP), ?, ?, ?, ?)`,
      [
        Number(inscripcion.id),
        inscripcion.codigo,
        Number(inscripcion.estudiante_id),
        carreraMysqlId,
        Number(inscripcion.semestre),
        inscripcion.turno,
        inscripcion.fecha_inscripcion || null,
        inscripcion.estado,
        inscripcion.observaciones || null,
        inscripcion.impreso_en || null,
        Number(inscripcion.impresiones || 0),
      ]
    );
  }

  for (const asignacion of asignacionesSqlite) {
    const asignaturaMysqlId = mapaAsignaturas.get(Number(asignacion.asignatura_id));
    if (!asignaturaMysqlId) {
      throw new Error(`No se encontro la asignatura ${asignacion.asignatura_id} en MySQL.`);
    }
    await conexion.execute(
      'INSERT IGNORE INTO inscripcion_asignaturas (inscripcion_id, asignatura_id) VALUES (?, ?)',
      [Number(asignacion.inscripcion_id), asignaturaMysqlId]
    );
  }

  await conexion.commit();

  const [verificacion] = await conexion.query(
    `SELECT
       (SELECT COUNT(*) FROM estudiantes) AS estudiantes,
       (SELECT COUNT(*) FROM inscripciones) AS inscripciones,
       (SELECT COUNT(*) FROM inscripcion_asignaturas) AS asignaciones,
       (SELECT COUNT(*) FROM carreras) AS carreras,
       (SELECT COUNT(*) FROM asignaturas) AS asignaturas`
  );
  const resumen = verificacion[0];

  const esperado = {
    estudiantes: estudiantesSqlite.length,
    inscripciones: inscripcionesSqlite.length,
    asignaciones: asignacionesSqlite.length,
  };

  console.log('');
  console.log(`Migracion SQLite -> MySQL (${DB_NAME}) completada.`);
  console.log(`Archivo SQLite: ${rutaSqlite}`);
  console.log(`Estudiantes: ${resumen.estudiantes} (esperados ${esperado.estudiantes})`);
  console.log(`Inscripciones: ${resumen.inscripciones} (esperadas ${esperado.inscripciones})`);
  console.log(`Asignaturas inscritas: ${resumen.asignaciones} (esperadas ${esperado.asignaciones})`);
  console.log(`Carreras: ${resumen.carreras} · Asignaturas de catalogo: ${resumen.asignaturas}`);
  console.log('');

  if (
    Number(resumen.estudiantes) !== esperado.estudiantes ||
    Number(resumen.inscripciones) !== esperado.inscripciones ||
    Number(resumen.asignaciones) !== esperado.asignaciones
  ) {
    throw new Error('Los conteos migrados no coinciden con el archivo SQLite.');
  }
} catch (error) {
  if (conexion) {
    try {
      await conexion.rollback();
    } catch {
      /* la transaccion pudo no haberse iniciado */
    }
  }
  console.error('');
  console.error('No se pudo completar la migracion:');
  console.error(error.message || error);
  process.exitCode = 1;
} finally {
  if (conexion) conexion.release();
  if (sqlite) sqlite.close();
  await pool.end();
}
