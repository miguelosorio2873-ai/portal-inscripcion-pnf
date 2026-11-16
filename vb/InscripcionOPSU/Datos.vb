Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports MySql.Data.MySqlClient
Imports InscripcionOPSU.Modelos

''' <summary>
''' Acceso a datos (MySQL). Toda la persistencia del sistema pasa por aquí.
''' </summary>
Friend NotInheritable Class Datos

    Private Sub New()
    End Sub

    ' ------------------------------------------------------------------ conexión

    Private Shared Function Abrir(Optional conBase As Boolean = True) As MySqlConnection
        Dim conexion As New MySqlConnection(Configuracion.Cadena(conBase))
        conexion.Open()
        Return conexion
    End Function

    Private Shared Sub Ejecutar(conexion As MySqlConnection, sql As String)
        Using comando As MySqlCommand = conexion.CreateCommand()
            comando.CommandText = sql
            comando.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>Crea la base de datos, las tablas y los catálogos iniciales.</summary>
    Public Shared Sub AsegurarEsquema()
        Dim baseDatos As String = Configuracion.Actual.BaseDatos
        Using conexion As MySqlConnection = Abrir(False)
            Ejecutar(conexion, "CREATE DATABASE IF NOT EXISTS `" & baseDatos & "` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci")
        End Using

        Using conexion As MySqlConnection = Abrir()
            For Each sql As String In Esquema.Creaciones
                Ejecutar(conexion, sql)
            Next
            For Each sql As String In Esquema.Carreras
                Ejecutar(conexion, sql)
            Next
            For Each sql As String In Esquema.Trayectos
                Ejecutar(conexion, sql)
            Next
            Ejecutar(conexion, Esquema.PeriodoInicial())
        End Using
    End Sub

    ' ------------------------------------------------------------------ catálogos

    Public Shared Function ListarCarreras() As List(Of Carrera)
        Dim lista As New List(Of Carrera)()
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText = "SELECT id, codigo, nombre FROM carreras WHERE activa = 1 ORDER BY nombre"
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    While lector.Read()
                        lista.Add(New Carrera With {
                            .Id = lector.GetInt32("id"),
                            .Codigo = lector.GetString("codigo"),
                            .Nombre = lector.GetString("nombre")
                        })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function

    Public Shared Function ListarTrayectos() As List(Of Integer)
        Dim lista As New List(Of Integer)()
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText = "SELECT numero FROM trayectos ORDER BY numero"
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    While lector.Read()
                        lista.Add(lector.GetInt32("numero"))
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function

    Public Shared Function PeriodoActivo() As Periodo
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText = "SELECT id, codigo, nombre, activo FROM periodos ORDER BY activo DESC, id DESC LIMIT 1"
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    If lector.Read() Then
                        Return New Periodo With {
                            .Id = lector.GetInt32("id"),
                            .Codigo = lector.GetString("codigo"),
                            .Nombre = lector.GetString("nombre"),
                            .Activo = lector.GetBoolean("activo")
                        }
                    End If
                End Using
            End Using
        End Using
        Return New Periodo With {.Id = 0, .Codigo = "", .Nombre = "Sin periodo"}
    End Function

    ' ------------------------------------------------------------------ estudiantes

    Public Shared Function BuscarEstudiante(cedula As String) As Estudiante
        Dim clave As String = Utilidades.FormatearCedula(cedula)
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT e.id, e.cedula, e.nombres, e.apellidos, e.sexo, e.fecha_nacimiento, e.email, e.telefono, " &
                    "       e.direccion, e.modalidad, e.opsu_estatus, " &
                    "       COALESCE((SELECT c.nombre FROM inscripciones i JOIN carreras c ON c.id = i.carrera_id " &
                    "                  WHERE i.estudiante_id = e.id ORDER BY i.id DESC LIMIT 1), '') AS carrera " &
                    "  FROM estudiantes e WHERE e.cedula = @cedula"
                comando.Parameters.AddWithValue("@cedula", clave)
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    If lector.Read() Then Return MapearEstudiante(lector)
                End Using
            End Using
        End Using
        Return Nothing
    End Function

    Public Shared Function ListarEstudiantes() As List(Of Estudiante)
        Dim lista As New List(Of Estudiante)()
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT e.id, e.cedula, e.nombres, e.apellidos, e.sexo, e.fecha_nacimiento, e.email, e.telefono, " &
                    "       e.direccion, e.modalidad, e.opsu_estatus, " &
                    "       COALESCE((SELECT c.nombre FROM inscripciones i JOIN carreras c ON c.id = i.carrera_id " &
                    "                  WHERE i.estudiante_id = e.id ORDER BY i.id DESC LIMIT 1), '') AS carrera " &
                    "  FROM estudiantes e ORDER BY e.apellidos, e.nombres"
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    While lector.Read()
                        lista.Add(MapearEstudiante(lector))
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function

    Private Shared Function MapearEstudiante(lector As MySqlDataReader) As Estudiante
        Dim estudiante As New Estudiante()
        estudiante.Id = lector.GetInt32("id")
        estudiante.Cedula = lector.GetString("cedula")
        estudiante.Nombres = lector.GetString("nombres")
        estudiante.Apellidos = lector.GetString("apellidos")
        If Not lector.IsDBNull(lector.GetOrdinal("sexo")) Then estudiante.Sexo = lector.GetString("sexo")
        If Not lector.IsDBNull(lector.GetOrdinal("fecha_nacimiento")) Then estudiante.FechaNacimiento = lector.GetDateTime("fecha_nacimiento")
        If Not lector.IsDBNull(lector.GetOrdinal("email")) Then estudiante.Email = lector.GetString("email")
        If Not lector.IsDBNull(lector.GetOrdinal("telefono")) Then estudiante.Telefono = lector.GetString("telefono")
        If Not lector.IsDBNull(lector.GetOrdinal("direccion")) Then estudiante.Direccion = lector.GetString("direccion")
        estudiante.Modalidad = If(String.Equals(lector.GetString("modalidad"), "OPSU", StringComparison.OrdinalIgnoreCase), TipoIngreso.OPSU, TipoIngreso.FLOTANTE)
        estudiante.OpsuEstatus = lector.GetString("opsu_estatus")
        If Not lector.IsDBNull(lector.GetOrdinal("carrera")) Then estudiante.Carrera = lector.GetString("carrera")
        Return estudiante
    End Function

    ''' <summary>Inserta o actualiza por cédula. Devuelve el id del estudiante.</summary>
    Public Shared Function GuardarEstudiante(estudiante As Estudiante) As Integer
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "INSERT INTO estudiantes (cedula, nombres, apellidos, sexo, fecha_nacimiento, email, telefono, direccion, modalidad) " &
                    "VALUES (@cedula, @nombres, @apellidos, @sexo, @nacimiento, @email, @telefono, @direccion, @modalidad) " &
                    "ON DUPLICATE KEY UPDATE " &
                    "  nombres = VALUES(nombres), apellidos = VALUES(apellidos), sexo = VALUES(sexo), " &
                    "  fecha_nacimiento = VALUES(fecha_nacimiento), email = VALUES(email), telefono = VALUES(telefono), " &
                    "  direccion = VALUES(direccion), modalidad = VALUES(modalidad), id = LAST_INSERT_ID(id)"
                comando.Parameters.AddWithValue("@cedula", Utilidades.FormatearCedula(estudiante.Cedula))
                comando.Parameters.AddWithValue("@nombres", estudiante.Nombres.Trim())
                comando.Parameters.AddWithValue("@apellidos", estudiante.Apellidos.Trim())
                comando.Parameters.AddWithValue("@sexo", If(String.IsNullOrWhiteSpace(estudiante.Sexo), CObj(DBNull.Value), estudiante.Sexo))
                If estudiante.FechaNacimiento.HasValue Then
                    comando.Parameters.AddWithValue("@nacimiento", estudiante.FechaNacimiento.Value.Date)
                Else
                    comando.Parameters.AddWithValue("@nacimiento", DBNull.Value)
                End If
                comando.Parameters.AddWithValue("@email", If(String.IsNullOrWhiteSpace(estudiante.Email), CObj(DBNull.Value), estudiante.Email.Trim()))
                comando.Parameters.AddWithValue("@telefono", If(String.IsNullOrWhiteSpace(estudiante.Telefono), CObj(DBNull.Value), estudiante.Telefono.Trim()))
                comando.Parameters.AddWithValue("@direccion", If(String.IsNullOrWhiteSpace(estudiante.Direccion), CObj(DBNull.Value), estudiante.Direccion.Trim()))
                comando.Parameters.AddWithValue("@modalidad", estudiante.ModalidadTexto)
                comando.ExecuteNonQuery()
                Return CInt(comando.LastInsertedId)
            End Using
        End Using
    End Function

    ' ------------------------------------------------------------------ inscripciones

    ''' <summary>Registra (o actualiza) la inscripción de un estudiante en un trayecto.</summary>
    Public Shared Function GuardarInscripcion(estudianteId As Integer, carreraId As Integer, trayecto As Integer,
                                              periodoId As Integer, modalidad As TipoIngreso, observaciones As String) As Integer
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "INSERT INTO inscripciones (estudiante_id, carrera_id, trayecto, periodo_id, modalidad, estatus, observaciones) " &
                    "VALUES (@estudiante, @carrera, @trayecto, @periodo, @modalidad, 'INSCRITA', @observaciones) " &
                    "ON DUPLICATE KEY UPDATE modalidad = VALUES(modalidad), estatus = 'INSCRITA', " &
                    "  observaciones = VALUES(observaciones), fecha_inscripcion = NOW(), id = LAST_INSERT_ID(id)"
                comando.Parameters.AddWithValue("@estudiante", estudianteId)
                comando.Parameters.AddWithValue("@carrera", carreraId)
                comando.Parameters.AddWithValue("@trayecto", trayecto)
                comando.Parameters.AddWithValue("@periodo", periodoId)
                comando.Parameters.AddWithValue("@modalidad", If(modalidad = TipoIngreso.OPSU, "OPSU", "FLOTANTE"))
                comando.Parameters.AddWithValue("@observaciones", If(String.IsNullOrWhiteSpace(observaciones), CObj(DBNull.Value), observaciones.Trim()))
                comando.ExecuteNonQuery()
                Return CInt(comando.LastInsertedId)
            End Using
        End Using
    End Function

    ''' <summary>Listado de inscripciones con filtros opcionales por trayecto, modalidad y carrera.</summary>
    Public Shared Function ListarInscripciones(trayecto As Integer, modalidad As String, carreraId As Integer) As List(Of Inscripcion)
        Dim lista As New List(Of Inscripcion)()
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT i.id, i.estudiante_id, e.cedula, CONCAT(e.nombres, ' ', e.apellidos) AS estudiante, " &
                    "       i.carrera_id, c.nombre AS carrera, i.trayecto, p.nombre AS periodo, i.modalidad, " &
                    "       i.estatus, i.fecha_inscripcion, COALESCE(i.observaciones, '') AS observaciones " &
                    "  FROM inscripciones i " &
                    "  JOIN estudiantes e ON e.id = i.estudiante_id " &
                    "  JOIN carreras c ON c.id = i.carrera_id " &
                    "  JOIN periodos p ON p.id = i.periodo_id " &
                    " WHERE (@trayecto = 0 OR i.trayecto = @trayecto) " &
                    "   AND (@modalidad = '' OR i.modalidad = @modalidad) " &
                    "   AND (@carrera = 0 OR i.carrera_id = @carrera) " &
                    " ORDER BY i.trayecto, c.nombre, e.apellidos, e.nombres"
                comando.Parameters.AddWithValue("@trayecto", trayecto)
                comando.Parameters.AddWithValue("@modalidad", If(modalidad Is Nothing, "", modalidad))
                comando.Parameters.AddWithValue("@carrera", carreraId)
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    While lector.Read()
                        Dim item As New Inscripcion()
                        item.Id = lector.GetInt32("id")
                        item.EstudianteId = lector.GetInt32("estudiante_id")
                        item.Cedula = lector.GetString("cedula")
                        item.Estudiante = lector.GetString("estudiante")
                        item.CarreraId = lector.GetInt32("carrera_id")
                        item.Carrera = lector.GetString("carrera")
                        item.Trayecto = lector.GetInt32("trayecto")
                        item.Periodo = lector.GetString("periodo")
                        item.Modalidad = If(String.Equals(lector.GetString("modalidad"), "OPSU", StringComparison.OrdinalIgnoreCase), TipoIngreso.OPSU, TipoIngreso.FLOTANTE)
                        item.Estatus = lector.GetString("estatus")
                        item.Fecha = lector.GetDateTime("fecha_inscripcion")
                        item.Observaciones = lector.GetString("observaciones")
                        lista.Add(item)
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function

    ' ------------------------------------------------------------------ OPSU

    ''' <summary>Registra la carga del archivo (o reutiliza la existente si el hash coincide).</summary>
    Public Shared Function RegistrarCargaOpsu(archivo As String, hash As String, periodoId As Integer, totalFilas As Integer) As Integer
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "INSERT INTO opsu_cargas (archivo, hash_sha256, periodo_id, total_filas) " &
                    "VALUES (@archivo, @hash, @periodo, @total) " &
                    "ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id), archivo = VALUES(archivo), " &
                    "  total_filas = VALUES(total_filas), cargado_en = NOW()"
                comando.Parameters.AddWithValue("@archivo", archivo)
                comando.Parameters.AddWithValue("@hash", hash)
                comando.Parameters.AddWithValue("@periodo", If(periodoId > 0, CObj(periodoId), DBNull.Value))
                comando.Parameters.AddWithValue("@total", totalFilas)
                comando.ExecuteNonQuery()
                Dim cargaId As Integer = CInt(comando.LastInsertedId)
                Using limpieza As MySqlCommand = conexion.CreateCommand()
                    limpieza.CommandText = "DELETE FROM opsu_comparaciones WHERE carga_id = @id; DELETE FROM opsu_registros WHERE carga_id = @id;"
                    limpieza.Parameters.AddWithValue("@id", cargaId)
                    limpieza.ExecuteNonQuery()
                End Using
                Return cargaId
            End Using
        End Using
    End Function

    Public Shared Sub GuardarRegistrosOpsu(cargaId As Integer, registros As List(Of OpsuRegistro))
        Using conexion As MySqlConnection = Abrir()
            Using transaccion As MySqlTransaction = conexion.BeginTransaction()
                Using comando As MySqlCommand = conexion.CreateCommand()
                    comando.Transaction = transaccion
                    comando.CommandText =
                        "INSERT INTO opsu_registros (carga_id, fila, cedula, nombres, apellidos, sexo, carrera_opsu, trayecto_opsu, estatus_opsu) " &
                        "VALUES (@carga, @fila, @cedula, @nombres, @apellidos, @sexo, @carrera, @trayecto, @estatus)"
                    For Each registro As OpsuRegistro In registros
                        comando.Parameters.Clear()
                        comando.Parameters.AddWithValue("@carga", cargaId)
                        comando.Parameters.AddWithValue("@fila", registro.Fila)
                        comando.Parameters.AddWithValue("@cedula", Utilidades.FormatearCedula(registro.Cedula))
                        comando.Parameters.AddWithValue("@nombres", registro.Nombres)
                        comando.Parameters.AddWithValue("@apellidos", registro.Apellidos)
                        comando.Parameters.AddWithValue("@sexo", If(String.IsNullOrWhiteSpace(registro.Sexo), CObj(DBNull.Value), registro.Sexo))
                        comando.Parameters.AddWithValue("@carrera", registro.Carrera)
                        comando.Parameters.AddWithValue("@trayecto", If(registro.Trayecto > 0, CObj(registro.Trayecto), DBNull.Value))
                        comando.Parameters.AddWithValue("@estatus", registro.Estatus)
                        comando.ExecuteNonQuery()
                    Next
                End Using
                transaccion.Commit()
            End Using
        End Using
    End Sub

    Public Shared Sub GuardarComparaciones(cargaId As Integer, comparaciones As List(Of Comparacion))
        Using conexion As MySqlConnection = Abrir()
            Using transaccion As MySqlTransaction = conexion.BeginTransaction()
                Using comando As MySqlCommand = conexion.CreateCommand()
                    comando.Transaction = transaccion
                    comando.CommandText =
                        "INSERT INTO opsu_comparaciones (carga_id, estudiante_id, cedula, nombre_opsu, nombre_sistema, " &
                        "  carrera_opsu, carrera_sistema, resultado, diferencias) " &
                        "VALUES (@carga, @estudiante, @cedula, @nombreOpsu, @nombreSistema, @carreraOpsu, @carreraSistema, @resultado, @diferencias)"
                    For Each comparacion As Comparacion In comparaciones
                        comando.Parameters.Clear()
                        comando.Parameters.AddWithValue("@carga", cargaId)
                        comando.Parameters.AddWithValue("@estudiante", If(comparacion.EstudianteId > 0, CObj(comparacion.EstudianteId), DBNull.Value))
                        comando.Parameters.AddWithValue("@cedula", comparacion.Cedula)
                        comando.Parameters.AddWithValue("@nombreOpsu", comparacion.NombreOpsu)
                        comando.Parameters.AddWithValue("@nombreSistema", comparacion.NombreSistema)
                        comando.Parameters.AddWithValue("@carreraOpsu", comparacion.CarreraOpsu)
                        comando.Parameters.AddWithValue("@carreraSistema", comparacion.CarreraSistema)
                        comando.Parameters.AddWithValue("@resultado", comparacion.Resultado.ToString())
                        comando.Parameters.AddWithValue("@diferencias", comparacion.Diferencias)
                        comando.ExecuteNonQuery()
                    Next
                End Using
                transaccion.Commit()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Aplica el resultado de la comparación: marca a los estudiantes OPSU y,
    ''' opcionalmente, crea los que aparecen en el archivo pero no en el sistema.
    ''' Devuelve cuántos estudiantes quedaron actualizados.
    ''' </summary>
    Public Shared Function AplicarComparacion(cargaId As Integer, importarFaltantes As Boolean) As Integer
        Dim afectados As Integer = 0
        Using conexion As MySqlConnection = Abrir()
            Using transaccion As MySqlTransaction = conexion.BeginTransaction()
                Using comando As MySqlCommand = conexion.CreateCommand()
                    comando.Transaction = transaccion
                    comando.CommandText =
                        "UPDATE estudiantes e " &
                        "  JOIN opsu_comparaciones c ON c.estudiante_id = e.id " &
                        "   SET e.modalidad = 'OPSU', " &
                        "       e.opsu_estatus = CASE c.resultado " &
                        "           WHEN 'COINCIDE' THEN 'COINCIDE' " &
                        "           WHEN 'CARRERA_DISTINTA' THEN 'CARRERA_DISTINTA' " &
                        "           WHEN 'DATOS_DISTINTOS' THEN 'DATOS_DISTINTOS' " &
                        "           ELSE 'NO_VERIFICADO' END, " &
                        "       e.opsu_verificado_en = NOW() " &
                        " WHERE c.carga_id = @carga AND c.estudiante_id IS NOT NULL"
                    comando.Parameters.AddWithValue("@carga", cargaId)
                    afectados += comando.ExecuteNonQuery()
                End Using

                Using comando As MySqlCommand = conexion.CreateCommand()
                    comando.Transaction = transaccion
                    comando.CommandText =
                        "UPDATE estudiantes e " &
                        "  JOIN opsu_comparaciones c ON c.estudiante_id = e.id " &
                        "   SET e.opsu_estatus = 'NO_APARECE', e.opsu_verificado_en = NOW() " &
                        " WHERE c.carga_id = @carga AND c.resultado = 'NO_APARECE_EN_OPSU'"
                    comando.Parameters.AddWithValue("@carga", cargaId)
                    afectados += comando.ExecuteNonQuery()
                End Using

                If importarFaltantes Then
                    Using comando As MySqlCommand = conexion.CreateCommand()
                        comando.Transaction = transaccion
                        comando.CommandText =
                            "INSERT INTO estudiantes (cedula, nombres, apellidos, sexo, modalidad, opsu_estatus, opsu_verificado_en) " &
                            "SELECT r.cedula, MAX(COALESCE(NULLIF(r.nombres, ''), 'SIN NOMBRE')), MAX(COALESCE(r.apellidos, '')), " &
                            "       MAX(r.sexo), 'OPSU', 'COINCIDE', NOW() " &
                            "  FROM opsu_registros r " &
                            "  JOIN opsu_comparaciones c ON c.carga_id = r.carga_id AND c.cedula = r.cedula " &
                            " WHERE r.carga_id = @carga AND c.resultado = 'NO_REGISTRADO_EN_SISTEMA' " &
                            " GROUP BY r.cedula " &
                            "ON DUPLICATE KEY UPDATE nombres = VALUES(nombres), apellidos = VALUES(apellidos), " &
                            "  modalidad = 'OPSU', opsu_estatus = 'COINCIDE', opsu_verificado_en = NOW()"
                        comando.Parameters.AddWithValue("@carga", cargaId)
                        afectados += comando.ExecuteNonQuery()
                    End Using
                End If

                transaccion.Commit()
            End Using
        End Using
        RegistrarBitacora("OPSU_APLICAR", "Carga " & cargaId.ToString() & " · importar faltantes=" & importarFaltantes.ToString() & " · afectados=" & afectados.ToString())
        Return afectados
    End Function

    Public Shared Sub RegistrarBitacora(accion As String, detalle As String)
        Try
            Using conexion As MySqlConnection = Abrir()
                Using comando As MySqlCommand = conexion.CreateCommand()
                    comando.CommandText = "INSERT INTO bitacora (accion, detalle, usuario) VALUES (@accion, @detalle, @usuario)"
                    comando.Parameters.AddWithValue("@accion", accion)
                    comando.Parameters.AddWithValue("@detalle", If(detalle.Length > 390, detalle.Substring(0, 390), detalle))
                    comando.Parameters.AddWithValue("@usuario", Environment.UserName)
                    comando.ExecuteNonQuery()
                End Using
            End Using
        Catch
            ' La bitácora nunca debe interrumpir la operación principal.
        End Try
    End Sub

    ''' <summary>Resumen rápido para la barra de estado: total, OPSU y flotantes.</summary>
    Public Shared Function Resumen() As String
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT COUNT(*) AS total, " &
                    "       SUM(CASE WHEN modalidad = 'OPSU' THEN 1 ELSE 0 END) AS opsu, " &
                    "       SUM(CASE WHEN modalidad = 'FLOTANTE' THEN 1 ELSE 0 END) AS flotantes " &
                    "  FROM inscripciones"
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    If lector.Read() Then
                        Dim total As Integer = If(lector.IsDBNull(0), 0, lector.GetInt32(0))
                        Dim opsu As Integer = If(lector.IsDBNull(1), 0, lector.GetInt32(1))
                        Dim flotantes As Integer = If(lector.IsDBNull(2), 0, lector.GetInt32(2))
                        Return String.Format("Inscripciones: {0} · OPSU: {1} · Flotantes: {2}", total, opsu, flotantes)
                    End If
                End Using
            End Using
        End Using
        Return "Inscripciones: 0"
    End Function

End Class
