Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports InscripcionOPSU.Modelos

''' <summary>
''' Pestaña de carga y comparación del archivo oficial de la OPSU.
''' Flujo: seleccionar archivo → analizar → revisar resultados → aplicar.
''' </summary>
Friend Partial Class FormPrincipal

    Private ReadOnly _txtArchivo As New TextBox()
    Private ReadOnly _btnElegir As New Button()
    Private ReadOnly _btnAnalizar As New Button()
    Private ReadOnly _btnAplicar As New Button()
    Private ReadOnly _chkImportar As New CheckBox()
    Private ReadOnly _lblResumen As New Label()
    Private ReadOnly _gridOpsu As New DataGridView()
    Private _cargaActual As Integer

    Private Sub ConstruirOpsu(pagina As TabPage)
        Dim panel As New Panel()
        panel.Dock = DockStyle.Top
        panel.Height = 178
        panel.Padding = New Padding(6)
        pagina.Controls.Add(panel)

        Dim tabla As New TableLayoutPanel()
        tabla.Dock = DockStyle.Top
        tabla.AutoSize = True
        tabla.ColumnCount = 3
        tabla.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 92))
        tabla.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        tabla.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        panel.Controls.Add(tabla)

        _txtArchivo.Dock = DockStyle.Fill
        _txtArchivo.ReadOnly = True
        _txtArchivo.PlaceholderText = "Ningún archivo seleccionado"

        ConfigurarBoton(_btnElegir, "Seleccionar archivo…", 150, Color.FromArgb(236, 239, 245), Color.Black)
        ConfigurarBoton(_btnAnalizar, "Analizar y comparar", 160, Color.FromArgb(21, 101, 192), Color.White)
        ConfigurarBoton(_btnAplicar, "Aplicar resultados", 150, Color.FromArgb(46, 125, 50), Color.White)
        _btnAnalizar.Enabled = False
        _btnAplicar.Enabled = False

        _chkImportar.Text = "Crear en el sistema los estudiantes que aparecen en OPSU y no existen"
        _chkImportar.AutoSize = True
        _chkImportar.Checked = True
        _chkImportar.Margin = New Padding(3, 12, 3, 4)

        tabla.Controls.Add(Etiqueta("Archivo OPSU:"), 0, 0)
        tabla.Controls.Add(_txtArchivo, 1, 0)
        tabla.Controls.Add(_btnElegir, 2, 0)
        tabla.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim acciones As New FlowLayoutPanel()
        acciones.AutoSize = True
        acciones.Dock = DockStyle.Fill
        acciones.Margin = New Padding(0, 8, 0, 4)
        acciones.Controls.Add(_btnAnalizar)
        acciones.Controls.Add(_btnAplicar)
        acciones.Controls.Add(_chkImportar)
        tabla.Controls.Add(acciones, 0, 1)
        tabla.SetColumnSpan(acciones, 3)
        tabla.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        _lblResumen.AutoSize = True
        _lblResumen.MaximumSize = New Size(1200, 0)
        _lblResumen.Margin = New Padding(3, 6, 3, 3)
        _lblResumen.ForeColor = Color.FromArgb(55, 65, 81)
        _lblResumen.Text = "Seleccione el archivo CSV o Excel entregado por la OPSU para compararlo con los registros del sistema."
        tabla.Controls.Add(_lblResumen, 0, 2)
        tabla.SetColumnSpan(_lblResumen, 3)
        tabla.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        ConfigurarGridOpsu()
        pagina.Controls.Add(_gridOpsu)
        _gridOpsu.BringToFront()

        AddHandler _btnElegir.Click, AddressOf ElegirArchivo_Click
        AddHandler _btnAnalizar.Click, AddressOf Analizar_Click
        AddHandler _btnAplicar.Click, AddressOf Aplicar_Click
        AddHandler _gridOpsu.CellFormatting, AddressOf GridOpsu_CellFormatting
    End Sub

    Private Sub ConfigurarGridOpsu()
        _gridOpsu.Dock = DockStyle.Fill
        _gridOpsu.ReadOnly = True
        _gridOpsu.AllowUserToAddRows = False
        _gridOpsu.AutoGenerateColumns = False
        _gridOpsu.RowHeadersVisible = False
        _gridOpsu.BackgroundColor = Color.White
        _gridOpsu.BorderStyle = BorderStyle.None
        _gridOpsu.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _gridOpsu.MultiSelect = False
        _gridOpsu.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _gridOpsu.EnableHeadersVisualStyles = False
        _gridOpsu.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(21, 101, 192)
        _gridOpsu.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        _gridOpsu.ColumnHeadersHeight = 32

        AgregarColumnaOpsu("Cedula", "Cédula", 80)
        AgregarColumnaOpsu("NombreMostrar", "Estudiante", 150)
        AgregarColumnaOpsu("CarreraOpsu", "Carrera OPSU", 140)
        AgregarColumnaOpsu("CarreraSistema", "Carrera sistema", 140)
        AgregarColumnaOpsu("ResultadoTexto", "Resultado", 120)
        AgregarColumnaOpsu("Diferencias", "Observaciones", 220)
    End Sub

    Private Sub AgregarColumnaOpsu(campo As String, titulo As String, peso As Integer)
        Dim columna As New DataGridViewTextBoxColumn()
        columna.DataPropertyName = campo
        columna.HeaderText = titulo
        columna.FillWeight = peso
        columna.SortMode = DataGridViewColumnSortMode.NotSortable
        _gridOpsu.Columns.Add(columna)
    End Sub

    ' ------------------------------------------------------------------ acciones

    Private Sub ElegirArchivo_Click(sender As Object, e As EventArgs)
        Using dialogo As New OpenFileDialog()
            dialogo.Title = "Seleccionar archivo oficial de la OPSU"
            dialogo.Filter = "Archivos OPSU (*.csv;*.xlsx;*.xls)|*.csv;*.xlsx;*.xls|Todos los archivos (*.*)|*.*"
            If dialogo.ShowDialog() = DialogResult.OK Then
                _txtArchivo.Text = dialogo.FileName
                _btnAnalizar.Enabled = True
                _btnAplicar.Enabled = False
                _cargaActual = 0
                _lblResumen.Text = "Archivo seleccionado. Pulse «Analizar y comparar» para verificarlo contra el sistema."
                Estado("Archivo OPSU: " & Path.GetFileName(dialogo.FileName))
            End If
        End Using
    End Sub

    Private Sub Analizar_Click(sender As Object, e As EventArgs)
        Try
            Dim ruta As String = _txtArchivo.Text
            If Not File.Exists(ruta) Then
                MessageBox.Show("Seleccione primero un archivo de la OPSU.", "Validación OPSU",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Cursor = Cursors.WaitCursor
            Estado("Leyendo archivo OPSU…")

            Dim registros As List(Of OpsuRegistro) = OpsuLector.Leer(ruta)
            If registros.Count = 0 Then
                MessageBox.Show("El archivo no contiene registros con cédula válida.", "Validación OPSU",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Estado("Comparando con los estudiantes registrados…")
            Dim estudiantes As List(Of Estudiante) = Datos.ListarEstudiantes()
            Dim comparaciones As List(Of Comparacion) = OpsuComparador.Comparar(registros, estudiantes)

            Dim periodo As Periodo = Datos.PeriodoActivo()
            Dim hash As String = Utilidades.HashArchivo(ruta)
            _cargaActual = Datos.RegistrarCargaOpsu(Path.GetFileName(ruta), hash, periodo.Id, registros.Count)
            Datos.GuardarRegistrosOpsu(_cargaActual, registros)
            Datos.GuardarComparaciones(_cargaActual, comparaciones)
            Datos.RegistrarBitacora("OPSU_CARGA",
                Path.GetFileName(ruta) & " · " & registros.Count.ToString() & " registros · " & comparaciones.Count.ToString() & " comparaciones")

            MostrarComparaciones(comparaciones)
            _btnAplicar.Enabled = True
            Estado("Comparación finalizada · " & comparaciones.Count.ToString() & " registros analizados")
        Catch ex As Exception
            MessageBox.Show("No se pudo procesar el archivo." & Environment.NewLine & ex.Message,
                            "Validación OPSU", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub MostrarComparaciones(comparaciones As List(Of Comparacion))
        _gridOpsu.DataSource = Nothing
        _gridOpsu.DataSource = comparaciones

        Dim resumen As Dictionary(Of ResultadoComparacion, Integer) = OpsuComparador.Resumen(comparaciones)
        _lblResumen.Text = String.Format(
            "Coinciden: {0}   ·   No aparecen en OPSU: {1}   ·   No registrados en el sistema: {2}   ·   " &
            "Datos distintos: {3}   ·   Carrera distinta: {4}",
            resumen(ResultadoComparacion.COINCIDE),
            resumen(ResultadoComparacion.NO_APARECE_EN_OPSU),
            resumen(ResultadoComparacion.NO_REGISTRADO_EN_SISTEMA),
            resumen(ResultadoComparacion.DATOS_DISTINTOS),
            resumen(ResultadoComparacion.CARRERA_DISTINTA))
    End Sub

    Private Sub GridOpsu_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 Then Return
        Dim comparacion As Comparacion = TryCast(_gridOpsu.Rows(e.RowIndex).DataBoundItem, Comparacion)
        If comparacion Is Nothing Then Return

        Select Case comparacion.Resultado
            Case ResultadoComparacion.COINCIDE
                e.CellStyle.BackColor = Color.FromArgb(226, 245, 226)
            Case ResultadoComparacion.NO_APARECE_EN_OPSU
                e.CellStyle.BackColor = Color.FromArgb(255, 243, 205)
            Case ResultadoComparacion.NO_REGISTRADO_EN_SISTEMA
                e.CellStyle.BackColor = Color.FromArgb(219, 234, 254)
            Case ResultadoComparacion.DATOS_DISTINTOS
                e.CellStyle.BackColor = Color.FromArgb(255, 228, 225)
            Case ResultadoComparacion.CARRERA_DISTINTA
                e.CellStyle.BackColor = Color.FromArgb(255, 235, 205)
        End Select
    End Sub

    Private Sub Aplicar_Click(sender As Object, e As EventArgs)
        If _cargaActual = 0 Then
            MessageBox.Show("Primero analice un archivo de la OPSU.", "Aplicar resultados",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim mensaje As String = "Se actualizará el tipo de ingreso y el estatus OPSU de los estudiantes comparados."
        If _chkImportar.Checked Then
            mensaje &= Environment.NewLine & "También se crearán los estudiantes que aparecen en el archivo y no existen en el sistema."
        End If
        mensaje &= Environment.NewLine & Environment.NewLine & "¿Desea continuar?"

        If MessageBox.Show(mensaje, "Aplicar resultados", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Return
        End If

        Try
            Cursor = Cursors.WaitCursor
            Dim afectados As Integer = Datos.AplicarComparacion(_cargaActual, _chkImportar.Checked)
            RefrescarListado()
            ActualizarResumen()
            MessageBox.Show("Resultados aplicados. Registros afectados: " & afectados.ToString(),
                            "Aplicar resultados", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Estado("Resultados OPSU aplicados · " & afectados.ToString() & " estudiantes actualizados")
        Catch ex As Exception
            MessageBox.Show("No se pudieron aplicar los resultados." & Environment.NewLine & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

End Class
