# Portal de Inscripción Universitaria · PNF

Sistema de inscripción en línea para los **Programas Nacionales de Formación** (Informática,
Veterinaria, Administración y Agroalimentaria), con una base de datos **MySQL/MariaDB de XAMPP**
compartida por el portal web y una aplicación de escritorio en **Visual Basic (.NET)** para imprimir
los comprobantes desde Control de Estudios.

```
┌─────────────────────────┐        ┌──────────────────────────┐
│  Navegador (estudiante) │        │  Visual Basic (operador) │
│  HTML + CSS + JS        │        │  Windows Forms + .NET    │
└────────────┬────────────┘        └────────────┬─────────────┘
             │ HTTP                             │ MySql.Data
             ▼                                  ▼
      ┌─────────────────┐              ┌──────────────────┐
      │ Node.js/Express │              │  MySQL/MariaDB   │
      │ EJS + sesiones  │─────────────▶│  portal_pnf      │
      └─────────────────┘              │  (XAMPP, 3306)   │
                                       └──────────────────┘
```

---

## Características

**Portal web**

- Registro de estudiantes con validación de cédula, correo, teléfono, fecha de nacimiento y contraseña.
- Inicio de sesión con cédula **o** correo electrónico.
- Inscripción a cualquiera de las 4 carreras, con selección de semestre (I–V), turno
  (Mañana/Tarde/Noche), condición del estudiante (alumno regular o población flotante) y
  asignaturas del semestre.
- Código único de comprobante con formato `INS-AAAA-000001`.
- Panel del estudiante con sus inscripciones, estado y acceso al comprobante.
- Comprobante oficial en pantalla y **versión imprimible** (CSS `@media print`, tamaño carta).
- Botón **Imprimir con Visual Basic**: el navegador solo envía el código y la aplicación de
  escritorio consulta los datos directamente en MySQL.
- Identidad institucional de la **UPTLL** en el encabezado, la portada y el comprobante.
- Protección de datos: cada estudiante solo puede ver sus propios comprobantes.

**Aplicación de escritorio (Visual Basic)**

- Se conecta **a la misma base MySQL** del portal.
- Búsqueda por cédula, código de comprobante, nombres o apellidos; filtros por carrera, semestre,
  turno, condición y estado, y filtro «solo pendientes de imprimir».
- Detalle completo: estudiante, carrera, semestre, turno, condición (alumno regular / población flotante), asignaturas, observaciones e historial de impresiones.
- **Vista previa** e **impresión** en tamaño carta, con encabezado institucional (logo UPTLL), datos,
  tabla de asignaturas, firmas y leyenda de verificación.
- **Impresión directa** a la impresora predeterminada desde el portal o desde
  **Archivo → Impresión directa** (`Ctrl + Shift + P`), sin diálogo de impresora.
- Registro automático del enlace `comprobantesvb://` para abrir la aplicación desde el navegador.
- Al imprimir actualiza en MySQL: `estado = 'IMPRESA'`, `impreso_en` y `impresiones + 1`.
- Conexión configurable desde **Archivo → Configurar conexión MySQL…**; se guarda en
  `%APPDATA%\ComprobantesVB\config.json`.

**Base de datos**

- MySQL/MariaDB en XAMPP, base `portal_pnf`, motor InnoDB y `utf8mb4`.
- 4 carreras, 5 semestres por carrera y 3 asignaturas por semestre (60 asignaturas iniciales).
- Contraseñas con **scrypt** (sal aleatoria), nunca en texto plano.
- Protección CSRF en todos los formularios, sesiones `HttpOnly`/`SameSite`, consultas preparadas.

---

## Requisitos

| Componente | Versión |
|---|---|
| XAMPP (MySQL/MariaDB) | MariaDB 10.4 o superior; probado con 10.4.32 en XAMPP |
| Node.js | 22.5 o superior (probado con 24.19) |
| .NET SDK (Windows) | 11.0 (probado con 11.0.100-preview) |
| Sistema operativo | Windows 10/11 para la aplicación de escritorio |
| Impresora | Cualquier impresora instalada o «Microsoft Print to PDF» |

