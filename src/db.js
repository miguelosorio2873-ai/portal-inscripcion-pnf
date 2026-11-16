/**
 * Capa de acceso a datos.
 *
 * La base de datos vive en MySQL/MariaDB (XAMPP) y es compartida por el
 * portal web y la aplicacion de escritorio en Visual Basic (.NET).
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
     duracion     VARCHAR(50)  NOT NULL DEFAULT '5 semestres',
     PRIMARY KEY (id),
     UNIQUE KEY uq_carreras_codigo (codigo),
     UNIQUE KEY uq_carreras_nombre (nombre)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS estudiantes (
     id                INT UNSIGNED NOT NULL AUTO_INCREMENT,
     cedula            VARCHAR(20)  NOT NULL,
     nombres           VARCHAR(120) NOT NULL,
     apellidos         VARCHAR(120) NOT NULL,
     email             VARCHAR(190) NOT NULL,
     telefono          VARCHAR(30)  NULL,
     fecha_nacimiento  DATE         NULL,
     sexo              CHAR(1)      NULL,
     direccion         VARCHAR(255) NULL,
     password_hash     VARCHAR(255) NOT NULL,
     creado_en         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
     PRIMARY KEY (id),
     UNIQUE KEY uq_estudiantes_cedula (cedula),
     UNIQUE KEY uq_estudiantes_email (email)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS asignaturas (
     id          INT UNSIGNED NOT NULL AUTO_INCREMENT,
     carrera_id  INT UNSIGNED NOT NULL,
     semestre    TINYINT UNSIGNED NOT NULL,
     codigo      VARCHAR(20)  NOT NULL,
     nombre      VARCHAR(160) NOT NULL,
     PRIMARY KEY (id),
     UNIQUE KEY uq_asignaturas_carrera_codigo (carrera_id, codigo),
     KEY idx_asignaturas_carrera (carrera_id, semestre),
     CONSTRAINT fk_asignaturas_carrera
       FOREIGN KEY (carrera_id) REFERENCES carreras(id) ON DELETE CASCADE,
     CONSTRAINT chk_asignaturas_semestre CHECK (semestre BETWEEN 1 AND 5)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,

  `CREATE TABLE IF NOT EXISTS inscripciones (
     id                 INT UNSIGNED NOT NULL AUTO_INCREMENT,
     codigo             VARCHAR(64) NOT NULL,
     estudiante_id      INT UNSIGNED NOT NULL,
     carrera_id         INT UNSIGNED NOT NULL,
     semestre           TINYINT UNSIGNED NOT NULL,
     turno              VARCHAR(20) NOT NULL,
     condicion          ENUM('REGULAR', 'FLOTANTE') NOT NULL DEFAULT 'REGULAR',
     fecha_inscripcion  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
     estado             ENUM('REGISTRADA', 'IMPRESA', 'ANULADA') NOT NULL DEFAULT 'REGISTRADA',
     observaciones      TEXT NULL,
     impreso_en         DATETIME NULL,
     impresiones        INT UNSIGNED NOT NULL DEFAULT 0,
     PRIMARY KEY (id),
     UNIQUE KEY uq_inscripciones_codigo (codigo),
     UNIQUE KEY uq_inscripciones_estudiante_carrera_semestre (estudiante_id, carrera_id, semestre),
     KEY idx_inscripciones_estudiante (estudiante_id),
     CONSTRAINT fk_inscripciones_estudiante
       FOREIGN KEY (estudiante_id) REFERENCES estudiantes(id) ON DELETE CASCADE,
     CONSTRAINT fk_inscripciones_carrera
       FOREIGN KEY (carrera_id) REFERENCES carreras(id),
     CONSTRAINT chk_inscripciones_semestre CHECK (semestre BETWEEN 1 AND 5)
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
];

async function crearEsquema() {
  for (const sentencia of SENTENCIAS_ESQUEMA) {
    await pool.query(sentencia);
  }
}

/**
 * Ajustes sobre bases de datos ya creadas: agrega columnas nuevas sin perder datos.
 */
async function migrarEsquema() {
  const [columnas] = await pool.query(
    `SELECT COLUMN_NAME
       FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = ? AND TABLE_NAME = 'inscripciones' AND COLUMN_NAME = 'condicion'`,
    [DB_NAME]
  );

  if (columnas.length === 0) {
    await pool.query(
      `ALTER TABLE inscripciones
         ADD COLUMN condicion ENUM('REGULAR', 'FLOTANTE') NOT NULL DEFAULT 'REGULAR' AFTER turno`
    );
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

    const semestres = ASIGNATURAS[carrera.codigo];
    for (let indice = 0; indice < semestres.length; indice += 1) {
      const semestre = indice + 1;
      const materias = semestres[indice];
      for (let posicion = 0; posicion < materias.length; posicion += 1) {
        const codigo = `${carrera.codigo}-${semestre}${String(posicion + 1).padStart(2, '0')}`;
        await pool.execute(
          'INSERT IGNORE INTO asignaturas (carrera_id, semestre, codigo, nombre) VALUES (?, ?, ?, ?)',
          [carreraId, semestre, codigo, materias[posicion]]
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

export async function listarAsignaturas(carreraId, semestre) {
  const [filas] = await pool.execute(
    'SELECT * FROM asignaturas WHERE carrera_id = ? AND semestre = ? ORDER BY codigo',
    [Number(carreraId), Number(semestre)]
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
            sexo, direccion, creado_en
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

export async function crearInscripcion({ estudianteId, carreraId, semestre, turno, condicion, asignaturaIds, observaciones }) {
  const conexion = await pool.getConnection();
  try {
    await conexion.beginTransaction();

    const temporal = `TMP-${randomUUID()}`;
    const [resultado] = await conexion.execute(
      `INSERT INTO inscripciones (codigo, estudiante_id, carrera_id, semestre, turno, condicion, observaciones)
       VALUES (?, ?, ?, ?, ?, ?, ?)`,
      [
        temporal,
        Number(estudianteId),
        Number(carreraId),
        Number(semestre),
        turno,
        condicion === 'FLOTANTE' ? 'FLOTANTE' : 'REGULAR',
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
            e.fecha_nacimiento, e.sexo, e.direccion
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
    `SELECT a.codigo, a.nombre, a.semestre
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
