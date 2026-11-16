Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>Funciones de apoyo: normalización de texto, cédulas, trayectos y similitud.</summary>
Friend NotInheritable Class Utilidades

    Private Sub New()
    End Sub

    ''' <summary>Mayúsculas, sin acentos y con espacios simples. Base de todas las comparaciones.</summary>
    Public Shared Function Normalizar(valor As String) As String
        If String.IsNullOrWhiteSpace(valor) Then Return String.Empty
        Dim descompuesto As String = valor.Replace(ChrW(&HFEFF), "").Normalize(NormalizationForm.FormD)
        Dim sb As New StringBuilder(descompuesto.Length)
        For Each c As Char In descompuesto
            If CharUnicodeInfo.GetUnicodeCategory(c) <> UnicodeCategory.NonSpacingMark Then sb.Append(c)
        Next
        Return Regex.Replace(sb.ToString().ToUpperInvariant(), "\s+", " ").Trim()
    End Function

    ''' <summary>Deja solo los dígitos de una cédula u otro documento.</summary>
    Public Shared Function SoloDigitos(valor As String) As String
        If String.IsNullOrWhiteSpace(valor) Then Return String.Empty
        Return New String(valor.Where(Function(c) Char.IsDigit(c)).ToArray())
    End Function

    ''' <summary>Cédula normalizada con formato V-12345678.</summary>
    Public Shared Function FormatearCedula(valor As String) As String
        Dim digitos As String = SoloDigitos(valor)
        If digitos.Length = 0 Then Return (valor & "").Trim().ToUpperInvariant()
        Return "V-" & digitos
    End Function

    ''' <summary>Clave de comparación de cédulas: solo dígitos.</summary>
    Public Shared Function ClaveCedula(valor As String) As String
        Return SoloDigitos(valor)
    End Function

    Public Shared Function NumeroRomano(numero As Integer) As String
        Select Case numero
            Case 1 : Return "I"
            Case 2 : Return "II"
            Case 3 : Return "III"
            Case 4 : Return "IV"
            Case 5 : Return "V"
            Case 6 : Return "VI"
            Case 7 : Return "VII"
            Case 8 : Return "VIII"
            Case 9 : Return "IX"
            Case 10 : Return "X"
            Case Else : Return numero.ToString(CultureInfo.InvariantCulture)
        End Select
    End Function

    Public Shared Function TrayectoTexto(numero As Integer) As String
        If numero <= 0 Then Return "Sin trayecto"
        Return "Trayecto " & NumeroRomano(numero)
    End Function

    ''' <summary>Similitud 0..1 entre dos textos (1 = idénticos).</summary>
    Public Shared Function Similitud(a As String, b As String) As Double
        Dim x As String = Normalizar(a)
        Dim y As String = Normalizar(b)
        If x.Length = 0 AndAlso y.Length = 0 Then Return 1.0
        Dim maximo As Integer = Math.Max(x.Length, y.Length)
        If maximo = 0 Then Return 1.0
        Return 1.0 - (Levenshtein(x, y) / CDbl(maximo))
    End Function

    Private Shared Function Levenshtein(a As String, b As String) As Integer
        Dim n As Integer = a.Length
        Dim m As Integer = b.Length
        Dim anterior(m) As Integer
        Dim actual(m) As Integer
        For j As Integer = 0 To m
            anterior(j) = j
        Next
        For i As Integer = 1 To n
            actual(0) = i
            For j As Integer = 1 To m
                Dim costo As Integer = If(a(i - 1) = b(j - 1), 0, 1)
                actual(j) = Math.Min(Math.Min(actual(j - 1) + 1, anterior(j) + 1), anterior(j - 1) + costo)
            Next
            Dim temporal() As Integer = anterior
            anterior = actual
            actual = temporal
        Next
        Return anterior(m)
    End Function

    ''' <summary>Compara nombres: iguales o muy parecidos (tolera errores de digitación).</summary>
    Public Shared Function NombresEquivalentes(a As String, b As String) As Boolean
        If Normalizar(a) = Normalizar(b) Then Return True
        Return Similitud(a, b) >= 0.88
    End Function

    ''' <summary>Compara carreras tolerando "PNF Informática" vs "INFORMATICA".</summary>
    Public Shared Function CarrerasEquivalentes(a As String, b As String) As Boolean
        Dim x As String = Normalizar(a)
        Dim y As String = Normalizar(b)
        If x.Length = 0 OrElse y.Length = 0 Then Return False
        If x = y Then Return True
        If x.Contains(y) OrElse y.Contains(x) Then Return True
        Dim palabrasX = x.Split(" "c)
        Dim palabrasY = y.Split(" "c)
        Dim comunes As Integer = palabrasX.Intersect(palabrasY).Count(Function(p) p.Length >= 4)
        Return comunes >= 1
    End Function

    ''' <summary>Hash SHA-256 del archivo, para no cargar dos veces el mismo listado.</summary>
    Public Shared Function HashArchivo(ruta As String) As String
        Using flujo As IO.FileStream = IO.File.OpenRead(ruta)
            Dim hash = SHA256.HashData(flujo)
            Return Convert.ToHexString(hash).ToLowerInvariant()
        End Using
    End Function

End Class
