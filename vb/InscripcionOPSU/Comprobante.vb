Option Strict On
Option Explicit On

Imports System.Windows.Forms
Imports InscripcionOPSU.Modelos

''' <summary>
''' Punto único de acceso al comprobante de inscripción: carga los datos desde
''' la base compartida, abre la vista previa e imprime registrando la salida.
''' </summary>
Friend NotInheritable Class Comprobante

    Private Sub New()
    End Sub

    ''' <summary>Lee el comprobante completo por su código. Devuelve Nothing si no existe.</summary>
    Public Shared Function Cargar(codigo As String) As InscripcionCompleta
        If String.IsNullOrWhiteSpace(codigo) Then Return Nothing
        Try
            Return Datos.ObtenerInscripcionCompleta(codigo.Trim())
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>Abre la vista previa del comprobante (permite imprimir desde la ventana).</summary>
    Public Shared Function Mostrar(codigo As String) As Boolean
        Dim datos As InscripcionCompleta = Cargar(codigo)
        If datos Is Nothing Then
            MessageBox.Show(
                "No se encontró el comprobante " & codigo & " en la base de datos." & Environment.NewLine &
                "Verifique el código e inténtelo de nuevo.",
                "Comprobante de inscripción", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If

        If String.Equals(datos.Estado, "ANULADA", StringComparison.OrdinalIgnoreCase) Then
            MessageBox.Show(
                "La inscripción " & datos.Codigo & " está ANULADA." & Environment.NewLine &
                "Puede consultarla, pero no debe entregarse al estudiante.",
                "Comprobante de inscripción", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If

        Using vista As New FormComprobante(datos)
            vista.ShowDialog()
        End Using

        Return True
    End Function

    ''' <summary>
    ''' Impresión solicitada desde el portal: abre la vista previa e imprime
    ''' directamente en la impresora predeterminada, registrando la impresión.
    ''' Devuelve 0 si el comprobante existe; 1 si no se encontró.
    ''' </summary>
    Public Shared Function ImprimirDirecto(codigo As String) As Integer
        Dim datos As InscripcionCompleta = Cargar(codigo)
        If datos Is Nothing Then
            MessageBox.Show(
                "No se encontró el comprobante " & codigo & " en la base de datos." & Environment.NewLine &
                "Abra el portal, verifique el código y vuelva a intentarlo.",
                "Impresión · Control de Estudios", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return 1
        End If

        If String.Equals(datos.Estado, "ANULADA", StringComparison.OrdinalIgnoreCase) Then
            MessageBox.Show(
                "La inscripción " & datos.Codigo & " está ANULADA y no se imprimirá." & Environment.NewLine &
                "Consulte con Control de Estudios.",
                "Impresión · Control de Estudios", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return 1
        End If

        Using vista As New FormComprobante(datos, True)
            vista.ShowDialog()
        End Using

        Return 0
    End Function

End Class