> El portal ya no usa SQLite. El archivo `data/inscripciones.db` se conserva únicamente como
> respaldo histórico y puede importarse a MySQL con `npm run db:migrar`.

---

## Puesta en marcha

### 1. Iniciar MySQL en XAMPP

Abrir el **Panel de Control de XAMPP** y pulsar **Start** en MySQL. Para usar phpMyAdmin, iniciar
también Apache y abrir <http://localhost/phpmyadmin>.

La configuración local por defecto es:

| Dato | Valor |
|---|---|
| Servidor | `127.0.0.1` |
| Puerto | `3306` |
| Usuario | `root` |
| Contraseña | vacía (instalación local de XAMPP) |
| Base de datos | `portal_pnf` |

> La base `inscripciones_db` que ya existía en este XAMPP pertenece a otro proyecto. **No se toca**.
> Todo el sistema usa `portal_pnf`.

### 2. Instalar dependencias del portal

Desde la carpeta del proyecto:

```powershell
npm install
```

### 3. Crear o migrar la base de datos

**Si ya tienes datos en el SQLite histórico** (recomendado para conservar las inscripciones):

```powershell
npm run db:migrar
```

Esto crea la base `portal_pnf`, las tablas, las carreras y las asignaturas, e importa estudiantes,
inscripciones y asignaturas desde `data/inscripciones.db`. El archivo SQLite no se modifica.

**Si empiezas desde cero:**

```powershell
npm run db:reset
npm run demo       # opcional: estudiantes e inscripciones de ejemplo
```

`db:reset` **borra** la base MySQL indicada en `DB_NAME` y la recrea vacía. No borra el archivo
SQLite de respaldo.

La web también crea las tablas y los datos iniciales automáticamente al arrancar si la base está
vacía.

### 4. Arrancar el portal

```powershell
npm start
```

Abrir <http://localhost:3000>.

Para usar otro puerto:

```powershell
$env:PORT=3001; npm start
```

### Configuración de la conexión

El portal y la app VB aceptan estas variables de entorno:

| Variable | Por defecto | Uso |
|---|---|---|
| `DB_HOST` | `127.0.0.1` | Servidor MySQL |
| `DB_PORT` | `3306` | Puerto |
| `DB_USER` | `root` | Usuario |
| `DB_PASSWORD` | vacía | Contraseña |
| `DB_NAME` | `portal_pnf` | Base de datos |
| `DB_CONNECTION_LIMIT` | `10` | Conexiones del pool (solo web) |

Ejemplo con un usuario dedicado:

```powershell
$env:DB_USER="pnf"
$env:DB_PASSWORD="ClaveSegura123"
$env:DB_NAME="portal_pnf"
npm start
```

En MySQL se puede crear ese usuario así:

```sql
CREATE USER 'pnf'@'localhost' IDENTIFIED BY 'ClaveSegura123';
GRANT ALL PRIVILEGES ON portal_pnf.* TO 'pnf'@'localhost';
FLUSH PRIVILEGES;
```

La app VB lee las mismas variables, pero si no existen usa el archivo
`%APPDATA%\ComprobantesVB\config.json` o los valores locales por defecto. Las variables de entorno
tienen prioridad sobre el archivo.

### Cuentas de ejemplo

| Usuario | Contraseña | Inscripciones |
|---|---|---|
| `demo@pnf.edu.ve` o `V-12345678` | `Demo1234` | PNF Informática (III, Mañana) y PNF Agroalimentaria (I, Tarde) |
| `jose@pnf.edu.ve` o `V-87654321` | `Demo1234` | PNF Veterinaria (II, Noche) |

Las cuentas de ejemplo solo se crean con `npm run demo` o si ya venían en la migración.

### Migrar otra vez desde SQLite

`npm run db:migrar` se niega a ejecutarse si la base MySQL ya tiene estudiantes, para no mezclar
datos. Para repetir la migración desde cero:

```powershell
npm run db:reset
npm run db:migrar
```

También se puede indicar otro archivo SQLite:

```powershell
$env:SQLITE_PATH="C:\ruta\a\otra\inscripciones.db"
npm run db:migrar
```

