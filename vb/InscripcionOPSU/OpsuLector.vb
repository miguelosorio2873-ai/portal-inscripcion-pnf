Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports ExcelDataReader
Imports InscripcionOPSU.Modelos

''' <summary>
''' Lectura del archivo oficial de la OPSU en formato CSV o Excel (.xlsx / .xls).
''' Detecta el separador, la codificación y los encabezados por sinónimos,
''' de modo que sirve para los listados que entrega la OPSU sin retocarlos.
''' </summary>
Friend NotInheritable Class OpsuLector

    Private Sub New()
    End Sub

    Shared Sub New()
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    End Sub

    ''' <summary>Sinónimos aceptados para cada campo (siempre normalizados).</summary>
    Private Shared ReadOnly Sinonimos As New Dictionary(Of String, String()) From {
        {"cedula", New String() {"CEDULA", "C I", "CI", "DOCUMENTO", "IDENTIFICACION", "CEDULA DE IDENTIDAD",
                                 "NRO CEDULA", "NUMERO DE CEDULA", "CEDULA ESTUDIANTE", "DOCUMENTO DE IDENTIDAD"}},
        {"nombres", New String() {"NOMBRES", "NOMBRE", "NOMBRE ESTUDIANTE", "NOMBRES ESTUDIANTE", "PRIMER NOMBRE"}},
        {"apellidos", New String() {"APELLIDOS", "APELLIDO", "APELLIDOS ESTUDIANTE", "PRIMER APELLIDO"}},
        {"nombre_completo", New String() {"NOMBRE COMPLETO", "NOMBRES Y APELLIDOS", "APELLIDOS Y NOMBRES", "ESTUDIANTE"}},
        {"sexo", New String() {"SEXO", "GENERO"}},
        {"carrera", New String() {"CARRERA", "CARRERA ASIGNADA", "PNF", "PROGRAMA", "PROGRAMA NACIONAL DE FORMACION",
                                  "OPCION", "CARRERA OPSU"}},
        {"trayecto", New String() {"TRAYECTO", "SEMESTRE", "NIVEL", "TRAYECTO ACADEMICO"}},
        {"estatus", New String() {"ESTATUS", "ESTADO", "CONDICION", "SITUACION", "ESTATUS OPSU"}}
    }

    ''' <summary>Lee el archivo y devuelve los registros válidos (con cédula).</summary>
    Public Shared Function Leer(ruta As String) As List(Of OpsuRegistro)
        If String.IsNullOrWhiteSpace(ruta) Then Throw New ArgumentException("Debe indicar la ruta del archivo OPSU.")
        If Not File.Exists(ruta) Then Throw New FileNotFoundException("No se encontró el archivo indicado.", ruta)

        Dim extension As String = Path.GetExtension(ruta).ToLowerInvariant()
        Dim filas As List(Of String())

        Select Case extension
            Case ".csv", ".txt"
                filas = LeerCsv(ruta)
            Case ".xlsx", ".xls"
                filas = LeerExcel(ruta)
            Case Else
                Throw New NotSupportedException("Formato no soportado: " & extension & ". Use CSV, XLSX o XLS.")
        End Select

        Return Interpretar(filas)
    End Function

    ' ------------------------------------------------------------------ CSV

    Private Shared Function LeerCsv(ruta As String) As List(Of String())
        Dim texto As String = LeerTexto(ruta)
        Dim lineas As String() = texto.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split(ChrW(10))
        Dim separador As Char = DetectarSeparador(lineas)
        Dim filas As New List(Of String)()
        Dim resultado As New List(Of String())

        For Each linea As String In lineas
            If linea.Trim().Length = 0 Then Continue For
            resultado.Add(DividirLinea(linea, separador))
        Next
        Return resultado
    End Function

    Private Shared Function LeerTexto(ruta As String) As String
        Dim bytes As Byte() = File.ReadAllBytes(ruta)
        Dim texto As String
        Try
            Dim utf8 As New UTF8Encoding(False, True)
            texto = utf8.GetString(bytes)
        Catch ex As DecoderFallbackException
            texto = Encoding.GetEncoding(1252).GetString(bytes)
        End Try
        ' Quita la marca de orden de bytes (BOM) si el archivo la trae.
        If texto.Length > 0 AndAlso texto(0) = ChrW(&HFEFF) Then texto = texto.Substring(1)
        Return texto
    End Function

    Private Shared Function DetectarSeparador(lineas As String()) As Char
        Dim candidatos As Char() = {";"c, ","c, ControlChars.Tab}
        Dim mejor As Char = ";"c
        Dim maximo As Integer = -1
        For Each candidato As Char In candidatos
            Dim conteo As Integer = 0
            For Each linea As String In lineas
                If linea.Trim().Length = 0 Then Continue For
                conteo = linea.Count(Function(c) c = candidato)
                Exit For
            Next
            If conteo > maximo Then
                maximo = conteo
                mejor = candidato
            End If
        Next
        Return mejor
    End Function

    ''' <summary>Divide una línea CSV respetando comillas dobles.</summary>
    Private Shared Function DividirLinea(linea As String, separador As Char) As String()
        Dim campos As New List(Of String)()
        Dim actual As New StringBuilder()
        Dim entreComillas As Boolean = False

        For i As Integer = 0 To linea.Length - 1
            Dim c As Char = linea(i)
            If c = """"c Then
                If entreComillas AndAlso i + 1 < linea.Length AndAlso linea(i + 1) = """"c Then
                    actual.Append(""""c)
                    i += 1
                Else
                    entreComillas = Not entreComillas
                End If
            ElseIf c = separador AndAlso Not entreComillas Then
                campos.Add(actual.ToString().Trim())
                actual.Clear()
            Else
                actual.Append(c)
            End If
        Next
        campos.Add(actual.ToString().Trim())
        Return campos.ToArray()
    End Function

    ' ------------------------------------------------------------------ Excel

    Private Shared Function LeerExcel(ruta As String) As List(Of String())
        Dim resultado As New List(Of String())
        Using flujo As FileStream = File.Open(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Using lector As IExcelDataReader = ExcelReaderFactory.CreateReader(flujo)
                Do
                    While lector.Read()
                        Dim valores As New List(Of String)()
                        For i As Integer = 0 To lector.FieldCount - 1
                            If lector.IsDBNull(i) Then
                                valores.Add("")
                            Else
                                valores.Add(Convert.ToString(lector.GetValue(i), CultureInfo.InvariantCulture))
                            End If
                        Next
                        resultado.Add(valores.ToArray())
                    End While
                Loop While lector.NextResult()
            End Using
        End Using
        Return resultado
    End Function

    ' ------------------------------------------------------------------ interpretación

    Private Shared Function Interpretar(filas As List(Of String())) As List(Of OpsuRegistro)
        Dim registros As New List(Of OpsuRegistro)()
        If filas.Count = 0 Then Return registros

        Dim indiceEncabezado As Integer = -1
        Dim mapa As Dictionary(Of String, Integer) = Nothing

        Dim limite As Integer = Math.Min(filas.Count, 20)
        For i As Integer = 0 To limite - 1
            Dim posible As Dictionary(Of String, Integer) = ConstruirMapa(filas(i))
            If posible.ContainsKey("cedula") Then
                indiceEncabezado = i
                mapa = posible
                Exit For
            End If
        Next

        If mapa Is Nothing Then
            Throw New InvalidDataException("No se encontró la columna de cédula. Verifique el archivo de la OPSU.")
        End If

        For i As Integer = indiceEncabezado + 1 To filas.Count - 1
            Dim fila As String() = filas(i)
            Dim cedula As String = Valor(fila, mapa, "cedula")
            If Utilidades.SoloDigitos(cedula).Length < 4 Then Continue For

            Dim registro As New OpsuRegistro()
            registro.Fila = i + 1
            registro.Cedula = Utilidades.FormatearCedula(cedula)
            registro.Nombres = Valor(fila, mapa, "nombres")
            registro.Apellidos = Valor(fila, mapa, "apellidos")
            registro.NombreArchivo = Valor(fila, mapa, "nombre_completo")
            registro.Sexo = NormalizarSexo(Valor(fila, mapa, "sexo"))
            registro.Carrera = Valor(fila, mapa, "carrera")
            registro.Trayecto = ParsearTrayecto(Valor(fila, mapa, "trayecto"))
            registro.Estatus = Valor(fila, mapa, "estatus")

            If String.IsNullOrWhiteSpace(registro.Nombres) AndAlso String.IsNullOrWhiteSpace(registro.Apellidos) _
               AndAlso String.IsNullOrWhiteSpace(registro.NombreArchivo) Then
                Continue For
            End If

            registros.Add(registro)
        Next

        Return registros
    End Function

    Private Shared Function ConstruirMapa(fila As String()) As Dictionary(Of String, Integer)
        Dim mapa As New Dictionary(Of String, Integer)()
        If fila Is Nothing Then Return mapa

        For columna As Integer = 0 To fila.Length - 1
            Dim titulo As String = Utilidades.Normalizar(fila(columna))
            If titulo.Length = 0 Then Continue For

            For Each campo As KeyValuePair(Of String, String()) In Sinonimos
                If mapa.ContainsKey(campo.Key) Then Continue For
                If campo.Value.Any(Function(sinonimo) String.Equals(Utilidades.Normalizar(sinonimo), titulo, StringComparison.Ordinal)) Then
                    mapa(campo.Key) = columna
                    Exit For
                End If
            Next
        Next

        ' Si viene "Nombre completo" no hacen falta nombres y apellidos por separado.
        If mapa.ContainsKey("nombre_completo") Then
            If Not mapa.ContainsKey("nombres") Then mapa("nombres") = mapa("nombre_completo")
            If Not mapa.ContainsKey("apellidos") Then mapa("apellidos") = mapa("nombre_completo")
        End If

        Return mapa
    End Function

    Private Shared Function Valor(fila As String(), mapa As Dictionary(Of String, Integer), campo As String) As String
        Dim indice As Integer
        If Not mapa.TryGetValue(campo, indice) Then Return String.Empty
        If indice < 0 OrElse indice >= fila.Length Then Return String.Empty
        Return (fila(indice) & "").Trim()
    End Function

    Private Shared Function NormalizarSexo(valor As String) As String
        Select Case Utilidades.Normalizar(valor)
            Case "M", "MASCULINO", "HOMBRE" : Return "M"
            Case "F", "FEMENINO", "MUJER" : Return "F"
            Case Else : Return ""
        End Select
    End Function

    Private Shared Function ParsearTrayecto(valor As String) As Integer
        Dim digitos As String = Utilidades.SoloDigitos(valor)
        Dim numero As Integer
        If digitos.Length > 0 AndAlso Integer.TryParse(digitos, numero) AndAlso numero >= 1 AndAlso numero <= 10 Then
            Return numero
        End If

        Dim normalizado As String = Utilidades.Normalizar(valor)
        Dim romanos As String() = {"I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X"}
        For i As Integer = 0 To romanos.Length - 1
            If normalizado = romanos(i) OrElse normalizado.EndsWith(" " & romanos(i)) Then Return i + 1
        Next
        Return 0
    End Function

End Class
