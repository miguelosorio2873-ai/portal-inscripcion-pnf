''' <summary>Fila del listado principal de inscripciones.</summary>
Public Class InscripcionResumen
    Public Property Id As Integer
    Public Property Codigo As String = String.Empty
    Public Property Cedula As String = String.Empty
    Public Property Estudiante As String = String.Empty
    Public Property Carrera As String = String.Empty
    Public Property Semestre As Integer
    Public Property Turno As String = String.Empty
    Public Property FechaInscripcion As String = String.Empty
    Public Property Estado As String = String.Empty
    Public Property Impresiones As Integer

    Public ReadOnly Property FechaTexto As String
        Get
            Return Utilidades.FormatearFecha(FechaInscripcion)
        End Get
    End Property

    Public ReadOnly Property SemestreTexto As String
        Get
            Return Utilidades.NumeroRomano(Semestre)
        End Get
    End Property
End Class

''' <summary>Asignatura incluida en una inscripción.</summary>
Public Class AsignaturaInscrita
    Public Property Codigo As String = String.Empty
    Public Property Nombre As String = String.Empty
    Public Property Semestre As Integer
End Class

''' <summary>Datos completos de una inscripción, usados en el detalle y el comprobante.</summary>
Public Class InscripcionCompleta
    Public Property Id As Integer
    Public Property Codigo As String = String.Empty
    Public Property Cedula As String = String.Empty
    Public Property Nombres As String = String.Empty
    Public Property Apellidos As String = String.Empty
    Public Property Email As String = String.Empty
    Public Property Telefono As String = String.Empty
    Public Property FechaNacimiento As String = String.Empty
    Public Property Direccion As String = String.Empty
    Public Property Carrera As String = String.Empty
    Public Property CarreraCodigo As String = String.Empty
    Public Property Semestre As Integer
    Public Property Turno As String = String.Empty
    Public Property FechaInscripcion As String = String.Empty
    Public Property Estado As String = String.Empty
    Public Property Observaciones As String = String.Empty
    Public Property ImpresoEn As String = String.Empty
    Public Property Impresiones As Integer
    Public Property Asignaturas As New List(Of AsignaturaInscrita)()

    Public ReadOnly Property NombreCompleto As String
        Get
            Return (Nombres & " " & Apellidos).Trim()
        End Get
    End Property

    Public ReadOnly Property SemestreTexto As String
        Get
            Return Utilidades.NumeroRomano(Semestre)
        End Get
    End Property

    Public ReadOnly Property FechaLargaTexto As String
        Get
            Return Utilidades.FechaLarga(FechaInscripcion)
        End Get
    End Property
End Class
