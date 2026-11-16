Imports System.Globalization
Imports MySql.Data.MySqlClient

''' <summary>Acceso a la base de datos MySQL/MariaDB compartida con el portal web.</summary>
Public NotInheritable Class Datos

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property CadenaConexion As String
        Get
            Return Configuracion.CadenaConexion
        End Get
    End Property

    Private Shared Function Abrir() As MySqlConnection
        Dim conexion As New MySqlConnection(CadenaConexion)
        Try
            conexion.Open()
            Return conexion
        Catch ex As Exception
            conexion.Dispose()
            Throw New InvalidOperationException(
                "No se pudo conectar con MySQL." & Environment.NewLine &
                "Conexión: " & Configuracion.DescripcionConexion & Environment.NewLine & Environment.NewLine &
                "Verifica que MySQL/MariaDB de XAMPP esté iniciado. Si usas otro usuario, puerto o" & Environment.NewLine &
                "base de datos, configúralo desde el menú Archivo → Configurar conexión MySQL.",
                ex)
        End Try
    End Function

    ''' <summary>Devuelve el mensaje de error de la conexión o una cadena vacía si es válida.</summary>
    Public Shared Function ValidarConexion(servidor As String, puerto As Integer, usuario As String,
                                           contrasena As String, baseDatos As String) As String
        If String.IsNullOrWhiteSpace(servidor) Then Return "Indica el servidor de MySQL."
        If String.IsNullOrWhiteSpace(usuario) Then Return "Indica el usuario de MySQL."
        If String.IsNullOrWhiteSpace(baseDatos) Then Return "Indica el nombre de la base de datos."
        If puerto < 1 OrElse puerto > 65535 Then Return "El puerto debe estar entre 1 y 65535."

        Try
            Using conexion As New MySqlConnection(
                Configuracion.CrearCadena(servidor, puerto, usuario, contrasena, baseDatos))
                conexion.Open()
                Using comando = conexion.CreateCommand()
                    comando.CommandText = "SELECT COUNT(*) FROM inscripciones"
                    comando.ExecuteScalar()
                End Using
            End Using
            Return String.Empty
        Catch ex As Exception
            Return "No se pudo conectar con MySQL." & Environment.NewLine &
                   "Servidor: " & servidor & ":" & puerto.ToString(CultureInfo.InvariantCulture) &
                   " · Base: " & baseDatos & Environment.NewLine & Environment.NewLine &
                   ex.Message
        End Try
    End Function

    ''' <summary>Carreras disponibles para el filtro del listado.</summary>
    Public Shared Function ListarCarreras() As List(Of OpcionFiltro)
        Dim lista As New List(Of OpcionFiltro)()

        Using conexion = Abrir()
            Using comando = conexion.CreateCommand()
                comando.CommandText = "SELECT id, nombre FROM carreras ORDER BY id"

                Using lector = comando.ExecuteReader()
                    While lector.Read()
                        lista.Add(New OpcionFiltro With {
                            .Valor = Entero(lector, 0).ToString(CultureInfo.InvariantCulture),
                            .Texto = Texto(lector, 1)
                        })
                    End While
                End Using
            End Using
        End Using

        Return lista
    End Function

    ''' <summary>
    ''' Busca inscripciones aplicando el texto libre y los filtros opcionales.
    ''' Un filtro con valor 0 o vacío no se aplica.
    ''' </summary>
    Public Shared Function Buscar(filtro As String, soloPendientes As Boolean,
                                  Optional carreraId As Integer = 0,
                                  Optional semestre As Integer = 0,
                                  Optional turno As String = "",
                                  Optional condicion As String = "",
                                  Optional estado As String = "") As List(Of InscripcionResumen)
        Dim resultado As New List(Of InscripcionResumen)()
        Dim busqueda As String = If(filtro, String.Empty).Trim()
        Dim turnoFiltro As String = If(turno, String.Empty).Trim()
        Dim condicionFiltro As String = If(condicion, String.Empty).Trim().ToUpperInvariant()
        Dim estadoFiltro As String = If(estado, String.Empty).Trim().ToUpperInvariant()

        Using conexion = Abrir()
            Using comando = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT i.id, i.codigo, e.cedula, " &
                    "       CONCAT(e.nombres, ' ', e.apellidos) AS estudiante, " &
                    "       c.nombre AS carrera, i.semestre, i.turno, " &
                    "       i.fecha_inscripcion, i.estado, i.impresiones, i.condicion " &
                    "  FROM inscripciones i " &
                    "  JOIN estudiantes e ON e.id = i.estudiante_id " &
                    "  JOIN carreras c ON c.id = i.carrera_id " &
                    " WHERE (@filtro = '' OR e.cedula LIKE @like OR i.codigo LIKE @like " &
                    "        OR e.nombres LIKE @like OR e.apellidos LIKE @like) " &
                    "   AND (@soloPendientes = 0 OR i.impresiones = 0) " &
                    "   AND (@carreraId = 0 OR i.carrera_id = @carreraId) " &
                    "   AND (@semestre = 0 OR i.semestre = @semestre) " &
                    "   AND (@turno = '' OR i.turno = @turno) " &
                    "   AND (@condicion = '' OR i.condicion = @condicion) " &
                    "   AND (@estado = '' OR i.estado = @estado) " &
                    " ORDER BY i.fecha_inscripcion DESC, i.id DESC " &
                    " LIMIT 500"

                comando.Parameters.AddWithValue("@filtro", busqueda)
                comando.Parameters.AddWithValue("@like", "%" & busqueda & "%")
                comando.Parameters.AddWithValue("@soloPendientes", If(soloPendientes, 1, 0))
                comando.Parameters.AddWithValue("@carreraId", carreraId)
                comando.Parameters.AddWithValue("@semestre", semestre)
                comando.Parameters.AddWithValue("@turno", turnoFiltro)
                comando.Parameters.AddWithValue("@condicion", condicionFiltro)
                comando.Parameters.AddWithValue("@estado", estadoFiltro)

                Using lector = comando.ExecuteReader()
                    While lector.Read()
                        resultado.Add(New InscripcionResumen With {
                            .Id = lector.GetInt32(0),
                            .Codigo = Texto(lector, 1),
                            .Cedula = Texto(lector, 2),
                            .Estudiante = Texto(lector, 3),
                            .Carrera = Texto(lector, 4),
                            .Semestre = Entero(lector, 5),
                            .Turno = Texto(lector, 6),
                            .Condicion = Texto(lector, 10),
                            .FechaInscripcion = Texto(lector, 7),
                            .Estado = Texto(lector, 8),
                            .Impresiones = Entero(lector, 9)
                        })
                    End While
                End Using
            End Using
        End Using

        Return resultado
    End Function

    ''' <summary>Busca una inscripción por su código de comprobante.</summary>
    Public Shared Function ObtenerIdPorCodigo(codigo As String) As Integer
        If String.IsNullOrWhiteSpace(codigo) Then Return 0

        Using conexion = Abrir()
            Using comando = conexion.CreateCommand()
                comando.CommandText = "SELECT id FROM inscripciones WHERE codigo = @codigo LIMIT 1"
                comando.Parameters.AddWithValue("@codigo", codigo.Trim())

                Dim valor As Object = comando.ExecuteScalar()
                If valor Is Nothing OrElse Convert.IsDBNull(valor) Then Return 0

                Return Convert.ToInt32(valor)
            End Using
        End Using
    End Function

    Public Shared Function ObtenerCompleta(id As Integer) As InscripcionCompleta
        Dim inscripcion As InscripcionCompleta = Nothing

        Using conexion = Abrir()
            Using comando = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT i.id, i.codigo, e.cedula, e.nombres, e.apellidos, e.email, e.telefono, " &
                    "       e.fecha_nacimiento, e.direccion, c.nombre, c.codigo, i.semestre, i.turno, " &
                    "       i.fecha_inscripcion, i.estado, i.observaciones, i.impreso_en, i.impresiones, " &
                    "       i.condicion " &
                    "  FROM inscripciones i " &
                    "  JOIN estudiantes e ON e.id = i.estudiante_id " &
                    "  JOIN carreras c ON c.id = i.carrera_id " &
                    " WHERE i.id = @id"
                comando.Parameters.AddWithValue("@id", id)

                Using lector = comando.ExecuteReader()
                    If lector.Read() Then
                        inscripcion = New InscripcionCompleta With {
                            .Id = lector.GetInt32(0),
                            .Codigo = Texto(lector, 1),
                            .Cedula = Texto(lector, 2),
                            .Nombres = Texto(lector, 3),
                            .Apellidos = Texto(lector, 4),
                            .Email = Texto(lector, 5),
                            .Telefono = Texto(lector, 6),
                            .FechaNacimiento = Texto(lector, 7),
                            .Direccion = Texto(lector, 8),
                            .Carrera = Texto(lector, 9),
                            .CarreraCodigo = Texto(lector, 10),
                            .Semestre = Entero(lector, 11),
                            .Turno = Texto(lector, 12),
                            .FechaInscripcion = Texto(lector, 13),
                            .Estado = Texto(lector, 14),
                            .Observaciones = Texto(lector, 15),
                            .ImpresoEn = Texto(lector, 16),
                            .Impresiones = Entero(lector, 17),
                            .Condicion = Texto(lector, 18)
                        }
                    End If
                End Using
            End Using

            If inscripcion Is Nothing Then Return Nothing

            Using comando = conexion.CreateCommand()
                comando.CommandText =
                    "SELECT a.codigo, a.nombre, a.semestre " &
                    "  FROM inscripcion_asignaturas ia " &
                    "  JOIN asignaturas a ON a.id = ia.asignatura_id " &
                    " WHERE ia.inscripcion_id = @id " &
                    " ORDER BY a.codigo"
                comando.Parameters.AddWithValue("@id", id)

                Using lector = comando.ExecuteReader()
                    While lector.Read()
                        inscripcion.Asignaturas.Add(New AsignaturaInscrita With {
                            .Codigo = Texto(lector, 0),
                            .Nombre = Texto(lector, 1),
                            .Semestre = Entero(lector, 2)
                        })
                    End While
                End Using
            End Using
        End Using

        Return inscripcion
    End Function

    ''' <summary>Marca la inscripción como impresa y aumenta el contador de impresiones.</summary>
    Public Shared Sub MarcarImprimida(id As Integer)
        Using conexion = Abrir()
            Using comando = conexion.CreateCommand()
                comando.CommandText =
                    "UPDATE inscripciones " &
                    "   SET estado = CASE WHEN estado = 'ANULADA' THEN 'ANULADA' ELSE 'IMPRESA' END, " &
                    "       impreso_en = NOW(), " &
                    "       impresiones = impresiones + 1 " &
                    " WHERE id = @id"
                comando.Parameters.AddWithValue("@id", id)
                comando.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Shared Function ContarInscripciones() As Integer
        Using conexion = Abrir()
            Using comando = conexion.CreateCommand()
                comando.CommandText = "SELECT COUNT(*) FROM inscripciones"
                Return Convert.ToInt32(comando.ExecuteScalar())
            End Using
        End Using
    End Function

    Private Shared Function Texto(lector As MySqlDataReader, indice As Integer) As String
        If lector.IsDBNull(indice) Then Return String.Empty
        Dim valor As Object = lector.GetValue(indice)
        If valor Is Nothing Then Return String.Empty

        If TypeOf valor Is DateTime Then
            Dim fecha As DateTime = DirectCast(valor, DateTime)
            If fecha.TimeOfDay = TimeSpan.Zero Then
                Return fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            End If
            Return fecha.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        End If

        Return Convert.ToString(valor, CultureInfo.InvariantCulture)
    End Function

    Private Shared Function Entero(lector As MySqlDataReader, indice As Integer) As Integer
        If lector.IsDBNull(indice) Then Return 0
        Return Convert.ToInt32(lector.GetValue(indice))
    End Function

End Class
