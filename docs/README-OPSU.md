# Sistema de Inscripción Universitaria · OPSU y Flotantes

Aplicación de escritorio para registrar e inscribir estudiantes universitarios,
diferenciando a los asignados por la **OPSU** de los **flotantes**, y validando
los datos contra el archivo oficial de la OPSU.

**Tecnología:** Visual Basic .NET (WinForms) · .NET 10 · MySQL / MariaDB.

> **Decisión de diseño:** el sistema **no utiliza turnos**. Toda la organización,
> los filtros y los reportes se hacen por **Trayecto** (I a V).

---

## 1. Estructura del proyecto

```
vb/InscripcionOPSU/
├── InscripcionOPSU.vbproj    Proyecto (net10.0-windows, RollForward)
├── Program.vb                Punto de entrada · modos --selftest y --leer
├── Configuracion.vb          Conexión MySQL (config.txt / variables de entorno)
├── Esquema.vb                DDL idempotente y catálogos iniciales
├── Modelos.vb                Estudiante, Inscripcion, OpsuRegistro, Comparacion
├── Utilidades.vb             Normalización, cédulas, trayectos, similitud (Levenshtein)
├── Datos.vb                  Repositorio MySQL (toda la persistencia)
├── OpsuLector.vb             Lectura de CSV / XLSX / XLS y mapeo de columnas
├── OpsuComparador.vb         Comparación y clasificación de resultados
├── Reporte.vb                Listados, vista previa e impresión
├── FormPrincipal.vb          Ventana principal (3 pestañas)
├── FormPrincipal.Registro.vb Registro e inscripción por trayecto
├── FormPrincipal.Opsu.vb     Carga y comparación del archivo OPSU
├── FormPrincipal.Reportes.vb Reportes filtrados
└── SelfTest.vb               Autoprueba funcional sin interfaz
```

---

## 2. Base de datos

Esquema completo en [`esquema-opsu.sql`](esquema-opsu.sql). La aplicación crea
la base `inscripcion_opsu` y sus tablas automáticamente al iniciar.

| Tabla | Para qué sirve |
|---|---|
| `carreras` | Catálogo de PNF / carreras |
| `trayectos` | Trayectos I a V (reemplaza al turno) |
| `periodos` | Periodo académico activo |
| `estudiantes` | Datos personales + `modalidad` (OPSU / FLOTANTE) + `opsu_estatus` |
| `inscripciones` | Estudiante + carrera + **trayecto** + periodo |
| `opsu_cargas` | Cada archivo cargado (nombre, hash SHA-256, total de filas) |
| `opsu_registros` | Filas crudas del archivo OPSU |
| `opsu_comparaciones` | Resultado por cédula y diferencias detectadas |
| `bitacora` | Auditoría de inscripciones, cargas y aplicaciones |

### Estados de la comparación

| Resultado | Significado |
|---|---|
| `COINCIDE` | La cédula, el nombre y la carrera coinciden |
| `CARRERA_DISTINTA` | Está en ambos lados, pero con carrera diferente |
| `DATOS_DISTINTOS` | Está en ambos lados, pero el nombre no coincide |
| `NO_APARECE_EN_OPSU` | El sistema lo tiene como OPSU y no está en el archivo |
| `NO_REGISTRADO_EN_SISTEMA` | Está en el archivo OPSU y no existe en el sistema |

---

## 3. Pantallas

### Pestaña «Registro e inscripción»

- Izquierda: datos del estudiante (cédula, nombres, apellidos, sexo, nacimiento,
  correo, teléfono) y datos de inscripción (**carrera, trayecto, tipo de ingreso**).
- Derecha: listado de inscritas con filtros por **trayecto**, **tipo de ingreso**
  y carrera.
- Botones: *Guardar e inscribir*, *Buscar cédula*, *Limpiar*.

### Pestaña «Validación OPSU»

