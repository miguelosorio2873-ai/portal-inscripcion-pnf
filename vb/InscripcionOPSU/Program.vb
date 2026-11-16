Option Strict On
Option Explicit On

Imports System
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Friend Module Program

    <STAThread()>
    Friend Sub Main(args As String())
        Try
            Console.OutputEncoding = Encoding.UTF8
        Catch
            ' Sin consola adjunta no es un problema.
        End Try

        If args.Any(Function(a) String.Equals(a, "--selftest", StringComparison.OrdinalIgnoreCase)) Then
            Environment.ExitCode = SelfTest.Ejecutar()
            Return
        End If

        ' Diagnóstico: --leer "archivo" muestra lo que el sistema entiende del archivo OPSU.
        Dim posicionLeer As Integer = Array.FindIndex(args, Function(a) String.Equals(a, "--leer", StringComparison.OrdinalIgnoreCase))
        If posicionLeer >= 0 AndAlso posicionLeer + 1 < args.Length Then
            Environment.ExitCode = LeerArchivoOpsu(args(posicionLeer + 1))
            Return
        End If

        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' Permite preparar el enlace desde un instalador o manualmente.
        If args.Any(Function(a) String.Equals(a, "--registrar", StringComparison.OrdinalIgnoreCase)) Then
            Environment.ExitCode = If(Protocolo.Registrar(), 0, 1)
            Return
        End If

        ' El enlace inscripcionopsu:// queda actualizado cada vez que se abre la aplicación.
        Protocolo.Registrar()

        ' Impresión solicitada desde el portal: solo llega el código del comprobante.
        If Protocolo.EsSolicitudDeImpresion(args) Then
            Dim codigo As String = Protocolo.CodigoDesdeArgumentos(args)
            If String.IsNullOrWhiteSpace(codigo) Then
                MessageBox.Show(
                    "Falta el código del comprobante." & Environment.NewLine &
                    "Uso: InscripcionOPSU.exe --imprimir INS-2026-000001",
                    "Impresión · Control de Estudios", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Environment.ExitCode = 1
                Return
            End If

            Try
                Datos.AsegurarEsquema()
            Catch ex As Exception
                MessageBox.Show(
                    "No se pudo preparar la base de datos MySQL." & Environment.NewLine & Environment.NewLine &
                    ex.Message & Environment.NewLine & Environment.NewLine &
                    "Verifique que el servicio MySQL esté iniciado en XAMPP y la configuración:" & Environment.NewLine &
                    Configuracion.Descripcion(),
                    "Impresión · Control de Estudios", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Environment.ExitCode = 1
                Return
            End Try

            Environment.ExitCode = Comprobante.ImprimirDirecto(codigo)
            Return
        End If

        Try
            Datos.AsegurarEsquema()
        Catch ex As Exception
            MessageBox.Show(
                "No se pudo preparar la base de datos MySQL." & Environment.NewLine & Environment.NewLine &
                ex.Message & Environment.NewLine & Environment.NewLine &
                "Verifique que el servicio MySQL esté iniciado en XAMPP y la configuración:" & Environment.NewLine &
                Configuracion.Descripcion(),
                "Inscripción Universitaria", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        Application.Run(New FormPrincipal())
    End Sub

    ''' <summary>Muestra por consola la interpretación del archivo OPSU (modo diagnóstico).</summary>
    Private Function LeerArchivoOpsu(ruta As String) As Integer
        Try
            Dim registros As Collections.Generic.List(Of Modelos.OpsuRegistro) = OpsuLector.Leer(ruta)
            Console.WriteLine("Archivo: " & ruta)
            Console.WriteLine("Registros leídos: " & registros.Count.ToString())
            Console.WriteLine("FILA  CEDULA        TRAYECTO   CARRERA                     ESTUDIANTE")
            For Each registro As Modelos.OpsuRegistro In registros
                Console.WriteLine(String.Format("{0,-5} {1,-13} {2,-10} {3,-27} {4}",
                                  registro.Fila, registro.Cedula, Utilidades.TrayectoTexto(registro.Trayecto),
                                  registro.Carrera, registro.NombreCompleto))
            Next
            Return 0
        Catch ex As Exception
            Console.WriteLine("ERROR: " & ex.Message)
            Return 1
        End Try
    End Function

End Module
