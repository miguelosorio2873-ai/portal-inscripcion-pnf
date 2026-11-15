-- ============================================================================
-- Portal de Inscripción Universitaria · PNF
-- Esquema de la base de datos MySQL/MariaDB compartida por la web y Visual Basic.
--
-- Base por defecto: portal_pnf (XAMPP, 127.0.0.1:3306, usuario root sin clave)
-- La web crea la base y las tablas automáticamente al iniciar.
-- Este documento es la referencia del modelo de datos.
-- ============================================================================

CREATE DATABASE IF NOT EXISTS portal_pnf
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE portal_pnf;

-- ---------------------------------------------------------------------------
-- Catálogo de carreras (Programas Nacionales de Formación)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS carreras (
  id           INT UNSIGNED NOT NULL AUTO_INCREMENT,
  codigo       VARCHAR(10)  NOT NULL,          -- INF, VET, ADM, AGR
  nombre       VARCHAR(120) NOT NULL,          -- PNF Informática, etc.
  descripcion  TEXT NULL,
  duracion     VARCHAR(50)  NOT NULL DEFAULT '5 semestres',
  PRIMARY KEY (id),
  UNIQUE KEY uq_carreras_codigo (codigo),
  UNIQUE KEY uq_carreras_nombre (nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Estudiantes registrados en el portal
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS estudiantes (
  id                INT UNSIGNED NOT NULL AUTO_INCREMENT,
  cedula            VARCHAR(20)  NOT NULL,     -- formato V-12345678
  nombres           VARCHAR(120) NOT NULL,
  apellidos         VARCHAR(120) NOT NULL,
  email             VARCHAR(190) NOT NULL,
  telefono          VARCHAR(30)  NULL,
  fecha_nacimiento  DATE         NULL,         -- YYYY-MM-DD
  sexo              CHAR(1)      NULL,         -- F, M, O
  direccion         VARCHAR(255) NULL,
  password_hash     VARCHAR(255) NOT NULL,     -- scrypt$sal$hash
  creado_en         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_estudiantes_cedula (cedula),
  UNIQUE KEY uq_estudiantes_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Asignaturas por carrera y semestre (3 por semestre en los datos iniciales)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asignaturas (
  id          INT UNSIGNED NOT NULL AUTO_INCREMENT,
  carrera_id  INT UNSIGNED NOT NULL,
  semestre    TINYINT UNSIGNED NOT NULL,
  codigo      VARCHAR(20)  NOT NULL,           -- INF-101, VET-203, etc.
  nombre      VARCHAR(160) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_asignaturas_carrera_codigo (carrera_id, codigo),
  KEY idx_asignaturas_carrera (carrera_id, semestre),
  CONSTRAINT fk_asignaturas_carrera
    FOREIGN KEY (carrera_id) REFERENCES carreras(id) ON DELETE CASCADE,
  CONSTRAINT chk_asignaturas_semestre CHECK (semestre BETWEEN 1 AND 5)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Inscripciones: una por estudiante, carrera y semestre
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inscripciones (
  id                 INT UNSIGNED NOT NULL AUTO_INCREMENT,
  codigo             VARCHAR(64) NOT NULL,     -- INS-2026-000001
  estudiante_id      INT UNSIGNED NOT NULL,
  carrera_id         INT UNSIGNED NOT NULL,
  semestre           TINYINT UNSIGNED NOT NULL,
  turno              VARCHAR(20) NOT NULL,     -- Mañana, Tarde, Noche
  fecha_inscripcion  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  estado             ENUM('REGISTRADA', 'IMPRESA', 'ANULADA') NOT NULL DEFAULT 'REGISTRADA',
  observaciones      TEXT NULL,
  impreso_en         DATETIME NULL,            -- se actualiza desde Visual Basic
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Asignaturas seleccionadas en cada inscripción
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inscripcion_asignaturas (
  inscripcion_id  INT UNSIGNED NOT NULL,
  asignatura_id   INT UNSIGNED NOT NULL,
  PRIMARY KEY (inscripcion_id, asignatura_id),
  CONSTRAINT fk_inscripcion_asignaturas_inscripcion
    FOREIGN KEY (inscripcion_id) REFERENCES inscripciones(id) ON DELETE CASCADE,
  CONSTRAINT fk_inscripcion_asignaturas_asignatura
    FOREIGN KEY (asignatura_id) REFERENCES asignaturas(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Consulta usada por la aplicación de Visual Basic para el listado
-- ---------------------------------------------------------------------------
-- SELECT i.id, i.codigo, e.cedula,
--        CONCAT(e.nombres, ' ', e.apellidos) AS estudiante,
--        c.nombre AS carrera, i.semestre, i.turno,
--        i.fecha_inscripcion, i.estado, i.impresiones
--   FROM inscripciones i
--   JOIN estudiantes e ON e.id = i.estudiante_id
--   JOIN carreras c ON c.id = i.carrera_id
--  ORDER BY i.fecha_inscripcion DESC, i.id DESC;

-- ---------------------------------------------------------------------------
-- Actualización que ejecuta Visual Basic al imprimir un comprobante
-- ---------------------------------------------------------------------------
-- UPDATE inscripciones
--    SET estado = CASE WHEN estado = 'ANULADA' THEN 'ANULADA' ELSE 'IMPRESA' END,
--        impreso_en = NOW(),
--        impresiones = impresiones + 1
--  WHERE id = ?;
