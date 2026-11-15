Imports System.Globalization
Imports System.IO
Imports System.Text.Json
Imports MySql.Data.MySqlClient

''' <summary>
''' Resuelve y guarda la conexion a MySQL/MariaDB compartida con el portal web.
''' Prioridad: variables de entorno DB_HOST, DB_PORT, DB_USER, DB_PASSWORD y
''' DB_NAME; luego el archivo de configuracion del usuario; luego los valores
''' locales de XAMPP (root sin contrasena, 127.0.0.1:3306, portal_pnf).
''' </summary>
Public NotInheritable Class Configuracion

    Private Shared ReadOnly CarpetaConfiguracion As String =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ComprobantesVB")

    Private Shared ReadOnly ArchivoConfiguracion As String =
        Path.Combine(CarpetaConfiguracion, "config.json")

    Private Shared _cargado As Boolean
    Private Shared _servidor As String = "127.0.0.1"
    Private Shared _puerto As Integer = 3306
    Private Shared _usuario As String = "root"
    Private Shared _contrasena As String = String.Empty
    Private Shared _baseDatos As String = "portal_pnf"

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property ArchivoDeConfiguracion As String
        Get
            Return ArchivoConfiguracion
        End Get
    End Property

    Public Shared ReadOnly Property Servidor As String
        Get
            AsegurarCargado()
            Return _servidor
        End Get
    End Property

    Public Shared ReadOnly Property Puerto As Integer
        Get
            AsegurarCargado()
            Return _puerto
        End Get
    End Property

    Public Shared ReadOnly Property Usuario As String
        Get
            AsegurarCargado()
            Return _usuario
        End Get
    End Property

    Public Shared ReadOnly Property Contrasena As String
        Get
            AsegurarCargado()
            Return _contrasena
        End Get
    End Property

    Public Shared ReadOnly Property BaseDatos As String
        Get
            AsegurarCargado()
            Return _baseDatos
        End Get
    End Property

    Public Shared ReadOnly Property CadenaConexion As String
        Get
            AsegurarCargado()
            Return CrearCadena(_servidor, _puerto, _usuario, _contrasena, _baseDatos)
        End Get
    End Property

    Public Shared ReadOnly Property DescripcionConexion As String
        Get
            AsegurarCargado()
            Return $"{_usuario}@{_servidor}:{_puerto}/{_baseDatos}"
        End Get
    End Property

    ''' <summary>Construye una cadena de conexion para MySQL.</summary>
    Public Shared Function CrearCadena(servidor As String, puerto As Integer, usuario As String,
                                       contrasena As String, baseDatos As String) As String
        Dim constructor As New MySqlConnectionStringBuilder()
        constructor.Server = If(servidor, String.Empty).Trim()
        constructor.Port = CUInt(puerto)
        constructor.UserID = If(usuario, String.Empty).Trim()
        constructor.Password = If(contrasena, String.Empty)
        constructor.Database = If(baseDatos, String.Empty).Trim()
        constructor.CharacterSet = "utf8mb4"
        constructor.SslMode = MySqlSslMode.Disabled
        constructor.ConnectionTimeout = CUInt(8)
        constructor.DefaultCommandTimeout = CUInt(30)
        constructor.AllowUserVariables = True
        Return constructor.ConnectionString
    End Function

    ''' <summary>Guarda la conexion elegida y la usa inmediatamente.</summary>
    Public Shared Sub Configurar(servidor As String, puerto As Integer, usuario As String,
                                 contrasena As String, baseDatos As String)
        _servidor = If(servidor, String.Empty).Trim()
        _puerto = puerto
        _usuario = If(usuario, String.Empty).Trim()
        _contrasena = If(contrasena, String.Empty)
        _baseDatos = If(baseDatos, String.Empty).Trim()
        _cargado = True
        Guardar()
    End Sub

    Private Shared Sub AsegurarCargado()
        If _cargado Then Return
        _cargado = True

        Try
            If File.Exists(ArchivoConfiguracion) Then
                Using documento = JsonDocument.Parse(File.ReadAllText(ArchivoConfiguracion))
                    Dim mysql As JsonElement
                    If documento.RootElement.ValueKind = JsonValueKind.Object AndAlso
                       documento.RootElement.TryGetProperty("mysql", mysql) AndAlso
                       mysql.ValueKind = JsonValueKind.Object Then
                        _servidor = LeerTexto(mysql, "servidor", _servidor)
                        _puerto = LeerEntero(mysql, "puerto", _puerto)
                        _usuario = LeerTexto(mysql, "usuario", _usuario)
                        _contrasena = LeerTextoVacio(mysql, "contrasena", _contrasena)
                        _baseDatos = LeerTexto(mysql, "baseDatos", _baseDatos)
                    End If
                End Using
            End If
        Catch
            ' Si el archivo esta danado se usan los valores por defecto.
        End Try

        ' Las variables de entorno tienen prioridad sobre el archivo.
        _servidor = EntornoTexto("DB_HOST", _servidor)
        _puerto = EntornoEntero("DB_PORT", _puerto)
        _usuario = EntornoTexto("DB_USER", _usuario)
        Dim contrasenaEntorno As String = Environment.GetEnvironmentVariable("DB_PASSWORD")
        If contrasenaEntorno IsNot Nothing Then _contrasena = contrasenaEntorno
        _baseDatos = EntornoTexto("DB_NAME", _baseDatos)

        If String.IsNullOrWhiteSpace(_servidor) Then _servidor = "127.0.0.1"
        If String.IsNullOrWhiteSpace(_usuario) Then _usuario = "root"
        If String.IsNullOrWhiteSpace(_baseDatos) Then _baseDatos = "portal_pnf"
        If _puerto < 1 OrElse _puerto > 65535 Then _puerto = 3306
    End Sub

    Private Shared Function LeerTexto(elemento As JsonElement, nombre As String, actual As String) As String
        Dim propiedad As JsonElement
        If elemento.TryGetProperty(nombre, propiedad) AndAlso propiedad.ValueKind = JsonValueKind.String Then
            Dim valor As String = propiedad.GetString()
            If Not String.IsNullOrWhiteSpace(valor) Then Return valor.Trim()
        End If
        Return actual
    End Function

    Private Shared Function LeerTextoVacio(elemento As JsonElement, nombre As String, actual As String) As String
        Dim propiedad As JsonElement
        If elemento.TryGetProperty(nombre, propiedad) AndAlso propiedad.ValueKind = JsonValueKind.String Then
            Return If(propiedad.GetString(), String.Empty)
        End If
        Return actual
    End Function

    Private Shared Function LeerEntero(elemento As JsonElement, nombre As String, actual As Integer) As Integer
        Dim propiedad As JsonElement
        Dim valor As Integer
        If elemento.TryGetProperty(nombre, propiedad) AndAlso
           propiedad.ValueKind = JsonValueKind.Number AndAlso
           propiedad.TryGetInt32(valor) AndAlso
           valor >= 1 AndAlso valor <= 65535 Then
            Return valor
        End If
        Return actual
    End Function

    Private Shared Function EntornoTexto(nombre As String, actual As String) As String
        Dim valor As String = Environment.GetEnvironmentVariable(nombre)
        If Not String.IsNullOrWhiteSpace(valor) Then Return valor.Trim()
        Return actual
    End Function

    Private Shared Function EntornoEntero(nombre As String, actual As Integer) As Integer
        Dim valor As String = Environment.GetEnvironmentVariable(nombre)
        Dim numero As Integer
        If Not String.IsNullOrWhiteSpace(valor) AndAlso
           Integer.TryParse(valor.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, numero) AndAlso
           numero >= 1 AndAlso numero <= 65535 Then
            Return numero
        End If
        Return actual
    End Function

    Private Shared Sub Guardar()
        Try
            Directory.CreateDirectory(CarpetaConfiguracion)
            Dim contenido = New With {
                .mysql = New With {
                    .servidor = _servidor,
                    .puerto = _puerto,
                    .usuario = _usuario,
                    .contrasena = _contrasena,
                    .baseDatos = _baseDatos
                }
            }
            Dim opciones As New JsonSerializerOptions With {.WriteIndented = True}
            File.WriteAllText(ArchivoConfiguracion, JsonSerializer.Serialize(contenido, opciones))
        Catch
            ' Guardar la preferencia no es critico para imprimir.
        End Try
    End Sub

End Class
