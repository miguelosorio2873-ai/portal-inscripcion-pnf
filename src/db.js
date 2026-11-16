/**
 * Capa de acceso a datos.
 *
 * La base de datos vive en MySQL/MariaDB (XAMPP) y es compartida por el
 * portal web y la aplicacion de escritorio en Visual Basic (.NET).
 *
 * Modelo unico: la inscripcion se organiza por TRAYECTO y por TIPO DE INGRESO
 * (OPSU / FLOTANTE). No se usa turno.
 *
 * Configuracion (variables de entorno):
 *   DB_HOST, DB_PORT, DB_USER, DB_PASSWORD, DB_NAME
 */
import mysql from 'mysql2/promise';
import { randomUUID } from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { DB_NAME, DB_SETTINGS } from './config-db.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
export const ROOT_DIR = path.resolve(__dirname, '..');

export { DB_NAME, DB_SETTINGS };

async function crearBaseDatos() {
  let administrador;
  try {
    administrador = await mysql.createConnection(DB_SETTINGS);
    await administrador.query(
      `CREATE DATABASE IF NOT EXISTS \`${DB_NAME}\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci`
    );
  } catch (error) {
    throw new Error(
      `No se pudo conectar a MySQL en ${DB_SETTINGS.host}:${DB_SETTINGS.port} ` +
        `(usuario ${DB_SETTINGS.user}, base ${DB_NAME}). ` +
        'Verifica que MySQL/MariaDB de XAMPP este iniciado y que las credenciales sean correctas.',
      { cause: error }
    );
  } finally {
    if (administrador) await administrador.end();
  }
}

await crearBaseDatos();

const limiteConexiones = Number(process.env.DB_CONNECTION_LIMIT || 10);

export const pool = mysql.createPool({
  ...DB_SETTINGS,
  database: DB_NAME,
  waitForConnections: true,
  connectionLimit: Number.isInteger(limiteConexiones) && limiteConexiones > 0 ? limiteConexiones : 10,
  queueLimit: 0,
  charset: 'utf8mb4',
  // Las fechas se devuelven como texto "YYYY-MM-DD HH:MM:SS" para que las
  // plantillas y la app de escritorio conserven el mismo formato local.
  dateStrings: true,
  decimalNumbers: true,
});

/* -------------------------------------------------------------------------- */
/* Esquema                                                                    */
/* -------------------------------------------------------------------------- */