### Reiniciar la base de datos

```powershell
npm run db:reset
npm run demo       # opcional
```

`db:reset` elimina **solo** la base MySQL `portal_pnf` (o la indicada en `DB_NAME`) y la recrea con
las carreras y asignaturas iniciales. **Se pierden estudiantes e inscripciones.**

---

## Aplicación de escritorio en Visual Basic

Compilar y ejecutar:

```powershell
cd vb\ComprobantesVB
dotnet build
dotnet run
```

O abrir `vb\Comprobantes.sln` (o `Comprobantes.slnx`) en Visual Studio.

El ejecutable queda en:

```
vb\ComprobantesVB\bin\Debug\net11.0-windows\ComprobantesVB.exe
```

### Uso

1. Iniciar MySQL/MariaDB desde XAMPP. La base `portal_pnf` debe existir; si no, arrancar el portal
   una vez (`npm start`) o ejecutar `npm run db:migrar` / `npm run db:reset`.
2. Al abrir la aplicación se conecta a `root@127.0.0.1:3306/portal_pnf` por defecto. Si usas otro
   usuario, puerto o base, abrir **Archivo → Configurar conexión MySQL…**, probar la conexión y
   guardar.
3. Escribir la cédula, el código o el nombre y pulsar **Buscar** (o `Enter`). `F5` recarga.
4. Seleccionar una fila para ver el detalle; **doble clic** abre la vista previa.
5. **Imprimir comprobante** (`Ctrl + P`): se elige la impresora y, al confirmar, el comprobante se
   envía a imprimir y la impresión queda registrada en MySQL.
6. **Archivo → Impresión directa** (`Ctrl + Shift + P`): envía el comprobante seleccionado a la
   impresora predeterminada sin abrir el diálogo.
7. **Archivo → Registrar impresión desde el portal**: vuelve a registrar el enlace
   `comprobantesvb://` si el ejecutable cambió de ubicación. También queda registrado al iniciar.
8. **Ayuda → Abrir phpMyAdmin**: abre `http://localhost/phpmyadmin` (requiere Apache iniciado).

La conexión se guarda en `%APPDATA%\ComprobantesVB\config.json`:

```json
{
  "mysql": {
    "servidor": "127.0.0.1",
    "puerto": 3306,
    "usuario": "root",
    "contrasena": "",
    "baseDatos": "portal_pnf"
  }
}
```

### Impresión directa desde el portal

El comprobante web incluye el botón **Imprimir con Visual Basic**. El flujo es:

1. Al abrir la aplicación por primera vez se registra el esquema `comprobantesvb://` para el usuario
   actual de Windows (`HKEY_CURRENT_USER`, sin permisos de administrador).
2. Al pulsar el botón, el navegador pide permiso para abrir la aplicación (según el navegador,
   puede recordar la elección).
3. El navegador envía únicamente el código, por ejemplo
   `comprobantesvb://imprimir/INS-2026-000001`.
4. Visual Basic consulta en MySQL el estudiante, la carrera, el semestre, las asignaturas y el resto
   de la información; imprime en la impresora predeterminada y actualiza `estado`, `impreso_en` e
   `impresiones`.
5. La aplicación muestra el resultado. Si el comprobante no existe o la inscripción está anulada,
   no imprime.

También se puede imprimir directamente desde el panel del estudiante con **Imprimir con VB**, o
usar **Imprimir en el navegador** como alternativa sin salir del portal. La impresión del navegador
genera el documento, pero no actualiza el estado de la inscripción en la base de datos; para dejar
registro de la impresión hay que usar Visual Basic.

**Requisitos y notas**

- La aplicación debe estar instalada en el mismo equipo donde se usa el navegador y tener acceso a
  MySQL (servidor local o remoto configurado).
- Para registrar el enlace manualmente:

  ```powershell
  & .\vb\ComprobantesVB\bin\Debug\net11.0-windows\ComprobantesVB.exe --registrar
  ```

- Si la impresora predeterminada es «Microsoft Print to PDF», Windows pedirá un nombre de archivo.
  Para imprimir sin diálogos, configura una impresora física como predeterminada.
