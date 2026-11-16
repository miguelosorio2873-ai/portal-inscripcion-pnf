Imports System.Globalization

''' <summary>Funciones de formato compartidas por la interfaz y la impresión.</summary>
Public Module Utilidades

    Public Function FormatearFecha(valor As String, Optional incluirHora As Boolean = True) As String
        If String.IsNullOrWhiteSpace(valor) Then Return "—"

        Dim fecha As DateTime
        Dim formatos As String() = {"yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss"}
        Dim valida As Boolean = DateTime.TryParseExact(
            valor, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, fecha)

        If Not valida Then
            valida = DateTime.TryParse(valor, CultureInfo.CurrentCulture, DateTimeStyles.None, fecha)
        End If

        If Not valida Then Return valor
        Return If(incluirHora, fecha.ToString("dd/MM/yyyy HH:mm"), fecha.ToString("dd/MM/yyyy"))
    End Function

    Public Function FechaLarga(valor As String) As String
        If String.IsNullOrWhiteSpace(valor) Then Return "—"

        Dim fecha As DateTime
        Dim formatos As String() = {"yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss"}
        If Not DateTime.TryParseExact(valor, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, fecha) Then
            If Not DateTime.TryParse(valor, CultureInfo.CurrentCulture, DateTimeStyles.None, fecha) Then
                Return valor
            End If
        End If

        Return fecha.ToString("d 'de' MMMM 'de' yyyy", New CultureInfo("es-ES"))
    End Function

    Public Function NumeroRomano(numero As Integer) As String
        Select Case numero
            Case 1 : Return "I"
            Case 2 : Return "II"
            Case 3 : Return "III"
            Case 4 : Return "IV"
            Case 5 : Return "V"
            Case Else : Return numero.ToString(CultureInfo.InvariantCulture)
        End Select
    End Function

    ''' <summary>Etiqueta legible de la condición del estudiante (REGULAR / FLOTANTE).</summary>
    Public Function CondicionTexto(valor As String) As String
        Select Case If(valor, String.Empty).Trim().ToUpperInvariant()
            Case "FLOTANTE" : Return "Población flotante"
            Case Else : Return "Alumno regular"
        End Select
    End Function

    Public Function TextoONulo(valor As String, Optional reemplazo As String = "—") As String
        Return If(String.IsNullOrWhiteSpace(valor), reemplazo, valor)
    End Function

End Module