const SENTENCIAS_ESQUEMA = [
  `CREATE TABLE IF NOT EXISTS carreras (
     id           INT UNSIGNED NOT NULL AUTO_INCREMENT,
     codigo       VARCHAR(10)  NOT NULL,
     nombre       VARCHAR(120) NOT NULL,
     descripcion  TEXT NULL,
     duracion     VARCHAR(50)  NOT NULL DEFAULT '5 trayectos',
     PRIMARY KEY (id),
     UNIQUE KEY uq_carreras_codigo (codigo),
     UNIQUE KEY uq_carreras_nombre (nombre)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS estudiantes (
     id                 INT UNSIGNED NOT NULL AUTO_INCREMENT,
     cedula             VARCHAR(20)  NOT NULL,
     nombres            VARCHAR(120) NOT NULL,
     apellidos          VARCHAR(120) NOT NULL,
     email              VARCHAR(190) NULL,
     telefono           VARCHAR(30)  NULL,
     fecha_nacimiento   DATE         NULL,
     sexo               CHAR(1)      NULL,
     direccion          VARCHAR(255) NULL,
     password_hash      VARCHAR(255) NULL,
     modalidad          ENUM('OPSU', 'FLOTANTE') NOT NULL DEFAULT 'FLOTANTE',
     opsu_estatus       ENUM('NO_VERIFICADO', 'COINCIDE', 'NO_APARECE', 'DATOS_DISTINTOS', 'CARRERA_DISTINTA', 'NO_REGISTRADO')
                        NOT NULL DEFAULT 'NO_VERIFICADO',
     opsu_verificado_en DATETIME NULL,
     creado_en          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
     PRIMARY KEY (id),
     UNIQUE KEY uq_estudiantes_cedula (cedula),
     UNIQUE KEY uq_estudiantes_email (email),
     KEY idx_estudiantes_modalidad (modalidad)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS asignaturas (
     id          INT UNSIGNED NOT NULL AUTO_INCREMENT,
     carrera_id  INT UNSIGNED NOT NULL,
     trayecto    TINYINT UNSIGNED NOT NULL,
     semestre    TINYINT UNSIGNED NULL,
     codigo      VARCHAR(20)  NOT NULL,
     nombre      VARCHAR(160) NOT NULL,
     PRIMARY KEY (id),
     UNIQUE KEY uq_asignaturas_carrera_codigo (carrera_id, codigo),
     KEY idx_asignaturas_carrera (carrera_id, semestre),
     KEY idx_asignaturas_trayecto (carrera_id, trayecto),
     CONSTRAINT fk_asignaturas_carrera
       FOREIGN KEY (carrera_id) REFERENCES carreras(id) ON DELETE CASCADE,
     CONSTRAINT chk_asignaturas_trayecto CHECK (trayecto BETWEEN 1 AND 5)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS inscripciones (
     id                 INT UNSIGNED NOT NULL AUTO_INCREMENT,
     codigo             VARCHAR(64) NOT NULL,
     estudiante_id      INT UNSIGNED NOT NULL,
     carrera_id         INT UNSIGNED NOT NULL,
     trayecto           TINYINT UNSIGNED NOT NULL,
     tipo_ingreso       ENUM('OPSU', 'FLOTANTE') NOT NULL DEFAULT 'FLOTANTE',
     semestre           TINYINT UNSIGNED NULL,
     turno              VARCHAR(20) NULL,
     condicion          ENUM('REGULAR', 'FLOTANTE') NULL,
     fecha_inscripcion  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
     estado             ENUM('REGISTRADA', 'IMPRESA', 'ANULADA') NOT NULL DEFAULT 'REGISTRADA',
     observaciones      TEXT NULL,
     impreso_en         DATETIME NULL,
     impresiones        INT UNSIGNED NOT NULL DEFAULT 0,
     PRIMARY KEY (id),
     UNIQUE KEY uq_inscripciones_codigo (codigo),
     UNIQUE KEY uq_inscripciones_estudiante_carrera_trayecto (estudiante_id, carrera_id, trayecto),
     KEY idx_inscripciones_estudiante (estudiante_id),
     KEY idx_inscripciones_trayecto (trayecto),
     KEY idx_inscripciones_tipo_ingreso (tipo_ingreso),
     CONSTRAINT fk_inscripciones_estudiante
       FOREIGN KEY (estudiante_id) REFERENCES estudiantes(id) ON DELETE CASCADE,
     CONSTRAINT fk_inscripciones_carrera
       FOREIGN KEY (carrera_id) REFERENCES carreras(id),
     CONSTRAINT chk_inscripciones_trayecto CHECK (trayecto BETWEEN 1 AND 5)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS inscripcion_asignaturas (
     inscripcion_id  INT UNSIGNED NOT NULL,
     asignatura_id   INT UNSIGNED NOT NULL,
     PRIMARY KEY (inscripcion_id, asignatura_id),
     CONSTRAINT fk_inscripcion_asignaturas_inscripcion
       FOREIGN KEY (inscripcion_id) REFERENCES inscripciones(id) ON DELETE CASCADE,
     CONSTRAINT fk_inscripcion_asignaturas_asignatura
       FOREIGN KEY (asignatura_id) REFERENCES asignaturas(id)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS opsu_cargas (
     id          INT UNSIGNED NOT NULL AUTO_INCREMENT,
     archivo     VARCHAR(255) NOT NULL,
     hash_sha256 CHAR(64)     NOT NULL,
     total_filas INT UNSIGNED NOT NULL DEFAULT 0,
     cargado_en  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
     PRIMARY KEY (id),
     UNIQUE KEY uq_opsu_cargas_hash (hash_sha256)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS opsu_registros (
     id            INT UNSIGNED NOT NULL AUTO_INCREMENT,
     carga_id      INT UNSIGNED NOT NULL,
     fila          INT UNSIGNED NOT NULL,
     cedula        VARCHAR(20)  NOT NULL,
     nombres       VARCHAR(160) NULL,
     apellidos     VARCHAR(160) NULL,
     sexo          CHAR(1)      NULL,
     carrera_opsu  VARCHAR(160) NULL,
     trayecto_opsu TINYINT UNSIGNED NULL,
     estatus_opsu  VARCHAR(60)  NULL,
     PRIMARY KEY (id),
     KEY idx_opsu_registros_carga (carga_id),
     KEY idx_opsu_registros_cedula (cedula),
     CONSTRAINT fk_opsu_registros_carga
       FOREIGN KEY (carga_id) REFERENCES opsu_cargas(id) ON DELETE CASCADE
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS opsu_comparaciones (
     id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
     carga_id        INT UNSIGNED NOT NULL,
     estudiante_id   INT UNSIGNED NULL,
     cedula          VARCHAR(20)  NOT NULL,
     nombre_opsu     VARCHAR(200) NULL,
     nombre_sistema  VARCHAR(200) NULL,
     carrera_opsu    VARCHAR(160) NULL,
     carrera_sistema VARCHAR(160) NULL,
     resultado       ENUM('COINCIDE', 'NO_APARECE_EN_OPSU', 'NO_REGISTRADO_EN_SISTEMA', 'DATOS_DISTINTOS', 'CARRERA_DISTINTA') NOT NULL,
     diferencias     VARCHAR(400) NULL,
     fecha           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
     PRIMARY KEY (id),
     KEY idx_opsu_comparaciones_carga (carga_id),
     KEY idx_opsu_comparaciones_cedula (cedula),
     CONSTRAINT fk_opsu_comparaciones_carga
       FOREIGN KEY (carga_id) REFERENCES opsu_cargas(id) ON DELETE CASCADE,
     CONSTRAINT fk_opsu_comparaciones_estudiante
       FOREIGN KEY (estudiante_id) REFERENCES estudiantes(id) ON DELETE SET NULL
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS bitacora (
     id      INT UNSIGNED NOT NULL AUTO_INCREMENT,
     accion  VARCHAR(60)  NOT NULL,
     detalle VARCHAR(400) NULL,
     usuario VARCHAR(80)  NOT NULL,
     fecha   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
     PRIMARY KEY (id),
     KEY idx_bitacora_fecha (fecha)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,
];

async function crearEsquema() {
  for (const sentencia of SENTENCIAS_ESQUEMA) {
    await pool.query(sentencia);
  }
}

/* ------------------------------ utilidades DDL ---------------------------- */

async function existeColumna(tabla, columna) {
  const [filas] = await pool.query(
    `SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = ? AND TABLE_NAME = ? AND COLUMN_NAME = ? LIMIT 1`,
    [DB_NAME, tabla, columna]
  );
  return filas.length > 0;
}

async function existeIndice(tabla, indice) {
  const [filas] = await pool.query(
    `SELECT 1 FROM information_schema.STATISTICS
      WHERE TABLE_SCHEMA = ? AND TABLE_NAME = ? AND INDEX_NAME = ? LIMIT 1`,
    [DB_NAME, tabla, indice]
  );
  return filas.length > 0;
}

async function columnaNulable(tabla, columna) {
  const [filas] = await pool.query(
    `SELECT IS_NULLABLE FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = ? AND TABLE_NAME = ? AND COLUMN_NAME = ? LIMIT 1`,
    [DB_NAME, tabla, columna]
  );
  return filas[0]?.IS_NULLABLE === 'YES';
}

/**
 * Ajustes sobre bases de datos ya creadas: agrega columnas nuevas sin perder
 * datos y migra los registros antiguos (semestre -> trayecto, turno fuera de uso).
 */
async function migrarEsquema() {
  /* ---- estudiantes ---- */
  if (!(await existeColumna('estudiantes', 'modalidad'))) {
    await pool.query(
      `ALTER TABLE estudiantes
         ADD COLUMN modalidad ENUM('OPSU', 'FLOTANTE') NOT NULL DEFAULT 'FLOTANTE' AFTER password_hash`
    );
  }
  if (!(await existeColumna('estudiantes', 'opsu_estatus'))) {
    await pool.query(
      `ALTER TABLE estudiantes
         ADD COLUMN opsu_estatus ENUM('NO_VERIFICADO', 'COINCIDE', 'NO_APARECE', 'DATOS_DISTINTOS', 'CARRERA_DISTINTA', 'NO_REGISTRADO')
         NOT NULL DEFAULT 'NO_VERIFICADO' AFTER modalidad`
    );
  }
  if (!(await existeColumna('estudiantes', 'opsu_verificado_en'))) {
    await pool.query('ALTER TABLE estudiantes ADD COLUMN opsu_verificado_en DATETIME NULL AFTER opsu_estatus');
  }
  if (!(await existeIndice('estudiantes', 'idx_estudiantes_modalidad'))) {
    await pool.query('ALTER TABLE estudiantes ADD KEY idx_estudiantes_modalidad (modalidad)');
  }
  if (!(await columnaNulable('estudiantes', 'email'))) {
    await pool.query('ALTER TABLE estudiantes MODIFY email VARCHAR(190) NULL');
  }
  if (!(await columnaNulable('estudiantes', 'password_hash'))) {
    await pool.query('ALTER TABLE estudiantes MODIFY password_hash VARCHAR(255) NULL');
  }

  /* ---- asignaturas: el nivel academico pasa a llamarse trayecto ---- */
  if (!(await existeColumna('asignaturas', 'trayecto'))) {
    await pool.query('ALTER TABLE asignaturas ADD COLUMN trayecto TINYINT UNSIGNED NULL AFTER carrera_id');
  }
  await pool.query('UPDATE asignaturas SET trayecto = semestre WHERE trayecto IS NULL AND semestre IS NOT NULL');
  await pool.query('UPDATE asignaturas SET trayecto = 1 WHERE trayecto IS NULL');
  if (await columnaNulable('asignaturas', 'trayecto')) {
    await pool.query('ALTER TABLE asignaturas MODIFY trayecto TINYINT UNSIGNED NOT NULL');
  }
  if (!(await existeIndice('asignaturas', 'idx_asignaturas_trayecto'))) {
    await pool.query('ALTER TABLE asignaturas ADD KEY idx_asignaturas_trayecto (carrera_id, trayecto)');
  }
  if (!(await columnaNulable('asignaturas', 'semestre'))) {
    await pool.query('ALTER TABLE asignaturas MODIFY semestre TINYINT UNSIGNED NULL');
  }

  /* ---- inscripciones ---- */
  if (!(await existeColumna('inscripciones', 'trayecto'))) {
    await pool.query('ALTER TABLE inscripciones ADD COLUMN trayecto TINYINT UNSIGNED NULL AFTER carrera_id');
  }
  if (!(await existeColumna('inscripciones', 'tipo_ingreso'))) {
    await pool.query(
      `ALTER TABLE inscripciones
         ADD COLUMN tipo_ingreso ENUM('OPSU', 'FLOTANTE') NULL AFTER trayecto`
    );
  }

  await pool.query('UPDATE inscripciones SET trayecto = semestre WHERE trayecto IS NULL AND semestre IS NOT NULL');
  await pool.query('UPDATE inscripciones SET trayecto = 1 WHERE trayecto IS NULL');
  await pool.query(
    `UPDATE inscripciones
        SET tipo_ingreso = IF(condicion = 'FLOTANTE', 'FLOTANTE', 'OPSU')
      WHERE tipo_ingreso IS NULL`
  );
  await pool.query("UPDATE inscripciones SET tipo_ingreso = 'FLOTANTE' WHERE tipo_ingreso IS NULL");

  if (await columnaNulable('inscripciones', 'trayecto')) {
    await pool.query('ALTER TABLE inscripciones MODIFY trayecto TINYINT UNSIGNED NOT NULL');
  }
  if (await columnaNulable('inscripciones', 'tipo_ingreso')) {
    await pool.query(
      `ALTER TABLE inscripciones
         MODIFY tipo_ingreso ENUM('OPSU', 'FLOTANTE') NOT NULL DEFAULT 'FLOTANTE'`
    );
  }
  if (!(await columnaNulable('inscripciones', 'semestre'))) {
    await pool.query('ALTER TABLE inscripciones MODIFY semestre TINYINT UNSIGNED NULL');
  }
  if (!(await columnaNulable('inscripciones', 'turno'))) {
    await pool.query('ALTER TABLE inscripciones MODIFY turno VARCHAR(20) NULL');
  }
  if (!(await columnaNulable('inscripciones', 'condicion'))) {
    await pool.query("ALTER TABLE inscripciones MODIFY condicion ENUM('REGULAR', 'FLOTANTE') NULL");
  }

  // La unicidad ahora es por trayecto, no por semestre.
  if (await existeIndice('inscripciones', 'uq_inscripciones_estudiante_carrera_semestre')) {
    await pool.query('ALTER TABLE inscripciones DROP INDEX uq_inscripciones_estudiante_carrera_semestre');
  }
  if (!(await existeIndice('inscripciones', 'uq_inscripciones_estudiante_carrera_trayecto'))) {
    await pool.query(
      'ALTER TABLE inscripciones ADD UNIQUE KEY uq_inscripciones_estudiante_carrera_trayecto (estudiante_id, carrera_id, trayecto)'
    );
  }
  if (!(await existeIndice('inscripciones', 'idx_inscripciones_trayecto'))) {
    await pool.query('ALTER TABLE inscripciones ADD KEY idx_inscripciones_trayecto (trayecto)');
  }
  if (!(await existeIndice('inscripciones', 'idx_inscripciones_tipo_ingreso'))) {
    await pool.query('ALTER TABLE inscripciones ADD KEY idx_inscripciones_tipo_ingreso (tipo_ingreso)');
  }
}

/* -------------------------------------------------------------------------- */
/* Datos iniciales                                                            */
/* -------------------------------------------------------------------------- */

const CARRERAS = [
  {
    codigo: 'INF',
    nombre: 'PNF Informática',
    descripcion: 'Formación en desarrollo de software, redes, bases de datos y seguridad informática.',
  },
  {
    codigo: 'VET',
    nombre: 'PNF Veterinaria',
    descripcion: 'Formación en salud animal, producción pecuaria y salud pública veterinaria.',
  },
  {
    codigo: 'ADM',
    nombre: 'PNF Administración',
    descripcion: 'Formación en gestión empresarial, finanzas, mercadeo y talento humano.',
  },
  {
    codigo: 'AGR',
    nombre: 'PNF Agroalimentaria',
    descripcion: 'Formación en producción vegetal, transformación agroindustrial e inocuidad alimentaria.',
  },
];

const ASIGNATURAS = {
  INF: [
    ['Matemática I', 'Introducción a la Informática', 'Lenguaje y Comunicación'],
    ['Matemática II', 'Algoritmos y Programación', 'Arquitectura del Computador'],
    ['Bases de Datos I', 'Programación Orientada a Objetos', 'Redes de Computadoras'],
    ['Bases de Datos II', 'Ingeniería de Software', 'Sistemas Operativos'],
    ['Desarrollo Web', 'Seguridad Informática', 'Proyecto Socio-Tecnológico'],
  ],
  VET: [
    ['Anatomía Animal I', 'Biología General', 'Química General'],
    ['Fisiología Animal', 'Bioquímica', 'Anatomía Animal II'],
    ['Microbiología Veterinaria', 'Nutrición Animal', 'Semiología'],
    ['Farmacología Veterinaria', 'Patología Veterinaria', 'Cirugía Básica'],
    ['Salud Pública Veterinaria', 'Producción Animal', 'Proyecto Socio-Comunitario'],
  ],
  ADM: [
    ['Contabilidad I', 'Fundamentos de Administración', 'Matemática Financiera'],
    ['Contabilidad II', 'Microeconomía', 'Estadística I'],
    ['Gerencia de Empresas', 'Macroeconomía', 'Derecho Mercantil'],
    ['Finanzas Corporativas', 'Mercadeo', 'Gestión del Talento Humano'],
    ['Planificación Estratégica', 'Formulación de Proyectos', 'Presupuesto Empresarial'],
  ],
  AGR: [
    ['Botánica General', 'Química Agrícola', 'Introducción a la Agroalimentaria'],
    ['Suelos y Fertilizantes', 'Fisiología Vegetal', 'Maquinaria Agrícola'],
    ['Producción Vegetal', 'Zootecnia General', 'Inocuidad Alimentaria'],
    ['Transformación Agroindustrial', 'Riego y Drenaje', 'Control de Plagas'],
    ['Gestión Agroempresarial', 'Tecnología de Alimentos', 'Proyecto Socio-Productivo'],
  ],
};

async function sembrarDatos() {
  for (const carrera of CARRERAS) {
    await pool.execute(
      'INSERT IGNORE INTO carreras (codigo, nombre, descripcion) VALUES (?, ?, ?)',
      [carrera.codigo, carrera.nombre, carrera.descripcion]
    );

    const [filas] = await pool.execute('SELECT id FROM carreras WHERE codigo = ?', [carrera.codigo]);
    if (filas.length === 0) {
      throw new Error(`No se pudo crear la carrera ${carrera.codigo}.`);
    }
    const carreraId = Number(filas[0].id);

    const [conteo] = await pool.execute(
      'SELECT COUNT(*) AS total FROM asignaturas WHERE carrera_id = ?',
      [carreraId]
    );
    if (Number(conteo[0].total) > 0) continue;

    const trayectos = ASIGNATURAS[carrera.codigo];
    for (let indice = 0; indice < trayectos.length; indice += 1) {
      const trayecto = indice + 1;
      const materias = trayectos[indice];
      for (let posicion = 0; posicion < materias.length; posicion += 1) {
        const codigo = `${carrera.codigo}-${trayecto}${String(posicion + 1).padStart(2, '0')}`;
        await pool.execute(
          'INSERT IGNORE INTO asignaturas (carrera_id, trayecto, semestre, codigo, nombre) VALUES (?, ?, ?, ?, ?)',
          [carreraId, trayecto, trayecto, codigo, materias[posicion]]
        );
      }
    }
  }
}

await crearEsquema();
await migrarEsquema();
await sembrarDatos();

/* -------------------------------------------------------------------------- */
/* Consultas                                                                  */
/* -------------------------------------------------------------------------- */

export async function listarCarreras() {
  const [filas] = await pool.query('SELECT * FROM carreras ORDER BY id');
  return filas;
}

export async function obtenerCarrera(id) {
  const [filas] = await pool.execute('SELECT * FROM carreras WHERE id = ?', [Number(id)]);
  return filas[0];
}

export async function listarAsignaturas(carreraId, trayecto) {
  const [filas] = await pool.execute(
    'SELECT * FROM asignaturas WHERE carrera_id = ? AND trayecto = ? ORDER BY codigo',
    [Number(carreraId), Number(trayecto)]
  );
  return filas;
}

export async function crearEstudiante(datos) {
  const [resultado] = await pool.execute(
    `INSERT INTO estudiantes
       (cedula, nombres, apellidos, email, telefono, fecha_nacimiento, sexo, direccion, password_hash)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
    [
      datos.cedula,
      datos.nombres,
      datos.apellidos,
      datos.email,
      datos.telefono || null,
      datos.fecha_nacimiento || null,
      datos.sexo || null,
      datos.direccion || null,
      datos.password_hash,
    ]
  );
  return Number(resultado.insertId);
}