- El enlace solo funciona en el equipo donde está registrada la aplicación. Si abres el portal desde
  otro equipo, usa **Imprimir en el navegador** o imprime en Control de Estudios.
- La aplicación de escritorio puede consultar todas las inscripciones; instálala solo en equipos
  autorizados de Control de Estudios.

### Prueba de impresión directa a PDF (sin interfaz)

Para probar sin modificar la base real, crea una copia de la base y cámbiale el nombre:

```powershell
# Ejemplo con mysqldump; también se puede hacer desde phpMyAdmin
& "C:\xampp\mysql\bin\mysql.exe" -u root -e "DROP DATABASE IF EXISTS portal_pnf_pruebas; CREATE DATABASE portal_pnf_pruebas;"
cmd /c '"C:\xampp\mysql\bin\mysqldump.exe" -u root portal_pnf | "C:\xampp\mysql\bin\mysql.exe" -u root portal_pnf_pruebas'

$env:DB_NAME = "portal_pnf_pruebas"
& .\vb\ComprobantesVB\bin\Debug\net11.0-windows\ComprobantesVB.exe `
    --imprimir INS-2026-000001 --pdf "$env:TEMP\comprobante-directo.pdf"
```

El código de salida es `0` si imprimió y registró la impresión, o `1` si falló. En modo `--pdf` no
se muestran mensajes; los errores se escriben en `comprobante-directo.pdf.error.txt`. La base real
`portal_pnf` no se modifica.

### Autoprueba (sin interfaz)

```powershell
cd vb\ComprobantesVB
dotnet build
& .\bin\Debug\net11.0-windows\ComprobantesVB.exe --selftest --print
Get-Content "$env:TEMP\comprobantes-selftest.txt"
```

- Conecta a MySQL, lee las inscripciones y renderiza el comprobante en
  `%TEMP%\comprobante-selftest.png`.
- Con `--print` lo imprime a PDF con «Microsoft Print to PDF» (`%TEMP%\comprobante-selftest.pdf`).
- Con `--marcar` prueba además el registro de impresión. **Modifica la base**; usar una copia
  (`DB_NAME=portal_pnf_pruebas`) si no se desea tocar la real.

---

## Base de datos

Base: `portal_pnf` en MySQL/MariaDB (XAMPP). La web crea las tablas y los datos iniciales
automáticamente. El esquema completo y las consultas que usa Visual Basic están documentados en
[`docs/esquema.sql`](docs/esquema.sql).

| Tabla | Contenido |
|---|---|
| `carreras` | PNF Informática (INF), Veterinaria (VET), Administración (ADM), Agroalimentaria (AGR) |
| `estudiantes` | Datos personales, credenciales y hash de contraseña |
| `asignaturas` | 3 asignaturas por cada semestre de cada carrera |
| `inscripciones` | Código único, carrera, semestre, turno, condición (REGULAR / FLOTANTE), fecha, estado, impresiones |
| `inscripcion_asignaturas` | Asignaturas seleccionadas en cada inscripción |

Estados de una inscripción:

| Estado | Significado |
|---|---|
| `REGISTRADA` | Inscripción creada en el portal, aún sin imprimir |
| `IMPRESA` | El comprobante fue impreso desde Visual Basic (o el estado fue actualizado) |
| `ANULADA` | Reservado para anulaciones administrativas |

Reglas principales:

- Una sola inscripción por **estudiante + carrera + semestre** (restricción `UNIQUE`).
- Código de comprobante generado dentro de una transacción a partir del id.
- `impresiones` es un contador acumulado; `impreso_en` guarda la última impresión.
- Las fechas se devuelven como texto `YYYY-MM-DD` / `YYYY-MM-DD HH:MM:SS` para conservar el formato
  local en la web y en la app VB.

### Respaldo y restauración

Desde phpMyAdmin: seleccionar `portal_pnf` → **Exportar** → formato SQL.

Desde consola:

```powershell
# Respaldo
cmd /c '"C:\xampp\mysql\bin\mysqldump.exe" -u root portal_pnf > "%USERPROFILE%\Desktop\portal_pnf.sql"'

