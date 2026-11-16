Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' Ventana principal: busca inscripciones en la base de datos compartida,
''' muestra el detalle y permite imprimir el comprobante.
''' </summary>
Public Class MainForm
    Inherits Form

    ' Controles principales
    Private ReadOnly _menu As New MenuStrip()
    Private ReadOnly _encabezado As New Panel()
    Private ReadOnly _logoEncabezado As New PictureBox()
    Private ReadOnly _barra As New Panel()
    Private ReadOnly _filaBusqueda As New FlowLayoutPanel()
    Private ReadOnly _filaFiltros As New FlowLayoutPanel()
    Private ReadOnly _txtBuscar As New TextBox()
    Private ReadOnly _chkPendientes As New CheckBox()
    Private ReadOnly _cboCarrera As New ComboBox()
    Private ReadOnly _cboSemestre As New ComboBox()
    Private ReadOnly _cboTurno As New ComboBox()
    Private ReadOnly _cboCondicion As New ComboBox()
    Private ReadOnly _cboEstado As New ComboBox()
    Private ReadOnly _dgv As New DataGridView()
    Private ReadOnly _split As New SplitContainer()
    Private ReadOnly _panelDetalle As New Panel()
    Private ReadOnly _lblTituloDetalle As New Label()
    Private ReadOnly _lblSubtituloDetalle As New Label()
    Private ReadOnly _tablaCampos As New TableLayoutPanel()
    Private ReadOnly _listaAsignaturas As New ListBox()
    Private ReadOnly _txtObservaciones As New TextBox()
    Private ReadOnly _btnVista As New Button()
    Private ReadOnly _btnImprimir As New Button()
    Private ReadOnly _estado As New StatusStrip()
    Private ReadOnly _lblRuta As New ToolStripStatusLabel()
    Private ReadOnly _lblConteo As New ToolStripStatusLabel()

    ' Estado
    Private ReadOnly _campos As New Dictionary(Of String, Label)()
    Private ReadOnly _fuenteEstado As New Font("Segoe UI", 8.5F, FontStyle.Bold)
    Private _logo As Image
    Private _lista As List(Of InscripcionResumen) = New List(Of InscripcionResumen)()
    Private ReadOnly _anchosBase As New Dictionary(Of String, Integer)()
    Private _actual As InscripcionCompleta
    Private _cargandoFiltros As Boolean

    Private Const AnchoPanelDetalle As Integer = 352

    Public Sub New()
        Text = "Comprobantes de Inscripción · Control de Estudios"
        StartPosition = FormStartPosition.CenterScreen
        MinimumSize = New Size(1040, 640)
        Size = New Size(1280, 760)
        BackColor = Color.FromArgb(244, 247, 251)
        Font = New Font("Segoe UI", 9.0F)
        KeyPreview = True
        AutoScaleMode = AutoScaleMode.Dpi

        ConstruirInterfaz()

        AddHandler Load, AddressOf AlCargarFormulario
        AddHandler KeyDown, AddressOf AlPresionarTecla
    End Sub

    ' -------------------------------------------------------------------- Interfaz

    Private Sub ConstruirInterfaz()
        SuspendLayout()

        ConstruirMenu()
        ConstruirEncabezado()
        ConstruirBarra()
        ConstruirTabla()
        ConstruirSplit()
        ConstruirEstado()

        MainMenuStrip = _menu
        Controls.Add(_split)
        Controls.Add(_estado)
        Controls.Add(_barra)
        Controls.Add(_encabezado)
        Controls.Add(_menu)

        ResumeLayout(True)
    End Sub

    Private Sub ConstruirMenu()
        _menu.Dock = DockStyle.Top
        _menu.Font = New Font("Segoe UI", 9.0F)
        _menu.BackColor = Color.White
        _menu.Padding = New Padding(12, 2, 0, 2)

        Dim archivo As New ToolStripMenuItem("&Archivo")

        Dim abrir As New ToolStripMenuItem("Configurar conexión &MySQL…")
        AddHandler abrir.Click, AddressOf SeleccionarBaseDatos

        Dim recargar As New ToolStripMenuItem("&Recargar") With {.ShortcutKeys = Keys.F5}
        AddHandler recargar.Click, Sub(sender As Object, e As EventArgs) CargarListado()

        Dim menuImprimirDirecto As New ToolStripMenuItem("&Impresión directa") With {
            .ShortcutKeys = Keys.Control Or Keys.Shift Or Keys.P
        }
        AddHandler menuImprimirDirecto.Click, Sub(sender As Object, e As EventArgs) ImprimirDirecto()

        Dim menuRegistrarPortal As New ToolStripMenuItem("Registrar impresión desde el &portal")
        AddHandler menuRegistrarPortal.Click, AddressOf RegistrarPortal

        Dim salir As New ToolStripMenuItem("&Salir") With {.ShortcutKeys = Keys.Alt Or Keys.F4}
        AddHandler salir.Click, Sub(sender As Object, e As EventArgs) Close()

        archivo.DropDownItems.Add(abrir)
        archivo.DropDownItems.Add(New ToolStripSeparator())
        archivo.DropDownItems.Add(recargar)
        archivo.DropDownItems.Add(menuImprimirDirecto)
        archivo.DropDownItems.Add(New ToolStripSeparator())
        archivo.DropDownItems.Add(menuRegistrarPortal)
        archivo.DropDownItems.Add(New ToolStripSeparator())
        archivo.DropDownItems.Add(salir)

        Dim ayuda As New ToolStripMenuItem("A&yuda")

        Dim carpeta As New ToolStripMenuItem("Abrir &phpMyAdmin")
        AddHandler carpeta.Click, AddressOf AbrirPhpMyAdmin

        Dim acerca As New ToolStripMenuItem("&Acerca de…")
        AddHandler acerca.Click, AddressOf MostrarAcercaDe

        ayuda.DropDownItems.Add(carpeta)
        ayuda.DropDownItems.Add(New ToolStripSeparator())
        ayuda.DropDownItems.Add(acerca)

        _menu.Items.Add(archivo)
        _menu.Items.Add(ayuda)
    End Sub

    Private Sub ConstruirEncabezado()
        _encabezado.Dock = DockStyle.Top
        _encabezado.Height = 88
        _encabezado.BackColor = Color.FromArgb(11, 58, 99)

        _logo = CargarLogo()
        Dim xTexto As Integer = 24

        If _logo IsNot Nothing Then
            _logoEncabezado.Image = _logo
            _logoEncabezado.SizeMode = PictureBoxSizeMode.Zoom
            _logoEncabezado.BackColor = Color.White
            _logoEncabezado.Location = New Point(24, 9)
            _logoEncabezado.Size = New Size(164, 70)
            _logoEncabezado.TabStop = False
            _encabezado.Controls.Add(_logoEncabezado)
            xTexto = 206
        End If

        Dim titulo As New Label With {
            .Text = "Comprobantes de Inscripción",
            .Font = New Font("Segoe UI", 16.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(xTexto, 15),
            .BackColor = Color.Transparent
        }

        Dim subtitulo As New Label With {
            .Text = "Programas Nacionales de Formación · Control de Estudios",
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.FromArgb(185, 210, 230),
            .AutoSize = True,
            .Location = New Point(xTexto + 2, 51),
            .BackColor = Color.Transparent
        }

        Dim sello As New Label With {
            .Text = "IMPRESIÓN OFICIAL",
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(224, 180, 60),
            .AutoSize = True,
            .BackColor = Color.Transparent
        }

        AddHandler _encabezado.Resize, Sub()
            sello.Location = New Point(Math.Max(24, _encabezado.ClientSize.Width - sello.Width - 26), 35)
        End Sub

        _encabezado.Controls.Add(titulo)
        _encabezado.Controls.Add(subtitulo)
        _encabezado.Controls.Add(sello)
    End Sub

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

    Private Sub ConstruirBarra()
        _barra.Dock = DockStyle.Top
        _barra.Height = 120
        _barra.BackColor = Color.White

        Dim contenedor As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .BackColor = Color.White,
            .Margin = New Padding(0)
        }
        contenedor.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        contenedor.RowStyles.Add(New RowStyle(SizeType.Absolute, 58.0F))
        contenedor.RowStyles.Add(New RowStyle(SizeType.Absolute, 56.0F))

        ConfigurarFila(_filaBusqueda)
        ConfigurarFila(_filaFiltros)
        _filaFiltros.AutoScroll = True

        ' ---- Fila 1: búsqueda por texto
        Dim etiqueta As New Label With {
            .Text = "Buscar",
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(36, 52, 71),
            .Margin = New Padding(0, 11, 10, 0)
        }

        _txtBuscar.Width = 310
        _txtBuscar.Font = New Font("Segoe UI", 10.0F)
        _txtBuscar.PlaceholderText = "Cédula, código o nombre del estudiante…"
        _txtBuscar.Margin = New Padding(0, 4, 10, 0)
        AddHandler _txtBuscar.KeyDown, Sub(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then
                e.SuppressKeyPress = True
                CargarListado()
            End If
        End Sub

        Dim botonBuscar As Button = CrearBoton("Buscar", True)
        botonBuscar.Width = 96
        botonBuscar.Margin = New Padding(0, 4, 8, 0)
        AddHandler botonBuscar.Click, Sub(sender As Object, e As EventArgs) CargarListado()

        Dim botonRecargar As Button = CrearBoton("Recargar", False)
        botonRecargar.Width = 100
        botonRecargar.Margin = New Padding(0, 4, 18, 0)
        AddHandler botonRecargar.Click, Sub(sender As Object, e As EventArgs) CargarListado()

        _chkPendientes.Text = "Solo pendientes de imprimir"
        _chkPendientes.AutoSize = True
        _chkPendientes.Font = New Font("Segoe UI", 9.0F)
        _chkPendientes.ForeColor = Color.FromArgb(52, 66, 82)
        _chkPendientes.Margin = New Padding(0, 11, 0, 0)
        AddHandler _chkPendientes.CheckedChanged, Sub(sender As Object, e As EventArgs)
            If _cargandoFiltros Then Return
            CargarListado()
        End Sub

        _filaBusqueda.Controls.Add(etiqueta)
        _filaBusqueda.Controls.Add(_txtBuscar)
        _filaBusqueda.Controls.Add(botonBuscar)
        _filaBusqueda.Controls.Add(botonRecargar)
        _filaBusqueda.Controls.Add(_chkPendientes)

        ' ---- Fila 2: filtros por carrera, semestre, turno, condición y estado
        _filaFiltros.Controls.Add(CrearEtiquetaFiltro("Carrera"))
        ConfigurarCombo(_cboCarrera, 200)
        _filaFiltros.Controls.Add(_cboCarrera)

        _filaFiltros.Controls.Add(CrearEtiquetaFiltro("Semestre"))
        ConfigurarCombo(_cboSemestre, 96)
        _filaFiltros.Controls.Add(_cboSemestre)

        _filaFiltros.Controls.Add(CrearEtiquetaFiltro("Turno"))
        ConfigurarCombo(_cboTurno, 92)
        _filaFiltros.Controls.Add(_cboTurno)

        _filaFiltros.Controls.Add(CrearEtiquetaFiltro("Condición"))
        ConfigurarCombo(_cboCondicion, 150)
        _filaFiltros.Controls.Add(_cboCondicion)

        _filaFiltros.Controls.Add(CrearEtiquetaFiltro("Estado"))
        ConfigurarCombo(_cboEstado, 118)
        _filaFiltros.Controls.Add(_cboEstado)

        Dim botonLimpiar As Button = CrearBoton("Limpiar filtros", False)
        botonLimpiar.Width = 122
        botonLimpiar.Margin = New Padding(4, 4, 0, 0)
        AddHandler botonLimpiar.Click, Sub(sender As Object, e As EventArgs) LimpiarFiltros()
        _filaFiltros.Controls.Add(botonLimpiar)

        contenedor.Controls.Add(_filaBusqueda, 0, 0)
        contenedor.Controls.Add(_filaFiltros, 0, 1)
        _barra.Controls.Add(contenedor)
    End Sub

    Private Shared Sub ConfigurarFila(fila As FlowLayoutPanel)
        fila.Dock = DockStyle.Fill
        fila.FlowDirection = FlowDirection.LeftToRight
        fila.WrapContents = False
        fila.BackColor = Color.White
        fila.Padding = New Padding(22, 10, 22, 4)
        fila.Margin = New Padding(0)
    End Sub

    Private Shared Function CrearEtiquetaFiltro(texto As String) As Label
        Return New Label With {
            .Text = texto,
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = Color.FromArgb(95, 113, 131),
            .Margin = New Padding(0, 11, 6, 0)
        }
    End Function

    Private Sub ConfigurarCombo(combo As ComboBox, ancho As Integer)
        combo.DropDownStyle = ComboBoxStyle.DropDownList
        combo.Width = ancho
        combo.Font = New Font("Segoe UI", 9.5F)
        combo.Margin = New Padding(0, 6, 10, 0)
        combo.FlatStyle = FlatStyle.Flat
        AddHandler combo.SelectedIndexChanged, AddressOf AlCambiarFiltro
    End Sub

    Private Sub AlCambiarFiltro(sender As Object, e As EventArgs)
        If _cargandoFiltros Then Return
        CargarListado()
    End Sub

    Private Function CrearBoton(texto As String, primario As Boolean) As Button
        Dim boton As New Button With {
            .Text = texto,
            .Height = 34,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .Cursor = Cursors.Hand,
            .UseVisualStyleBackColor = False
        }

        If primario Then
            boton.BackColor = Color.FromArgb(13, 79, 139)
            boton.ForeColor = Color.White
            boton.FlatAppearance.BorderSize = 0
        Else
            boton.BackColor = Color.White
            boton.ForeColor = Color.FromArgb(36, 52, 71)
            boton.FlatAppearance.BorderSize = 1
            boton.FlatAppearance.BorderColor = Color.FromArgb(203, 215, 227)
        End If

        Return boton
    End Function

    Private Sub ConstruirTabla()
        With _dgv
            .Dock = DockStyle.Fill
            .BackgroundColor = Color.White
            .BorderStyle = BorderStyle.None
            .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            .GridColor = Color.FromArgb(232, 238, 245)
            .RowHeadersVisible = False
            .AllowUserToAddRows = False
            .AllowUserToDeleteRows = False
            .AllowUserToResizeRows = False
            .AllowUserToOrderColumns = False
            .ReadOnly = True
            .MultiSelect = False
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .ScrollBars = ScrollBars.Both
            .AutoGenerateColumns = False
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            .ColumnHeadersHeight = 38
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            .EnableHeadersVisualStyles = False
            .DefaultCellStyle.Font = New Font("Segoe UI", 8.8F)
            .DefaultCellStyle.Padding = New Padding(6, 0, 6, 0)
            .DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 241, 250)
            .DefaultCellStyle.SelectionForeColor = Color.FromArgb(16, 36, 58)
            .ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(238, 244, 250)
            .ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(11, 58, 99)
            .ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
            .ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 244, 250)
            .ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(11, 58, 99)
            .RowTemplate.Height = 34
        End With

        AgregarColumna("colCodigo", "Código", "Codigo", 128, DataGridViewContentAlignment.MiddleLeft)
        AgregarColumna("colCedula", "Cédula", "Cedula", 100, DataGridViewContentAlignment.MiddleLeft)
        AgregarColumna("colEstudiante", "Estudiante", "Estudiante", 116, DataGridViewContentAlignment.MiddleLeft)
        AgregarColumna("colSemestre", "Sem.", "SemestreTexto", 32, DataGridViewContentAlignment.MiddleCenter)
        AgregarColumna("colTurno", "Turno", "Turno", 72, DataGridViewContentAlignment.MiddleLeft)
        AgregarColumna("colCondicion", "Condición", "CondicionTexto", 102, DataGridViewContentAlignment.MiddleLeft)
        AgregarColumna("colFecha", "Fecha", "FechaCortaTexto", 84, DataGridViewContentAlignment.MiddleLeft)
        AgregarColumna("colEstado", "Estado", "Estado", 94, DataGridViewContentAlignment.MiddleCenter)

        AddHandler _dgv.SelectionChanged, Sub(sender As Object, e As EventArgs) ActualizarDetalle()
        AddHandler _dgv.CellFormatting, AddressOf FormatearCelda
        AddHandler _dgv.SizeChanged, Sub(sender As Object, e As EventArgs) RepartirSobrante()
        AddHandler _dgv.CellDoubleClick, Sub(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then MostrarVistaPrevia()
        End Sub
    End Sub

    Private Sub AgregarColumna(nombre As String, encabezado As String, propiedad As String,
                               peso As Integer, alineacion As DataGridViewContentAlignment)
        Dim columna As New DataGridViewTextBoxColumn With {
            .Name = nombre,
            .HeaderText = encabezado,
            .DataPropertyName = propiedad,
            .FillWeight = peso,
            .MinimumWidth = 40,
            .SortMode = DataGridViewColumnSortMode.NotSortable
        }
        columna.DefaultCellStyle.Alignment = alineacion
        _dgv.Columns.Add(columna)
    End Sub

    ''' <summary>
    ''' Ajusta cada columna al ancho que necesita su contenido y luego reparte
    ''' el espacio libre. Así ningún texto se muestra con puntos suspensivos.
    ''' </summary>
    Private Sub AjustarColumnas()
        _anchosBase.Clear()

        If _dgv.Rows.Count > 0 Then
            For Each columna As DataGridViewColumn In _dgv.Columns
                columna.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            Next
            _dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells)
        End If

        For Each columna As DataGridViewColumn In _dgv.Columns
            Dim ancho As Integer = Math.Max(columna.MinimumWidth, columna.Width)
            columna.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            columna.Width = ancho
            _anchosBase(columna.Name) = ancho
        Next

        RepartirSobrante()
    End Sub

    ''' <summary>Devuelve las columnas a su ancho base y da el sobrante a Estudiante.</summary>
    Private Sub RepartirSobrante()
        If _anchosBase.Count = 0 Then Return

        For Each columna As DataGridViewColumn In _dgv.Columns
            Dim ancho As Integer
            If _anchosBase.TryGetValue(columna.Name, ancho) AndAlso columna.Width <> ancho Then
                columna.Width = ancho
            End If
        Next

        Dim usado As Integer = 0
        For Each columna As DataGridViewColumn In _dgv.Columns
            usado += columna.Width
        Next

        Dim sobrante As Integer = _dgv.ClientSize.Width - usado - 1
        If sobrante > 0 AndAlso _dgv.Columns.Contains("colEstudiante") Then
            _dgv.Columns("colEstudiante").Width += sobrante
        End If
    End Sub

    Private Sub ConstruirSplit()
        _split.Dock = DockStyle.Fill
        _split.BackColor = Color.FromArgb(221, 229, 238)
        _split.SplitterWidth = 1
        _split.FixedPanel = FixedPanel.Panel2
        _split.Panel1.BackColor = Color.FromArgb(244, 247, 251)
        _split.Panel1.Padding = New Padding(22, 18, 12, 16)
        _split.Panel2.BackColor = Color.White
        _split.Panel2.Padding = New Padding(20, 16, 20, 14)
        _split.Panel1.Controls.Add(_dgv)

        ConstruirDetalle()
        Dim acciones As TableLayoutPanel = ConstruirAcciones()

        _split.Panel2.Controls.Add(_panelDetalle)
        _split.Panel2.Controls.Add(acciones)
    End Sub

    Private Sub ConstruirDetalle()
        _panelDetalle.Dock = DockStyle.Fill
        _panelDetalle.AutoScroll = True
        _panelDetalle.BackColor = Color.White
        _panelDetalle.Padding = New Padding(0, 2, 4, 0)

        Dim contenido As New TableLayoutPanel With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .ColumnCount = 1,
            .RowCount = 0,
            .BackColor = Color.White
        }
        contenido.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        With _lblTituloDetalle
            .Text = "Sin selección"
            .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold)
            .ForeColor = Color.FromArgb(16, 36, 58)
            .AutoEllipsis = True
            .TextAlign = ContentAlignment.MiddleLeft
        End With
        AgregarFilaDetalle(contenido, _lblTituloDetalle, 30, New Padding(0, 0, 0, 0))

        With _lblSubtituloDetalle
            .Text = "Selecciona una inscripción de la lista"
            .Font = New Font("Segoe UI", 9.0F)
            .ForeColor = Color.FromArgb(95, 113, 131)
            .AutoEllipsis = True
            .TextAlign = ContentAlignment.MiddleLeft
        End With
        AgregarFilaDetalle(contenido, _lblSubtituloDetalle, 22, New Padding(0, 0, 0, 6))

        Dim separador As New Panel With {
            .BackColor = Color.FromArgb(221, 229, 238),
            .Height = 1
        }
        AgregarFilaDetalle(contenido, separador, 1, New Padding(0, 0, 0, 10))

        ConstruirTablaCampos()
        AgregarFilaDetalle(contenido, _tablaCampos, 0, New Padding(0, 0, 0, 8))

        Dim tituloAsignaturas As New Label With {
            .Text = "Asignaturas inscritas",
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(11, 58, 99),
            .TextAlign = ContentAlignment.BottomLeft
        }
        AgregarFilaDetalle(contenido, tituloAsignaturas, 26, New Padding(0, 4, 0, 4))

        With _listaAsignaturas
            .BorderStyle = BorderStyle.FixedSingle
            .Font = New Font("Consolas", 8.5F)
            .IntegralHeight = False
            .BackColor = Color.FromArgb(250, 252, 254)
            .ForeColor = Color.FromArgb(28, 43, 58)
        End With
        AgregarFilaDetalle(contenido, _listaAsignaturas, 104, New Padding(0, 0, 0, 10))

        Dim tituloObservaciones As New Label With {
            .Text = "Observaciones",
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(11, 58, 99),
            .TextAlign = ContentAlignment.BottomLeft
        }
        AgregarFilaDetalle(contenido, tituloObservaciones, 24, New Padding(0, 0, 0, 4))

        With _txtObservaciones
            .Multiline = True
            .ReadOnly = True
            .ScrollBars = ScrollBars.Vertical
            .BorderStyle = BorderStyle.FixedSingle
            .BackColor = Color.FromArgb(250, 252, 254)
            .ForeColor = Color.FromArgb(52, 66, 82)
            .Font = New Font("Segoe UI", 8.8F)
            .TabStop = False
        End With
        AgregarFilaDetalle(contenido, _txtObservaciones, 62, New Padding(0, 0, 0, 8))

        _panelDetalle.Controls.Add(contenido)
    End Sub

    Private Sub ConstruirTablaCampos()
        With _tablaCampos
            .ColumnCount = 2
            .RowCount = 0
            .AutoSize = True
            .AutoSizeMode = AutoSizeMode.GrowAndShrink
            .Dock = DockStyle.Fill
            .BackColor = Color.White
        End With
        _tablaCampos.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 96.0F))
        _tablaCampos.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        AgregarCampo("Cedula", "Cédula")
        AgregarCampo("Estudiante", "Estudiante")
        AgregarCampo("Carrera", "Carrera")
        AgregarCampo("Semestre", "Semestre")
        AgregarCampo("Turno", "Turno")
        AgregarCampo("Condicion", "Condición")
        AgregarCampo("Fecha", "Fecha")
        AgregarCampo("Estado", "Estado")
        AgregarCampo("Impresiones", "Impresiones")
        AgregarCampo("Correo", "Correo")
    End Sub

    Private Sub AgregarCampo(clave As String, etiqueta As String)
        Dim fila As Integer = _tablaCampos.RowCount
        _tablaCampos.RowCount = fila + 1
        _tablaCampos.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim titulo As New Label With {
            .Text = etiqueta,
            .Font = New Font("Segoe UI", 8.5F),
            .ForeColor = Color.FromArgb(95, 113, 131),
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .Margin = New Padding(0, 2, 6, 2)
        }

        Dim valor As New Label With {
            .Text = "—",
            .Font = New Font("Segoe UI", If(clave = "Correo", 8.3F, 9.0F), FontStyle.Bold),
            .ForeColor = Color.FromArgb(16, 36, 58),
            .AutoSize = True,
            .MaximumSize = New Size(If(clave = "Correo", 206, 216), 0),
            .Margin = New Padding(0, 2, 0, 2)
        }

        _tablaCampos.Controls.Add(titulo, 0, fila)
        _tablaCampos.Controls.Add(valor, 1, fila)
        _campos(clave) = valor
    End Sub

    Private Sub AgregarFilaDetalle(contenido As TableLayoutPanel, control As Control, alto As Integer, margen As Padding)
        Dim fila As Integer = contenido.RowCount
        contenido.RowCount = fila + 1

        If alto > 0 Then
            contenido.RowStyles.Add(New RowStyle(SizeType.Absolute, CSng(alto)))
        Else
            contenido.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        End If

        control.Dock = DockStyle.Fill
        control.Margin = margen
        contenido.Controls.Add(control, 0, fila)
    End Sub

    Private Function ConstruirAcciones() As TableLayoutPanel
        Dim acciones As New TableLayoutPanel With {
            .Dock = DockStyle.Bottom,
            .Height = 106,
            .ColumnCount = 1,
            .RowCount = 2,
            .BackColor = Color.White,
            .Padding = New Padding(0, 8, 0, 0)
        }
        acciones.RowStyles.Add(New RowStyle(SizeType.Absolute, 46.0F))
        acciones.RowStyles.Add(New RowStyle(SizeType.Absolute, 52.0F))

        With _btnVista
            .Text = "Vista previa"
            .Dock = DockStyle.Fill
            .Height = 38
            .Margin = New Padding(0, 0, 0, 8)
            .FlatStyle = FlatStyle.Flat
            .BackColor = Color.White
            .ForeColor = Color.FromArgb(13, 79, 139)
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
            .Cursor = Cursors.Hand
            .UseVisualStyleBackColor = False
            .Enabled = False
        End With
        _btnVista.FlatAppearance.BorderColor = Color.FromArgb(13, 79, 139)
        _btnVista.FlatAppearance.BorderSize = 1
        AddHandler _btnVista.Click, Sub(sender As Object, e As EventArgs) MostrarVistaPrevia()

        With _btnImprimir
            .Text = "Imprimir comprobante"
            .Dock = DockStyle.Fill
            .Height = 40
            .Margin = New Padding(0, 0, 0, 0)
            .FlatStyle = FlatStyle.Flat
            .BackColor = Color.FromArgb(13, 79, 139)
            .ForeColor = Color.White
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
            .Cursor = Cursors.Hand
            .UseVisualStyleBackColor = False
            .Enabled = False
        End With
        _btnImprimir.FlatAppearance.BorderSize = 0
        AddHandler _btnImprimir.Click, Sub(sender As Object, e As EventArgs) ImprimirComprobante()

        acciones.Controls.Add(_btnVista, 0, 0)
        acciones.Controls.Add(_btnImprimir, 0, 1)
        Return acciones
    End Function

    Private Sub ConstruirEstado()
        _estado.Dock = DockStyle.Bottom
        _estado.BackColor = Color.White
        _estado.SizingGrip = False
        _estado.Font = New Font("Segoe UI", 8.5F)
        _estado.Padding = New Padding(16, 0, 16, 0)

        With _lblRuta
            .Text = "MySQL: " & Configuracion.DescripcionConexion
            .Spring = True
            .TextAlign = ContentAlignment.MiddleLeft
            .ForeColor = Color.FromArgb(95, 113, 131)
        End With

        With _lblConteo
            .Text = "0 inscripciones"
            .BorderSides = ToolStripStatusLabelBorderSides.Left
            .BorderStyle = Border3DStyle.Etched
            .ForeColor = Color.FromArgb(36, 52, 71)
        End With

        _estado.Items.AddRange(New ToolStripItem() {_lblRuta, _lblConteo})
        _estado.Renderer = New ToolStripProfessionalRenderer()
    End Sub

    ' ------------------------------------------------------------------- Eventos

    Private Sub AlCargarFormulario(sender As Object, e As EventArgs)
        _split.SplitterDistance = Math.Max(360, _split.Width - AnchoPanelDetalle)
        _split.Panel1MinSize = 460
        _split.Panel2MinSize = 340
        InicializarFiltros()
        CargarListado()
    End Sub

    ''' <summary>Carga los valores de los combos de filtro.</summary>
    Private Sub InicializarFiltros()
        _cargandoFiltros = True
        Try
            _cboCarrera.Items.Clear()
            _cboCarrera.Items.Add(New OpcionFiltro With {.Valor = "0", .Texto = "Todas las carreras"})
            Try
                For Each carrera As OpcionFiltro In Datos.ListarCarreras()
                    _cboCarrera.Items.Add(carrera)
                Next
            Catch ex As Exception
                ' Si la base no responde, CargarListado mostrará el error.
            End Try
            _cboCarrera.SelectedIndex = 0

            _cboSemestre.Items.Clear()
            _cboSemestre.Items.Add(New OpcionFiltro With {.Valor = "0", .Texto = "Todos"})
            For numero As Integer = 1 To 5
                _cboSemestre.Items.Add(New OpcionFiltro With {
                    .Valor = numero.ToString(),
                    .Texto = Utilidades.NumeroRomano(numero)
                })
            Next
            _cboSemestre.SelectedIndex = 0

            _cboTurno.Items.Clear()
            _cboTurno.Items.Add(New OpcionFiltro With {.Valor = "", .Texto = "Todos"})
            For Each turno As String In {"Mañana", "Tarde", "Noche"}
                _cboTurno.Items.Add(New OpcionFiltro With {.Valor = turno, .Texto = turno})
            Next
            _cboTurno.SelectedIndex = 0

            _cboCondicion.Items.Clear()
            _cboCondicion.Items.Add(New OpcionFiltro With {.Valor = "", .Texto = "Todas"})
            _cboCondicion.Items.Add(New OpcionFiltro With {.Valor = "REGULAR", .Texto = "Alumno regular"})
            _cboCondicion.Items.Add(New OpcionFiltro With {.Valor = "FLOTANTE", .Texto = "Población flotante"})
            _cboCondicion.SelectedIndex = 0

            _cboEstado.Items.Clear()
            _cboEstado.Items.Add(New OpcionFiltro With {.Valor = "", .Texto = "Todos"})
            For Each estado As String In {"REGISTRADA", "IMPRESA", "ANULADA"}
                _cboEstado.Items.Add(New OpcionFiltro With {.Valor = estado, .Texto = estado})
            Next
            _cboEstado.SelectedIndex = 0
        Finally
            _cargandoFiltros = False
        End Try
    End Sub

    Private Shared Function ValorSeleccionado(combo As ComboBox) As String
        Dim opcion As OpcionFiltro = TryCast(combo.SelectedItem, OpcionFiltro)
        Return If(opcion Is Nothing, String.Empty, opcion.Valor)
    End Function

    Private Shared Function EnteroSeleccionado(combo As ComboBox) As Integer
        Dim numero As Integer
        Integer.TryParse(ValorSeleccionado(combo), numero)
        Return numero
    End Function

    Private Sub LimpiarFiltros()
        _cargandoFiltros = True
        Try
            _txtBuscar.Clear()
            _chkPendientes.Checked = False
            _cboCarrera.SelectedIndex = 0
            _cboSemestre.SelectedIndex = 0
            _cboTurno.SelectedIndex = 0
            _cboCondicion.SelectedIndex = 0
            _cboEstado.SelectedIndex = 0
        Finally
            _cargandoFiltros = False
        End Try
        CargarListado()
    End Sub

    Private Sub AlPresionarTecla(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.F5 Then
            e.SuppressKeyPress = True
            CargarListado()
        ElseIf e.Control AndAlso e.KeyCode = Keys.P Then
            e.SuppressKeyPress = True
            ImprimirComprobante()
        End If
    End Sub

    Private Sub CargarListado(Optional mantenerId As Integer = 0)
        Try
            _lista = Datos.Buscar(_txtBuscar.Text, _chkPendientes.Checked,
                                  EnteroSeleccionado(_cboCarrera), EnteroSeleccionado(_cboSemestre),
                                  ValorSeleccionado(_cboTurno), ValorSeleccionado(_cboCondicion),
                                  ValorSeleccionado(_cboEstado))

            _dgv.DataSource = Nothing
            _dgv.DataSource = _lista
            AjustarColumnas()

            Dim filtrado As Boolean = _txtBuscar.Text.Trim().Length > 0 OrElse _chkPendientes.Checked OrElse
                                      EnteroSeleccionado(_cboCarrera) > 0 OrElse EnteroSeleccionado(_cboSemestre) > 0 OrElse
                                      ValorSeleccionado(_cboTurno).Length > 0 OrElse ValorSeleccionado(_cboCondicion).Length > 0 OrElse
                                      ValorSeleccionado(_cboEstado).Length > 0

            _lblConteo.Text = _lista.Count.ToString() & " inscripción(es)" & If(filtrado, " · filtrado", "")
            _lblRuta.Text = "MySQL: " & Configuracion.DescripcionConexion

            If mantenerId > 0 Then
                SeleccionarPorId(mantenerId)
            ElseIf _dgv.Rows.Count > 0 Then
                _dgv.ClearSelection()
                _dgv.CurrentCell = _dgv.Rows(0).Cells(0)
                _dgv.Rows(0).Selected = True
            Else
                ActualizarDetalle()
            End If
        Catch ex As Exception
            MostrarError("No se pudo leer la base de datos.", ex)
        End Try
    End Sub

    Private Sub SeleccionarPorId(id As Integer)
        For Each fila As DataGridViewRow In _dgv.Rows
            Dim item As InscripcionResumen = TryCast(fila.DataBoundItem, InscripcionResumen)
            If item IsNot Nothing AndAlso item.Id = id Then
                _dgv.CurrentCell = fila.Cells(0)
                fila.Selected = True
                Return
            End If
        Next
    End Sub

    Private Sub FormatearCelda(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        If _dgv.Columns(e.ColumnIndex).Name <> "colEstado" Then Return

        Dim estado As String = Convert.ToString(e.Value)
        Select Case estado
            Case "IMPRESA"
                e.CellStyle.ForeColor = Color.FromArgb(23, 105, 74)
            Case "REGISTRADA"
                e.CellStyle.ForeColor = Color.FromArgb(138, 90, 18)
            Case Else
                e.CellStyle.ForeColor = Color.FromArgb(95, 113, 131)
        End Select
        e.CellStyle.Font = _fuenteEstado
    End Sub

    Private Sub ActualizarDetalle()
        Dim fila As InscripcionResumen = Nothing
        If _dgv.CurrentRow IsNot Nothing Then
            fila = TryCast(_dgv.CurrentRow.DataBoundItem, InscripcionResumen)
        End If

        If fila Is Nothing Then
            _actual = Nothing
            _lblTituloDetalle.Text = "Sin selección"
            _lblSubtituloDetalle.Text = "Selecciona una inscripción de la lista"
            For Each campo As Label In _campos.Values
                campo.Text = "—"
            Next
            _listaAsignaturas.Items.Clear()
            _txtObservaciones.Text = String.Empty
            _btnVista.Enabled = False
            _btnImprimir.Enabled = False
            Return
        End If

        Try
            _actual = Datos.ObtenerCompleta(fila.Id)
            If _actual Is Nothing Then Return

            _lblTituloDetalle.Text = _actual.Codigo
            _lblSubtituloDetalle.Text = _actual.NombreCompleto & " · " & _actual.Cedula

            _campos("Cedula").Text = _actual.Cedula
            _campos("Estudiante").Text = _actual.NombreCompleto
            _campos("Carrera").Text = _actual.Carrera
            _campos("Semestre").Text = _actual.SemestreTexto
            _campos("Turno").Text = _actual.Turno
            _campos("Condicion").Text = _actual.CondicionTexto
            _campos("Fecha").Text = Utilidades.FormatearFecha(_actual.FechaInscripcion)
            _campos("Estado").Text = _actual.Estado
            _campos("Impresiones").Text = _actual.Impresiones.ToString() &
                If(_actual.Impresiones = 1, " impresión", " impresiones")
            ' El correo no tiene espacios: se parte antes de la arroba para que quepa en el panel.
            Dim correo As String = Utilidades.TextoONulo(_actual.Email)
            _campos("Correo").Text = correo.Replace("@", Environment.NewLine & "@")

            Select Case _actual.Estado
                Case "IMPRESA"
                    _campos("Estado").ForeColor = Color.FromArgb(23, 105, 74)
                Case "REGISTRADA"
                    _campos("Estado").ForeColor = Color.FromArgb(138, 90, 18)
                Case Else
                    _campos("Estado").ForeColor = Color.FromArgb(95, 113, 131)
            End Select

            _listaAsignaturas.Items.Clear()
            If _actual.Asignaturas.Count = 0 Then
                _listaAsignaturas.Items.Add("Sin asignaturas registradas")
            Else
                For Each asignatura As AsignaturaInscrita In _actual.Asignaturas
                    _listaAsignaturas.Items.Add(asignatura.Codigo.PadRight(10) & asignatura.Nombre)
                Next
            End If

            _txtObservaciones.Text = If(String.IsNullOrWhiteSpace(_actual.Observaciones),
                                         "Sin observaciones.", _actual.Observaciones)

            _btnVista.Enabled = True
            _btnImprimir.Enabled = True
        Catch ex As Exception
            MostrarError("No se pudo cargar el detalle de la inscripción.", ex)
        End Try
    End Sub

    ' ---------------------------------------------------------------- Impresión

    Private Function HaySeleccion() As Boolean
        If _actual Is Nothing Then
            MessageBox.Show(Me, "Selecciona una inscripción de la lista.", "Comprobantes",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If
        Return True
    End Function

    Private Sub MostrarVistaPrevia()
        If Not HaySeleccion() Then Return

        Try
            Using impresora As New ComprobantePrinter(_actual)
                Using dialogo As New PrintPreviewDialog()
                    dialogo.Document = impresora.Documento
                    dialogo.Width = 1024
                    dialogo.Height = 780
                    dialogo.StartPosition = FormStartPosition.CenterParent
                    dialogo.UseAntiAlias = True
                    dialogo.Text = "Vista previa · " & _actual.Codigo
                    dialogo.ShowDialog(Me)
                End Using
            End Using
        Catch ex As Exception
            MostrarError("No se pudo generar la vista previa.", ex)
        End Try
    End Sub

    Private Sub ImprimirComprobante()
        ImprimirSeleccion(True)
    End Sub

    Private Sub ImprimirDirecto()
        ImprimirSeleccion(False)
    End Sub

    Private Sub ImprimirSeleccion(preguntarImpresora As Boolean)
        If Not HaySeleccion() Then Return

        If _actual.Estado = "ANULADA" Then
            Dim respuesta As DialogResult = MessageBox.Show(
                Me,
                "La inscripción está ANULADA. ¿Deseas imprimir el comprobante de todos modos?",
                "Inscripción anulada",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning)
            If respuesta <> DialogResult.Yes Then Return
        End If

        Dim id As Integer = _actual.Id

        Try
            Using impresora As New ComprobantePrinter(_actual)
                Dim documento = impresora.Documento

                If preguntarImpresora Then
                    Using dialogo As New PrintDialog()
                        dialogo.Document = documento
                        dialogo.AllowSomePages = False
                        dialogo.UseEXDialog = True

                        If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                        documento.Print()
                    End Using
                Else
                    If Not documento.PrinterSettings.IsValid Then
                        MessageBox.Show(Me,
                            "No hay una impresora válida configurada en el sistema." & Environment.NewLine &
                            "Configura una impresora predeterminada o usa «Imprimir comprobante» para elegir otra.",
                            "Error de impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    documento.Print()
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show(Me,
                "No se pudo enviar el comprobante a la impresora." & Environment.NewLine &
                Environment.NewLine & ex.Message,
                "Error de impresión", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        Try
            Datos.MarcarImprimida(id)
            CargarListado(id)
            MessageBox.Show(Me,
                "Comprobante enviado a la impresora." & Environment.NewLine &
                "La impresión quedó registrada en la base de datos.",
                "Impresión registrada", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MostrarError("El comprobante se imprimió, pero no se pudo registrar la impresión.", ex)
        End Try
    End Sub

    ' ------------------------------------------------------------------- Archivo

    Private Sub SeleccionarBaseDatos(sender As Object, e As EventArgs)
        Using dialogo As New ConexionForm()
            If dialogo.ShowDialog(Me) = DialogResult.OK Then
                CargarListado()
            End If
        End Using
    End Sub

    Private Sub RegistrarPortal(sender As Object, e As EventArgs)
        If Protocolo.Registrar() Then
            MessageBox.Show(Me,
                "La impresión desde el portal quedó habilitada para este usuario de Windows." & Environment.NewLine &
                "En el comprobante web usa «Imprimir con Visual Basic».",
                "Impresión desde el portal", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            MessageBox.Show(Me,
                "No se pudo registrar el enlace comprobantesvb://." & Environment.NewLine &
                "Puedes seguir imprimiendo desde esta ventana.",
                "Impresión desde el portal", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    Private Sub AbrirPhpMyAdmin(sender As Object, e As EventArgs)
        Try
            Process.Start(New ProcessStartInfo("http://localhost/phpmyadmin") With {.UseShellExecute = True})
        Catch ex As Exception
            MostrarError("No se pudo abrir phpMyAdmin. Inicia Apache desde el panel de XAMPP.", ex)
        End Try
    End Sub

    Private Sub MostrarAcercaDe(sender As Object, e As EventArgs)
        MessageBox.Show(
            Me,
            "Comprobantes de Inscripción · Control de Estudios" & Environment.NewLine & Environment.NewLine &
            "Aplicación de escritorio en Visual Basic (.NET) que se conecta a la misma" & Environment.NewLine &
            "base de datos MySQL/MariaDB del portal web de inscripción." & Environment.NewLine & Environment.NewLine &
            "Funciones: búsqueda por cédula, código o nombre; vista previa; impresión" & Environment.NewLine &
            "en tamaño carta y registro de cada impresión." & Environment.NewLine & Environment.NewLine &
            "Conexión: " & Configuracion.DescripcionConexion,
            "Acerca de", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub MostrarError(mensaje As String, ex As Exception)
        MessageBox.Show(Me,
            mensaje & Environment.NewLine & Environment.NewLine & ex.Message,
            "Comprobantes de Inscripción", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

    ''' <summary>Informe de ubicación de los controles, usado por la autoprueba.</summary>
    Public Function ObtenerInformeDiseno() As String
        Dim informe As New StringBuilder()
        informe.AppendLine("Cliente: " & ClientSize.Width.ToString() & "x" & ClientSize.Height.ToString())
        informe.AppendLine("Menú: " & _menu.Bounds.ToString())
        informe.AppendLine("Encabezado: " & _encabezado.Bounds.ToString())
        If _logo IsNot Nothing Then
            informe.AppendLine("Logo encabezado: " & _logoEncabezado.Bounds.ToString())
        Else
            informe.AppendLine("Logo encabezado: no encontrado")
        End If
        informe.AppendLine("Barra de búsqueda: " & _barra.Bounds.ToString())
        informe.AppendLine("Filtros: carrera=" & ValorSeleccionado(_cboCarrera) &
                           " semestre=" & ValorSeleccionado(_cboSemestre) &
                           " turno=" & ValorSeleccionado(_cboTurno) &
                           " condición=" & ValorSeleccionado(_cboCondicion) &
                           " estado=" & ValorSeleccionado(_cboEstado) &
                           " · controles=" & _filaFiltros.Controls.Count.ToString())
        informe.AppendLine("Split: " & _split.Bounds.ToString() & " · distancia " & _split.SplitterDistance.ToString())
        informe.AppendLine("Grid: " & _dgv.Bounds.ToString())
        Dim anchos As New StringBuilder()
        For Each columna As DataGridViewColumn In _dgv.Columns
            anchos.Append(columna.Name).Append("=").Append(columna.Width.ToString()).Append(" ")
        Next
        informe.AppendLine("Anchos de columna: " & anchos.ToString().Trim())
        informe.AppendLine("Panel detalle: " & _panelDetalle.Bounds.ToString())
        informe.AppendLine("Lista asignaturas: " & _listaAsignaturas.Bounds.ToString())
        informe.AppendLine("Botón vista previa: " & _btnVista.Bounds.ToString())
        informe.AppendLine("Botón imprimir: " & _btnImprimir.Bounds.ToString())
        informe.AppendLine("Barra de estado: " & _estado.Bounds.ToString())
        informe.AppendLine("Filas de detalle: " & _tablaCampos.RowCount.ToString())
        Return informe.ToString()
    End Function

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _fuenteEstado.Dispose()
            If _logo IsNot Nothing Then _logo.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
