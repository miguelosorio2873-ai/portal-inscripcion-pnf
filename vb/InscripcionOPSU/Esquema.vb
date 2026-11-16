Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Globalization

''' <summary>
''' Definición del esquema MySQL. Se ejecuta al iniciar la aplicación y es
''' idempotente: nunca borra datos existentes.
''' </summary>
Friend NotInheritable Class Esquema

    Private Sub New()
    End Sub

    Private Const Motor As String = " ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci"

    ''' <summary>Arma un CREATE TABLE a partir de sus columnas y restricciones.</summary>
    Private Shared Function Tabla(nombre As String, ParamArray partes As String()) As String
        Return "CREATE TABLE IF NOT EXISTS " & nombre & " (" & String.Join(", ", partes) & ")" & Motor
    End Function

    Public Shared ReadOnly Creaciones As String()
    Public Shared ReadOnly Carreras As String()
    Public Shared ReadOnly Trayectos As String()

    Shared Sub New()
        Dim tablas As New List(Of String)()

        tablas.Add(Tabla("carreras",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "codigo VARCHAR(10) NOT NULL",
            "nombre VARCHAR(120) NOT NULL",
            "activa TINYINT(1) NOT NULL DEFAULT 1",
            "PRIMARY KEY (id)",
            "UNIQUE KEY uq_carreras_codigo (codigo)",
            "UNIQUE KEY uq_carreras_nombre (nombre)"))

        tablas.Add(Tabla("trayectos",
            "numero TINYINT UNSIGNED NOT NULL",
            "nombre VARCHAR(40) NOT NULL",
            "PRIMARY KEY (numero)"))

        tablas.Add(Tabla("periodos",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "codigo VARCHAR(20) NOT NULL",
            "nombre VARCHAR(60) NOT NULL",
            "inicio DATE NULL",
            "fin DATE NULL",
            "activo TINYINT(1) NOT NULL DEFAULT 0",
            "PRIMARY KEY (id)",
            "UNIQUE KEY uq_periodos_codigo (codigo)"))

        tablas.Add(Tabla("estudiantes",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "cedula VARCHAR(20) NOT NULL",
            "nombres VARCHAR(120) NOT NULL",
            "apellidos VARCHAR(120) NOT NULL",
            "sexo CHAR(1) NULL",
            "fecha_nacimiento DATE NULL",
            "email VARCHAR(190) NULL",
            "telefono VARCHAR(30) NULL",
            "direccion VARCHAR(255) NULL",
            "modalidad ENUM('OPSU','FLOTANTE') NOT NULL DEFAULT 'FLOTANTE'",
            "opsu_estatus ENUM('NO_VERIFICADO','COINCIDE','NO_APARECE','DATOS_DISTINTOS','CARRERA_DISTINTA','NO_REGISTRADO') NOT NULL DEFAULT 'NO_VERIFICADO'",
            "opsu_verificado_en DATETIME NULL",
            "creado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
            "actualizado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP",
            "PRIMARY KEY (id)",
            "UNIQUE KEY uq_estudiantes_cedula (cedula)",
            "KEY idx_estudiantes_modalidad (modalidad)"))

        tablas.Add(Tabla("inscripciones",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "estudiante_id INT UNSIGNED NOT NULL",
            "carrera_id INT UNSIGNED NOT NULL",
            "trayecto TINYINT UNSIGNED NOT NULL",
            "periodo_id INT UNSIGNED NOT NULL",
            "modalidad ENUM('OPSU','FLOTANTE') NOT NULL",
            "estatus ENUM('PREINSCRITA','INSCRITA','ANULADA') NOT NULL DEFAULT 'INSCRITA'",
            "fecha_inscripcion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
            "observaciones VARCHAR(255) NULL",
            "PRIMARY KEY (id)",
            "UNIQUE KEY uq_inscripcion (estudiante_id, periodo_id, carrera_id, trayecto)",
            "KEY idx_inscripciones_trayecto (trayecto)",
            "KEY idx_inscripciones_modalidad (modalidad)",
            "CONSTRAINT fk_inscripciones_estudiante FOREIGN KEY (estudiante_id) REFERENCES estudiantes (id) ON DELETE CASCADE",
            "CONSTRAINT fk_inscripciones_carrera FOREIGN KEY (carrera_id) REFERENCES carreras (id)",
            "CONSTRAINT fk_inscripciones_periodo FOREIGN KEY (periodo_id) REFERENCES periodos (id)"))

        tablas.Add(Tabla("opsu_cargas",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "archivo VARCHAR(255) NOT NULL",
            "hash_sha256 CHAR(64) NOT NULL",
            "periodo_id INT UNSIGNED NULL",
            "total_filas INT UNSIGNED NOT NULL DEFAULT 0",
            "cargado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
            "PRIMARY KEY (id)",
            "UNIQUE KEY uq_opsu_cargas_hash (hash_sha256)",
            "CONSTRAINT fk_opsu_cargas_periodo FOREIGN KEY (periodo_id) REFERENCES periodos (id)"))

        tablas.Add(Tabla("opsu_registros",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "carga_id INT UNSIGNED NOT NULL",
            "fila INT UNSIGNED NOT NULL",
            "cedula VARCHAR(20) NOT NULL",
            "nombres VARCHAR(160) NULL",
            "apellidos VARCHAR(160) NULL",
            "sexo CHAR(1) NULL",
            "carrera_opsu VARCHAR(160) NULL",
            "trayecto_opsu TINYINT UNSIGNED NULL",
            "estatus_opsu VARCHAR(60) NULL",
            "PRIMARY KEY (id)",
            "KEY idx_opsu_registros_carga (carga_id)",
            "KEY idx_opsu_registros_cedula (cedula)",
            "CONSTRAINT fk_opsu_registros_carga FOREIGN KEY (carga_id) REFERENCES opsu_cargas (id) ON DELETE CASCADE"))

        tablas.Add(Tabla("opsu_comparaciones",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "carga_id INT UNSIGNED NOT NULL",
            "estudiante_id INT UNSIGNED NULL",
            "cedula VARCHAR(20) NOT NULL",
            "nombre_opsu VARCHAR(200) NULL",
            "nombre_sistema VARCHAR(200) NULL",
            "carrera_opsu VARCHAR(160) NULL",
            "carrera_sistema VARCHAR(160) NULL",
            "resultado ENUM('COINCIDE','NO_APARECE_EN_OPSU','NO_REGISTRADO_EN_SISTEMA','DATOS_DISTINTOS','CARRERA_DISTINTA') NOT NULL",
            "diferencias VARCHAR(400) NULL",
            "fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
            "PRIMARY KEY (id)",
            "KEY idx_opsu_comparaciones_carga (carga_id)",
            "KEY idx_opsu_comparaciones_cedula (cedula)",
            "CONSTRAINT fk_opsu_comparaciones_carga FOREIGN KEY (carga_id) REFERENCES opsu_cargas (id) ON DELETE CASCADE",
            "CONSTRAINT fk_opsu_comparaciones_estudiante FOREIGN KEY (estudiante_id) REFERENCES estudiantes (id) ON DELETE SET NULL"))

        tablas.Add(Tabla("bitacora",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "accion VARCHAR(60) NOT NULL",
            "detalle VARCHAR(400) NULL",
            "usuario VARCHAR(80) NOT NULL",
            "fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
            "PRIMARY KEY (id)",
            "KEY idx_bitacora_fecha (fecha)"))

        Creaciones = tablas.ToArray()

        Carreras = New String() {
            "INSERT IGNORE INTO carreras (codigo, nombre) VALUES ('INF', 'PNF Informática')",
            "INSERT IGNORE INTO carreras (codigo, nombre) VALUES ('VET', 'PNF Veterinaria')",
            "INSERT IGNORE INTO carreras (codigo, nombre) VALUES ('ADM', 'PNF Administración')",
            "INSERT IGNORE INTO carreras (codigo, nombre) VALUES ('AGR', 'PNF Agroalimentaria')"
        }

        Trayectos = New String() {
            "INSERT IGNORE INTO trayectos (numero, nombre) VALUES (1, 'Trayecto I')",
            "INSERT IGNORE INTO trayectos (numero, nombre) VALUES (2, 'Trayecto II')",
            "INSERT IGNORE INTO trayectos (numero, nombre) VALUES (3, 'Trayecto III')",
            "INSERT IGNORE INTO trayectos (numero, nombre) VALUES (4, 'Trayecto IV')",
            "INSERT IGNORE INTO trayectos (numero, nombre) VALUES (5, 'Trayecto V')"
        }
    End Sub

    ''' <summary>Periodo académico del año en curso, activo por defecto.</summary>
    Public Shared Function PeriodoInicial() As String
        Dim anio As Integer = DateTime.Now.Year
        Dim codigo As String = anio.ToString(CultureInfo.InvariantCulture) & "-1"
        Dim nombre As String = "Periodo académico " & anio.ToString(CultureInfo.InvariantCulture) & "-1"
        Return "INSERT IGNORE INTO periodos (codigo, nombre, activo) VALUES ('" & codigo & "', '" & nombre & "', 1)"
    End Function

End Class
