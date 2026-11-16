# Sistema de Inscripción Universitaria · OPSU y Flotantes

Aplicación de escritorio del **mismo sistema** que el portal web: registra e
inscribe estudiantes por **Trayecto**, diferencia a los asignados por la **OPSU**
de los **flotantes**, valida los datos contra el archivo oficial de la OPSU y
**emite el comprobante de inscripción**.

**Tecnología:** Visual Basic .NET (WinForms) · .NET 10 · MySQL / MariaDB.

> **Sistema único:** el portal web y esta aplicación comparten la base
> **`portal_pnf`**, las mismas tablas y los mismos registros. No hay bases
> separadas ni sincronización.
>
> **Decisión de diseño:** no existen turnos (mañana / tarde / noche). Toda la
> organización, los filtros y los reportes se hacen por **Trayecto (I a V)** y
> por **tipo de ingreso (OPSU / Flotante)**.

---

## 1. Estructura del proyecto

```
vb/InscripcionOPSU/
├── InscripcionOPSU.vbproj    Proyecto (net10.0-windows, RollForward)
├── Program.vb                Entrada · --selftest, --leer, --registrar y --imprimir
├── Configuracion.vb          Conexión MySQL (config.txt / OPSU_DB)
├── Esquema.vb                DDL idempotente y catálogos iniciales
├── Modelos.vb                Estudiante, Inscripcion, InscripcionCompleta, OPSU
├── Utilidades.vb             Normalización, cédulas, trayectos, fechas, similitud
├── Datos.vb                  Repositorio MySQL (inscripciones, comprobante, OPSU)
├── Comprobante.vb            Carga, vista previa e impresión del comprobante
├── ComprobantePrinter.vb     Dibujo e impresión en tamaño carta
├── FormComprobante.vb        Vista previa, botón Imprimir y registro de salida
├── Protocolo.vb              Enlace inscripcionopsu:// para el portal
├── OpsuLector.vb             Lectura de CSV / XLSX / XLS y mapeo de columnas
├── OpsuComparador.vb         Comparación y clasificación de resultados
├── Reporte.vb                Listados, vista previa e impresión
├── FormPrincipal.vb          Ventana principal (3 pestañas)
├── FormPrincipal.Registro.vb Registro, inscripción y acceso al comprobante
├── FormPrincipal.Opsu.vb     Carga y comparación del archivo OPSU
├── FormPrincipal.Reportes.vb Reportes filtrados
└── SelfTest.vb               Autoprueba funcional sin interfaz
```

El logo institucional se copia desde `public/img/logo-uptll.jpg` a
`Recursos/logo-uptll.jpg` durante la compilación.

---

## 2. Base de datos compartida

El esquema completo está en [`esquema.sql`](esquema.sql). La aplicación usa la
misma base que el portal (`portal_pnf`) y crea o ajusta las tablas al iniciar,
sin borrar datos.

| Tabla | Para qué sirve |
|---|---|
| `carreras` | Catálogo de PNF / carreras |
| `estudiantes` | Datos personales + `modalidad` (OPSU / FLOTANTE) + `opsu_estatus` |
| `asignaturas` | 3 asignaturas por carrera y **trayecto** (60 iniciales) |
| `inscripciones` | Estudiante + carrera + **trayecto** + **tipo de ingreso** + comprobante |
| `inscripcion_asignaturas` | Asignaturas inscritas en cada comprobante |
| `opsu_cargas` | Cada archivo cargado (nombre, hash SHA-256, total de filas) |
| `opsu_registros` | Filas crudas del archivo OPSU |
| `opsu_comparaciones` | Resultado por cédula y diferencias detectadas |
| `bitacora` | Auditoría de inscripciones, cargas, aplicaciones e impresiones |

Reglas principales:

- Una sola inscripción por **estudiante + carrera + trayecto**
  (`uq_inscripciones_estudiante_carrera_trayecto`).
- Código único de comprobante `INS-AAAA-000001`.
- `estado`: `REGISTRADA` → `IMPRESA`; `ANULADA` queda reservado a Control de Estudios.
- `impresiones` es un contador acumulado e `impreso_en` guarda la última impresión.
- `semestre`, `turno` y `condicion` se conservan solo como columnas legado
  NULLables para no romper datos anteriores.

### Estados de la comparación OPSU

| Resultado | Significado |
|---|---|
| `COINCIDE` | Cédula, nombre y carrera coinciden |
| `CARRERA_DISTINTA` | Está en ambos lados, pero con carrera diferente |
| `DATOS_DISTINTOS` | Está en ambos lados, pero el nombre no coincide |
| `NO_APARECE_EN_OPSU` | El sistema lo tiene como OPSU y no está en el archivo |
| `NO_REGISTRADO_EN_SISTEMA` | Está en el archivo OPSU y no existe en el sistema |

---

## 3. Pantallas

### Pestaña «Registro e inscripción»

- Izquierda: datos del estudiante (cédula, nombres, apellidos, sexo, nacimiento,
  correo, teléfono) y datos de inscripción (**carrera, trayecto, tipo de ingreso**).
