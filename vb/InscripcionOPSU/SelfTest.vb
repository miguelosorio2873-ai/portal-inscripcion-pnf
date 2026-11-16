Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports MySql.Data.MySqlClient
Imports InscripcionOPSU.Modelos

''' <summary>
''' Autoprueba sin interfaz: crea una base temporal, registra estudiantes,
''' carga un CSV de la OPSU, compara, aplica resultados y genera un reporte.
''' No toca la base real de trabajo.
''' </summary>
Friend NotInheritable Class SelfTest

    Private Sub New()
    End Sub

    Private Shared ReadOnly Log As New StringBuilder()
    Private Shared Correcto As Boolean = True

    Public Shared Function Ejecutar() As Integer
        Try
            Environment.SetEnvironmentVariable("OPSU_DB", "inscripcion_opsu_prueba")
            Configuracion.Recargar()

            Escribir("AUTOPRUEBA · Inscripción Universitaria OPSU")
            Escribir("Base de datos temporal: " & Configuracion.Descripcion())
            Escribir("")

            EliminarBase()
            Datos.AsegurarEsquema()
            Verificar(Datos.ListarCarreras().Count >= 4, "Catálogo de carreras creado")
            Verificar(Datos.ListarTrayectos().Count = 5, "Trayectos I a V creados")

            Dim periodo As Periodo = Datos.PeriodoActivo()
            Verificar(periodo.Id > 0, "Periodo académico activo: " & periodo.Nombre)

            Dim carreras As List(Of Carrera) = Datos.ListarCarreras()
            Dim informatica As Carrera = carreras.First(Function(c) c.Codigo = "INF")
            Dim veterinaria As Carrera = carreras.First(Function(c) c.Codigo = "VET")
            Dim administracion As Carrera = carreras.First(Function(c) c.Codigo = "ADM")

            ' ---- estudiantes de prueba ----
            CrearEstudiante("V-11111111", "Ana", "Pérez", TipoIngreso.OPSU, informatica, 1, periodo)
            CrearEstudiante("V-22222222", "Luis", "Gómez", TipoIngreso.FLOTANTE, veterinaria, 2, periodo)
            CrearEstudiante("V-33333333", "Carlos", "Ruiz", TipoIngreso.OPSU, informatica, 3, periodo)
            CrearEstudiante("V-44444444", "María", "Díaz", TipoIngreso.OPSU, informatica, 2, periodo)
            CrearEstudiante("V-55555555", "OPSU", "Fantasma", TipoIngreso.OPSU, administracion, 5, periodo)

            Dim total As Integer = Datos.ListarInscripciones(0, "", 0).Count
            Verificar(total = 5, "Se registraron 5 inscripciones (sin turnos, solo trayecto)")

            ' ---- archivo OPSU ----
            Dim rutaCsv As String = Path.Combine(Path.GetTempPath(), "opsu-prueba-selftest.csv")
            File.WriteAllText(rutaCsv,
                "CEDULA;NOMBRES;APELLIDOS;CARRERA;TRAYECTO;ESTATUS" & vbCrLf &
                "V-11111111;Ana;Pérez;PNF Informática;I;ASIGNADO" & vbCrLf &
                "V-33333333;Carlos;Ruiz;PNF Veterinaria;III;ASIGNADO" & vbCrLf &
                "V-44444444;Pedro;Díaz;PNF Informática;II;ASIGNADO" & vbCrLf &
                "V-66666666;Rosa;Nuevo;PNF Administración;V;ASIGNADO" & vbCrLf,
                New UTF8Encoding(True))

            Dim registros As List(Of OpsuRegistro) = OpsuLector.Leer(rutaCsv)
            Verificar(registros.Count = 4, "Lectura del CSV OPSU: 4 registros")
            Verificar(registros(0).Cedula = "V-11111111", "Cédula reconocida: " & registros(0).Cedula)
            Verificar(registros(1).Trayecto = 3, "Trayecto romano III convertido a 3")

            ' ---- comparación ----
            Dim comparaciones As List(Of Comparacion) = OpsuComparador.Comparar(registros, Datos.ListarEstudiantes())
            Verificar(comparaciones.Count = 5, "Comparaciones generadas: " & comparaciones.Count.ToString())

            Verificar(Resultado(comparaciones, "V-11111111") = ResultadoComparacion.COINCIDE, "Ana Pérez coincide con OPSU")
            Verificar(Resultado(comparaciones, "V-33333333") = ResultadoComparacion.CARRERA_DISTINTA, "Carlos Ruiz: carrera distinta detectada")
            Verificar(Resultado(comparaciones, "V-44444444") = ResultadoComparacion.DATOS_DISTINTOS, "María Díaz: datos distintos detectados")
            Verificar(Resultado(comparaciones, "V-55555555") = ResultadoComparacion.NO_APARECE_EN_OPSU, "OPSU Fantasma: no aparece en el archivo")
            Verificar(Resultado(comparaciones, "V-66666666") = ResultadoComparacion.NO_REGISTRADO_EN_SISTEMA, "Rosa Nuevo: no registrada en el sistema")

            ' ---- persistencia de la carga ----
            Dim hash As String = Utilidades.HashArchivo(rutaCsv)
            Dim cargaId As Integer = Datos.RegistrarCargaOpsu("opsu-prueba-selftest.csv", hash, periodo.Id, registros.Count)
            Datos.GuardarRegistrosOpsu(cargaId, registros)
            Datos.GuardarComparaciones(cargaId, comparaciones)
            Verificar(cargaId > 0, "Carga OPSU registrada con id " & cargaId.ToString())

            ' ---- aplicar resultados ----
            Dim afectados As Integer = Datos.AplicarComparacion(cargaId, True)
            Verificar(afectados >= 4, "Resultados aplicados a " & afectados.ToString() & " registros")

            Dim rosa As Estudiante = Datos.BuscarEstudiante("V-66666666")
            Verificar(rosa IsNot Nothing AndAlso rosa.Modalidad = TipoIngreso.OPSU, "Estudiante faltante importado como OPSU")

            Dim ana As Estudiante = Datos.BuscarEstudiante("V-11111111")
            Verificar(ana.OpsuEstatus = "COINCIDE", "Ana Pérez quedó marcada como COINCIDE")

            ' ---- inscripción adicional de un flotante ----
            CrearEstudiante("V-77777777", "Pedro", "Flotante", TipoIngreso.FLOTANTE, veterinaria, 4, periodo)
            Dim trayectoCuatro As List(Of Inscripcion) = Datos.ListarInscripciones(4, "", 0)
            Verificar(trayectoCuatro.Count = 1, "Filtro por Trayecto IV devuelve 1 inscripción")

            Dim flotantes As List(Of Inscripcion) = Datos.ListarInscripciones(0, "FLOTANTE", 0)
            Verificar(flotantes.Count = 2, "Filtro por ingreso Flotante devuelve 2 inscripciones")
            Verificar(flotantes.All(Function(i) i.Trayecto > 0), "Todas las inscripciones tienen trayecto")

            ' ---- reporte ----
            Dim lineas As List(Of String) = Reporte.ConstruirLineas(trayectoCuatro, "Listado por trayecto")
            Verificar(lineas.Any(Function(l) l.Contains("Pedro Flotante")), "El reporte incluye al estudiante")
            Verificar(lineas.Any(Function(l) l.Contains("Trayecto IV")), "El reporte muestra el trayecto")
            Verificar(Not lineas.Any(Function(l) l.ToUpperInvariant().Contains("TURNO")), "El reporte no contiene turnos")

            Escribir("")
            Escribir("Total de inscripciones al final: " & Datos.ListarInscripciones(0, "", 0).Count.ToString())
            Escribir("Resumen: " & Datos.Resumen())
        Catch ex As Exception
            Correcto = False
            Escribir("ERROR: " & ex.ToString())
        Finally
            Try
                EliminarBase()
            Catch
            End Try
        End Try

        Escribir("")
        Escribir("RESULTADO: " & If(Correcto, "OK", "CON ERRORES"))

        Dim destino As String = Path.Combine(Path.GetTempPath(), "opsu-selftest.txt")
        File.WriteAllText(destino, Log.ToString(), New UTF8Encoding(True))
        Console.WriteLine(Log.ToString())
        Return If(Correcto, 0, 1)
    End Function

    Private Shared Sub CrearEstudiante(cedula As String, nombres As String, apellidos As String,
                                       modalidad As TipoIngreso, carrera As Carrera, trayecto As Integer,
                                       periodo As Periodo)
        Dim estudiante As New Estudiante With {
            .Cedula = cedula,
            .Nombres = nombres,
            .Apellidos = apellidos,
            .Modalidad = modalidad
        }
        Dim id As Integer = Datos.GuardarEstudiante(estudiante)
        Datos.GuardarInscripcion(id, carrera.Id, trayecto, periodo.Id, modalidad, "Autoprueba")
    End Sub

    Private Shared Function Resultado(comparaciones As List(Of Comparacion), cedula As String) As ResultadoComparacion
        Dim encontrada As Comparacion = comparaciones.FirstOrDefault(
            Function(c) Utilidades.ClaveCedula(c.Cedula) = Utilidades.ClaveCedula(cedula))
        If encontrada Is Nothing Then Return ResultadoComparacion.COINCIDE
        Return encontrada.Resultado
    End Function

    Private Shared Sub EliminarBase()
        Using conexion As New MySqlConnection(Configuracion.Cadena(False))
            conexion.Open()
            Using comando As MySqlCommand = conexion.CreateCommand()
                comando.CommandText = "DROP DATABASE IF EXISTS `" & Configuracion.Actual.BaseDatos & "`"
                comando.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Private Shared Sub Verificar(condicion As Boolean, descripcion As String)
        If Not condicion Then Correcto = False
        Escribir(If(condicion, "  [OK]   ", "  [FALLA] ") & descripcion)
    End Sub

    Private Shared Sub Escribir(texto As String)
        Log.AppendLine(texto)
    End Sub

End Class
