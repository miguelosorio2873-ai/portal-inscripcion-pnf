Imports System
Imports System.Linq
Imports System.Windows.Forms

Friend Module Program

    <STAThread()>
    Friend Sub Main(args As String())
        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' Modo de autoprueba sin interfaz: escribe un informe en la carpeta temporal.
        If args.Any(Function(argumento) String.Equals(argumento, "--selftest", StringComparison.OrdinalIgnoreCase)) Then
            Dim imprimirPdf As Boolean = args.Any(Function(argumento) String.Equals(argumento, "--print", StringComparison.OrdinalIgnoreCase))
            Dim marcarImpresion As Boolean = args.Any(Function(argumento) String.Equals(argumento, "--marcar", StringComparison.OrdinalIgnoreCase))
            SelfTest.Ejecutar(imprimirPdf, marcarImpresion)
            Return
        End If

        ' Permite preparar el enlace desde un instalador o manualmente.
        If args.Any(Function(argumento) String.Equals(argumento, "--registrar", StringComparison.OrdinalIgnoreCase)) Then
            Environment.ExitCode = If(Protocolo.Registrar(), 0, 1)
            Return
        End If

        ' El enlace comprobantesvb:// queda actualizado cada vez que se abre la aplicación.
        Protocolo.Registrar()

        ' Impresión solicitada desde el portal: el código es lo único que envía el navegador.
        Dim codigo As String = Protocolo.CodigoDesdeArgumentos(args)
        If Not String.IsNullOrWhiteSpace(codigo) Then
            Environment.ExitCode = ImpresionDirecta.Ejecutar(codigo, args)
            Return
        End If

        If args.Any(Function(argumento) String.Equals(argumento, "--imprimir", StringComparison.OrdinalIgnoreCase)) Then
            MessageBox.Show("Falta el código del comprobante." & Environment.NewLine &
                            "Uso: ComprobantesVB.exe --imprimir INS-2026-000001",
                            "Impresión directa", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Environment.ExitCode = 1
            Return
        End If

        If args.Any(Function(argumento) argumento.StartsWith(Protocolo.Esquema & "://", StringComparison.OrdinalIgnoreCase)) Then
            MessageBox.Show("El enlace de impresión no es válido." & Environment.NewLine &
                            "Abre el comprobante desde el portal e inténtalo de nuevo.",
                            "Impresión directa", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Environment.ExitCode = 1
            Return
        End If

        Application.Run(New MainForm())
    End Sub

End Module
