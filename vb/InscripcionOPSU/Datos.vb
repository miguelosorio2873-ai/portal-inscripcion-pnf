Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports MySql.Data.MySqlClient
Imports InscripcionOPSU.Modelos

''' <summary>
''' Acceso a datos (MySQL). Trabaja sobre la MISMA base que el portal web
''' (portal_pnf): inscripciones por trayecto y tipo de ingreso, sin turnos.
''' </summary>
Friend NotInheritable Class Datos

    Private Sub New()
    End Sub

    ''' <summary>Trayectos válidos del PNF (I a V).</summary>
    Public Shared ReadOnly Trayectos As Integer() = {1, 2, 3, 4, 5}

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

    ''' <summary>Ejecuta un ajuste tolerando que la columna o el índice ya exista.</summary>
    Private Shared Sub EjecutarAjuste(conexion As MySqlConnection, sql As String)
        Try
            Ejecutar(conexion, sql)
        Catch ex As MySqlException
            ' 1060 columna duplicada · 1061 índice duplicado · 1091 no existe
            If ex.Number <> 1060 AndAlso ex.Number <> 1061 AndAlso ex.Number <> 1091 Then Throw
        End Try
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
            For Each sql As String In Esquema.Ajustes
                EjecutarAjuste(conexion, sql)
            Next
            For Each sql As String In Esquema.Asignaturas
                Ejecutar(conexion, sql)
            Next
        End Using
    End Sub

    ' ------------------------------------------------------------------ catálogos

    Public Shared Function ListarCarreras() As List(Of Carrera)
        Dim lista As New List(Of Carrera)()
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText = "SELECT id, codigo, nombre FROM carreras ORDER BY nombre"
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

    ''' <summary>Los trayectos son fijos (I a V), igual que en el portal.</summary>
    Public Shared Function ListarTrayectos() As List(Of Integer)
        Return New List(Of Integer)(Trayectos)
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

    ''' <summary>
    ''' Registra (o actualiza) la inscripción de un estudiante en un trayecto.
    ''' Devuelve el código del comprobante. Si es nueva, inscribe todas las
    ''' asignaturas del trayecto.
    ''' </summary>
    Public Shared Function GuardarInscripcion(estudianteId As Integer, carreraId As Integer, trayecto As Integer,
                                              tipoIngreso As TipoIngreso, observaciones As String) As String
        If Not Trayectos.Contains(trayecto) Then
            Throw New ArgumentOutOfRangeException(NameOf(trayecto), "El trayecto debe estar entre I y V.")
        End If

        Using conexion As MySqlConnection = Abrir()
            Dim existenteId As Integer = 0
            Dim codigo As String = ""

            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT id, codigo FROM inscripciones " &
                    " WHERE estudiante_id = @estudiante AND carrera_id = @carrera AND trayecto = @trayecto LIMIT 1"
                comando.Parameters.AddWithValue("@estudiante", estudianteId)
                comando.Parameters.AddWithValue("@carrera", carreraId)
                comando.Parameters.AddWithValue("@trayecto", trayecto)
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    If lector.Read() Then
                        existenteId = lector.GetInt32("id")
                        codigo = lector.GetString("codigo")
                    End If
                End Using
            End Using

            Using transaccion As MySqlTransaction = conexion.BeginTransaction()
                If existenteId > 0 Then
                    Using comando As MySqlCommand = conexion.CreateCommand()
                        comando.Transaction = transaccion
                        comando.CommandText =
                            "UPDATE inscripciones SET tipo_ingreso = @ingreso, estado = 'REGISTRADA', " &
                            "  observaciones = @observaciones, fecha_inscripcion = NOW(), impreso_en = NULL, impresiones = 0 " &
                            " WHERE id = @id"
                        comando.Parameters.AddWithValue("@ingreso", If(tipoIngreso = TipoIngreso.OPSU, "OPSU", "FLOTANTE"))
                        comando.Parameters.AddWithValue("@observaciones", If(String.IsNullOrWhiteSpace(observaciones), CObj(DBNull.Value), observaciones.Trim()))
                        comando.Parameters.AddWithValue("@id", existenteId)
                        comando.ExecuteNonQuery()
                    End Using
                Else
                    Using comando As MySqlCommand = conexion.CreateCommand()
                        comando.Transaction = transaccion
                        comando.CommandText =
                            "INSERT INTO inscripciones (codigo, estudiante_id, carrera_id, trayecto, tipo_ingreso, observaciones) " &
                            "VALUES (@temporal, @estudiante, @carrera, @trayecto, @ingreso, @observaciones)"
                        comando.Parameters.AddWithValue("@temporal", "TMP-" & Guid.NewGuid().ToString("N"))
                        comando.Parameters.AddWithValue("@estudiante", estudianteId)
                        comando.Parameters.AddWithValue("@carrera", carreraId)
                        comando.Parameters.AddWithValue("@trayecto", trayecto)
                        comando.Parameters.AddWithValue("@ingreso", If(tipoIngreso = TipoIngreso.OPSU, "OPSU", "FLOTANTE"))
                        comando.Parameters.AddWithValue("@observaciones", If(String.IsNullOrWhiteSpace(observaciones), CObj(DBNull.Value), observaciones.Trim()))
                        comando.ExecuteNonQuery()
                        existenteId = CInt(comando.LastInsertedId)
                    End Using

                    codigo = String.Format(CultureInfo.InvariantCulture, "INS-{0}-{1:000000}", DateTime.Now.Year, existenteId)
                    Using comando As MySqlCommand = conexion.CreateCommand()
                        comando.Transaction = transaccion
                        comando.CommandText = "UPDATE inscripciones SET codigo = @codigo WHERE id = @id"
                        comando.Parameters.AddWithValue("@codigo", codigo)
                        comando.Parameters.AddWithValue("@id", existenteId)
                        comando.ExecuteNonQuery()
                    End Using

                    Using comando As MySqlCommand = conexion.CreateCommand()
                        comando.Transaction = transaccion
                        comando.CommandText =
                            "INSERT IGNORE INTO inscripcion_asignaturas (inscripcion_id, asignatura_id) " &
                            "SELECT @inscripcion, id FROM asignaturas WHERE carrera_id = @carrera AND trayecto = @trayecto"
                        comando.Parameters.AddWithValue("@inscripcion", existenteId)
                        comando.Parameters.AddWithValue("@carrera", carreraId)
                        comando.Parameters.AddWithValue("@trayecto", trayecto)
                        comando.ExecuteNonQuery()
                    End Using
                End If

                transaccion.Commit()
            End Using

            Return codigo
        End Using
    End Function

    ''' <summary>Listado de inscripciones con filtros opcionales por trayecto, ingreso y carrera.</summary>
    Public Shared Function ListarInscripciones(trayecto As Integer, tipoIngreso As String, carreraId As Integer) As List(Of Inscripcion)
        Dim lista As New List(Of Inscripcion)()
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT i.id, i.codigo, i.estudiante_id, e.cedula, CONCAT(e.nombres, ' ', e.apellidos) AS estudiante, " &
                    "       i.carrera_id, c.nombre AS carrera, i.trayecto, i.tipo_ingreso, i.estado, " &
                    "       i.fecha_inscripcion, COALESCE(i.observaciones, '') AS observaciones, " &
                    "       i.impreso_en, i.impresiones, " &
                    "       (SELECT COUNT(*) FROM inscripcion_asignaturas ia WHERE ia.inscripcion_id = i.id) AS total_asignaturas " &
                    "  FROM inscripciones i " &
                    "  JOIN estudiantes e ON e.id = i.estudiante_id " &
                    "  JOIN carreras c ON c.id = i.carrera_id " &
                    " WHERE (@trayecto = 0 OR i.trayecto = @trayecto) " &
                    "   AND (@ingreso = '' OR i.tipo_ingreso = @ingreso) " &
                    "   AND (@carrera = 0 OR i.carrera_id = @carrera) " &
                    " ORDER BY i.trayecto, c.nombre, e.apellidos, e.nombres"
                comando.Parameters.AddWithValue("@trayecto", trayecto)
                comando.Parameters.AddWithValue("@ingreso", If(tipoIngreso Is Nothing, "", tipoIngreso))
                comando.Parameters.AddWithValue("@carrera", carreraId)
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    While lector.Read()
                        lista.Add(MapearInscripcion(lector))
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function

    Private Shared Function MapearInscripcion(lector As MySqlDataReader) As Inscripcion
        Dim item As New Inscripcion()
        item.Id = lector.GetInt32("id")
        item.Codigo = lector.GetString("codigo")
        item.EstudianteId = lector.GetInt32("estudiante_id")
        item.Cedula = lector.GetString("cedula")
        item.Estudiante = lector.GetString("estudiante")
        item.CarreraId = lector.GetInt32("carrera_id")
        item.Carrera = lector.GetString("carrera")
        item.Trayecto = lector.GetInt32("trayecto")
        item.Modalidad = If(String.Equals(lector.GetString("tipo_ingreso"), "OPSU", StringComparison.OrdinalIgnoreCase), TipoIngreso.OPSU, TipoIngreso.FLOTANTE)
        item.Estatus = lector.GetString("estado")
        item.Fecha = lector.GetDateTime("fecha_inscripcion")
        item.Observaciones = lector.GetString("observaciones")
        item.Impresiones = lector.GetInt32("impresiones")
        If Not lector.IsDBNull(lector.GetOrdinal("impreso_en")) Then item.ImpresoEn = lector.GetDateTime("impreso_en")
        item.TotalAsignaturas = lector.GetInt32("total_asignaturas")
        Return item
    End Function

    ''' <summary>Datos completos del comprobante (estudiante, inscripción y asignaturas).</summary>
    Public Shared Function ObtenerInscripcionCompleta(codigo As String) As InscripcionCompleta
        Dim datos As InscripcionCompleta = Nothing

        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT i.id, i.codigo, i.trayecto, i.tipo_ingreso, i.fecha_inscripcion, i.estado, " &
                    "       COALESCE(i.observaciones, '') AS observaciones, i.impreso_en, i.impresiones, " &
                    "       e.cedula, e.nombres, e.apellidos, e.email, e.telefono, e.fecha_nacimiento, e.direccion, " &
                    "       c.nombre AS carrera " &
                    "  FROM inscripciones i " &
                    "  JOIN estudiantes e ON e.id = i.estudiante_id " &
                    "  JOIN carreras c ON c.id = i.carrera_id " &
                    " WHERE i.codigo = @codigo LIMIT 1"
                comando.Parameters.AddWithValue("@codigo", codigo.Trim())
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    If lector.Read() Then
                        datos = New InscripcionCompleta()
                        datos.InscripcionId = lector.GetInt32("id")
                        datos.Codigo = lector.GetString("codigo")
                        datos.Trayecto = lector.GetInt32("trayecto")
                        datos.TipoIngreso = If(String.Equals(lector.GetString("tipo_ingreso"), "OPSU", StringComparison.OrdinalIgnoreCase), TipoIngreso.OPSU, TipoIngreso.FLOTANTE)
                        datos.FechaInscripcion = lector.GetDateTime("fecha_inscripcion")
                        datos.Estado = lector.GetString("estado")
                        datos.Observaciones = lector.GetString("observaciones")
                        datos.Impresiones = lector.GetInt32("impresiones")
                        If Not lector.IsDBNull(lector.GetOrdinal("impreso_en")) Then datos.ImpresoEn = lector.GetDateTime("impreso_en")
                        datos.Cedula = lector.GetString("cedula")
                        datos.Nombres = lector.GetString("nombres")
                        datos.Apellidos = lector.GetString("apellidos")
                        If Not lector.IsDBNull(lector.GetOrdinal("email")) Then datos.Email = lector.GetString("email")
                        If Not lector.IsDBNull(lector.GetOrdinal("telefono")) Then datos.Telefono = lector.GetString("telefono")
                        If Not lector.IsDBNull(lector.GetOrdinal("fecha_nacimiento")) Then datos.FechaNacimiento = lector.GetDateTime("fecha_nacimiento")
                        If Not lector.IsDBNull(lector.GetOrdinal("direccion")) Then datos.Direccion = lector.GetString("direccion")
                        datos.Carrera = lector.GetString("carrera")
                    End If
                End Using
            End Using

            If datos IsNot Nothing Then
                datos.Asignaturas = ListarAsignaturasDeInscripcion(datos.InscripcionId)
            End If
        End Using

        Return datos
    End Function

    Public Shared Function ListarAsignaturasDeInscripcion(inscripcionId As Integer) As List(Of AsignaturaInscrita)
        Dim lista As New List(Of AsignaturaInscrita)()
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT a.codigo, a.nombre, a.trayecto " &
                    "  FROM inscripcion_asignaturas ia " &
                    "  JOIN asignaturas a ON a.id = ia.asignatura_id " &
                    " WHERE ia.inscripcion_id = @id ORDER BY a.codigo"
                comando.Parameters.AddWithValue("@id", inscripcionId)
                Using lector As MySqlDataReader = comando.ExecuteReader()
                    While lector.Read()
                        lista.Add(New AsignaturaInscrita With {
                            .Codigo = lector.GetString("codigo"),
                            .Nombre = lector.GetString("nombre"),
                            .Trayecto = lector.GetInt32("trayecto")
                        })
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function

    ''' <summary>Marca el comprobante como impreso y suma una impresión. Devuelve el total.</summary>
    Public Shared Function RegistrarImpresion(codigo As String) As Integer
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "UPDATE inscripciones SET estado = IF(estado = 'ANULADA', 'ANULADA', 'IMPRESA'), " &
                    "       impresiones = impresiones + 1, impreso_en = NOW() " &
                    " WHERE codigo = @codigo"
                comando.Parameters.AddWithValue("@codigo", codigo.Trim())
                If comando.ExecuteNonQuery() = 0 Then Return -1
            End Using

            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText = "SELECT impresiones FROM inscripciones WHERE codigo = @codigo"
                comando.Parameters.AddWithValue("@codigo", codigo.Trim())
                Dim total As Object = comando.ExecuteScalar()
                RegistrarBitacora("IMPRESION", "Comprobante " & codigo.Trim() & " · impresiones=" & Convert.ToString(total, CultureInfo.InvariantCulture))
                Return Convert.ToInt32(total, CultureInfo.InvariantCulture)
            End Using
        End Using
    End Function

    ' ------------------------------------------------------------------ OPSU

    ''' <summary>Registra la carga del archivo (o reutiliza la existente si el hash coincide).</summary>
    Public Shared Function RegistrarCargaOpsu(archivo As String, hash As String, totalFilas As Integer) As Integer
        Using conexion As MySqlConnection = Abrir()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText =
                    "INSERT INTO opsu_cargas (archivo, hash_sha256, total_filas) " &
                    "VALUES (@archivo, @hash, @total) " &
                    "ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id), archivo = VALUES(archivo), " &
                    "  total_filas = VALUES(total_filas), cargado_en = NOW()"
                comando.Parameters.AddWithValue("@archivo", archivo)
                comando.Parameters.AddWithValue("@hash", hash)
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
                    "       SUM(CASE WHEN tipo_ingreso = 'OPSU' THEN 1 ELSE 0 END) AS opsu, " &
                    "       SUM(CASE WHEN tipo_ingreso = 'FLOTANTE' THEN 1 ELSE 0 END) AS flotantes " &
                    "  FROM inscripciones WHERE estado <> 'ANULADA'"
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
