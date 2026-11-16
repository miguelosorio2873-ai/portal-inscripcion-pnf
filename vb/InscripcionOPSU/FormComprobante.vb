Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Windows.Forms
Imports InscripcionOPSU.Modelos

''' <summary>
''' Vista previa del comprobante de inscripción, con impresión y registro de la
''' salida. Se abre desde el listado o cuando el portal envía el código por el
''' enlace inscripcionopsu://.
''' </summary>
Friend NotInheritable Class FormComprobante
    Inherits Form

    Private _datos As InscripcionCompleta
    Private ReadOnly _imprimirAlAbrir As Boolean

    Private ReadOnly _btnImprimir As New Button()
    Private ReadOnly _btnCerrar As New Button()
    Private ReadOnly _imagen As New PictureBox()
    Private ReadOnly _lblEstado As New Label()

    Private _vista As Bitmap

    Public Sub New(datos As InscripcionCompleta, Optional imprimirAlAbrir As Boolean = False)
        _datos = datos
        _imprimirAlAbrir = imprimirAlAbrir

        Text = "Comprobante de inscripción · " & datos.Codigo
        StartPosition = FormStartPosition.CenterParent
        MinimumSize = New Size(720, 620)
        Size = New Size(900, 1000)
        Font = New Font("Segoe UI", 9.5F)
        BackColor = Color.FromArgb(236, 240, 245)

        Construir()
        ActualizarVista()
    End Sub

    Private Sub Construir()
        Dim barra As New Panel()
        barra.Dock = DockStyle.Top
        barra.Height = 56
        barra.Padding = New Padding(12, 10, 12, 8)
        barra.BackColor = Color.White
        Controls.Add(barra)

        Dim acciones As New FlowLayoutPanel()
        acciones.Dock = DockStyle.Left
        acciones.AutoSize = True
        acciones.WrapContents = False
        barra.Controls.Add(acciones)

        ConfigurarBoton(_btnImprimir, "Imprimir comprobante", 170, Color.FromArgb(21, 101, 192), Color.White)
        ConfigurarBoton(_btnCerrar, "Cerrar", 100, Color.FromArgb(236, 239, 245), Color.Black)
        acciones.Controls.Add(_btnImprimir)
        acciones.Controls.Add(_btnCerrar)

        _lblEstado.AutoSize = False
        _lblEstado.Dock = DockStyle.Fill
        _lblEstado.TextAlign = ContentAlignment.MiddleRight
        _lblEstado.ForeColor = Color.FromArgb(71, 85, 105)
        _lblEstado.Text = "Vista previa"
        barra.Controls.Add(_lblEstado)
        _lblEstado.BringToFront()

        Dim marco As New Panel()
        marco.Dock = DockStyle.Fill
        marco.Padding = New Padding(12)
        marco.BackColor = Color.FromArgb(210, 216, 224)
        Controls.Add(marco)

        _imagen.Dock = DockStyle.Fill
        _imagen.SizeMode = PictureBoxSizeMode.Zoom
        _imagen.BackColor = Color.White
        marco.Controls.Add(_imagen)

        Dim pie As New Label()
        pie.Dock = DockStyle.Bottom
        pie.Height = 42
        pie.Padding = New Padding(12, 8, 12, 0)
        pie.TextAlign = ContentAlignment.MiddleLeft
        pie.ForeColor = Color.FromArgb(71, 85, 105)
        pie.Font = New Font("Segoe UI", 8.5F)
        pie.Text = "El comprobante se imprime en tamaño carta. Cada impresión queda registrada en la base de datos compartida con el portal."
        Controls.Add(pie)
        pie.BringToFront()

        AddHandler _btnImprimir.Click, AddressOf Imprimir_Click
        AddHandler _btnCerrar.Click, Sub(sender As Object, e As EventArgs) Close()
    End Sub

    Private Sub ConfigurarBoton(boton As Button, texto As String, ancho As Integer, fondo As Color, letra As Color)
        boton.Text = texto
        boton.Width = ancho
        boton.Height = 34
        boton.FlatStyle = FlatStyle.Flat
        boton.FlatAppearance.BorderSize = 0
        boton.BackColor = fondo
        boton.ForeColor = letra
        boton.Margin = New Padding(0, 0, 8, 0)
        boton.Cursor = Cursors.Hand
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        If _imprimirAlAbrir Then Imprimir(False)
    End Sub

    ''' <summary>Genera la imagen del comprobante para la vista previa.</summary>
    Private Sub ActualizarVista()
        Try
            Using impresora As New ComprobantePrinter(_datos)
                Dim nueva As Bitmap = impresora.Renderizar(850, 1100)

                _imagen.Image = Nothing
                If _vista IsNot Nothing Then _vista.Dispose()
                _vista = nueva
                _imagen.Image = _vista
            End Using

            Dim detalle As String = "Vista previa · " & _datos.Codigo
            If _datos.Impresiones > 0 Then
                detalle &= " · impresiones: " & _datos.Impresiones.ToString()
            End If
            _lblEstado.Text = detalle
        Catch ex As Exception
            MessageBox.Show("No se pudo generar la vista previa del comprobante." & Environment.NewLine & ex.Message,
                            "Comprobante", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Imprimir_Click(sender As Object, e As EventArgs)
        Imprimir(True)
    End Sub

    ''' <summary>
    ''' Imprime el comprobante. Con <paramref name="conDialogo"/> permite elegir
    ''' impresora; sin él usa directamente la impresora predeterminada.
    ''' </summary>
    Private Sub Imprimir(conDialogo As Boolean)
        Dim impreso As Boolean = False

        Try
            Using impresora As New ComprobantePrinter(_datos)
                Dim documento As PrintDocument = impresora.Documento

                If Not documento.PrinterSettings.IsValid Then
                    MessageBox.Show(
                        "No hay una impresora válida configurada en el sistema." & Environment.NewLine &
                        "Instale o seleccione una impresora predeterminada.",
                        "Imprimir comprobante", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                If conDialogo Then
                    Using dialogo As New PrintDialog()
                        dialogo.Document = documento
                        dialogo.UseEXDialog = True
                        dialogo.AllowSomePages = False
                        If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                    End Using
                End If

                Cursor = Cursors.WaitCursor
                documento.Print()
                impreso = True
            End Using
        Catch ex As Exception
            MessageBox.Show("No se pudo imprimir el comprobante." & Environment.NewLine & ex.Message,
                            "Imprimir comprobante", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try

        If Not impreso Then Return

        Try
            Dim total As Integer = Datos.RegistrarImpresion(_datos.Codigo)
            Dim recargado As InscripcionCompleta = Comprobante.Cargar(_datos.Codigo)
            If recargado IsNot Nothing Then _datos = recargado
            ActualizarVista()

            If total >= 0 Then
                _lblEstado.Text = "Impreso · impresiones registradas: " & total.ToString()
            End If
        Catch ex As Exception
            MessageBox.Show(
                "El comprobante se imprimió, pero no se pudo registrar la impresión en la base de datos." &
                Environment.NewLine & ex.Message,
                "Imprimir comprobante", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _imagen.Image = Nothing
            If _vista IsNot Nothing Then
                _vista.Dispose()
                _vista = Nothing
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