- Derecha: listado de inscritos con filtros por **trayecto**, **tipo de ingreso**
  y carrera, y la columna **Comprobante**.
- Botones: *Guardar e inscribir*, *Buscar cédula*, *Limpiar* y *Comprobante*.
- Al guardar se muestra el código y se ofrece abrir la vista previa del
  comprobante. También se abre con doble clic sobre una fila.

### Pestaña «Validación OPSU»

1. **Seleccionar archivo…** (CSV, XLSX o XLS).
2. **Analizar y comparar** → lee el archivo, lo guarda, compara y colorea resultados.
3. **Aplicar resultados** → marca a los estudiantes como OPSU y actualiza su
   estatus. Opcionalmente crea los que aparecen en OPSU y no existen.

Resumen en vivo: coinciden, no aparecen, no registrados, datos distintos y
carrera distinta.

### Pestaña «Reportes»

- Filtros: **trayecto**, **tipo de ingreso** y carrera.
- **Vista previa** e **Imprimir** con `PrintDocument`.
- **Exportar CSV** del listado filtrado.

### Comprobante

El comprobante se dibuja en tamaño carta con el logo institucional, los datos
del estudiante, carrera, **trayecto**, **tipo de ingreso**, asignaturas, firmas
y código de verificación. La vista previa permite imprimir con diálogo de
impresora; la impresión solicitada desde el portal usa la impresora
predeterminada. En ambos casos se actualiza `estado = 'IMPRESA'`,
`impreso_en` e `impresiones + 1`.

---

## 4. Impresión desde el portal

1. El portal muestra el enlace `inscripcionopsu://imprimir/INS-2026-000001`.
2. Al abrir la aplicación por primera vez se registra el esquema
   `inscripcionopsu://` en `HKEY_CURRENT_USER` (sin permisos de administrador).
3. El navegador envía únicamente el código; Visual Basic consulta todos los
   datos en `portal_pnf`.
4. La aplicación abre la vista previa, imprime y registra la impresión.

También se puede preparar el enlace manualmente:

```powershell
& .\vb\InscripcionOPSU\bin\Debug\net10.0-windows\InscripcionOPSU.exe --registrar
```

---

## 5. Formato del archivo OPSU

Acepta `.csv`, `.txt`, `.xlsx` y `.xls`. No requiere renombrar columnas: el
lector reconoce los encabezados por sinónimos.

| Campo | Encabezados aceptados (sin distinguir mayúsculas ni acentos) |
|---|---|
| Cédula | `CEDULA`, `C.I`, `CI`, `DOCUMENTO`, `IDENTIFICACION`, `CEDULA DE IDENTIDAD` |
| Nombres | `NOMBRES`, `NOMBRE`, `NOMBRE ESTUDIANTE` |
| Apellidos | `APELLIDOS`, `APELLIDO` |
| Nombre completo | `NOMBRE COMPLETO`, `NOMBRES Y APELLIDOS`, `ESTUDIANTE` |
| Carrera | `CARRERA`, `CARRERA ASIGNADA`, `PNF`, `PROGRAMA`, `OPCION` |
| Trayecto | `TRAYECTO`, `NIVEL` |
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

## 6. Compilar y ejecutar

```powershell
dotnet build "vb\InscripcionOPSU\InscripcionOPSU.vbproj" -c Debug

# Abrir la aplicación
.\vb\InscripcionOPSU\bin\Debug\net10.0-windows\InscripcionOPSU.exe

# Autoprueba completa (base temporal, no toca datos reales)
.\vb\InscripcionOPSU\bin\Debug\net10.0-windows\InscripcionOPSU.exe --selftest

# Diagnosticar un archivo OPSU sin abrir la interfaz
.\vb\InscripcionOPSU\bin\Debug\net10.0-windows\InscripcionOPSU.exe --leer "C:\ruta\archivo.xlsx"
```

### Configuración de la conexión

Valores por defecto: `root` sin contraseña en `127.0.0.1:3306`, base **`portal_pnf`**.

Para cambiarlos, cree `%APPDATA%\InscripcionOPSU\config.txt`:

```
servidor=127.0.0.1
puerto=3306
usuario=root
clave=
basedatos=portal_pnf
```

También puede usarse la variable de entorno `OPSU_DB` para apuntar a otra base
(la autoprueba la usa para crear `portal_pnf_prueba`).

---

## 7. Autoprueba

`--selftest` crea la base temporal `portal_pnf_prueba` y comprueba:

- catálogo de 4 carreras, 5 trayectos y 60 asignaturas;
- inscripciones por trayecto, códigos y prevención de duplicados;
- carga del comprobante desde la base y render de la vista previa;
- textos del comprobante (código, trayecto, ingreso; sin turnos ni semestres);
- registro de impresiones y estado `IMPRESA`;
- lectura y comparación de un CSV de la OPSU;
- importación de faltantes y filtros por trayecto e ingreso;
- reporte por trayecto.

Al finalizar **elimina la base temporal**. Escribe el informe en
`%TEMP%\opsu-selftest.txt` y devuelve código `0` si todo pasó correctamente.
