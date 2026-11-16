Option Strict On
Option Explicit On

Imports System.IO
Imports System.Reflection
Imports System.Windows.Forms
Imports Microsoft.Win32

''' <summary>
''' Registra el esquema inscripcionopsu:// en el usuario de Windows para que el
''' portal web pueda abrir el sistema y solicitar la impresión de un comprobante.
''' El portal solo envía el código: los datos se consultan en la base compartida.
''' </summary>
Friend NotInheritable Class Protocolo

    Public Const Esquema As String = "inscripcionopsu"
    Private Const ClaveClase As String = "Software\Classes\" & Esquema

    Private Sub New()
    End Sub

    ''' <summary>Registra o actualiza el enlace en HKEY_CURRENT_USER (no requiere administrador).</summary>
    Public Shared Function Registrar() As Boolean
        Try
            Dim ejecutable As String = RutaAplicacion()
            Dim comando As String = ComandoApertura()
            If String.IsNullOrWhiteSpace(ejecutable) OrElse String.IsNullOrWhiteSpace(comando) Then
                Return False
            End If

            Using clase As RegistryKey = Registry.CurrentUser.CreateSubKey(ClaveClase, True)
                If clase Is Nothing Then Return False

                clase.SetValue("", "URL:Inscripción Universitaria PNF", RegistryValueKind.String)
                clase.SetValue("URL Protocol", "", RegistryValueKind.String)

                Using icono As RegistryKey = clase.CreateSubKey("DefaultIcon", True)
                    If icono IsNot Nothing Then
                        icono.SetValue("", """" & RutaIcono() & """,0", RegistryValueKind.String)
                    End If
                End Using

                Using shell As RegistryKey = clase.CreateSubKey("shell", True)
                    If shell Is Nothing Then Return False

                    Using abrir As RegistryKey = shell.CreateSubKey("open", True)
                        If abrir Is Nothing Then Return False

                        Using comandoClave As RegistryKey = abrir.CreateSubKey("command", True)
                            If comandoClave Is Nothing Then Return False
                            comandoClave.SetValue("", comando, RegistryValueKind.String)
                        End Using
                    End Using
                End Using
            End Using

            Return True
        Catch
            Return False
        End Try
    End Function

    ''' <summary>Indica si el enlace ya apunta a esta misma aplicación.</summary>
    Public Shared Function EstaRegistrado() As Boolean
        Try
            Using clave As RegistryKey = Registry.CurrentUser.OpenSubKey(ClaveClase & "\shell\open\command")
                If clave Is Nothing Then Return False

                Dim comando As String = TryCast(clave.GetValue(""), String)
                If String.IsNullOrWhiteSpace(comando) Then Return False

                Return comando.IndexOf(RutaAplicacion(), StringComparison.OrdinalIgnoreCase) >= 0
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Extrae el código de un comprobante de argumentos como:
    ''' inscripcionopsu://imprimir/INS-2026-000001 o --imprimir INS-2026-000001.
    ''' </summary>
    Public Shared Function CodigoDesdeArgumentos(args As String()) As String
        If args Is Nothing Then Return Nothing

        For indice As Integer = 0 To args.Length - 1
            Dim argumento As String = args(indice)

            If String.Equals(argumento, "--imprimir", StringComparison.OrdinalIgnoreCase) Then
                If indice + 1 < args.Length Then Return LimpiarCodigo(args(indice + 1))
                Return Nothing
            End If

            If argumento.StartsWith(Esquema & "://", StringComparison.OrdinalIgnoreCase) Then
                Dim codigo As String = CodigoDesdeUri(argumento)
                If codigo IsNot Nothing Then Return codigo
            End If
        Next

        Return Nothing
    End Function

    ''' <summary>Indica si los argumentos piden imprimir un comprobante (válido o no).</summary>
    Public Shared Function EsSolicitudDeImpresion(args As String()) As Boolean
        If args Is Nothing Then Return False

        For Each argumento As String In args
            If String.Equals(argumento, "--imprimir", StringComparison.OrdinalIgnoreCase) Then Return True
            If argumento.StartsWith(Esquema & "://", StringComparison.OrdinalIgnoreCase) Then Return True
        Next

        Return False
    End Function

    Private Shared Function CodigoDesdeUri(valor As String) As String
        Try
            Dim uri As New Uri(valor)
            If Not String.Equals(uri.Host, "imprimir", StringComparison.OrdinalIgnoreCase) Then Return Nothing

            Dim codigo As String = Uri.UnescapeDataString(uri.AbsolutePath).Trim("/"c)
            Return LimpiarCodigo(codigo)
        Catch
            Return Nothing
        End Try
    End Function

    Private Shared Function LimpiarCodigo(valor As String) As String
        If String.IsNullOrWhiteSpace(valor) Then Return Nothing

        Dim limpio As String = valor.Trim().Trim("/"c)
        If limpio.Length = 0 Then Return Nothing

        Return limpio.ToUpperInvariant()
    End Function

    Private Shared Function RutaAplicacion() As String
        Dim proceso As String = Environment.ProcessPath
        If Not String.IsNullOrWhiteSpace(proceso) AndAlso
           Not String.Equals(Path.GetFileName(proceso), "dotnet.exe", StringComparison.OrdinalIgnoreCase) Then
            Return proceso
        End If

        Return Application.ExecutablePath
    End Function

    Private Shared Function RutaIcono() As String
        Dim apphost As String = Path.Combine(AppContext.BaseDirectory, "InscripcionOPSU.exe")
        If File.Exists(apphost) Then Return apphost

        Return RutaAplicacion()
    End Function

    Private Shared Function ComandoApertura() As String
        Dim proceso As String = Environment.ProcessPath

        ' Al ejecutar con "dotnet run" el proceso es dotnet.exe; hay que pasar el ensamblado.
        If Not String.IsNullOrWhiteSpace(proceso) AndAlso
           String.Equals(Path.GetFileName(proceso), "dotnet.exe", StringComparison.OrdinalIgnoreCase) Then
            Dim entrada As Assembly = Assembly.GetEntryAssembly()
            If entrada IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(entrada.Location) Then
                Return """" & proceso & """ """ & entrada.Location & """ ""%1"""
            End If
        End If

        Dim ejecutable As String = RutaAplicacion()
        If String.IsNullOrWhiteSpace(ejecutable) Then Return Nothing

        Return """" & ejecutable & """ ""%1"""
    End Function

End Class
