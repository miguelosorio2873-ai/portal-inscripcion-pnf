Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Linq
Imports InscripcionOPSU.Modelos

''' <summary>
''' Compara el archivo oficial de la OPSU con los estudiantes registrados
''' en el sistema y clasifica cada caso.
''' </summary>
Friend NotInheritable Class OpsuComparador

    Private Sub New()
    End Sub

    Public Shared Function Comparar(registros As List(Of OpsuRegistro), estudiantes As List(Of Estudiante)) As List(Of Comparacion)
        Dim comparaciones As New List(Of Comparacion)()
        If registros Is Nothing Then Return comparaciones
        If estudiantes Is Nothing Then estudiantes = New List(Of Estudiante)()

        Dim indice As New Dictionary(Of String, Estudiante)()
        For Each estudiante As Estudiante In estudiantes
            Dim clave As String = Utilidades.ClaveCedula(estudiante.Cedula)
            If clave.Length > 0 AndAlso Not indice.ContainsKey(clave) Then indice(clave) = estudiante
        Next

        Dim vistas As New HashSet(Of String)()

        For Each registro As OpsuRegistro In registros
            Dim clave As String = Utilidades.ClaveCedula(registro.Cedula)
            If clave.Length = 0 Then Continue For
            If vistas.Contains(clave) Then Continue For   ' cédulas repetidas en el archivo
            vistas.Add(clave)

            Dim comparacion As New Comparacion With {
                .Cedula = Utilidades.FormatearCedula(registro.Cedula),
                .NombreOpsu = registro.NombreCompleto,
                .CarreraOpsu = registro.Carrera
            }

            Dim estudiante As Estudiante = Nothing
            If indice.TryGetValue(clave, estudiante) Then
                comparacion.EstudianteId = estudiante.Id
                comparacion.NombreSistema = estudiante.NombreCompleto
                comparacion.CarreraSistema = estudiante.Carrera
                comparacion.Resultado = Clasificar(estudiante, registro, comparacion.Diferencias)
            Else
                comparacion.Resultado = ResultadoComparacion.NO_REGISTRADO_EN_SISTEMA
                comparacion.Diferencias = "La cédula aparece en OPSU pero no está registrada en el sistema."
            End If

            comparaciones.Add(comparacion)
        Next

        ' Estudiantes que el sistema tiene como OPSU y que no aparecen en el archivo.
        For Each estudiante As Estudiante In estudiantes
            If estudiante.Modalidad <> TipoIngreso.OPSU Then Continue For
            Dim clave As String = Utilidades.ClaveCedula(estudiante.Cedula)
            If clave.Length = 0 OrElse vistas.Contains(clave) Then Continue For

            comparaciones.Add(New Comparacion With {
                .EstudianteId = estudiante.Id,
                .Cedula = estudiante.Cedula,
                .NombreSistema = estudiante.NombreCompleto,
                .CarreraSistema = estudiante.Carrera,
                .Resultado = ResultadoComparacion.NO_APARECE_EN_OPSU,
                .Diferencias = "Está registrado como OPSU pero no aparece en el archivo cargado."
            })
        Next

        Return comparaciones
    End Function

    Private Shared Function Clasificar(estudiante As Estudiante, registro As OpsuRegistro, ByRef diferencias As String) As ResultadoComparacion
        Dim problemas As New List(Of String)()

        Dim nombreOpsu As String = registro.NombreCompleto
        Dim nombreSistema As String = estudiante.NombreCompleto
        Dim nombreOk As Boolean = String.IsNullOrWhiteSpace(nombreOpsu) OrElse Utilidades.NombresEquivalentes(nombreOpsu, nombreSistema)
        If Not nombreOk Then
            problemas.Add("Nombre distinto (OPSU: " & nombreOpsu & " / sistema: " & nombreSistema & ")")
        End If

        Dim carreraOk As Boolean = True
        If Not String.IsNullOrWhiteSpace(registro.Carrera) AndAlso Not String.IsNullOrWhiteSpace(estudiante.Carrera) Then
            carreraOk = Utilidades.CarrerasEquivalentes(registro.Carrera, estudiante.Carrera)
            If Not carreraOk Then
                problemas.Add("Carrera distinta (OPSU: " & registro.Carrera & " / sistema: " & estudiante.Carrera & ")")
            End If
        End If

        If registro.Trayecto > 0 AndAlso registro.Trayecto > 5 Then
            problemas.Add("Trayecto fuera de rango: " & registro.Trayecto.ToString())
        End If

        diferencias = String.Join("; ", problemas)

        If Not carreraOk Then Return ResultadoComparacion.CARRERA_DISTINTA
        If Not nombreOk Then Return ResultadoComparacion.DATOS_DISTINTOS
        If problemas.Count > 0 Then Return ResultadoComparacion.DATOS_DISTINTOS
        Return ResultadoComparacion.COINCIDE
    End Function

    ''' <summary>Cuenta los resultados para mostrarlos en el resumen.</summary>
    Public Shared Function Resumen(comparaciones As List(Of Comparacion)) As Dictionary(Of ResultadoComparacion, Integer)
        Dim conteo As New Dictionary(Of ResultadoComparacion, Integer)()
        For Each valor As ResultadoComparacion In [Enum].GetValues(Of ResultadoComparacion)()
            conteo(valor) = 0
        Next
        For Each comparacion As Comparacion In comparaciones
            conteo(comparacion.Resultado) += 1
        Next
        Return conteo
    End Function

End Class
