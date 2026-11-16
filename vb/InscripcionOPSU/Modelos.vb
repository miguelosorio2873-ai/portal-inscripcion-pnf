Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Globalization

''' <summary>
''' Modelos de datos del sistema de inscripción universitaria.
''' No existe el concepto de "turno": toda la organización es por TRAYECTO.
''' </summary>
Namespace Modelos

    ''' <summary>Modalidad de ingreso del estudiante.</summary>
    Public Enum TipoIngreso As Integer
        OPSU = 1
        FLOTANTE = 2
    End Enum

    ''' <summary>Resultado de comparar un registro contra el archivo oficial de la OPSU.</summary>
    Public Enum ResultadoComparacion As Integer
        COINCIDE = 1
        NO_APARECE_EN_OPSU = 2
        NO_REGISTRADO_EN_SISTEMA = 3
        DATOS_DISTINTOS = 4
        CARRERA_DISTINTA = 5
    End Enum

    ''' <summary>Opción genérica para ComboBox (valor interno + texto visible).</summary>
    Public Class Opcion
        Public Property Valor As String = ""
        Public Property Texto As String = ""

        Public Sub New()
        End Sub

        Public Sub New(valor As String, texto As String)
            Me.Valor = valor
            Me.Texto = texto
        End Sub

        Public Overrides Function ToString() As String
            Return Texto
        End Function
    End Class

    Public Class Carrera
        Public Property Id As Integer
        Public Property Codigo As String = ""
        Public Property Nombre As String = ""

        Public Overrides Function ToString() As String
            Return Nombre
        End Function
    End Class

    Public Class Estudiante
        Public Property Id As Integer
        Public Property Cedula As String = ""
        Public Property Nombres As String = ""
        Public Property Apellidos As String = ""
        Public Property Sexo As String = ""
        Public Property FechaNacimiento As Date?
        Public Property Email As String = ""
        Public Property Telefono As String = ""
        Public Property Direccion As String = ""
        Public Property Modalidad As TipoIngreso = TipoIngreso.FLOTANTE
        Public Property OpsuEstatus As String = "NO_VERIFICADO"
        ''' <summary>Carrera de su inscripción más reciente (solo lectura desde la base).</summary>
        Public Property Carrera As String = ""

        Public ReadOnly Property NombreCompleto As String
            Get
                Return (Nombres & " " & Apellidos).Trim()
            End Get
        End Property

        Public ReadOnly Property ModalidadTexto As String
            Get
                Return If(Modalidad = TipoIngreso.OPSU, "OPSU", "Flotante")
            End Get
        End Property

        Public ReadOnly Property OpsuEstatusTexto As String
            Get
                Select Case OpsuEstatus
                    Case "COINCIDE" : Return "Coincide con OPSU"
                    Case "NO_APARECE" : Return "No aparece en OPSU"
                    Case "DATOS_DISTINTOS" : Return "Datos distintos"
                    Case "CARRERA_DISTINTA" : Return "Carrera distinta"
                    Case "NO_REGISTRADO" : Return "No registrado"
                    Case Else : Return "Sin verificar"
                End Select
            End Get
        End Property
    End Class

    Public Class Inscripcion
        Public Property Id As Integer
        Public Property Codigo As String = ""
        Public Property EstudianteId As Integer
        Public Property Cedula As String = ""
        Public Property Estudiante As String = ""
        Public Property CarreraId As Integer
        Public Property Carrera As String = ""
        Public Property Trayecto As Integer
        Public Property Modalidad As TipoIngreso
        Public Property Estatus As String = "REGISTRADA"
        Public Property Fecha As Date
        Public Property Observaciones As String = ""
        Public Property Impresiones As Integer
        Public Property ImpresoEn As Date?
        Public Property TotalAsignaturas As Integer

        Public ReadOnly Property TrayectoTexto As String
            Get
                Return Utilidades.TrayectoTexto(Trayecto)
            End Get
        End Property

        Public ReadOnly Property ModalidadTexto As String
            Get
                Return If(Modalidad = TipoIngreso.OPSU, "OPSU", "Flotante")
            End Get
        End Property

        Public ReadOnly Property FechaTexto As String
            Get
                Return If(Fecha = Date.MinValue, "", Fecha.ToString("dd/MM/yyyy"))
            End Get
        End Property
    End Class

    ''' <summary>Asignatura concreta asociada a una inscripción.</summary>
    Public Class AsignaturaInscrita
        Public Property Codigo As String = ""
        Public Property Nombre As String = ""
        Public Property Trayecto As Integer
    End Class

    ''' <summary>Datos completos para imprimir el comprobante de inscripción.</summary>
    Public Class InscripcionCompleta
        Public Property InscripcionId As Integer
        Public Property Codigo As String = ""
        Public Property Cedula As String = ""
        Public Property Nombres As String = ""
        Public Property Apellidos As String = ""
        Public Property Email As String = ""
        Public Property Telefono As String = ""
        Public Property FechaNacimiento As Date?
        Public Property Direccion As String = ""
        Public Property Carrera As String = ""
        Public Property Trayecto As Integer
        Public Property TipoIngreso As TipoIngreso
        Public Property FechaInscripcion As Date
        Public Property Estado As String = "REGISTRADA"
        Public Property Observaciones As String = ""
        Public Property Impresiones As Integer
        Public Property ImpresoEn As Date?
        Public Property Asignaturas As New List(Of AsignaturaInscrita)()

        Public ReadOnly Property NombreCompleto As String
            Get
                Return (Nombres & " " & Apellidos).Trim()
            End Get
        End Property

        Public ReadOnly Property TrayectoTexto As String
            Get
                Return Utilidades.TrayectoTexto(Trayecto)
            End Get
        End Property

        Public ReadOnly Property TipoIngresoTexto As String
            Get
                Return If(TipoIngreso = TipoIngreso.OPSU, "OPSU", "Flotante")
            End Get
        End Property

        Public ReadOnly Property FechaLargaTexto As String
            Get
                If FechaInscripcion = Date.MinValue Then Return ""
                Return FechaInscripcion.ToString("dd 'de' MMMM 'de' yyyy", New CultureInfo("es-VE"))
            End Get
        End Property

        Public ReadOnly Property FechaTexto As String
            Get
                If FechaInscripcion = Date.MinValue Then Return ""
                Return FechaInscripcion.ToString("dd/MM/yyyy HH:mm")
            End Get
        End Property
    End Class

    ''' <summary>Una fila leída del archivo oficial de la OPSU (CSV o Excel).</summary>
    Public Class OpsuRegistro
        Public Property Fila As Integer
        Public Property Cedula As String = ""
        Public Property Nombres As String = ""
        Public Property Apellidos As String = ""
        Public Property NombreArchivo As String = ""
        Public Property Sexo As String = ""
        Public Property Carrera As String = ""
        Public Property Trayecto As Integer
        Public Property Estatus As String = ""

        Public ReadOnly Property NombreCompleto As String
            Get
                If Not String.IsNullOrWhiteSpace(NombreArchivo) Then Return NombreArchivo.Trim()
                Return (Nombres & " " & Apellidos).Trim()
            End Get
        End Property
    End Class

    ''' <summary>Resultado de comparar un estudiante con el archivo OPSU.</summary>
    Public Class Comparacion
        Public Property EstudianteId As Integer
        Public Property Cedula As String = ""
        Public Property NombreOpsu As String = ""
        Public Property NombreSistema As String = ""
        Public Property CarreraOpsu As String = ""
        Public Property CarreraSistema As String = ""
        Public Property Resultado As ResultadoComparacion
        Public Property Diferencias As String = ""

        Public ReadOnly Property ResultadoTexto As String
            Get
                Select Case Resultado
                    Case ResultadoComparacion.COINCIDE : Return "Coincide"
                    Case ResultadoComparacion.NO_APARECE_EN_OPSU : Return "No aparece en OPSU"
                    Case ResultadoComparacion.NO_REGISTRADO_EN_SISTEMA : Return "No registrado en el sistema"
                    Case ResultadoComparacion.DATOS_DISTINTOS : Return "Datos distintos"
                    Case ResultadoComparacion.CARRERA_DISTINTA : Return "Carrera distinta"
                    Case Else : Return "Sin resultado"
                End Select
            End Get
        End Property

        Public ReadOnly Property NombreMostrar As String
            Get
                If Not String.IsNullOrWhiteSpace(NombreSistema) Then Return NombreSistema
                Return NombreOpsu
            End Get
        End Property
    End Class

End Namespace
