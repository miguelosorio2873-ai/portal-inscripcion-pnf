Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Drawing.Printing
Imports System.IO
Imports System.Linq
Imports System.Text

''' <summary>
''' Prueba automática sin interacción: lee la base de datos, genera el
''' comprobante en una imagen y deja un informe en la carpeta temporal.
''' Se ejecuta con:  ComprobantesVB.exe --selftest [--print]
''' </summary>
Public NotInheritable Class SelfTest

    Private Sub New()
    End Sub

    Public Shared Sub Ejecutar(Optional imprimirPdf As Boolean = False, Optional marcarImpresion As Boolean = False)
        Dim reporte As New StringBuilder()
        Dim rutaReporte As String = Path.Combine(Path.GetTempPath(), "comprobantes-selftest.txt")
        Dim rutaImagen As String = Path.Combine(Path.GetTempPath(), "comprobante-selftest.png")
        Dim correcto As Boolean = True

        Try
            reporte.AppendLine("AUTOPRUEBA · ComprobantesVB")
            reporte.AppendLine("Fecha: " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            reporte.AppendLine("Base de datos: MySQL " & Configuracion.DescripcionConexion)
            reporte.AppendLine()

            Dim inscripciones As List(Of InscripcionResumen) = Datos.Buscar("", False)
            reporte.AppendLine("Inscripciones encontradas: " & inscripciones.Count.ToString())

            If inscripciones.Count = 0 Then
                reporte.AppendLine("No hay inscripciones para probar.")
                correcto = False
            Else
                Dim resumen As InscripcionResumen = inscripciones(0)
                Dim completa As InscripcionCompleta = Datos.ObtenerCompleta(resumen.Id)

                If completa Is Nothing Then
                    reporte.AppendLine("No se pudo obtener el detalle de la inscripción.")
                    correcto = False
                Else
                    reporte.AppendLine("Comprobante: " & completa.Codigo)
                    reporte.AppendLine("Estudiante: " & completa.NombreCompleto & " (" & completa.Cedula & ")")
                    reporte.AppendLine("Carrera: " & completa.Carrera)
                    reporte.AppendLine("Semestre: " & completa.SemestreTexto & " · Turno: " & completa.Turno &
                                       " · Condición: " & completa.CondicionTexto)
                    reporte.AppendLine("Estado: " & completa.Estado & " · Impresiones: " & completa.Impresiones.ToString())
                    reporte.AppendLine("Asignaturas: " & completa.Asignaturas.Count.ToString())
                    For Each asignatura As AsignaturaInscrita In completa.Asignaturas
                        reporte.AppendLine("  - " & asignatura.Codigo & "  " & asignatura.Nombre)
                    Next
                    reporte.AppendLine()

                    Using impresora As New ComprobantePrinter(completa)
                        impresora.RegistrarTextos = True

                        reporte.AppendLine("Logo institucional: " & If(impresora.LogoDisponible, "sí", "no"))
                        If Not impresora.LogoDisponible Then
                            reporte.AppendLine("FALLA: no se encontró Recursos\logo-uptll.jpg junto al ejecutable.")
                            correcto = False
                        End If

                        Using imagen As Bitmap = impresora.Renderizar(850, 1100)
                            imagen.Save(rutaImagen, ImageFormat.Png)
                            reporte.AppendLine("Imagen generada: " & rutaImagen)
                            reporte.AppendLine("Tamaño: " & imagen.Width.ToString() & "x" & imagen.Height.ToString())

                            correcto = AnalizarImagen(imagen, reporte) AndAlso correcto
                        End Using

                        reporte.AppendLine()
                        reporte.AppendLine("Textos dibujados: " & impresora.TextosDibujados.Count.ToString())
                        For Each linea As String In impresora.TextosDibujados
                            reporte.AppendLine("  " & linea)
                        Next

                        correcto = ValidarTextos(impresora.TextosDibujados, completa, reporte) AndAlso correcto
                    End Using

                    If imprimirPdf Then
                        correcto = GenerarPdf(completa, reporte) AndAlso correcto
                    End If

                    If marcarImpresion Then
                        Dim impresionesAntes As Integer = completa.Impresiones
                        Datos.MarcarImprimida(completa.Id)
                        Dim actualizada As InscripcionCompleta = Datos.ObtenerCompleta(completa.Id)

                        reporte.AppendLine("Registro de impresión: estado=" & actualizada.Estado &
                                           ", impresiones=" & actualizada.Impresiones.ToString() &
                                           ", impreso_en=" & actualizada.ImpresoEn)

                        If actualizada.Estado <> "IMPRESA" OrElse actualizada.Impresiones <> impresionesAntes + 1 Then
                            reporte.AppendLine("FALLA: no se registró la impresión en la base de datos.")
                            correcto = False
                        End If
                    End If
                End If
            End If

            reporte.AppendLine()
            reporte.AppendLine("--- Enlaces de impresión ---")
            Dim enlacePortal As String = Protocolo.CodigoDesdeArgumentos({"comprobantesvb://imprimir/INS-2026-000001"})
            Dim enlaceArgumento As String = Protocolo.CodigoDesdeArgumentos({"--imprimir", "ins-2026-000001"})
            reporte.AppendLine("Enlace del portal: " & If(enlacePortal, "(ninguno)"))
            reporte.AppendLine("Argumento --imprimir: " & If(enlaceArgumento, "(ninguno)"))

            If enlacePortal <> "INS-2026-000001" OrElse enlaceArgumento <> "INS-2026-000001" Then
                reporte.AppendLine("FALLA: no se pudo interpretar el código del comprobante.")
                correcto = False
            End If

            reporte.AppendLine()
            reporte.AppendLine("--- Diseño de la ventana principal ---")
            Using formulario As New MainForm()
                formulario.Show()
                Application.DoEvents()
                formulario.PerformLayout()
                reporte.AppendLine(formulario.ObtenerInformeDiseno())
                formulario.Hide()
            End Using

            reporte.AppendLine("RESULTADO: " & If(correcto, "OK", "REVISAR"))
        Catch ex As Exception
            correcto = False
            reporte.AppendLine("RESULTADO: ERROR")
            reporte.AppendLine(ex.ToString())
        End Try

        Try
            File.WriteAllText(rutaReporte, reporte.ToString(), New UTF8Encoding(False))
        Catch
            ' Si no se puede escribir el informe, no hay más que hacer.
        End Try

        Environment.ExitCode = If(correcto, 0, 1)
    End Sub

    ''' <summary>Envía el comprobante a "Microsoft Print to PDF" para validar la impresión real.</summary>
    Private Shared Function GenerarPdf(datos As InscripcionCompleta, reporte As StringBuilder) As Boolean
        Dim rutaPdf As String = Path.Combine(Path.GetTempPath(), "comprobante-selftest.pdf")

        Try
            If File.Exists(rutaPdf) Then File.Delete(rutaPdf)

            Using impresora As New ComprobantePrinter(datos)
                Dim ajustes As PrinterSettings = impresora.Documento.PrinterSettings
                ajustes.PrinterName = "Microsoft Print to PDF"

                If Not ajustes.IsValid Then
                    reporte.AppendLine("AVISO: la impresora 'Microsoft Print to PDF' no está disponible.")
                    Return True
                End If

                ajustes.PrintToFile = True
                ajustes.PrintFileName = rutaPdf
                impresora.Documento.Print()
            End Using

            If Not File.Exists(rutaPdf) Then
                reporte.AppendLine("FALLA: no se generó el PDF de prueba.")
                Return False
            End If

            Dim tamano As Long = New FileInfo(rutaPdf).Length
            reporte.AppendLine("PDF generado: " & rutaPdf & " (" & tamano.ToString() & " bytes)")

            Dim contenido As Byte() = File.ReadAllBytes(rutaPdf)
            Dim texto As String = Encoding.Latin1.GetString(contenido)
            Dim paginas As Integer = System.Text.RegularExpressions.Regex.Matches(texto, "/Type\s*/Page[^s]").Count
            reporte.AppendLine("Páginas del PDF: " & paginas.ToString())

            If tamano < 1000 OrElse paginas = 0 Then
                reporte.AppendLine("FALLA: el PDF generado está vacío.")
                Return False
            End If

            Return True
        Catch ex As Exception
            reporte.AppendLine("AVISO: no se pudo imprimir a PDF: " & ex.Message)
            Return True
        End Try
    End Function

    Private Shared Function ValidarTextos(textos As List(Of String), datos As InscripcionCompleta,
                                          reporte As StringBuilder) As Boolean
        Dim correcto As Boolean = True
        Dim todo As String = String.Join(Environment.NewLine, textos)

        Dim requeridos As String() = {
            "CONSTANCIA DE INSCRIPCIÓN",
            datos.Codigo,
            datos.NombreCompleto,
            datos.Cedula,
            datos.Carrera,
            datos.CondicionTexto,
            "Firma del estudiante",
            "Control de Estudios"
        }

        For Each requerido As String In requeridos
            If Not todo.Contains(requerido) Then
                reporte.AppendLine("FALLA: falta el texto en el comprobante: " & requerido)
                correcto = False
            End If
        Next

        For Each asignatura As AsignaturaInscrita In datos.Asignaturas
            If Not todo.Contains(asignatura.Nombre) Then
                reporte.AppendLine("FALLA: falta la asignatura: " & asignatura.Nombre)
                correcto = False
            End If
        Next

        For Each linea As String In textos
            Dim partes As String() = linea.Split("|"c)
            If partes.Length < 2 Then Continue For

            Dim coordenadas As String() = partes(0).Split(","c)
            Dim x As Single
            Dim y As Single
            If Single.TryParse(coordenadas(0), x) AndAlso Single.TryParse(coordenadas(1), y) Then
                If x < 0 OrElse x > 790 OrElse y < 0 OrElse y > 1045 Then
                    reporte.AppendLine("FALLA: texto fuera del área imprimible: " & linea)
                    correcto = False
                End If
            End If
        Next

        Return correcto
    End Function

    Private Shared Function AnalizarImagen(imagen As Bitmap, reporte As StringBuilder) As Boolean
        Dim bandas As Integer = 6
        Dim porcentajes(bandas - 1) As Double
        Dim paso As Integer = 3

        For banda As Integer = 0 To bandas - 1
            Dim desde As Integer = imagen.Height * banda \ bandas
            Dim hasta As Integer = imagen.Height * (banda + 1) \ bandas
            Dim tinta As Integer = 0
            Dim total As Integer = 0

            For y As Integer = desde To hasta - 1 Step paso
                For x As Integer = 0 To imagen.Width - 1 Step paso
                    Dim color As Color = imagen.GetPixel(x, y)
                    total += 1
                    If color.R < 245 OrElse color.G < 245 OrElse color.B < 245 Then tinta += 1
                Next
            Next

            porcentajes(banda) = If(total = 0, 0, tinta * 100.0 / total)
            reporte.AppendLine($"Tinta banda {banda + 1} ({desde}-{hasta} px): {porcentajes(banda):F1}%")
        Next

        Dim correcto As Boolean = True

        If porcentajes.Sum() < 1.0 Then
            reporte.AppendLine("FALLA: la imagen está prácticamente en blanco.")
            correcto = False
        End If

        If porcentajes(0) <= 0.3 Then
            reporte.AppendLine("FALLA: el encabezado no contiene texto.")
            correcto = False
        End If

        If porcentajes(bandas - 1) <= 0.05 Then
            reporte.AppendLine("FALLA: el pie del comprobante no contiene texto.")
            correcto = False
        End If

        Dim tintaMargenDerecho As Integer = 0
        Dim tintaMargenInferior As Integer = 0

        For y As Integer = 0 To imagen.Height - 1
            Dim color As Color = imagen.GetPixel(imagen.Width - 4, y)
            If color.R < 250 OrElse color.G < 250 OrElse color.B < 250 Then tintaMargenDerecho += 1
        Next

        For x As Integer = 0 To imagen.Width - 1
            Dim color As Color = imagen.GetPixel(x, imagen.Height - 4)
            If color.R < 250 OrElse color.G < 250 OrElse color.B < 250 Then tintaMargenInferior += 1
        Next

        reporte.AppendLine("Pixeles con tinta en margen derecho: " & tintaMargenDerecho.ToString())
        reporte.AppendLine("Pixeles con tinta en margen inferior: " & tintaMargenInferior.ToString())

        If tintaMargenDerecho > 0 OrElse tintaMargenInferior > 0 Then
            reporte.AppendLine("AVISO: hay contenido dentro del margen de la página.")
            correcto = False
        End If

        Return correcto
    End Function

End Class
