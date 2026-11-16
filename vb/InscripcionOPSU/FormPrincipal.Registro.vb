Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports InscripcionOPSU.Modelos

''' <summary>Pestaña de registro del estudiante e inscripción por trayecto.</summary>
Friend Partial Class FormPrincipal

    Private ReadOnly _txtCedula As New TextBox()
    Private ReadOnly _txtNombres As New TextBox()
    Private ReadOnly _txtApellidos As New TextBox()
    Private ReadOnly _cboSexo As New ComboBox()
    Private ReadOnly _dtpNacimiento As New DateTimePicker()
    Private ReadOnly _txtEmail As New TextBox()
    Private ReadOnly _txtTelefono As New TextBox()
    Private ReadOnly _cboCarrera As New ComboBox()
    Private ReadOnly _cboTrayecto As New ComboBox()
    Private ReadOnly _cboModalidad As New ComboBox()
    Private ReadOnly _txtObservaciones As New TextBox()
    Private ReadOnly _btnGuardar As New Button()
    Private ReadOnly _btnLimpiar As New Button()
    Private ReadOnly _btnBuscar As New Button()
    Private ReadOnly _btnComprobante As New Button()

    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _cboFiltroTrayecto As New ComboBox()
    Private ReadOnly _cboFiltroModalidad As New ComboBox()
    Private ReadOnly _cboFiltroCarrera As New ComboBox()
    Private ReadOnly _lblTotal As New Label()

    ''' <summary>
    ''' Ancho deseado para el formulario. El SplitContainer aún no tiene su tamaño
    ''' definitivo cuando se construye la pestaña, así que se aplica en OnShown.
    ''' </summary>
    Private Const AnchoFormulario As Integer = 470

    Private _division As SplitContainer

    Private Sub ConstruirRegistro(pagina As TabPage)
        Dim division As New SplitContainer()
        division.Dock = DockStyle.Fill
        division.FixedPanel = FixedPanel.Panel1
        _division = division
        pagina.Controls.Add(division)

        ' ---------------- formulario ----------------
        Dim desplazamiento As New Panel()
        desplazamiento.Dock = DockStyle.Fill
        desplazamiento.AutoScroll = True
        desplazamiento.Padding = New Padding(10)
        division.Panel1.Controls.Add(desplazamiento)

        Dim tabla As New TableLayoutPanel()
        tabla.Dock = DockStyle.Top
        tabla.AutoSize = True
        tabla.ColumnCount = 2
        tabla.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
        tabla.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        desplazamiento.Controls.Add(tabla)

        _txtCedula.Dock = DockStyle.Fill
        _txtCedula.MaxLength = 20
        _txtNombres.Dock = DockStyle.Fill
        _txtApellidos.Dock = DockStyle.Fill
        _txtEmail.Dock = DockStyle.Fill
        _txtTelefono.Dock = DockStyle.Fill
        _txtObservaciones.Dock = DockStyle.Fill
        _txtObservaciones.Multiline = True
        _txtObservaciones.Height = 60
        _txtObservaciones.MaxLength = 250

        _cboSexo.DropDownStyle = ComboBoxStyle.DropDownList
        _cboSexo.Items.Add(New Opcion("", "(sin especificar)"))
        _cboSexo.Items.Add(New Opcion("F", "Femenino"))
        _cboSexo.Items.Add(New Opcion("M", "Masculino"))
        _cboSexo.SelectedIndex = 0
        _cboSexo.Dock = DockStyle.Fill

        _dtpNacimiento.Format = DateTimePickerFormat.Short
        _dtpNacimiento.ShowCheckBox = True
        _dtpNacimiento.Checked = False
        _dtpNacimiento.Dock = DockStyle.Fill

        For Each combo As ComboBox In New ComboBox() {_cboCarrera, _cboTrayecto, _cboModalidad}
            combo.DropDownStyle = ComboBoxStyle.DropDownList
            combo.Dock = DockStyle.Fill
        Next

        _cboModalidad.Items.Add(New Opcion("OPSU", "OPSU (asignado por la OPSU)"))
        _cboModalidad.Items.Add(New Opcion("FLOTANTE", "Flotante"))
        _cboModalidad.SelectedIndex = 1

        Dim fila As Integer = 0
        fila = AgregarTitulo(tabla, "Datos del estudiante", fila)
        fila = AgregarCampo(tabla, "Cédula *", _txtCedula, fila)
        fila = AgregarCampo(tabla, "Nombres *", _txtNombres, fila)
        fila = AgregarCampo(tabla, "Apellidos *", _txtApellidos, fila)
        fila = AgregarCampo(tabla, "Sexo", _cboSexo, fila)
        fila = AgregarCampo(tabla, "Nacimiento", _dtpNacimiento, fila)
        fila = AgregarCampo(tabla, "Correo", _txtEmail, fila)
        fila = AgregarCampo(tabla, "Teléfono", _txtTelefono, fila)
        fila = AgregarTitulo(tabla, "Inscripción", fila)
        fila = AgregarCampo(tabla, "Carrera *", _cboCarrera, fila)
        fila = AgregarCampo(tabla, "Trayecto *", _cboTrayecto, fila)
        fila = AgregarCampo(tabla, "Tipo de ingreso *", _cboModalidad, fila)
        fila = AgregarCampo(tabla, "Observaciones", _txtObservaciones, fila)

        Dim botones As New FlowLayoutPanel()
        botones.Dock = DockStyle.Fill
        botones.AutoSize = True
        botones.Margin = New Padding(3, 12, 3, 3)
        ConfigurarBoton(_btnGuardar, "Guardar e inscribir", 150, Color.FromArgb(21, 101, 192), Color.White)
        ConfigurarBoton(_btnBuscar, "Buscar cédula", 120, Color.FromArgb(236, 239, 245), Color.Black)
        ConfigurarBoton(_btnLimpiar, "Limpiar", 90, Color.FromArgb(236, 239, 245), Color.Black)
        ConfigurarBoton(_btnComprobante, "Comprobante", 120, Color.FromArgb(46, 125, 50), Color.White)
        _btnComprobante.Enabled = False
        botones.Controls.Add(_btnGuardar)
        botones.Controls.Add(_btnBuscar)
        botones.Controls.Add(_btnLimpiar)
        botones.Controls.Add(_btnComprobante)

        tabla.Controls.Add(botones, 0, fila)
        tabla.SetColumnSpan(botones, 2)
        tabla.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        AddHandler _btnGuardar.Click, AddressOf Guardar_Click
        AddHandler _btnBuscar.Click, AddressOf Buscar_Click
        AddHandler _btnLimpiar.Click, AddressOf Limpiar_Click
        AddHandler _btnComprobante.Click, AddressOf VerComprobante_Click
        AddHandler _txtCedula.KeyDown, AddressOf Cedula_KeyDown

        ' ---------------- listado ----------------
        Dim barra As New Panel()
        barra.Dock = DockStyle.Top
        barra.Height = 46
        barra.Padding = New Padding(6, 6, 6, 4)
        division.Panel2.Controls.Add(barra)

        Dim filtros As New FlowLayoutPanel()
        filtros.Dock = DockStyle.Fill
        barra.Controls.Add(filtros)

        _cboFiltroTrayecto.DropDownStyle = ComboBoxStyle.DropDownList
        _cboFiltroTrayecto.Width = 130
        _cboFiltroModalidad.DropDownStyle = ComboBoxStyle.DropDownList
        _cboFiltroModalidad.Width = 150
        _cboFiltroCarrera.DropDownStyle = ComboBoxStyle.DropDownList
        _cboFiltroCarrera.Width = 210

        filtros.Controls.Add(Etiqueta("Trayecto:"))
        filtros.Controls.Add(_cboFiltroTrayecto)
        filtros.Controls.Add(Etiqueta("Ingreso:"))
        filtros.Controls.Add(_cboFiltroModalidad)
        filtros.Controls.Add(Etiqueta("Carrera:"))
        filtros.Controls.Add(_cboFiltroCarrera)

        _lblTotal.AutoSize = True
        _lblTotal.Margin = New Padding(12, 10, 3, 3)
        _lblTotal.ForeColor = Color.DimGray
        filtros.Controls.Add(_lblTotal)

        ConfigurarGrid()
        division.Panel2.Controls.Add(_grid)
        _grid.BringToFront()

        AddHandler _grid.SelectionChanged, AddressOf Grid_SelectionChanged
        AddHandler _grid.CellDoubleClick, AddressOf Grid_CellDoubleClick
        AddHandler _cboFiltroTrayecto.SelectedIndexChanged, AddressOf Filtro_Changed
        AddHandler _cboFiltroModalidad.SelectedIndexChanged, AddressOf Filtro_Changed
        AddHandler _cboFiltroCarrera.SelectedIndexChanged, AddressOf Filtro_Changed

        CargarCatalogos()
    End Sub

    ' ------------------------------------------------------------------ ayudas de UI

    Private Function Etiqueta(texto As String) As Label
        Return New Label() With {
            .Text = texto,
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .Margin = New Padding(3, 9, 3, 3)
        }
    End Function

    Private Function AgregarTitulo(tabla As TableLayoutPanel, texto As String, fila As Integer) As Integer
        Dim titulo As New Label()
        titulo.Text = texto
        titulo.AutoSize = True
        titulo.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        titulo.ForeColor = Color.FromArgb(21, 101, 192)
        titulo.Margin = New Padding(3, 12, 3, 4)
        tabla.Controls.Add(titulo, 0, fila)
        tabla.SetColumnSpan(titulo, 2)
        tabla.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Return fila + 1
    End Function

    Private Function AgregarCampo(tabla As TableLayoutPanel, texto As String, control As Control, fila As Integer) As Integer
        tabla.Controls.Add(Etiqueta(texto), 0, fila)
        control.Margin = New Padding(3, 5, 3, 5)
        tabla.Controls.Add(control, 1, fila)
        tabla.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Return fila + 1
    End Function

    Private Sub ConfigurarBoton(boton As Button, texto As String, ancho As Integer, fondo As Color, letra As Color)
        boton.Text = texto
        boton.Width = ancho
        boton.Height = 34
        boton.FlatStyle = FlatStyle.Flat
        boton.FlatAppearance.BorderSize = 0
        boton.BackColor = fondo
        boton.ForeColor = letra
        boton.Margin = New Padding(3, 3, 8, 3)
        boton.Cursor = Cursors.Hand
    End Sub

    Private Sub ConfigurarGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.AutoGenerateColumns = False
        _grid.RowHeadersVisible = False
        _grid.BackgroundColor = Color.White
        _grid.BorderStyle = BorderStyle.None
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.MultiSelect = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _grid.EnableHeadersVisualStyles = False
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(21, 101, 192)
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        _grid.ColumnHeadersHeight = 32
        _grid.RowTemplate.Height = 26

        AgregarColumna("TrayectoTexto", "Trayecto", 80)
        AgregarColumna("Cedula", "Cédula", 80)
        AgregarColumna("Estudiante", "Estudiante", 150)
        AgregarColumna("Carrera", "Carrera", 130)
        AgregarColumna("ModalidadTexto", "Ingreso", 70)
        AgregarColumna("Estatus", "Estatus", 70)
        AgregarColumna("FechaTexto", "Fecha", 70)
        AgregarColumna("Codigo", "Comprobante", 110)
    End Sub

    Private Sub AgregarColumna(campo As String, titulo As String, peso As Integer)
        Dim columna As New DataGridViewTextBoxColumn()
        columna.DataPropertyName = campo
        columna.HeaderText = titulo
        columna.FillWeight = peso
        columna.SortMode = DataGridViewColumnSortMode.NotSortable
        _grid.Columns.Add(columna)
    End Sub

    ' ------------------------------------------------------------------ catálogos

    Private Sub CargarCatalogos()
        Dim carreras As List(Of Carrera) = Datos.ListarCarreras()

        _cboCarrera.Items.Clear()
        For Each carrera As Carrera In carreras
            _cboCarrera.Items.Add(carrera)
        Next
        If _cboCarrera.Items.Count > 0 Then _cboCarrera.SelectedIndex = 0

        _cboTrayecto.Items.Clear()
        For Each numero As Integer In Datos.ListarTrayectos()
            _cboTrayecto.Items.Add(New Opcion(numero.ToString(), Utilidades.TrayectoTexto(numero)))
        Next
        If _cboTrayecto.Items.Count > 0 Then _cboTrayecto.SelectedIndex = 0

        _cboFiltroTrayecto.Items.Clear()
        _cboFiltroTrayecto.Items.Add(New Opcion("0", "Todos"))
        For Each numero As Integer In Datos.ListarTrayectos()
            _cboFiltroTrayecto.Items.Add(New Opcion(numero.ToString(), Utilidades.TrayectoTexto(numero)))
        Next
        _cboFiltroTrayecto.SelectedIndex = 0

        _cboFiltroModalidad.Items.Clear()
        _cboFiltroModalidad.Items.Add(New Opcion("", "Todas"))
        _cboFiltroModalidad.Items.Add(New Opcion("OPSU", "OPSU"))
        _cboFiltroModalidad.Items.Add(New Opcion("FLOTANTE", "Flotante"))
        _cboFiltroModalidad.SelectedIndex = 0

        _cboFiltroCarrera.Items.Clear()
        _cboFiltroCarrera.Items.Add(New Opcion("0", "Todas"))
        For Each carrera As Carrera In carreras
            _cboFiltroCarrera.Items.Add(New Opcion(carrera.Id.ToString(), carrera.Nombre))
        Next
        _cboFiltroCarrera.SelectedIndex = 0
    End Sub

    ' ------------------------------------------------------------------ acciones

    ''' <summary>Aplica el ancho del formulario cuando la ventana ya tiene su tamaño real.</summary>
    Friend Sub AjustarDivision()
        If _division Is Nothing Then Return
        Dim ancho As Integer = _division.Width
        If ancho < 760 Then Return   ' todavía no hay espacio suficiente

        Try
            _division.Panel1MinSize = 380
            _division.Panel2MinSize = 300
            Dim maximo As Integer = ancho - _division.Panel2MinSize - _division.SplitterWidth
            If maximo > _division.Panel1MinSize Then
                _division.SplitterDistance = Math.Min(AnchoFormulario, maximo)
            End If
        Catch ex As InvalidOperationException
            ' El contenedor aún no admite el ajuste; se reintenta en el próximo cambio de tamaño.
        End Try
    End Sub

    Private Sub Cedula_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            Buscar_Click(sender, EventArgs.Empty)
        End If
    End Sub

    Private Sub Buscar_Click(sender As Object, e As EventArgs)
        Try
            Dim cedula As String = _txtCedula.Text.Trim()
            If Utilidades.SoloDigitos(cedula).Length = 0 Then
                MessageBox.Show("Escriba una cédula para buscar.", "Buscar", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim estudiante As Estudiante = Datos.BuscarEstudiante(cedula)
            If estudiante Is Nothing Then
                MessageBox.Show("No existe un estudiante con esa cédula. Puede registrarlo.", "Buscar",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            _txtCedula.Text = estudiante.Cedula
            _txtNombres.Text = estudiante.Nombres
            _txtApellidos.Text = estudiante.Apellidos
            _txtEmail.Text = estudiante.Email
            _txtTelefono.Text = estudiante.Telefono
            SeleccionarOpcion(_cboSexo, estudiante.Sexo)
            _cboModalidad.SelectedIndex = If(estudiante.Modalidad = TipoIngreso.OPSU, 0, 1)

            If estudiante.FechaNacimiento.HasValue Then
                _dtpNacimiento.Value = estudiante.FechaNacimiento.Value
                _dtpNacimiento.Checked = True
            End If

            Estado("Estudiante encontrado: " & estudiante.NombreCompleto & " · " & estudiante.OpsuEstatusTexto)
        Catch ex As Exception
            MessageBox.Show("No se pudo buscar el estudiante." & Environment.NewLine & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Limpiar_Click(sender As Object, e As EventArgs)
        _txtCedula.Clear()
        _txtNombres.Clear()
        _txtApellidos.Clear()
        _txtEmail.Clear()
        _txtTelefono.Clear()
        _txtObservaciones.Clear()
        _dtpNacimiento.Checked = False
        _cboSexo.SelectedIndex = 0
        _cboModalidad.SelectedIndex = 1
        If _cboCarrera.Items.Count > 0 Then _cboCarrera.SelectedIndex = 0
        If _cboTrayecto.Items.Count > 0 Then _cboTrayecto.SelectedIndex = 0
        _txtCedula.Focus()
    End Sub

    Private Sub Guardar_Click(sender As Object, e As EventArgs)
        Try
            Dim errores As New List(Of String)()
            If Utilidades.SoloDigitos(_txtCedula.Text).Length < 6 Then errores.Add("• La cédula debe tener al menos 6 dígitos.")
            If _txtNombres.Text.Trim().Length = 0 Then errores.Add("• Indique los nombres.")
            If _txtApellidos.Text.Trim().Length = 0 Then errores.Add("• Indique los apellidos.")

            Dim carrera As Carrera = TryCast(_cboCarrera.SelectedItem, Carrera)
            If carrera Is Nothing Then errores.Add("• Seleccione la carrera.")

            Dim opcionTrayecto As Opcion = TryCast(_cboTrayecto.SelectedItem, Opcion)
            If opcionTrayecto Is Nothing Then errores.Add("• Seleccione el trayecto.")

            Dim opcionModalidad As Opcion = TryCast(_cboModalidad.SelectedItem, Opcion)
            If opcionModalidad Is Nothing Then errores.Add("• Seleccione el tipo de ingreso.")

            If errores.Count > 0 Then
                MessageBox.Show(String.Join(Environment.NewLine, errores), "Datos incompletos",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim modalidad As TipoIngreso = If(opcionModalidad.Valor = "OPSU", TipoIngreso.OPSU, TipoIngreso.FLOTANTE)
            Dim sexo As String = ""
            Dim opcionSexo As Opcion = TryCast(_cboSexo.SelectedItem, Opcion)
            If opcionSexo IsNot Nothing Then sexo = opcionSexo.Valor

            Dim estudiante As New Estudiante With {
                .Cedula = Utilidades.FormatearCedula(_txtCedula.Text),
                .Nombres = _txtNombres.Text.Trim(),
                .Apellidos = _txtApellidos.Text.Trim(),
                .Sexo = sexo,
                .Email = _txtEmail.Text.Trim(),
                .Telefono = _txtTelefono.Text.Trim(),
                .Modalidad = modalidad
            }
            If _dtpNacimiento.Checked Then estudiante.FechaNacimiento = _dtpNacimiento.Value.Date

            Dim estudianteId As Integer = Datos.GuardarEstudiante(estudiante)
            Dim trayecto As Integer = Integer.Parse(opcionTrayecto.Valor)
            Dim codigo As String = Datos.GuardarInscripcion(estudianteId, carrera.Id, trayecto,
                                                            modalidad, _txtObservaciones.Text)

            Datos.RegistrarBitacora("INSCRIPCION",
                estudiante.Cedula & " · " & carrera.Nombre & " · " & opcionTrayecto.Texto & " · " &
                modalidad.ToString() & " · " & codigo)

            RefrescarListado()
            ActualizarResumen()
            Limpiar_Click(sender, e)

            Dim respuesta As DialogResult = MessageBox.Show(
                "Inscripción registrada correctamente." & Environment.NewLine & Environment.NewLine &
                estudiante.NombreCompleto & " · " & opcionTrayecto.Texto & " · " &
                If(modalidad = TipoIngreso.OPSU, "OPSU", "Flotante") & Environment.NewLine &
                "Comprobante: " & codigo & Environment.NewLine & Environment.NewLine &
                "¿Desea ver e imprimir el comprobante ahora?",
                "Inscripción registrada", MessageBoxButtons.YesNo, MessageBoxIcon.Information)

            If respuesta = DialogResult.Yes Then Comprobante.Mostrar(codigo)
        Catch ex As Exception
            MessageBox.Show("No se pudo guardar la inscripción." & Environment.NewLine & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ------------------------------------------------------------------ listado

    Private Sub Filtro_Changed(sender As Object, e As EventArgs)
        Try
            RefrescarListado()
        Catch ex As Exception
            Estado("No se pudo filtrar: " & ex.Message)
        End Try
    End Sub

    Friend Sub RefrescarListado()
        Dim trayecto As Integer = 0
        Dim opcionTrayecto As Opcion = TryCast(_cboFiltroTrayecto.SelectedItem, Opcion)
        If opcionTrayecto IsNot Nothing Then Integer.TryParse(opcionTrayecto.Valor, trayecto)

        Dim modalidad As String = ""
        Dim opcionModalidad As Opcion = TryCast(_cboFiltroModalidad.SelectedItem, Opcion)
        If opcionModalidad IsNot Nothing Then modalidad = opcionModalidad.Valor

        Dim carrera As Integer = 0
        Dim opcionCarrera As Opcion = TryCast(_cboFiltroCarrera.SelectedItem, Opcion)
        If opcionCarrera IsNot Nothing Then Integer.TryParse(opcionCarrera.Valor, carrera)

        _grid.DataSource = Datos.ListarInscripciones(trayecto, modalidad, carrera)
        _lblTotal.Text = "Registros: " & _grid.RowCount.ToString()
        _btnComprobante.Enabled = _grid.RowCount > 0
    End Sub

    ''' <summary>Inscripción seleccionada en el listado (Nothing si no hay fila).</summary>
    Private Function InscripcionSeleccionada() As Inscripcion
        If _grid.CurrentRow Is Nothing Then Return Nothing
        Return TryCast(_grid.CurrentRow.DataBoundItem, Inscripcion)
    End Function

    Private Sub Grid_SelectionChanged(sender As Object, e As EventArgs)
        _btnComprobante.Enabled = InscripcionSeleccionada() IsNot Nothing
    End Sub

    Private Sub Grid_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 Then Return
        VerComprobante_Click(sender, EventArgs.Empty)
    End Sub

    Private Sub VerComprobante_Click(sender As Object, e As EventArgs)
        Dim inscripcion As Inscripcion = InscripcionSeleccionada()
        If inscripcion Is Nothing Then
            MessageBox.Show("Seleccione una inscripción del listado para ver su comprobante.",
                            "Comprobante", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Comprobante.Mostrar(inscripcion.Codigo)
        RefrescarListado()
    End Sub

    Private Shared Sub SeleccionarOpcion(combo As ComboBox, valor As String)
        For Each item As Object In combo.Items
            Dim opcion As Opcion = TryCast(item, Opcion)
            If opcion IsNot Nothing AndAlso String.Equals(opcion.Valor, valor, StringComparison.OrdinalIgnoreCase) Then
                combo.SelectedItem = opcion
                Return
            End If
        Next
        If combo.Items.Count > 0 Then combo.SelectedIndex = 0
    End Sub

End Class
