Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Diálogo para configurar y probar la conexión con MySQL/MariaDB.</summary>
Friend NotInheritable Class ConexionForm
    Inherits Form

    Private ReadOnly _txtServidor As New TextBox()
    Private ReadOnly _numPuerto As New NumericUpDown()
    Private ReadOnly _txtUsuario As New TextBox()
    Private ReadOnly _txtContrasena As New TextBox()
    Private ReadOnly _txtBaseDatos As New TextBox()
    Private ReadOnly _lblEstado As New Label()
    Private ReadOnly _btnProbar As New Button()
    Private ReadOnly _btnGuardar As New Button()
    Private ReadOnly _btnCancelar As New Button()

    Public Sub New()
        Text = "Conexión a MySQL"
        StartPosition = FormStartPosition.CenterParent
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        ClientSize = New Size(480, 350)
        BackColor = Color.FromArgb(244, 247, 251)
        Font = New Font("Segoe UI", 9.0F)
        AutoScaleMode = AutoScaleMode.Dpi

        ConstruirInterfaz()
    End Sub

    Private Sub ConstruirInterfaz()
        SuspendLayout()

        Dim encabezado As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 64,
            .BackColor = Color.FromArgb(11, 58, 99)
        }

        Dim titulo As New Label With {
            .Text = "Conexión a MySQL",
            .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(20, 9),
            .BackColor = Color.Transparent
        }

        Dim subtitulo As New Label With {
            .Text = "Base de datos compartida con el portal web",
            .Font = New Font("Segoe UI", 8.5F),
            .ForeColor = Color.FromArgb(185, 210, 230),
            .AutoSize = True,
            .Location = New Point(22, 38),
            .BackColor = Color.Transparent
        }

        encabezado.Controls.Add(titulo)
        encabezado.Controls.Add(subtitulo)

        Dim tabla As New TableLayoutPanel With {
            .Location = New Point(24, 78),
            .Size = New Size(432, 170),
            .ColumnCount = 2,
            .RowCount = 5,
            .BackColor = Color.Transparent
        }
        tabla.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 112.0F))
        tabla.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        For fila As Integer = 0 To 4
            tabla.RowStyles.Add(New RowStyle(SizeType.Absolute, 32.0F))
        Next

        _txtServidor.Text = Configuracion.Servidor
        _numPuerto.Minimum = 1
        _numPuerto.Maximum = 65535
        _numPuerto.Value = Math.Min(65535, Math.Max(1, Configuracion.Puerto))
        _numPuerto.ThousandsSeparator = False
        _txtUsuario.Text = Configuracion.Usuario
        _txtContrasena.Text = Configuracion.Contrasena
        _txtContrasena.UseSystemPasswordChar = True
        _txtBaseDatos.Text = Configuracion.BaseDatos

        AgregarCampo(tabla, 0, "Servidor", _txtServidor)
        AgregarCampo(tabla, 1, "Puerto", _numPuerto)
        AgregarCampo(tabla, 2, "Usuario", _txtUsuario)
        AgregarCampo(tabla, 3, "Contraseña", _txtContrasena)
        AgregarCampo(tabla, 4, "Base de datos", _txtBaseDatos)

        With _lblEstado
            .Location = New Point(24, 252)
            .Size = New Size(432, 20)
            .AutoEllipsis = True
            .ForeColor = Color.FromArgb(95, 113, 131)
            .Text = "XAMPP: inicia MySQL/MariaDB antes de probar la conexión."
        End With

        ConfigurarBoton(_btnProbar, "Probar conexión", False)
        _btnProbar.Location = New Point(24, 282)
        _btnProbar.Size = New Size(136, 36)
        AddHandler _btnProbar.Click, AddressOf ProbarConexion

        ConfigurarBoton(_btnGuardar, "Guardar", True)
        _btnGuardar.Location = New Point(274, 282)
        _btnGuardar.Size = New Size(94, 36)
        AddHandler _btnGuardar.Click, AddressOf GuardarConexion

        ConfigurarBoton(_btnCancelar, "Cancelar", False)
        _btnCancelar.Location = New Point(374, 282)
        _btnCancelar.Size = New Size(82, 36)
        AddHandler _btnCancelar.Click, Sub(sender As Object, e As EventArgs) Close()

        AcceptButton = _btnGuardar
        CancelButton = _btnCancelar

        Controls.Add(encabezado)
        Controls.Add(tabla)
        Controls.Add(_lblEstado)
        Controls.Add(_btnProbar)
        Controls.Add(_btnGuardar)
        Controls.Add(_btnCancelar)

        ResumeLayout(True)
    End Sub

    Private Shared Sub AgregarCampo(tabla As TableLayoutPanel, fila As Integer, texto As String, control As Control)
        Dim etiqueta As New Label With {
            .Text = texto,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .ForeColor = Color.FromArgb(52, 66, 82)
        }

        control.Dock = DockStyle.Fill
        control.Margin = New Padding(0, 4, 0, 4)

        tabla.Controls.Add(etiqueta, 0, fila)
        tabla.Controls.Add(control, 1, fila)
    End Sub

    Private Shared Sub ConfigurarBoton(boton As Button, texto As String, primario As Boolean)
        boton.Text = texto
        boton.FlatStyle = FlatStyle.Flat
        boton.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        boton.Cursor = Cursors.Hand
        boton.UseVisualStyleBackColor = False

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
    End Sub

    Private Function ValidarCampos() As Boolean
        If String.IsNullOrWhiteSpace(_txtServidor.Text) Then
            MessageBox.Show(Me, "Indica el servidor de MySQL.", "Conexión a MySQL",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _txtServidor.Focus()
            Return False
        End If

        If String.IsNullOrWhiteSpace(_txtUsuario.Text) Then
            MessageBox.Show(Me, "Indica el usuario de MySQL.", "Conexión a MySQL",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _txtUsuario.Focus()
            Return False
        End If

        If String.IsNullOrWhiteSpace(_txtBaseDatos.Text) Then
            MessageBox.Show(Me, "Indica el nombre de la base de datos.", "Conexión a MySQL",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _txtBaseDatos.Focus()
            Return False
        End If

        Return True
    End Function

    Private Sub ProbarConexion(sender As Object, e As EventArgs)
        If Not ValidarCampos() Then Return

        Cursor = Cursors.WaitCursor
        Try
            Dim problema As String = Datos.ValidarConexion(
                _txtServidor.Text, CInt(_numPuerto.Value), _txtUsuario.Text,
                _txtContrasena.Text, _txtBaseDatos.Text)

            If problema.Length = 0 Then
                _lblEstado.ForeColor = Color.FromArgb(23, 105, 74)
                _lblEstado.Text = "Conexión correcta. La base de datos está disponible."
            Else
                _lblEstado.ForeColor = Color.FromArgb(170, 45, 45)
                _lblEstado.Text = "No se pudo conectar con MySQL."
                MessageBox.Show(Me, problema, "Conexión a MySQL",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub GuardarConexion(sender As Object, e As EventArgs)
        If Not ValidarCampos() Then Return

        Cursor = Cursors.WaitCursor
        Try
            Dim problema As String = Datos.ValidarConexion(
                _txtServidor.Text, CInt(_numPuerto.Value), _txtUsuario.Text,
                _txtContrasena.Text, _txtBaseDatos.Text)

            If problema.Length > 0 Then
                _lblEstado.ForeColor = Color.FromArgb(170, 45, 45)
                _lblEstado.Text = "No se pudo conectar con MySQL."
                MessageBox.Show(Me, problema, "Conexión a MySQL",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Configuracion.Configurar(
                _txtServidor.Text, CInt(_numPuerto.Value), _txtUsuario.Text,
                _txtContrasena.Text, _txtBaseDatos.Text)

            DialogResult = DialogResult.OK
            Close()
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

End Class
