Option Strict On
Option Explicit On

Imports System.Collections.Generic

''' <summary>
''' Esquema MySQL unificado con el portal web (base portal_pnf).
''' La inscripción se organiza por TRAYECTO y TIPO DE INGRESO; no hay turnos
''' ni periodos académicos. Se ejecuta al iniciar y es idempotente: nunca
''' borra datos existentes.
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
    Public Shared ReadOnly Ajustes As String()
    Public Shared ReadOnly Carreras As String()
    Public Shared ReadOnly Asignaturas As String()

    Shared Sub New()
        Dim tablas As New List(Of String)()

        tablas.Add(Tabla("carreras",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "codigo VARCHAR(10) NOT NULL",
            "nombre VARCHAR(120) NOT NULL",
            "descripcion TEXT NULL",
            "duracion VARCHAR(50) NOT NULL DEFAULT '5 trayectos'",
            "PRIMARY KEY (id)",
            "UNIQUE KEY uq_carreras_codigo (codigo)",
            "UNIQUE KEY uq_carreras_nombre (nombre)"))

        tablas.Add(Tabla("estudiantes",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "cedula VARCHAR(20) NOT NULL",
            "nombres VARCHAR(120) NOT NULL",
            "apellidos VARCHAR(120) NOT NULL",
            "email VARCHAR(190) NULL",
            "telefono VARCHAR(30) NULL",
            "fecha_nacimiento DATE NULL",
            "sexo CHAR(1) NULL",
            "direccion VARCHAR(255) NULL",
            "password_hash VARCHAR(255) NULL",
            "modalidad ENUM('OPSU','FLOTANTE') NOT NULL DEFAULT 'FLOTANTE'",
            "opsu_estatus ENUM('NO_VERIFICADO','COINCIDE','NO_APARECE','DATOS_DISTINTOS','CARRERA_DISTINTA','NO_REGISTRADO') NOT NULL DEFAULT 'NO_VERIFICADO'",
            "opsu_verificado_en DATETIME NULL",
            "creado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
            "PRIMARY KEY (id)",
            "UNIQUE KEY uq_estudiantes_cedula (cedula)",
            "UNIQUE KEY uq_estudiantes_email (email)",
            "KEY idx_estudiantes_modalidad (modalidad)"))

        tablas.Add(Tabla("asignaturas",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "carrera_id INT UNSIGNED NOT NULL",
            "trayecto TINYINT UNSIGNED NOT NULL",
            "semestre TINYINT UNSIGNED NULL",
            "codigo VARCHAR(20) NOT NULL",
            "nombre VARCHAR(160) NOT NULL",
            "PRIMARY KEY (id)",
            "UNIQUE KEY uq_asignaturas_carrera_codigo (carrera_id, codigo)",
            "KEY idx_asignaturas_trayecto (carrera_id, trayecto)",
            "CONSTRAINT fk_asignaturas_carrera FOREIGN KEY (carrera_id) REFERENCES carreras (id) ON DELETE CASCADE"))

        tablas.Add(Tabla("inscripciones",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "codigo VARCHAR(64) NOT NULL",
            "estudiante_id INT UNSIGNED NOT NULL",
            "carrera_id INT UNSIGNED NOT NULL",
            "trayecto TINYINT UNSIGNED NOT NULL",
            "tipo_ingreso ENUM('OPSU','FLOTANTE') NOT NULL DEFAULT 'FLOTANTE'",
            "semestre TINYINT UNSIGNED NULL",
            "turno VARCHAR(20) NULL",
            "condicion ENUM('REGULAR','FLOTANTE') NULL",
            "fecha_inscripcion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
            "estado ENUM('REGISTRADA','IMPRESA','ANULADA') NOT NULL DEFAULT 'REGISTRADA'",
            "observaciones TEXT NULL",
            "impreso_en DATETIME NULL",
            "impresiones INT UNSIGNED NOT NULL DEFAULT 0",
            "PRIMARY KEY (id)",
            "UNIQUE KEY uq_inscripciones_codigo (codigo)",
            "UNIQUE KEY uq_inscripciones_estudiante_carrera_trayecto (estudiante_id, carrera_id, trayecto)",
            "KEY idx_inscripciones_trayecto (trayecto)",
            "KEY idx_inscripciones_tipo_ingreso (tipo_ingreso)",
            "CONSTRAINT fk_inscripciones_estudiante FOREIGN KEY (estudiante_id) REFERENCES estudiantes (id) ON DELETE CASCADE",
            "CONSTRAINT fk_inscripciones_carrera FOREIGN KEY (carrera_id) REFERENCES carreras (id)"))

        tablas.Add(Tabla("inscripcion_asignaturas",
            "inscripcion_id INT UNSIGNED NOT NULL",
            "asignatura_id INT UNSIGNED NOT NULL",
            "PRIMARY KEY (inscripcion_id, asignatura_id)",
            "CONSTRAINT fk_inscripcion_asignaturas_inscripcion FOREIGN KEY (inscripcion_id) REFERENCES inscripciones (id) ON DELETE CASCADE",
            "CONSTRAINT fk_inscripcion_asignaturas_asignatura FOREIGN KEY (asignatura_id) REFERENCES asignaturas (id)"))

        tablas.Add(Tabla("opsu_cargas",
            "id INT UNSIGNED NOT NULL AUTO_INCREMENT",
            "archivo VARCHAR(255) NOT NULL",
            "hash_sha256 CHAR(64) NOT NULL",
            "total_filas INT UNSIGNED NOT NULL DEFAULT 0",
            "cargado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP",
            "PRIMARY KEY (id)",
            "UNIQUE KEY uq_opsu_cargas_hash (hash_sha256)"))

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

        ' Ajustes para bases creadas por versiones anteriores. Se ejecutan uno a
        ' uno; los errores de "columna/índice ya existe" se ignoran.
        Ajustes = New String() {
            "ALTER TABLE estudiantes ADD COLUMN modalidad ENUM('OPSU','FLOTANTE') NOT NULL DEFAULT 'FLOTANTE'",
            "ALTER TABLE estudiantes ADD COLUMN opsu_estatus ENUM('NO_VERIFICADO','COINCIDE','NO_APARECE','DATOS_DISTINTOS','CARRERA_DISTINTA','NO_REGISTRADO') NOT NULL DEFAULT 'NO_VERIFICADO'",
            "ALTER TABLE estudiantes ADD COLUMN opsu_verificado_en DATETIME NULL",
            "ALTER TABLE estudiantes MODIFY email VARCHAR(190) NULL",
            "ALTER TABLE estudiantes MODIFY password_hash VARCHAR(255) NULL",
            "ALTER TABLE asignaturas ADD COLUMN trayecto TINYINT UNSIGNED NULL",
            "UPDATE asignaturas SET trayecto = semestre WHERE trayecto IS NULL AND semestre IS NOT NULL",
            "UPDATE asignaturas SET trayecto = 1 WHERE trayecto IS NULL",
            "ALTER TABLE asignaturas MODIFY trayecto TINYINT UNSIGNED NOT NULL",
            "ALTER TABLE asignaturas MODIFY semestre TINYINT UNSIGNED NULL",
            "ALTER TABLE inscripciones ADD COLUMN trayecto TINYINT UNSIGNED NULL",
            "ALTER TABLE inscripciones ADD COLUMN tipo_ingreso ENUM('OPSU','FLOTANTE') NULL",
            "UPDATE inscripciones SET trayecto = semestre WHERE trayecto IS NULL AND semestre IS NOT NULL",
            "UPDATE inscripciones SET trayecto = 1 WHERE trayecto IS NULL",
            "UPDATE inscripciones SET tipo_ingreso = IF(condicion = 'FLOTANTE', 'FLOTANTE', 'OPSU') WHERE tipo_ingreso IS NULL",
            "ALTER TABLE inscripciones MODIFY trayecto TINYINT UNSIGNED NOT NULL",
            "ALTER TABLE inscripciones MODIFY tipo_ingreso ENUM('OPSU','FLOTANTE') NOT NULL DEFAULT 'FLOTANTE'",
            "ALTER TABLE inscripciones MODIFY semestre TINYINT UNSIGNED NULL",
            "ALTER TABLE inscripciones MODIFY turno VARCHAR(20) NULL",
            "ALTER TABLE inscripciones MODIFY condicion ENUM('REGULAR','FLOTANTE') NULL",
            "ALTER TABLE inscripciones DROP INDEX uq_inscripciones_estudiante_carrera_semestre",
            "ALTER TABLE inscripciones ADD UNIQUE KEY uq_inscripciones_estudiante_carrera_trayecto (estudiante_id, carrera_id, trayecto)",
            "ALTER TABLE inscripciones ADD KEY idx_inscripciones_trayecto (trayecto)",
            "ALTER TABLE inscripciones ADD KEY idx_inscripciones_tipo_ingreso (tipo_ingreso)"
        }

        Carreras = New String() {
            "INSERT IGNORE INTO carreras (codigo, nombre) VALUES ('INF', 'PNF Informática')",
            "INSERT IGNORE INTO carreras (codigo, nombre) VALUES ('VET', 'PNF Veterinaria')",
            "INSERT IGNORE INTO carreras (codigo, nombre) VALUES ('ADM', 'PNF Administración')",
            "INSERT IGNORE INTO carreras (codigo, nombre) VALUES ('AGR', 'PNF Agroalimentaria')"
        }

        ' Tres asignaturas por carrera y trayecto, con el mismo código que el portal.
        Asignaturas = ConstruirAsignaturas()
    End Sub

    ''' <summary>Matriz de asignaturas por carrera (5 trayectos × 3 materias).</summary>
    Private Shared Function ConstruirAsignaturas() As String()
        Dim materias As New Dictionary(Of String, String()()) From {
            {"INF", New String()() {
                New String() {"Matemática I", "Introducción a la Informática", "Lenguaje y Comunicación"},
                New String() {"Matemática II", "Algoritmos y Programación", "Arquitectura del Computador"},
                New String() {"Bases de Datos I", "Programación Orientada a Objetos", "Redes de Computadoras"},
                New String() {"Bases de Datos II", "Ingeniería de Software", "Sistemas Operativos"},
                New String() {"Desarrollo Web", "Seguridad Informática", "Proyecto Socio-Tecnológico"}}},
            {"VET", New String()() {
                New String() {"Anatomía Animal I", "Biología General", "Química General"},
                New String() {"Fisiología Animal", "Bioquímica", "Anatomía Animal II"},
                New String() {"Microbiología Veterinaria", "Nutrición Animal", "Semiología"},
                New String() {"Farmacología Veterinaria", "Patología Veterinaria", "Cirugía Básica"},
                New String() {"Salud Pública Veterinaria", "Producción Animal", "Proyecto Socio-Comunitario"}}},
            {"ADM", New String()() {
                New String() {"Contabilidad I", "Fundamentos de Administración", "Matemática Financiera"},
                New String() {"Contabilidad II", "Microeconomía", "Estadística I"},
                New String() {"Gerencia de Empresas", "Macroeconomía", "Derecho Mercantil"},
                New String() {"Finanzas Corporativas", "Mercadeo", "Gestión del Talento Humano"},
                New String() {"Planificación Estratégica", "Formulación de Proyectos", "Presupuesto Empresarial"}}},
            {"AGR", New String()() {
                New String() {"Botánica General", "Química Agrícola", "Introducción a la Agroalimentaria"},
                New String() {"Suelos y Fertilizantes", "Fisiología Vegetal", "Maquinaria Agrícola"},
                New String() {"Producción Vegetal", "Zootecnia General", "Inocuidad Alimentaria"},
                New String() {"Transformación Agroindustrial", "Riego y Drenaje", "Control de Plagas"},
                New String() {"Gestión Agroempresarial", "Tecnología de Alimentos", "Proyecto Socio-Productivo"}}}
        }

        Dim sentencias As New List(Of String)()
        For Each carrera As KeyValuePair(Of String, String()()) In materias
            For trayecto As Integer = 1 To carrera.Value.Length
                Dim lista As String() = carrera.Value(trayecto - 1)
                For posicion As Integer = 0 To lista.Length - 1
                    Dim codigo As String = carrera.Key & "-" & trayecto.ToString() & (posicion + 1).ToString("00")
                    sentencias.Add(
                        "INSERT IGNORE INTO asignaturas (carrera_id, trayecto, semestre, codigo, nombre) " &
                        "SELECT id, " & trayecto.ToString() & ", " & trayecto.ToString() & ", '" & codigo & "', '" &
                        lista(posicion).Replace("'", "''") & "' FROM carreras WHERE codigo = '" & carrera.Key & "'")
                Next
            Next
        Next
        Return sentencias.ToArray()
    End Function

End Class
