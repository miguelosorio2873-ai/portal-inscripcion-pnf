Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Globalization
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports InscripcionOPSU.Modelos

''' <summary>
''' Reportes de inscripción: listado filtrado por trayecto y tipo de ingreso,
''' con vista previa e impresión (flujo clásico de escritorio).
''' </summary>
Friend NotInheritable Class Reporte

    Private Sub New()
    End Sub

    ''' <summary>Texto del listado, usado para la autoprueba y para exportar.</summary>
    Public Shared Function ConstruirLineas(inscripciones As List(Of Inscripcion), titulo As String) As List(Of String)
        Dim lineas As New List(Of String)()
        lineas.Add("CONTROL DE ESTUDIOS · INSCRIPCIÓN UNIVERSITARIA")
        lineas.Add(titulo)
        lineas.Add("Emitido: " & DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture))
        lineas.Add("")
        lineas.Add(Pad("TRAYECTO", 12) & Pad("CEDULA", 13) & Pad("ESTUDIANTE", 30) & Pad("CARRERA", 24) & Pad("INGRESO", 11) & "ESTATUS")
        lineas.Add(New String("-"c, 100))
        For Each item As Inscripcion In inscripciones
            lineas.Add(Pad(item.TrayectoTexto, 12) & Pad(item.Cedula, 13) & Pad(item.Estudiante, 30) &
                       Pad(item.Carrera, 24) & Pad(item.ModalidadTexto, 11) & item.Estatus)
        Next
        lineas.Add("")
        lineas.Add("Total de inscripciones: " & inscripciones.Count.ToString(CultureInfo.InvariantCulture))
        Return lineas
    End Function

    Private Shared Function Pad(valor As String, ancho As Integer) As String
        Dim texto As String = If(valor, "")
        If texto.Length >= ancho Then Return texto.Substring(0, ancho - 1) & " "
        Return texto.PadRight(ancho)
    End Function

    Public Shared Sub Previsualizar(inscripciones As List(Of Inscripcion), titulo As String)
        Dim documento As PrintDocument = CrearDocumento(inscripciones, titulo)
        Using dialogo As New PrintPreviewDialog()
            dialogo.Document = documento
            dialogo.WindowState = FormWindowState.Maximized
            dialogo.Text = "Vista previa · " & titulo
            dialogo.ShowDialog()
        End Using
        documento.Dispose()
    End Sub

    Public Shared Sub Imprimir(inscripciones As List(Of Inscripcion), titulo As String)
        Dim documento As PrintDocument = CrearDocumento(inscripciones, titulo)
        Using dialogo As New PrintDialog()
            dialogo.Document = documento
            dialogo.UseEXDialog = True
            If dialogo.ShowDialog() = DialogResult.OK Then
                documento.Print()
            End If
        End Using
        documento.Dispose()
    End Sub

    Private NotInheritable Class Trabajo
        Public Property Filas As List(Of Inscripcion)
        Public Property Titulo As String = ""
        Public Property Indice As Integer
        Public Property Pagina As Integer = 1
    End Class

    Private Shared Function CrearDocumento(inscripciones As List(Of Inscripcion), titulo As String) As PrintDocument
        Dim trabajo As New Trabajo With {
            .Filas = If(inscripciones, New List(Of Inscripcion)()),
            .Titulo = titulo
        }
        Dim documento As New PrintDocument()
        documento.DocumentName = titulo
        AddHandler documento.PrintPage, Sub(sender As Object, e As PrintPageEventArgs) ImprimirPagina(trabajo, e)
        Return documento
    End Function

    Private Shared Sub ImprimirPagina(trabajo As Trabajo, e As PrintPageEventArgs)
        Dim margen As Single = 40
        Dim ancho As Single = e.MarginBounds.Width
        Dim y As Single = e.MarginBounds.Top
        Dim fuenteTitulo As New Font("Segoe UI", 13, FontStyle.Bold)
        Dim fuenteTexto As New Font("Segoe UI", 9)
        Dim fuenteEncabezado As New Font("Segoe UI", 9, FontStyle.Bold)

        If trabajo.Indice = 0 Then
            e.Graphics.DrawString("CONTROL DE ESTUDIOS", fuenteEncabezado, Brushes.DimGray, margen, y)
            y += 18
            e.Graphics.DrawString(trabajo.Titulo, fuenteTitulo, Brushes.Black, margen, y)
            y += 26
            e.Graphics.DrawString("Emitido: " & DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                                  fuenteTexto, Brushes.Black, margen, y)
            y += 22
            e.Graphics.DrawLine(Pens.Black, margen, y, margen + ancho, y)
            y += 10
        End If

        ' Encabezados de columna
        Dim columnas As Single() = {0, 0.11F, 0.24F, 0.58F, 0.79F, 0.9F}
        Dim titulos As String() = {"TRAYECTO", "CÉDULA", "ESTUDIANTE", "CARRERA", "INGRESO", "ESTATUS"}

        y = DibujarFila(e.Graphics, titulos, columnas, ancho, margen, y, fuenteEncabezado)
        e.Graphics.DrawLine(Pens.Gray, margen, y - 2, margen + ancho, y - 2)

        While trabajo.Indice < trabajo.Filas.Count
            If y > e.MarginBounds.Bottom - 60 Then
                trabajo.Pagina += 1
                e.HasMorePages = True
                e.Graphics.DrawString("Página " & trabajo.Pagina.ToString(CultureInfo.InvariantCulture),
                                      fuenteTexto, Brushes.Gray, e.MarginBounds.Right - 60, e.MarginBounds.Bottom)
                Return
            End If

            Dim item As Inscripcion = trabajo.Filas(trabajo.Indice)
            Dim valores As String() = {
                item.TrayectoTexto,
                item.Cedula,
                item.Estudiante,
                item.Carrera,
                item.ModalidadTexto,
                item.Estatus
            }
            y = DibujarFila(e.Graphics, valores, columnas, ancho, margen, y, fuenteTexto)
            trabajo.Indice += 1
        End While

        e.Graphics.DrawLine(Pens.Gray, margen, y + 4, margen + ancho, y + 4)
        e.Graphics.DrawString("Total de inscripciones: " & trabajo.Filas.Count.ToString(CultureInfo.InvariantCulture),
                              fuenteEncabezado, Brushes.Black, margen, y + 10)
        e.Graphics.DrawString("Página " & trabajo.Pagina.ToString(CultureInfo.InvariantCulture),
                              fuenteTexto, Brushes.Gray, e.MarginBounds.Right - 60, e.MarginBounds.Bottom)
        e.HasMorePages = False
    End Sub

    Private Shared Function DibujarFila(g As Graphics, valores As String(), columnas As Single(),
                                        ancho As Single, margen As Single, y As Single, fuente As Font) As Single
        For i As Integer = 0 To valores.Length - 1
            Dim x As Single = margen + (ancho * columnas(i))
            Dim limite As Single = If(i = valores.Length - 1, ancho, ancho * (columnas(Math.Min(i + 1, columnas.Length - 1)))) - 4
            Dim rect As New RectangleF(x, y, Math.Max(limite, 40), 18)
            g.DrawString(valores(i), fuente, Brushes.Black, rect)
        Next
        Return y + 17
    End Function

End Class
