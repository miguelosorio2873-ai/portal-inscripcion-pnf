Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.IO

''' <summary>
''' Dibuja e imprime el comprobante de inscripción en tamaño carta.
''' Todas las coordenadas están en centésimas de pulgada, la unidad que usan
''' los objetos Graphics de impresión en .NET.
''' </summary>
Public Class ComprobantePrinter
    Implements IDisposable

    Private ReadOnly _datos As InscripcionCompleta
    Private ReadOnly _documento As PrintDocument

    ' Fuentes
    Private ReadOnly _fLogo As New Font("Segoe UI", 10.0F, FontStyle.Bold)
    Private ReadOnly _fInstitucion As New Font("Segoe UI", 11.5F, FontStyle.Bold)
    Private ReadOnly _fSubtitulo As New Font("Segoe UI", 8.5F)
    Private ReadOnly _fTipo As New Font("Segoe UI", 7.5F, FontStyle.Bold)
    Private ReadOnly _fCodigoGrande As New Font("Consolas", 10.5F, FontStyle.Bold)
    Private ReadOnly _fTitulo As New Font("Segoe UI", 12.5F, FontStyle.Bold)
    Private ReadOnly _fCuerpo As New Font("Segoe UI", 9.2F)
    Private ReadOnly _fCuerpoFuerte As New Font("Segoe UI", 9.2F, FontStyle.Bold)
    Private ReadOnly _fEtiqueta As New Font("Segoe UI", 8.2F)
    Private ReadOnly _fSeccion As New Font("Segoe UI", 8.0F, FontStyle.Bold)
    Private ReadOnly _fCodigo As New Font("Consolas", 8.2F)
    Private ReadOnly _fMini As New Font("Segoe UI", 7.2F)
    Private ReadOnly _fFirma As New Font("Segoe UI", 8.2F, FontStyle.Bold)

    ' Pinceles
    Private ReadOnly _azul As New SolidBrush(Color.FromArgb(11, 58, 99))
    Private ReadOnly _azulClaro As New SolidBrush(Color.FromArgb(20, 102, 171))
    Private ReadOnly _tinta As New SolidBrush(Color.FromArgb(28, 43, 58))
    Private ReadOnly _gris As New SolidBrush(Color.FromArgb(95, 113, 131))
    Private ReadOnly _oro As New SolidBrush(Color.FromArgb(224, 180, 60))
    Private ReadOnly _blanco As New SolidBrush(Color.White)
    Private ReadOnly _fondoTabla As New SolidBrush(Color.FromArgb(238, 244, 250))
    Private ReadOnly _fondoObservaciones As New SolidBrush(Color.FromArgb(247, 249, 252))

    ' Plumas
    Private ReadOnly _linea As New Pen(Color.FromArgb(206, 218, 230), 0.8F)
    Private ReadOnly _lineaFuerte As New Pen(Color.FromArgb(11, 58, 99), 2.4F)
    Private ReadOnly _lineaFirma As New Pen(Color.FromArgb(80, 96, 112), 0.9F)

    ' Formatos de texto
    Private ReadOnly _formatoIzq As New StringFormat() With {
        .Trimming = StringTrimming.EllipsisCharacter,
        .FormatFlags = StringFormatFlags.NoWrap
    }
    Private ReadOnly _formatoDer As New StringFormat() With {
        .Trimming = StringTrimming.EllipsisCharacter,
        .FormatFlags = StringFormatFlags.NoWrap,
        .Alignment = StringAlignment.Far
    }
    Private ReadOnly _formatoCentro As New StringFormat() With {
        .Trimming = StringTrimming.EllipsisCharacter,
        .FormatFlags = StringFormatFlags.NoWrap,
        .Alignment = StringAlignment.Center
    }
    Private ReadOnly _formatoWrap As New StringFormat() With {
        .Trimming = StringTrimming.Word,
        .FormatFlags = StringFormatFlags.LineLimit
    }

    Private _pagina As Integer
    Private _filaAsignatura As Integer
    Private ReadOnly _logo As Image = CargarLogo()

    ''' <summary>Indica si se encontró el logo institucional junto al ejecutable.</summary>
    Public ReadOnly Property LogoDisponible As Boolean
        Get
            Return _logo IsNot Nothing
        End Get
    End Property

    Private Shared Function CargarLogo() As Image
        Dim ruta As String = Path.Combine(AppContext.BaseDirectory, "Recursos", "logo-uptll.jpg")
        If Not File.Exists(ruta) Then Return Nothing

        Try
            Using original As Image = Image.FromFile(ruta)
                Return New Bitmap(original)
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>Cuando es True, cada texto dibujado se registra para la autoprueba.</summary>
    Public Property RegistrarTextos As Boolean = False

    Public ReadOnly Property TextosDibujados As New List(Of String)()

    Public Sub New(datos As InscripcionCompleta)
        _datos = datos
        _documento = New PrintDocument()
        _documento.DocumentName = "Comprobante " & datos.Codigo
        _documento.DefaultPageSettings.Landscape = False
        _documento.DefaultPageSettings.PaperSize = New PaperSize("Carta", 850, 1100)
        _documento.DefaultPageSettings.Margins = New Margins(60, 60, 55, 55)

        AddHandler _documento.BeginPrint, AddressOf AlIniciarImpresion
        AddHandler _documento.PrintPage, AddressOf AlImprimirPagina
    End Sub

    Public ReadOnly Property Documento As PrintDocument
        Get
            Return _documento
        End Get
    End Property

    ''' <summary>Renderiza el comprobante a una imagen (útil para pruebas y vista previa).</summary>
    Public Function Renderizar(ancho As Integer, alto As Integer) As Bitmap
        Dim imagen As New Bitmap(ancho, alto)
        imagen.SetResolution(100.0F, 100.0F)

        Using g As Graphics = Graphics.FromImage(imagen)
            g.Clear(Color.White)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.InterpolationMode = InterpolationMode.HighQualityBicubic
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit

            _pagina = 0
            _filaAsignatura = 0

            Dim margenes As Margins = _documento.DefaultPageSettings.Margins
            Dim area As New Rectangle(
                margenes.Left,
                margenes.Top,
                ancho - margenes.Left - margenes.Right,
                alto - margenes.Top - margenes.Bottom)

            Dim quedan As Boolean
            DibujarDocumento(g, area, False, quedan)
        End Using

        Return imagen
    End Function

    Private Sub AlIniciarImpresion(sender As Object, e As PrintEventArgs)
        _pagina = 0
        _filaAsignatura = 0
    End Sub

    Private Sub AlImprimirPagina(sender As Object, e As PrintPageEventArgs)
        If e.Graphics Is Nothing Then Return

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic
        e.Graphics.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit

        Dim quedan As Boolean
        DibujarDocumento(e.Graphics, e.MarginBounds, True, quedan)

        e.HasMorePages = quedan
        If quedan Then _pagina += 1
    End Sub

    Private Sub DibujarDocumento(g As Graphics, area As Rectangle, paginar As Boolean, ByRef quedan As Boolean)
        quedan = False
        Dim y As Single

        If _pagina = 0 Then
            y = DibujarEncabezado(g, area)
            y = DibujarIntro(g, area, y)
            y = DibujarDatos(g, area, y)
        Else
            y = DibujarEncabezadoContinuacion(g, area)
        End If

        y = DibujarTabla(g, area, y, paginar, quedan)
        If quedan Then Return

        y = DibujarObservaciones(g, area, y)
        DibujarFirmasYPie(g, area)
    End Sub

    ' ------------------------------------------------------------------ Encabezado

    Private Function DibujarEncabezado(g As Graphics, area As Rectangle) As Single
        Dim y As Single = area.Top
        Dim altoLogo As Single = 54.0F

        If _logo IsNot Nothing Then
            Dim anchoLogo As Single = 270.0F
            altoLogo = anchoLogo * _logo.Height / _logo.Width
            g.DrawImage(_logo, New RectangleF(area.Left, y, anchoLogo, altoLogo))

            Dim xDerecha As Single = area.Right - 330
            Dim yTexto As Single = y + Math.Max(0.0F, (altoLogo - 70.0F) / 2.0F)

            DibujarTexto(g, "COMPROBANTE DE INSCRIPCIÓN", _fTipo, _gris,
                         New RectangleF(xDerecha, yTexto + 2, 330, 12), _formatoDer)
            DibujarTexto(g, _datos.Codigo, _fCodigoGrande, _azul,
                         New RectangleF(xDerecha, yTexto + 16, 330, 18), _formatoDer)
            DibujarTexto(g, "Programas Nacionales de Formación", _fCuerpoFuerte, _azul,
                         New RectangleF(xDerecha, yTexto + 40, 330, 14), _formatoDer)
            DibujarTexto(g, "Coordinación de Control de Estudios", _fSubtitulo, _gris,
                         New RectangleF(xDerecha, yTexto + 56, 330, 14), _formatoDer)
        Else
            g.FillRectangle(_azul, area.Left, y, 54, 54)
            Using lapizOro As New Pen(Color.FromArgb(224, 180, 60), 1.6F)
                g.DrawRectangle(lapizOro, area.Left + 3, y + 3, 48, 48)
            End Using
            DibujarTexto(g, "PNF", _fLogo, _blanco, New RectangleF(area.Left, y + 17, 54, 20), _formatoCentro)

            DibujarTexto(g, "PROGRAMAS NACIONALES DE FORMACIÓN", _fInstitucion, _azul,
                         New RectangleF(area.Left + 66, y + 2, 430, 20), _formatoIzq)
            DibujarTexto(g, "Coordinación de Control de Estudios", _fSubtitulo, _gris,
                         New RectangleF(area.Left + 66, y + 26, 430, 16), _formatoIzq)

            DibujarTexto(g, "COMPROBANTE DE INSCRIPCIÓN", _fTipo, _gris,
                         New RectangleF(area.Right - 300, y + 4, 300, 12), _formatoDer)
            DibujarTexto(g, _datos.Codigo, _fCodigoGrande, _azul,
                         New RectangleF(area.Right - 300, y + 18, 300, 18), _formatoDer)
        End If

        Dim yLinea As Single = y + altoLogo + 10
        g.DrawLine(_lineaFuerte, area.Left, yLinea, area.Right, yLinea)
        Using lapizOro As New Pen(Color.FromArgb(224, 180, 60), 4.0F)
            g.DrawLine(lapizOro, area.Left, yLinea, area.Left + 90, yLinea)
        End Using

        Return y + altoLogo + 24
    End Function

    Private Function DibujarEncabezadoContinuacion(g As Graphics, area As Rectangle) As Single
        Dim y As Single = area.Top
        g.FillRectangle(_azul, area.Left, y, area.Width, 34)
        DibujarTexto(g, "Comprobante " & _datos.Codigo & " · " & _datos.NombreCompleto,
                     _fCuerpoFuerte, _blanco, New RectangleF(area.Left + 12, y + 9, area.Width - 140, 16), _formatoIzq)
        DibujarTexto(g, "Página " & (_pagina + 1).ToString(),
                     _fMini, _blanco, New RectangleF(area.Right - 120, y + 11, 108, 12), _formatoDer)
        Return y + 50
    End Function

    ' --------------------------------------------------------------------- Cuerpo

    Private Function DibujarIntro(g As Graphics, area As Rectangle, y As Single) As Single
        DibujarTexto(g, "CONSTANCIA DE INSCRIPCIÓN", _fTitulo, _azul,
                     New RectangleF(area.Left, y, area.Width, 24), _formatoCentro)
        y += 30

        Dim texto As String =
            "Se certifica que el (la) ciudadano(a) " & _datos.NombreCompleto &
            ", titular de la cédula de identidad " & _datos.Cedula &
            ", quedó inscrito(a) en el " & _datos.Carrera &
            " para el semestre " & _datos.SemestreTexto &
            ", en el turno " & _datos.Turno & "."

        Dim alto As Single = g.MeasureString(texto, _fCuerpo, New SizeF(area.Width, 300), _formatoWrap).Height
        DibujarTexto(g, texto, _fCuerpo, _tinta, New RectangleF(area.Left, y, area.Width, alto + 4), _formatoWrap)

        Return y + alto + 12
    End Function

    Private Function DibujarDatos(g As Graphics, area As Rectangle, y As Single) As Single
        Dim anchoCol As Single = (area.Width - 24) / 2.0F
        Dim xIzq As Single = area.Left
        Dim xDer As Single = area.Left + anchoCol + 24
        Dim yInicio As Single = y

        DibujarSeccion(g, "DATOS DEL ESTUDIANTE", xIzq, y, anchoCol)
        DibujarSeccion(g, "DATOS DE LA INSCRIPCIÓN", xDer, y, anchoCol)
        y += 22

        Dim filasIzquierda As String(,) = {
            {"Cédula", _datos.Cedula},
            {"Estudiante", _datos.NombreCompleto},
            {"Correo", _datos.Email},
            {"Teléfono", Utilidades.TextoONulo(_datos.Telefono)},
            {"Nacimiento", Utilidades.FormatearFecha(_datos.FechaNacimiento, False)}
        }

        For i As Integer = 0 To filasIzquierda.GetLength(0) - 1
            DibujarFilaDato(g, xIzq, y, anchoCol, filasIzquierda(i, 0), filasIzquierda(i, 1))
            y += 17
        Next

        Dim yDerecha As Single = yInicio + 22
        Dim filasDerecha As String(,) = {
            {"Código", _datos.Codigo},
            {"Carrera", _datos.Carrera},
            {"Semestre", _datos.SemestreTexto},
            {"Turno", _datos.Turno},
            {"Fecha", Utilidades.FormatearFecha(_datos.FechaInscripcion)},
            {"Estado", _datos.Estado}
        }

        For i As Integer = 0 To filasDerecha.GetLength(0) - 1
            DibujarFilaDato(g, xDer, yDerecha, anchoCol, filasDerecha(i, 0), filasDerecha(i, 1))
            yDerecha += 17
        Next

        y = Math.Max(y, yDerecha) + 4

        DibujarTexto(g, "Dirección", _fEtiqueta, _gris,
                     New RectangleF(area.Left, y + 2, 90, 14), _formatoIzq)
        DibujarTexto(g, Utilidades.TextoONulo(_datos.Direccion), _fEtiqueta, _tinta,
                     New RectangleF(area.Left + 92, y + 2, area.Width - 92, 28), _formatoWrap)

        Return y + 34
    End Function

    Private Sub DibujarSeccion(g As Graphics, titulo As String, x As Single, y As Single, ancho As Single)
        DibujarTexto(g, titulo, _fSeccion, _azulClaro, New RectangleF(x, y, ancho, 14), _formatoIzq)
        g.DrawLine(_linea, x, y + 16, x + ancho, y + 16)
    End Sub

    Private Sub DibujarFilaDato(g As Graphics, x As Single, y As Single, ancho As Single,
                                etiqueta As String, valor As String)
        DibujarTexto(g, etiqueta, _fEtiqueta, _gris, New RectangleF(x, y + 2, 92, 14), _formatoIzq)
        DibujarTexto(g, valor, _fEtiqueta, _tinta, New RectangleF(x + 92, y + 2, ancho - 92, 14), _formatoIzq)
    End Sub

    ' --------------------------------------------------------------- Asignaturas

    Private Function DibujarTabla(g As Graphics, area As Rectangle, yEntrada As Single,
                                  paginar As Boolean, ByRef quedan As Boolean) As Single
        Dim y As Single = yEntrada
        quedan = False

        DibujarSeccion(g, "ASIGNATURAS INSCRITAS", area.Left, y, area.Width)
        y += 22

        Const altoFila As Single = 22.0F
        Dim anchoCodigo As Single = 92.0F
        Dim anchoSemestre As Single = 72.0F
        Dim xNombre As Single = area.Left + anchoCodigo
        Dim anchoNombre As Single = area.Width - anchoCodigo - anchoSemestre
        Dim xSemestre As Single = area.Right - anchoSemestre

        g.FillRectangle(_fondoTabla, area.Left, y, area.Width, altoFila)
        g.DrawRectangle(_linea, area.Left, y, area.Width, altoFila)
        g.DrawLine(_linea, xNombre, y, xNombre, y + altoFila)
        g.DrawLine(_linea, xSemestre, y, xSemestre, y + altoFila)

        DibujarTexto(g, "CÓDIGO", _fSeccion, _azul,
                     New RectangleF(area.Left + 8, y + 6, anchoCodigo - 14, 12), _formatoIzq)
        DibujarTexto(g, "ASIGNATURA", _fSeccion, _azul,
                     New RectangleF(xNombre + 8, y + 6, anchoNombre - 14, 12), _formatoIzq)
        DibujarTexto(g, "SEM.", _fSeccion, _azul,
                     New RectangleF(xSemestre, y + 6, anchoSemestre, 12), _formatoCentro)
        y += altoFila

        If _datos.Asignaturas.Count = 0 Then
            g.DrawRectangle(_linea, area.Left, y, area.Width, altoFila)
            DibujarTexto(g, "Sin asignaturas registradas.", _fCuerpo, _gris,
                         New RectangleF(area.Left + 8, y + 5, area.Width - 16, 14), _formatoIzq)
            Return y + altoFila
        End If

        For i As Integer = _filaAsignatura To _datos.Asignaturas.Count - 1
            If paginar AndAlso y + altoFila > area.Bottom - 280 Then
                quedan = True
                Exit For
            End If

            Dim asignatura As AsignaturaInscrita = _datos.Asignaturas(i)

            g.DrawRectangle(_linea, area.Left, y, area.Width, altoFila)
            g.DrawLine(_linea, xNombre, y, xNombre, y + altoFila)
            g.DrawLine(_linea, xSemestre, y, xSemestre, y + altoFila)

            DibujarTexto(g, asignatura.Codigo, _fCodigo, _tinta,
                         New RectangleF(area.Left + 8, y + 6, anchoCodigo - 14, 13), _formatoIzq)
            DibujarTexto(g, asignatura.Nombre, _fCuerpo, _tinta,
                         New RectangleF(xNombre + 8, y + 5, anchoNombre - 14, 14), _formatoIzq)
            DibujarTexto(g, Utilidades.NumeroRomano(asignatura.Semestre), _fCuerpo, _tinta,
                         New RectangleF(xSemestre, y + 5, anchoSemestre, 14), _formatoCentro)

            _filaAsignatura = i + 1
            y += altoFila
        Next

        Return y
    End Function

    Private Function DibujarObservaciones(g As Graphics, area As Rectangle, y As Single) As Single
        Dim texto As String = If(_datos.Observaciones, String.Empty).Trim()
        If texto.Length = 0 Then Return y

        DibujarSeccion(g, "OBSERVACIONES", area.Left, y, area.Width)
        y += 22

        Dim alto As Single = g.MeasureString(texto, _fCuerpo, New SizeF(area.Width - 24, 200), _formatoWrap).Height + 12
        g.FillRectangle(_fondoObservaciones, area.Left, y, area.Width, alto)
        g.FillRectangle(_oro, area.Left, y, 3, alto)
        DibujarTexto(g, texto, _fCuerpo, _tinta,
                     New RectangleF(area.Left + 12, y + 6, area.Width - 24, alto - 10), _formatoWrap)

        Return y + alto + 8
    End Function

    Private Sub DibujarFirmasYPie(g As Graphics, area As Rectangle)
        Dim yFirma As Single = area.Bottom - 150
        Dim anchoFirma As Single = 220.0F
        Dim x1 As Single = area.Left + 40
        Dim x2 As Single = area.Right - anchoFirma - 40

        g.DrawLine(_lineaFirma, x1, yFirma, x1 + anchoFirma, yFirma)
        DibujarTexto(g, "Firma del estudiante", _fFirma, _tinta,
                     New RectangleF(x1, yFirma + 5, anchoFirma, 14), _formatoCentro)
        DibujarTexto(g, _datos.NombreCompleto & " · " & _datos.Cedula, _fMini, _gris,
                     New RectangleF(x1, yFirma + 19, anchoFirma, 12), _formatoCentro)

        g.DrawLine(_lineaFirma, x2, yFirma, x2 + anchoFirma, yFirma)
        DibujarTexto(g, "Control de Estudios", _fFirma, _tinta,
                     New RectangleF(x2, yFirma + 5, anchoFirma, 14), _formatoCentro)
        DibujarTexto(g, "Sello y firma", _fMini, _gris,
                     New RectangleF(x2, yFirma + 19, anchoFirma, 12), _formatoCentro)

        g.DrawLine(_linea, area.Left, area.Bottom - 70, area.Right, area.Bottom - 70)
        DibujarTexto(g, "Documento generado por el Portal de Inscripción PNF. Válido con sello húmedo de Control de Estudios.",
                     _fMini, _gris, New RectangleF(area.Left, area.Bottom - 62, area.Width, 12), _formatoIzq)

        Dim detalleImpresion As String = String.Empty
        If _datos.Impresiones > 0 Then
            detalleImpresion = " · Impreso el " & Utilidades.FormatearFecha(_datos.ImpresoEn) &
                               " (" & _datos.Impresiones.ToString() & ")"
        End If

        DibujarTexto(g, "Emitido el " & _datos.FechaLargaTexto & ". Código de verificación: " &
                        _datos.Codigo & "." & detalleImpresion,
                     _fMini, _gris, New RectangleF(area.Left, area.Bottom - 50, area.Width, 12), _formatoIzq)
    End Sub

    Private Sub DibujarTexto(g As Graphics, texto As String, fuente As Font, pincel As Brush,
                             rect As RectangleF, formato As StringFormat)
        If RegistrarTextos Then
            TextosDibujados.Add($"{rect.X:0},{rect.Y:0} | {texto}")
        End If
        g.DrawString(texto, fuente, pincel, rect, formato)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        _documento.Dispose()

        _fLogo.Dispose()
        _fInstitucion.Dispose()
        _fSubtitulo.Dispose()
        _fTipo.Dispose()
        _fCodigoGrande.Dispose()
        _fTitulo.Dispose()
        _fCuerpo.Dispose()
        _fCuerpoFuerte.Dispose()
        _fEtiqueta.Dispose()
        _fSeccion.Dispose()
        _fCodigo.Dispose()
        _fMini.Dispose()
        _fFirma.Dispose()

        _azul.Dispose()
        _azulClaro.Dispose()
        _tinta.Dispose()
        _gris.Dispose()
        _oro.Dispose()
        _blanco.Dispose()
        _fondoTabla.Dispose()
        _fondoObservaciones.Dispose()

        _linea.Dispose()
        _lineaFuerte.Dispose()
        _lineaFirma.Dispose()

        _formatoIzq.Dispose()
        _formatoDer.Dispose()
        _formatoCentro.Dispose()
        _formatoWrap.Dispose()

        If _logo IsNot Nothing Then _logo.Dispose()
    End Sub

End Class