export async function estudiantePorId(id) {
  const [filas] = await pool.execute(
    `SELECT id, cedula, nombres, apellidos, email, telefono, fecha_nacimiento,
            sexo, direccion, modalidad, opsu_estatus, creado_en
       FROM estudiantes WHERE id = ?`,
    [Number(id)]
  );
  return filas[0];
}

export async function estudiantePorCedula(cedula) {
  const [filas] = await pool.execute('SELECT * FROM estudiantes WHERE cedula = ?', [cedula]);
  return filas[0];
}

export async function estudiantePorEmail(email) {
  const [filas] = await pool.execute('SELECT * FROM estudiantes WHERE email = ?', [email]);
  return filas[0];
}

export async function crearInscripcion({ estudianteId, carreraId, trayecto, tipoIngreso, asignaturaIds, observaciones }) {
  const conexion = await pool.getConnection();
  try {
    await conexion.beginTransaction();

    const temporal = `TMP-${randomUUID()}`;
    const [resultado] = await conexion.execute(
      `INSERT INTO inscripciones (codigo, estudiante_id, carrera_id, trayecto, tipo_ingreso, observaciones)
       VALUES (?, ?, ?, ?, ?, ?)`,
      [
        temporal,
        Number(estudianteId),
        Number(carreraId),
        Number(trayecto),
        tipoIngreso === 'OPSU' ? 'OPSU' : 'FLOTANTE',
        observaciones || null,
      ]
    );

    const id = Number(resultado.insertId);
    const codigo = `INS-${new Date().getFullYear()}-${String(id).padStart(6, '0')}`;
    await conexion.execute('UPDATE inscripciones SET codigo = ? WHERE id = ?', [codigo, id]);

    for (const asignaturaId of asignaturaIds) {
      await conexion.execute(
        'INSERT IGNORE INTO inscripcion_asignaturas (inscripcion_id, asignatura_id) VALUES (?, ?)',
        [id, Number(asignaturaId)]
      );
    }

    await conexion.commit();
    return codigo;
  } catch (error) {
    await conexion.rollback();
    throw error;
  } finally {
    conexion.release();
  }
}

