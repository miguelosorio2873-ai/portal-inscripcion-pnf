Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports InscripcionOPSU.Modelos

''' <summary>Pestaña de reportes: listados por trayecto y tipo de ingreso.</summary>
Friend Partial Class FormPrincipal

    Private ReadOnly _cboRepTrayecto As New ComboBox()
    Private ReadOnly _cboRepModalidad As New ComboBox()
    Private ReadOnly _cboRepCarrera As New ComboBox()
    Private ReadOnly _btnVistaPrevia As New Button()
    Private ReadOnly _btnImprimir As New Button()
    Private ReadOnly _btnExportar As New Button()
    Private ReadOnly _lblRepResumen As New Label()
    Private ReadOnly _gridReporte As New DataGridView()

    Private Sub ConstruirReportes(pagina As TabPage)
        Dim panel As New Panel()
        panel.Dock = DockStyle.Top
        panel.Height = 132
        panel.Padding = New Padding(6)
        pagina.Controls.Add(panel)

        Dim tabla As New TableLayoutPanel()
        tabla.Dock = DockStyle.Top
        tabla.AutoSize = True
        tabla.ColumnCount = 2
        tabla.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 92))
        tabla.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        panel.Controls.Add(tabla)

        For Each combo As ComboBox In New ComboBox() {_cboRepTrayecto, _cboRepModalidad, _cboRepCarrera}
            combo.DropDownStyle = ComboBoxStyle.DropDownList
            combo.Width = 240
        Next

        Dim filtros As New FlowLayoutPanel()
        filtros.AutoSize = True
        filtros.Dock = DockStyle.Fill
        filtros.Controls.Add(_cboRepTrayecto)
        filtros.Controls.Add(_cboRepModalidad)
        filtros.Controls.Add(_cboRepCarrera)

        tabla.Controls.Add(Etiqueta("Filtros:"), 0, 0)
        tabla.Controls.Add(filtros, 1, 0)
        tabla.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        ConfigurarBoton(_btnVistaPrevia, "Vista previa", 130, Color.FromArgb(21, 101, 192), Color.White)
        ConfigurarBoton(_btnImprimir, "Imprimir", 110, Color.FromArgb(46, 125, 50), Color.White)
        ConfigurarBoton(_btnExportar, "Exportar CSV", 120, Color.FromArgb(236, 239, 245), Color.Black)

        Dim acciones As New FlowLayoutPanel()
        acciones.AutoSize = True
        acciones.Dock = DockStyle.Fill
        acciones.Margin = New Padding(0, 8, 0, 4)
        acciones.Controls.Add(_btnVistaPrevia)
        acciones.Controls.Add(_btnImprimir)
        acciones.Controls.Add(_btnExportar)

        _lblRepResumen.AutoSize = True
        _lblRepResumen.Margin = New Padding(6, 12, 3, 3)
        _lblRepResumen.ForeColor = Color.DimGray
        acciones.Controls.Add(_lblRepResumen)

        tabla.Controls.Add(acciones, 0, 1)
        tabla.SetColumnSpan(acciones, 2)
        tabla.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        ConfigurarGridReporte()
        pagina.Controls.Add(_gridReporte)
        _gridReporte.BringToFront()

        AddHandler _cboRepTrayecto.SelectedIndexChanged, AddressOf Reporte_Changed
        AddHandler _cboRepModalidad.SelectedIndexChanged, AddressOf Reporte_Changed
        AddHandler _cboRepCarrera.SelectedIndexChanged, AddressOf Reporte_Changed
        AddHandler _btnVistaPrevia.Click, AddressOf VistaPrevia_Click
        AddHandler _btnImprimir.Click, AddressOf Imprimir_Click
        AddHandler _btnExportar.Click, AddressOf Exportar_Click

        CargarFiltrosReporte()
    End Sub

    Private Sub ConfigurarGridReporte()
        _gridReporte.Dock = DockStyle.Fill
        _gridReporte.ReadOnly = True
        _gridReporte.AllowUserToAddRows = False
        _gridReporte.AutoGenerateColumns = False
        _gridReporte.RowHeadersVisible = False
        _gridReporte.BackgroundColor = Color.White
        _gridReporte.BorderStyle = BorderStyle.None
        _gridReporte.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _gridReporte.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _gridReporte.EnableHeadersVisualStyles = False
        _gridReporte.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(55, 71, 79)
        _gridReporte.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        _gridReporte.ColumnHeadersHeight = 32

        AgregarColumnaReporte("TrayectoTexto", "Trayecto", 80)
        AgregarColumnaReporte("Cedula", "Cédula", 80)
        AgregarColumnaReporte("Estudiante", "Estudiante", 160)
        AgregarColumnaReporte("Carrera", "Carrera", 140)
        AgregarColumnaReporte("ModalidadTexto", "Ingreso", 70)
        AgregarColumnaReporte("Estatus", "Estatus", 70)
        AgregarColumnaReporte("FechaTexto", "Fecha", 70)
    End Sub

    Private Sub AgregarColumnaReporte(campo As String, titulo As String, peso As Integer)
        Dim columna As New DataGridViewTextBoxColumn()
        columna.DataPropertyName = campo
        columna.HeaderText = titulo
        columna.FillWeight = peso
        columna.SortMode = DataGridViewColumnSortMode.NotSortable
        _gridReporte.Columns.Add(columna)
    End Sub

    Private Sub CargarFiltrosReporte()
        _cboRepTrayecto.Items.Clear()
        _cboRepTrayecto.Items.Add(New Opcion("0", "Todos los trayectos"))
        For Each numero As Integer In Datos.ListarTrayectos()
            _cboRepTrayecto.Items.Add(New Opcion(numero.ToString(), Utilidades.TrayectoTexto(numero)))
        Next
        _cboRepTrayecto.SelectedIndex = 0

        _cboRepModalidad.Items.Clear()
        _cboRepModalidad.Items.Add(New Opcion("", "Todos los ingresos"))
        _cboRepModalidad.Items.Add(New Opcion("OPSU", "Solo OPSU"))
        _cboRepModalidad.Items.Add(New Opcion("FLOTANTE", "Solo flotantes"))
        _cboRepModalidad.SelectedIndex = 0

        _cboRepCarrera.Items.Clear()
        _cboRepCarrera.Items.Add(New Opcion("0", "Todas las carreras"))
        For Each carrera As Carrera In Datos.ListarCarreras()
            _cboRepCarrera.Items.Add(New Opcion(carrera.Id.ToString(), carrera.Nombre))
        Next
        _cboRepCarrera.SelectedIndex = 0

        ActualizarReporte()
    End Sub

    Private Sub Reporte_Changed(sender As Object, e As EventArgs)
        Try
            ActualizarReporte()
        Catch ex As Exception
            Estado("No se pudo actualizar el reporte: " & ex.Message)
        End Try
    End Sub

    Private Function DatosReporte() As List(Of Inscripcion)
        Dim trayecto As Integer = 0
        Dim opcionTrayecto As Opcion = TryCast(_cboRepTrayecto.SelectedItem, Opcion)
        If opcionTrayecto IsNot Nothing Then Integer.TryParse(opcionTrayecto.Valor, trayecto)

        Dim modalidad As String = ""
        Dim opcionModalidad As Opcion = TryCast(_cboRepModalidad.SelectedItem, Opcion)
        If opcionModalidad IsNot Nothing Then modalidad = opcionModalidad.Valor

        Dim carrera As Integer = 0
        Dim opcionCarrera As Opcion = TryCast(_cboRepCarrera.SelectedItem, Opcion)
        If opcionCarrera IsNot Nothing Then Integer.TryParse(opcionCarrera.Valor, carrera)

        Return Datos.ListarInscripciones(trayecto, modalidad, carrera)
    End Function

    Private Sub ActualizarReporte()
        Dim lista As List(Of Inscripcion) = DatosReporte()
        _gridReporte.DataSource = lista
        _lblRepResumen.Text = "Registros en el reporte: " & lista.Count.ToString(CultureInfo.InvariantCulture)
    End Sub

    Private Function TituloReporte() As String
        Dim opcionTrayecto As Opcion = TryCast(_cboRepTrayecto.SelectedItem, Opcion)
        Dim opcionModalidad As Opcion = TryCast(_cboRepModalidad.SelectedItem, Opcion)
        Dim opcionCarrera As Opcion = TryCast(_cboRepCarrera.SelectedItem, Opcion)

        Dim trayecto As String = If(opcionTrayecto Is Nothing, "Todos los trayectos", opcionTrayecto.Texto)
        Dim modalidad As String = If(opcionModalidad Is Nothing, "Todos los ingresos", opcionModalidad.Texto)
        Dim carrera As String = If(opcionCarrera Is Nothing, "Todas las carreras", opcionCarrera.Texto)

        Return "Inscripciones por trayecto · " & trayecto & " · " & modalidad & " · " & carrera
    End Function

    Private Sub VistaPrevia_Click(sender As Object, e As EventArgs)
        Dim lista As List(Of Inscripcion) = DatosReporte()
        If lista.Count = 0 Then
            MessageBox.Show("No hay inscripciones para los filtros seleccionados.", "Reporte",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Try
            Reporte.Previsualizar(lista, TituloReporte())
        Catch ex As Exception
            MessageBox.Show("No se pudo generar la vista previa." & Environment.NewLine & ex.Message,
                            "Reporte", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Imprimir_Click(sender As Object, e As EventArgs)
        Dim lista As List(Of Inscripcion) = DatosReporte()
        If lista.Count = 0 Then
            MessageBox.Show("No hay inscripciones para los filtros seleccionados.", "Reporte",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Try
            Reporte.Imprimir(lista, TituloReporte())
        Catch ex As Exception
            MessageBox.Show("No se pudo imprimir el reporte." & Environment.NewLine & ex.Message,
                            "Reporte", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Exportar_Click(sender As Object, e As EventArgs)
        Dim lista As List(Of Inscripcion) = DatosReporte()
        If lista.Count = 0 Then
            MessageBox.Show("No hay inscripciones para exportar.", "Reporte",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dialogo As New SaveFileDialog()
            dialogo.Title = "Exportar reporte a CSV"
            dialogo.Filter = "Archivo CSV (*.csv)|*.csv"
            dialogo.FileName = "inscripciones_" & DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture) & ".csv"
            If dialogo.ShowDialog() <> DialogResult.OK Then Return

            Dim contenido As New StringBuilder()
            contenido.AppendLine("Trayecto;Cedula;Estudiante;Carrera;Ingreso;Estatus;Fecha")
            For Each item As Inscripcion In lista
                contenido.AppendLine(String.Join(";", item.TrayectoTexto, item.Cedula, item.Estudiante,
                                                 item.Carrera, item.ModalidadTexto, item.Estatus, item.FechaTexto))
            Next
            File.WriteAllText(dialogo.FileName, contenido.ToString(), New UTF8Encoding(True))
            MessageBox.Show("Reporte exportado en:" & Environment.NewLine & dialogo.FileName,
                            "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

End Class
