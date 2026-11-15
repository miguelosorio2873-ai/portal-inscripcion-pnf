Imports System
Imports System.Drawing.Printing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Impresión solicitada desde el portal: recibe únicamente el código, consulta
''' todos los datos en la base de datos compartida, imprime en la impresora
''' predeterminada y registra la impresión.
''' </summary>
Friend NotInheritable Class ImpresionDirecta

    Private Sub New()
    End Sub

    Public Shared Function Ejecutar(codigo As String, args As String()) As Integer
        Dim rutaPdf As String = ObtenerOpcion(args, "--pdf")
        Dim silencioso As Boolean = Not String.IsNullOrWhiteSpace(rutaPdf)

        Try
            If silencioso Then PrepararArchivo(rutaPdf)

            If String.IsNullOrWhiteSpace(codigo) Then
                Return Fallar("No se recibió el código del comprobante.", silencioso, rutaPdf)
            End If

            Dim id As Integer = Datos.ObtenerIdPorCodigo(codigo)
            If id <= 0 Then
                Return Fallar("No se encontró el comprobante " & codigo & " en la base de datos.",
                              silencioso, rutaPdf)
            End If

            Dim inscripcion As InscripcionCompleta = Datos.ObtenerCompleta(id)
            If inscripcion Is Nothing Then
                Return Fallar("No se pudieron leer los datos del comprobante " & codigo & ".",
                              silencioso, rutaPdf)
            End If

            If String.Equals(inscripcion.Estado, "ANULADA", StringComparison.OrdinalIgnoreCase) Then
                Return Fallar("La inscripción " & codigo & " está ANULADA." & Environment.NewLine &
                              "Imprímala desde la aplicación de escritorio si necesita una copia.",
                              silencioso, rutaPdf)
            End If

            Using impresora As New ComprobantePrinter(inscripcion)
                Dim documento As PrintDocument = impresora.Documento

                If silencioso Then
                    documento.PrinterSettings.PrinterName = "Microsoft Print to PDF"
                    documento.PrinterSettings.PrintToFile = True
                    documento.PrinterSettings.PrintFileName = rutaPdf
                End If

                If Not documento.PrinterSettings.IsValid Then
                    Return Fallar("No hay una impresora válida configurada en el sistema." & Environment.NewLine &
                                  "Configura una impresora predeterminada o usa la impresión desde el navegador.",
                                  silencioso, rutaPdf)
                End If

                documento.Print()

                If silencioso AndAlso
                   (Not File.Exists(rutaPdf) OrElse New FileInfo(rutaPdf).Length = 0) Then
                    Return Fallar("La impresión no generó el archivo PDF esperado.", silencioso, rutaPdf)
                End If
            End Using

            Try
                Datos.MarcarImprimida(id)
            Catch ex As Exception
                Return Fallar("El comprobante se imprimió, pero no se pudo registrar la impresión." &
                              Environment.NewLine & ex.Message, silencioso, rutaPdf, ex)
            End Try

            If Not silencioso Then
                MessageBox.Show(
                    "Comprobante " & inscripcion.Codigo & " de " & inscripcion.NombreCompleto & " enviado a la impresora." &
                    Environment.NewLine &
                    "La impresión quedó registrada en la base de datos.",
                    "Impresión directa · Control de Estudios",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)
            End If

            Return 0
        Catch ex As Exception
            Return Fallar("No se pudo imprimir el comprobante." & Environment.NewLine & ex.Message,
                          silencioso, rutaPdf, ex)
        End Try
    End Function

    Private Shared Function Fallar(mensaje As String, silencioso As Boolean, rutaPdf As String,
                                   Optional excepcion As Exception = Nothing) As Integer
        If silencioso Then
            Try
                If Not String.IsNullOrWhiteSpace(rutaPdf) Then
                    Dim detalle As String = mensaje
                    If excepcion IsNot Nothing Then
                        detalle &= Environment.NewLine & excepcion.ToString()
                    End If
                    File.WriteAllText(rutaPdf & ".error.txt", detalle)
                End If
            Catch
                ' El código de salida sigue indicando el fallo.
            End Try
        Else
            MessageBox.Show(mensaje, "Impresión directa · Control de Estudios",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If

        Return 1
    End Function

    Private Shared Sub PrepararArchivo(rutaPdf As String)
        Dim carpeta As String = Path.GetDirectoryName(rutaPdf)
        If Not String.IsNullOrWhiteSpace(carpeta) Then Directory.CreateDirectory(carpeta)

        If File.Exists(rutaPdf) Then File.Delete(rutaPdf)

        Dim errores As String = rutaPdf & ".error.txt"
        If File.Exists(errores) Then File.Delete(errores)
    End Sub

    Private Shared Function ObtenerOpcion(args As String(), nombre As String) As String
        If args Is Nothing Then Return Nothing

        For indice As Integer = 0 To args.Length - 2
            If String.Equals(args(indice), nombre, StringComparison.OrdinalIgnoreCase) Then
                Return args(indice + 1)
            End If
        Next

        Return Nothing
    End Function

End Class
