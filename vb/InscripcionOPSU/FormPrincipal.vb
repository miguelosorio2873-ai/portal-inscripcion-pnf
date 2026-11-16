Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Ventana principal. Se organiza en tres pestañas: registro e inscripción,
''' validación contra el archivo OPSU y reportes.
''' NO existe ningún concepto de turno en el sistema.
''' </summary>
Friend Partial Class FormPrincipal
    Inherits Form

    Private ReadOnly _tabs As New TabControl()
    Private ReadOnly _estado As New StatusStrip()
    Private ReadOnly _lblEstado As New ToolStripStatusLabel()

    Public Sub New()
        Text = "Inscripción Universitaria · OPSU y Flotantes"
        StartPosition = FormStartPosition.CenterScreen
        MinimumSize = New Size(1120, 700)
        Size = New Size(1320, 820)
        Font = New Font("Segoe UI", 9.5F)
        BackColor = Color.FromArgb(245, 247, 250)
        Construir()
    End Sub

    Private Sub Construir()
        _tabs.Dock = DockStyle.Fill
        Controls.Add(_tabs)

        Dim paginaRegistro As New TabPage("Registro e inscripción")
        Dim paginaOpsu As New TabPage("Validación OPSU")
        Dim paginaReportes As New TabPage("Reportes")

        paginaRegistro.Padding = New Padding(8)
        paginaOpsu.Padding = New Padding(8)
        paginaReportes.Padding = New Padding(8)

        _tabs.TabPages.Add(paginaRegistro)
        _tabs.TabPages.Add(paginaOpsu)
        _tabs.TabPages.Add(paginaReportes)

        ConstruirRegistro(paginaRegistro)
        ConstruirOpsu(paginaOpsu)
        ConstruirReportes(paginaReportes)

        _lblEstado.Text = "Iniciando…"
        _estado.Items.Add(_lblEstado)
        _estado.Dock = DockStyle.Bottom
        Controls.Add(_estado)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Try
            RefrescarListado()
        Catch ex As Exception
            MessageBox.Show("No se pudo cargar el listado." & Environment.NewLine & ex.Message,
                            "Inscripción OPSU", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
        ActualizarResumen()
    End Sub

    Friend Sub Estado(texto As String)
        _lblEstado.Text = texto
    End Sub

    Friend Sub ActualizarResumen()
        Try
            Estado(Datos.Resumen() & "  ·  " & Configuracion.Descripcion())
        Catch ex As Exception
            Estado("Sin conexión con MySQL: " & ex.Message)
        End Try
    End Sub

End Class
