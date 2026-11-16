-- ============================================================================
--  Portal de Inscripción Universitaria · PNF
--  Esquema único compartido por el portal web y la aplicación Visual Basic.
--
--  Base: portal_pnf  ·  MySQL / MariaDB (XAMPP)
--  Modelo: TRAYECTO (I a V) + TIPO DE INGRESO (OPSU / FLOTANTE).
--  NO existen turnos (mañana / tarde / noche).
--
--  Este archivo documenta el esquema real. La creación y las migraciones las
--  realizan automáticamente src/db.js (portal) y vb/InscripcionOPSU/Esquema.vb
--  (escritorio); ambos son idempotentes y no borran datos.
-- ============================================================================

CREATE DATABASE IF NOT EXISTS `portal_pnf`
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `portal_pnf`;

-- ---------------------------------------------------------------------------
-- Catálogos
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS `carreras` (
  `id`          INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `codigo`      VARCHAR(10)  NOT NULL,
  `nombre`      VARCHAR(120) NOT NULL,
  `descripcion` TEXT         NULL,
  `duracion`    VARCHAR(50)  NOT NULL DEFAULT '5 trayectos',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_carreras_codigo` (`codigo`),
  UNIQUE KEY `uq_carreras_nombre` (`nombre`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `estudiantes` (
  `id`                 INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `cedula`             VARCHAR(20)  NOT NULL,
  `nombres`            VARCHAR(120) NOT NULL,
  `apellidos`          VARCHAR(120) NOT NULL,
  -- La web exige correo y contraseña; el escritorio puede registrar
  -- estudiantes sin cuenta web, por eso ambas columnas son NULLables.
  `email`              VARCHAR(190) NULL,
  `telefono`           VARCHAR(30)  NULL,
  `fecha_nacimiento`   DATE         NULL,
  `sexo`               CHAR(1)      NULL,
  `direccion`          VARCHAR(255) NULL,
  `password_hash`      VARCHAR(255) NULL,
  `modalidad`          ENUM('OPSU','FLOTANTE') NOT NULL DEFAULT 'FLOTANTE',
  `opsu_estatus`       ENUM('NO_VERIFICADO','COINCIDE','NO_APARECE','DATOS_DISTINTOS','CARRERA_DISTINTA','NO_REGISTRADO')
                       NOT NULL DEFAULT 'NO_VERIFICADO',
  `opsu_verificado_en` DATETIME     NULL,
  `creado_en`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_estudiantes_cedula` (`cedula`),
  UNIQUE KEY `uq_estudiantes_email` (`email`),
  KEY `idx_estudiantes_modalidad` (`modalidad`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3 asignaturas por carrera y trayecto (60 filas iniciales).
-- La columna `semestre` queda como legado NULLable; la organización es `trayecto`.
CREATE TABLE IF NOT EXISTS `asignaturas` (
  `id`         INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `carrera_id` INT UNSIGNED NOT NULL,
  `trayecto`   TINYINT UNSIGNED NOT NULL,
  `semestre`   TINYINT UNSIGNED NULL,
  `codigo`     VARCHAR(20)  NOT NULL,
  `nombre`     VARCHAR(160) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_asignaturas_carrera_codigo` (`carrera_id`, `codigo`),
  KEY `idx_asignaturas_trayecto` (`carrera_id`, `trayecto`),
  CONSTRAINT `fk_asignaturas_carrera` FOREIGN KEY (`carrera_id`)
    REFERENCES `carreras` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Inscripciones
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS `inscripciones` (
  `id`                INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `codigo`            VARCHAR(64)  NOT NULL,       -- INS-AAAA-000001
  `estudiante_id`     INT UNSIGNED NOT NULL,
  `carrera_id`        INT UNSIGNED NOT NULL,
  `trayecto`          TINYINT UNSIGNED NOT NULL,   -- 1 a 5 (I a V)
  `tipo_ingreso`      ENUM('OPSU','FLOTANTE') NOT NULL DEFAULT 'FLOTANTE',
  -- Columnas legado: se conservan NULLables para no romper datos anteriores.
  `semestre`          TINYINT UNSIGNED NULL,
  `turno`             VARCHAR(20) NULL,
  `condicion`         ENUM('REGULAR','FLOTANTE') NULL,
  `fecha_inscripcion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `estado`            ENUM('REGISTRADA','IMPRESA','ANULADA') NOT NULL DEFAULT 'REGISTRADA',
  `observaciones`     TEXT NULL,
  `impreso_en`        DATETIME NULL,
  `impresiones`       INT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_inscripciones_codigo` (`codigo`),
  UNIQUE KEY `uq_inscripciones_estudiante_carrera_trayecto` (`estudiante_id`, `carrera_id`, `trayecto`),
  KEY `idx_inscripciones_trayecto` (`trayecto`),
  KEY `idx_inscripciones_tipo_ingreso` (`tipo_ingreso`),
  CONSTRAINT `fk_inscripciones_estudiante` FOREIGN KEY (`estudiante_id`)
    REFERENCES `estudiantes` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_inscripciones_carrera` FOREIGN KEY (`carrera_id`)
    REFERENCES `carreras` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `inscripcion_asignaturas` (
  `inscripcion_id` INT UNSIGNED NOT NULL,
  `asignatura_id`  INT UNSIGNED NOT NULL,
  PRIMARY KEY (`inscripcion_id`, `asignatura_id`),
  CONSTRAINT `fk_inscripcion_asignaturas_inscripcion` FOREIGN KEY (`inscripcion_id`)
    REFERENCES `inscripciones` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_inscripcion_asignaturas_asignatura` FOREIGN KEY (`asignatura_id`)
    REFERENCES `asignaturas` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Validación contra el archivo oficial de la OPSU
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS `opsu_cargas` (
  `id`          INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `archivo`     VARCHAR(255) NOT NULL,
  `hash_sha256` CHAR(64)     NOT NULL,
  `total_filas` INT UNSIGNED NOT NULL DEFAULT 0,
  `cargado_en`  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_opsu_cargas_hash` (`hash_sha256`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `opsu_registros` (
  `id`            INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `carga_id`      INT UNSIGNED NOT NULL,
  `fila`          INT UNSIGNED NOT NULL,
  `cedula`        VARCHAR(20)  NOT NULL,
  `nombres`       VARCHAR(160) NULL,
  `apellidos`     VARCHAR(160) NULL,
  `sexo`          CHAR(1)      NULL,
  `carrera_opsu`  VARCHAR(160) NULL,
  `trayecto_opsu` TINYINT UNSIGNED NULL,
  `estatus_opsu`  VARCHAR(60)  NULL,
  PRIMARY KEY (`id`),
  KEY `idx_opsu_registros_carga` (`carga_id`),
  KEY `idx_opsu_registros_cedula` (`cedula`),
  CONSTRAINT `fk_opsu_registros_carga` FOREIGN KEY (`carga_id`)
    REFERENCES `opsu_cargas` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `opsu_comparaciones` (
  `id`              INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `carga_id`        INT UNSIGNED NOT NULL,
  `estudiante_id`   INT UNSIGNED NULL,
  `cedula`          VARCHAR(20)  NOT NULL,
  `nombre_opsu`     VARCHAR(200) NULL,
  `nombre_sistema`  VARCHAR(200) NULL,
  `carrera_opsu`    VARCHAR(160) NULL,
  `carrera_sistema` VARCHAR(160) NULL,
  `resultado`       ENUM('COINCIDE','NO_APARECE_EN_OPSU','NO_REGISTRADO_EN_SISTEMA','DATOS_DISTINTOS','CARRERA_DISTINTA') NOT NULL,
  `diferencias`     VARCHAR(400) NULL,
  `fecha`           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_opsu_comparaciones_carga` (`carga_id`),
  KEY `idx_opsu_comparaciones_cedula` (`cedula`),
  CONSTRAINT `fk_opsu_comparaciones_carga` FOREIGN KEY (`carga_id`)
    REFERENCES `opsu_cargas` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_opsu_comparaciones_estudiante` FOREIGN KEY (`estudiante_id`)
    REFERENCES `estudiantes` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Auditoría
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS `bitacora` (
  `id`      INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `accion`  VARCHAR(60)  NOT NULL,
  `detalle` VARCHAR(400) NULL,
  `usuario` VARCHAR(80)  NOT NULL,
  `fecha`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_bitacora_fecha` (`fecha`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Consultas de referencia
-- ---------------------------------------------------------------------------

-- Asignaturas de una carrera y trayecto (la web y el escritorio usan esta).
-- SELECT id, codigo, nombre
--   FROM asignaturas
--  WHERE carrera_id = ? AND trayecto = ?
--  ORDER BY codigo;

-- Inscripciones con datos del estudiante y su carrera.
-- SELECT i.codigo, i.trayecto, i.tipo_ingreso, i.estado, i.impresiones,
--        e.cedula, CONCAT(e.nombres, ' ', e.apellidos) AS estudiante,
--        c.nombre AS carrera
--   FROM inscripciones i
--   JOIN estudiantes e ON e.id = i.estudiante_id
--   JOIN carreras c    ON c.id = i.carrera_id
--  WHERE i.trayecto = ?
--  ORDER BY c.nombre, e.apellidos, e.nombres;

-- Comprobante completo por código.
-- SELECT i.*, e.cedula, e.nombres, e.apellidos, c.nombre AS carrera
--   FROM inscripciones i
--   JOIN estudiantes e ON e.id = i.estudiante_id
--   JOIN carreras c    ON c.id = i.carrera_id
--  WHERE i.codigo = ?;