export async function inscripcionesDeEstudiante(estudianteId) {
  const [filas] = await pool.execute(
    `SELECT i.*, c.nombre AS carrera, c.codigo AS carrera_codigo,
            (SELECT COUNT(*) FROM inscripcion_asignaturas ia WHERE ia.inscripcion_id = i.id) AS total_asignaturas
       FROM inscripciones i
       JOIN carreras c ON c.id = i.carrera_id
      WHERE i.estudiante_id = ?
      ORDER BY i.fecha_inscripcion DESC, i.id DESC`,
    [Number(estudianteId)]
  );
  return filas;
}

export async function obtenerInscripcion(codigo) {
  const [filas] = await pool.execute(
    `SELECT i.*, c.nombre AS carrera, c.codigo AS carrera_codigo,
            e.cedula, e.nombres, e.apellidos, e.email, e.telefono,
            e.fecha_nacimiento, e.sexo, e.direccion, e.modalidad
       FROM inscripciones i
       JOIN carreras c ON c.id = i.carrera_id
       JOIN estudiantes e ON e.id = i.estudiante_id
      WHERE i.codigo = ?`,
    [codigo]
  );
  return filas[0];
}

export async function asignaturasDeInscripcion(inscripcionId) {
  const [filas] = await pool.execute(
    `SELECT a.codigo, a.nombre, a.trayecto
       FROM inscripcion_asignaturas ia
       JOIN asignaturas a ON a.id = ia.asignatura_id
      WHERE ia.inscripcion_id = ?
      ORDER BY a.codigo`,
    [Number(inscripcionId)]
  );
  return filas;
}

export async function contarInscripcionesPorCarrera() {
  const [filas] = await pool.query(
    `SELECT c.id, c.nombre, c.codigo, COUNT(i.id) AS total
       FROM carreras c
       LEFT JOIN inscripciones i ON i.carrera_id = c.id AND i.estado <> 'ANULADA'
      GROUP BY c.id, c.nombre, c.codigo
      ORDER BY c.id`
  );
  return filas;
}

export async function totalInscripciones() {
  const [filas] = await pool.query(
    "SELECT COUNT(*) AS total FROM inscripciones WHERE estado <> 'ANULADA'"
  );
  return Number(filas[0].total);
}