1. **Seleccionar archivo…** (CSV, XLSX o XLS).
2. **Analizar y comparar** → lee el archivo, lo guarda, compara y colorea resultados.
3. **Aplicar resultados** → marca a los estudiantes como OPSU y actualiza su
   estatus. Opcionalmente crea los que aparecen en OPSU y no existen.

Resumen en vivo: coinciden, no aparecen, no registrados, datos distintos y
carrera distinta.

### Pestaña «Reportes»

- Filtros: **trayecto**, **tipo de ingreso** y carrera.
- **Vista previa** e **Imprimir** (flujo clásico de escritorio, `PrintDocument`).
- **Exportar CSV** del listado filtrado.

---

## 4. Formato del archivo OPSU

Acepta `.csv`, `.txt`, `.xlsx` y `.xls`. No requiere renombrar columnas: el
lector reconoce los encabezados por sinónimos.

| Campo | Encabezados aceptados (sin distinguir mayúsculas ni acentos) |
|---|---|
| Cédula | `CEDULA`, `C.I`, `CI`, `DOCUMENTO`, `IDENTIFICACION`, `CEDULA DE IDENTIDAD` |
| Nombres | `NOMBRES`, `NOMBRE`, `NOMBRE ESTUDIANTE` |
| Apellidos | `APELLIDOS`, `APELLIDO` |
| Nombre completo | `NOMBRE COMPLETO`, `NOMBRES Y APELLIDOS`, `ESTUDIANTE` |
| Carrera | `CARRERA`, `CARRERA ASIGNADA`, `PNF`, `PROGRAMA`, `OPCION` |
| Trayecto | `TRAYECTO`, `SEMESTRE`, `NIVEL` |
| Estatus | `ESTATUS`, `ESTADO`, `CONDICION`, `SITUACION` |
| Sexo | `SEXO`, `GENERO` |

Detalles soportados:

- Separadores `;`, `,` o tabulación (se detectan automáticamente).
- Codificación UTF-8 (con o sin BOM) y Windows-1252.
- Cédulas con o sin guion: `V-12345678`, `12345678`, `V 12.345.678`.
- Trayecto en número (`3`) o romano (`III`).
- Filas de título antes del encabezado (se buscan en las primeras 20 filas).

Archivo de ejemplo: [`ejemplo-opsu.csv`](ejemplo-opsu.csv).

---

## 5. Compilar y ejecutar

```powershell
dotnet build "vb\InscripcionOPSU\InscripcionOPSU.vbproj" -c Debug

# Abrir la aplicación
.\vb\InscripcionOPSU\bin\Debug\net10.0-windows\InscripcionOPSU.exe

# Autoprueba completa (usa una base temporal, no toca datos reales)
.\vb\InscripcionOPSU\bin\Debug\net10.0-windows\InscripcionOPSU.exe --selftest

# Diagnosticar un archivo OPSU sin abrir la interfaz
.\vb\InscripcionOPSU\bin\Debug\net10.0-windows\InscripcionOPSU.exe --leer "C:\ruta\archivo.xlsx"
```

### Configuración de la conexión

Valores por defecto: `root` sin contraseña en `127.0.0.1:3306`, base `inscripcion_opsu`.

Para cambiarlos, cree `%APPDATA%\InscripcionOPSU\config.txt`:

```
servidor=127.0.0.1
puerto=3306
usuario=root
clave=
basedatos=inscripcion_opsu
```

También puede usarse la variable de entorno `OPSU_DB` para apuntar a otra base.

---

## 6. Autoprueba

`--selftest` crea la base `inscripcion_opsu_prueba`, registra estudiantes,
genera un CSV de la OPSU, lo compara, aplica los resultados, filtra por trayecto
y arma un reporte. Al finalizar **elimina la base temporal**.

Escribe el informe en `%TEMP%\opsu-selftest.txt` y devuelve código `0` si todo
pasó correctamente.