# Restauración (sobrescribe los datos de esa base)
cmd /c '"C:\xampp\mysql\bin\mysql.exe" -u root portal_pnf < "%USERPROFILE%\Desktop\portal_pnf.sql"'
```

El archivo `data/inscripciones.db` y la copia `data/inscripciones.db.bak-antes-mysql` son respaldos
históricos del sistema anterior; ya no se leen ni se escriben.

---

## Estructura del proyecto

```
.
├─ package.json
├─ server.js                 Arranque del servidor web (MySQL)
├─ src/
│  ├─ app.js                 Rutas, validaciones y renderizado
│  ├─ config-db.js           Configuración de MySQL (DB_HOST, DB_PORT, …)
│  ├─ db.js                  Esquema, datos iniciales y consultas MySQL
│  └─ seguridad.js           scrypt, sesiones y CSRF
├─ views/                    Plantillas EJS (portada, registro, panel, comprobante…)
├─ public/
│  ├─ css/                   Estilos generales e impresión
│  ├─ js/                    JavaScript del cliente
│  └─ img/logo-uptll.jpg     Logo institucional UPTLL
├─ scripts/
│  ├─ migrar-sqlite-a-mysql.js  npm run db:migrar
│  ├─ reset-db.js               npm run db:reset
│  └─ demo.js                   npm run demo
├─ data/                     Respaldo SQLite histórico (no usado por la app)
├─ docs/esquema.sql          Documentación del modelo MySQL
└─ vb/
   ├─ Comprobantes.sln     Solución clásica de Visual Studio
   ├─ Comprobantes.slnx    Solución en formato XML
   └─ ComprobantesVB/
      ├─ Program.vb          Punto de entrada, --selftest, --registrar y --imprimir
      ├─ MainForm.vb         Ventana principal (búsqueda, detalle, impresión directa)
      ├─ Datos.vb            Acceso a MySQL con MySql.Data
      ├─ ConexionForm.vb     Diálogo de configuración y prueba de conexión
      ├─ ComprobantePrinter.vb  Diseño e impresión del comprobante
      ├─ Protocolo.vb        Enlace comprobantesvb:// para imprimir desde el portal
      ├─ ImpresionDirecta.vb Impresión por código, sin interfaz
      ├─ Configuracion.vb    Conexión MySQL y preferencias
      ├─ Modelos.vb          Clases de datos
      ├─ Utilidades.vb       Formatos de fecha y números romanos
      ├─ SelfTest.vb         Autoprueba de lectura, render e impresión
      └─ Recursos/
         ├─ logo-uptll.jpg   Logo para la ventana y el comprobante
         └─ logo-uptll.ico   Icono del ejecutable
```

---

## Identidad visual

El logo de la **Universidad Politécnica Territorial de Los Llanos «Juana Ramírez» (UPTLL)** se usa en:

| Ubicación | Archivo |
|---|---|
| Cabecera, portada y comprobante web | `public/img/logo-uptll.jpg` |
| Ventana y comprobante de Visual Basic | `vb/ComprobantesVB/Recursos/logo-uptll.jpg` |
| Icono del ejecutable de Windows | `vb/ComprobantesVB/Recursos/logo-uptll.ico` |

Para sustituirlo, reemplaza esos archivos conservando el nombre (el diseño escala el logo a las
medidas indicadas). Si cambias la relación de aspecto, ajusta la altura en
`public/css/estilos.css` (`.marca-imagen`, `.hero-marca img` y `.comprobante-logo`) y el ancho en
`ComprobantePrinter.vb` (`DibujarEncabezado`).

---

## API interna

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/carreras` | Lista de carreras en JSON |
| GET | `/api/asignaturas?carrera_id=1&semestre=3` | Asignaturas de una carrera y semestre |

El resto de rutas son páginas HTML con formularios protegidos por CSRF.

---

## Seguridad implementada

