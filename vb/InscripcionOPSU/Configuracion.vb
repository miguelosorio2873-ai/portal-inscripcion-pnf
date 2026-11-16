Option Strict On
Option Explicit On

Imports System.IO
Imports MySql.Data.MySqlClient

''' <summary>
''' Configuración de la conexión a MySQL.
''' La base de datos es la MISMA que usa el portal web (portal_pnf).
''' Se puede sobrescribir con el archivo %APPDATA%\InscripcionOPSU\config.txt
''' o con la variable de entorno OPSU_DB (útil para pruebas).
''' </summary>
Friend NotInheritable Class Configuracion

    Public Property Servidor As String = "127.0.0.1"
    Public Property Puerto As Integer = 3306
    Public Property Usuario As String = "root"
    Public Property Clave As String = ""
    Public Property BaseDatos As String = "portal_pnf"

    Private Shared _actual As Configuracion

    Private Shared ReadOnly Property Ruta As String
        Get
            Return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "InscripcionOPSU", "config.txt")
        End Get
    End Property

    Public Shared ReadOnly Property Actual As Configuracion
        Get
            If _actual Is Nothing Then _actual = Cargar()
            Return _actual
        End Get
    End Property

    ''' <summary>Fuerza una relectura (lo usa la autoprueba al cambiar de base).</summary>
    Public Shared Sub Recargar()
        _actual = Cargar()
    End Sub

    Private Shared Function Cargar() As Configuracion
        Dim config As New Configuracion()

        If File.Exists(Ruta) Then
            For Each linea As String In File.ReadAllLines(Ruta)
                Dim limpia As String = linea.Trim()
                If limpia.Length = 0 OrElse limpia.StartsWith("#") OrElse Not limpia.Contains("=") Then Continue For
                Dim partes() As String = limpia.Split(New Char() {"="c}, 2)
                Dim clave As String = partes(0).Trim().ToLowerInvariant()
                Dim valor As String = partes(1).Trim()
                Select Case clave
                    Case "servidor" : config.Servidor = valor
                    Case "puerto" : config.Puerto = Integer.Parse(valor)
                    Case "usuario" : config.Usuario = valor
                    Case "clave" : config.Clave = valor
                    Case "basedatos", "base" : config.BaseDatos = valor
                End Select
            Next
        End If

        Dim basePrueba As String = Environment.GetEnvironmentVariable("OPSU_DB")
        If Not String.IsNullOrWhiteSpace(basePrueba) Then config.BaseDatos = basePrueba.Trim()

        Dim servidorPrueba As String = Environment.GetEnvironmentVariable("OPSU_HOST")
        If Not String.IsNullOrWhiteSpace(servidorPrueba) Then config.Servidor = servidorPrueba.Trim()

        Return config
    End Function

    ''' <summary>Cadena de conexión. Sin base de datos sirve para crear el esquema.</summary>
    Public Shared Function Cadena(Optional incluirBase As Boolean = True) As String
        Dim constructor As New MySqlConnectionStringBuilder()
        constructor.Server = Actual.Servidor
        constructor.Port = CUInt(Actual.Puerto)
        constructor.UserID = Actual.Usuario
        constructor.Password = Actual.Clave
        constructor.CharacterSet = "utf8mb4"
        constructor.AllowUserVariables = True
        constructor.DefaultCommandTimeout = 60UI
        If incluirBase Then constructor.Database = Actual.BaseDatos
        Return constructor.ConnectionString
    End Function

    ''' <summary>Texto descriptivo para la barra de estado y los reportes.</summary>
    Public Shared Function Descripcion() As String
        Return String.Format("{0}@{1}:{2}/{3}", Actual.Usuario, Actual.Servidor, Actual.Puerto, Actual.BaseDatos)
    End Function

End Class