- Contraseñas con `scrypt` + sal aleatoria y comparación en tiempo constante.
- Sesiones en memoria con cookie `HttpOnly`, `SameSite=Lax` y expiración de 8 horas.
- Token CSRF en todos los formularios `POST`.
- Control de propiedad: un estudiante no puede abrir el comprobante de otro.
- Validación doble (cliente y servidor) y consultas SQL parametrizadas.
- Cabeceras `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options` y `Referrer-Policy`.
- Límite de intentos de inicio de sesión por IP (8 intentos / 10 minutos).
- Los errores de base de datos no se muestran al usuario.
- La conexión local de XAMPP usa `root` sin contraseña; para un equipo compartido se recomienda crear
  un usuario dedicado con permisos solo sobre `portal_pnf`.

---

## Pruebas realizadas

- **Web con MySQL:** portada, API, login, panel, comprobante, registro de estudiante y creación de
  inscripción; los datos se leen y escriben en `portal_pnf`.
- **Migración:** 3 estudiantes, 4 inscripciones y 12 asignaturas inscritas importadas desde SQLite,
  con los mismos códigos `INS-2026-000001` a `INS-2026-000004` y contadores en cero.
- **Diseño responsivo:** sin desbordes horizontales en 1440, 1024, 768 y 390 px.
- **Impresión web:** el comprobante se genera en **1 página** tamaño carta con todos los datos.
- **Visual Basic:** autoprueba con lectura de MySQL, render del comprobante (texto y coordenadas
  verificados), PDF real de 1 página y prueba de impresión directa + registro en una copia de la base.
- **Protocolo:** `comprobantesvb://imprimir/…` probado con una copia `portal_pnf_pruebas`; la base
  real `portal_pnf` quedó intacta.

---

## Solución de problemas

| Problema | Solución |
|---|---|
| `ECONNREFUSED 127.0.0.1:3306` | Iniciar MySQL/MariaDB desde el Panel de Control de XAMPP. |
| `Access denied for user 'root'` | Revisar `DB_USER`/`DB_PASSWORD` o configurar la conexión desde la app VB. |
| `Unknown database 'portal_pnf'` | Ejecutar `npm run db:migrar` o `npm run db:reset`, o arrancar la web una vez. |
| `Table 'portal_pnf.inscripciones' doesn't exist` | La base existe pero no el esquema; ejecutar la web o `npm run db:reset`. |
| La app VB no conecta | Verificar MySQL iniciado y usar **Archivo → Configurar conexión MySQL…**. |
| «El puerto 3000 ya está en uso» | Cerrar el proceso o ejecutar `$env:PORT=3001; npm start`. |
| `ER_DUP_ENTRY` al registrar | La cédula, el correo o la combinación estudiante/carrera/semestre ya existe. |
| No aparece la impresora | Instalar una impresora o usar «Microsoft Print to PDF»; comprobar en Configuración de Windows. |
| Acentos extraños en la consola | Es un tema de la consola de Windows; la base y los archivos usan UTF-8/utf8mb4. |
| Necesito volver a empezar | `npm run db:reset` (borra estudiantes e inscripciones) y luego `npm run demo` si se desean ejemplos. |
| Quiero recuperar los datos SQLite | Restaurar `data/inscripciones.db.bak-antes-mysql` y ejecutar `npm run db:migrar` sobre una base vacía. |

---

## Sistema de inscripción OPSU (escritorio)

Módulo independiente en `vb/InscripcionOPSU` para el registro de estudiantes por
trayecto y la validación contra el archivo oficial de la OPSU.

- Base de datos propia: `inscripcion_opsu` (no modifica `portal_pnf`).
- Sin turnos: la clasificación es solo por **Trayecto**.
- Importa CSV/XLSX/XLS de la OPSU y clasifica coincidencias, faltantes y diferencias.
- Reportes filtrados por trayecto y tipo de ingreso, con vista previa e impresión.

Documentación completa: [`docs/README-OPSU.md`](docs/README-OPSU.md) ·
Esquema: [`docs/esquema-opsu.sql`](docs/esquema-opsu.sql) ·
Ejemplo de archivo: [`docs/ejemplo-opsu.csv`](docs/ejemplo-opsu.csv).

---

Proyecto académico de inscripción universitaria · Control de Estudios · PNF.
