# Portal de Inscripción Universitaria · PNF

Sistema **único** de inscripción para los Programas Nacionales de Formación
(Informática, Veterinaria, Administración y Agroalimentaria). El estudiante se
inscribe desde el **portal web** y Control de Estudios trabaja con una
**aplicación de escritorio en Visual Basic (.NET)** que usa la misma base de
datos, el mismo modelo y los mismos registros.

**Modelo académico:** inscripción por **Trayecto (I a V)** y **tipo de ingreso
(OPSU o Flotante)**. No existen turnos (mañana / tarde / noche).

```
┌─────────────────────────┐        ┌──────────────────────────────┐
│  Navegador (estudiante) │        │  Visual Basic (Control de    │
│  HTML + CSS + JS        │        │  Estudios): registro, OPSU,  │
│                         │        │  reportes e impresión        │
└────────────┬────────────┘        └──────────────┬───────────────┘
             │ HTTP                               │ MySql.Data
             ▼                                    ▼
      ┌─────────────────┐              ┌──────────────────────────┐
      │ Node.js/Express │              │  MySQL/MariaDB (XAMPP)   │
      │ EJS + sesiones  │─────────────▶│  portal_pnf              │
      └─────────────────┘              │  una sola base, 3306     │
                                       └──────────────────────────┘
```

---

## Qué incluye

**Portal web**

- Registro e inicio de sesión de estudiantes (cédula o correo).
- Inscripción a cualquiera de las 4 carreras con selección de **trayecto**,
  **tipo de ingreso** y asignaturas del trayecto.
- Una sola inscripción por **estudiante + carrera + trayecto**.
- Código único de comprobante `INS-AAAA-000001`.
- Panel del estudiante con sus inscripciones, estado e impresiones.
- Comprobante oficial en pantalla y versión imprimible en tamaño carta.
- Botón **Imprimir con Visual Basic**: el navegador solo envía el código; la
  aplicación consulta los datos directamente en MySQL.
- Identidad institucional de la **UPTLL** en portada, cabecera y comprobante.
- Control de propiedad: cada estudiante solo ve sus propios comprobantes.

**Aplicación de escritorio (Visual Basic)**

- Se conecta a la **misma base `portal_pnf`** del portal.
- Pestaña **Registro e inscripción** por trayecto, con listado filtrable por
  trayecto, tipo de ingreso y carrera.
- Botón **Comprobante**: vista previa, impresión con o sin diálogo y registro
  automático de la impresión.
- Pestaña **Validación OPSU**: lee CSV/XLSX/XLS, compara contra los estudiantes
  registrados, clasifica diferencias y opcionalmente importa faltantes.
- Pestaña **Reportes**: filtros por trayecto, ingreso y carrera; vista previa,
  impresión y exportación a CSV.
- Enlace `inscripcionopsu://imprimir/<código>` registrado en Windows para abrir
  la aplicación desde el navegador.

**Base de datos**

- MySQL/MariaDB de XAMPP, base `portal_pnf`, InnoDB y `utf8mb4`.
- 4 carreras, 5 trayectos por carrera y 3 asignaturas por trayecto (60 iniciales).
- Contraseñas con **scrypt**; nunca en texto plano.
- Protección CSRF, sesiones `HttpOnly`/`SameSite` y consultas preparadas.

---

## Requisitos

| Componente | Versión |
|---|---|
| XAMPP (MySQL/MariaDB) | MariaDB 10.4 o superior (probado con 10.4.32) |
| Node.js | 22.5 o superior (probado con 24.19) |
| .NET SDK (Windows) | 10 u 11 (probado con 11.0.100-preview) |
| Sistema operativo | Windows 10/11 para la aplicación de escritorio |
| Impresora | Cualquiera instalada o «Microsoft Print to PDF» |

> El portal ya no usa SQLite. `data/inscripciones.db` se conserva solo como
> respaldo histórico y puede importarse con `npm run db:migrar`.
> La base `inscripciones_db` que ya existía en el XAMPP pertenece a otro
> proyecto y **no se toca**.

---

## Puesta en marcha

### 1. Iniciar MySQL en XAMPP

Abrir el **Panel de Control de XAMPP** y pulsar **Start** en MySQL. Para usar
phpMyAdmin, iniciar también Apache y abrir <http://localhost/phpmyadmin>.

Configuración local por defecto:

| Dato | Valor |
|---|---|
| Servidor | `127.0.0.1` |
| Puerto | `3306` |
| Usuario | `root` |
| Contraseña | vacía (instalación local de XAMPP) |
| Base de datos | `portal_pnf` |

### 2. Instalar dependencias del portal

```powershell
npm install
```

### 3. Crear o migrar la base de datos

**Si vienes del SQLite histórico** (conserva estudiantes e inscripciones):

```powershell
npm run db:migrar
```

**Si empiezas desde cero:**

```powershell
npm run db:reset
npm run demo       # opcional: estudiantes e inscripciones de ejemplo
```

`db:reset` **borra** la base indicada en `DB_NAME` y la recrea vacía.
La web también crea las tablas y los datos iniciales al arrancar si la base no
tiene esquema.

### 4. Arrancar el portal

```powershell
npm start
```

Abrir <http://localhost:3000>. Para usar otro puerto:

```powershell
$env:PORT=3001; npm start
```

### Cuentas de ejemplo

| Usuario | Contraseña | Inscripciones |
|---|---|---|
| `demo@pnf.edu.ve` o `V-12345678` | `Demo1234` | PNF Informática (Trayecto III, OPSU) y PNF Agroalimentaria (Trayecto I) |
| `jose@pnf.edu.ve` o `V-87654321` | `Demo1234` | PNF Veterinaria (Trayecto II, OPSU) |

Las cuentas solo se crean con `npm run demo` o si ya venían en la migración.

### Configuración de la conexión

El portal y la app VB aceptan estas variables de entorno:

| Variable | Por defecto | Uso |
|---|---|---|
| `DB_HOST` | `127.0.0.1` | Servidor MySQL (portal) |
| `DB_PORT` | `3306` | Puerto |
| `DB_USER` | `root` | Usuario |
| `DB_PASSWORD` | vacía | Contraseña |
| `DB_NAME` | `portal_pnf` | Base de datos |
| `DB_CONNECTION_LIMIT` | `10` | Conexiones del pool (solo web) |
| `OPSU_DB` | — | Sobrescribe la base en la app VB (pruebas) |
| `OPSU_HOST` | — | Sobrescribe el servidor en la app VB (pruebas) |

La app VB usa además `%APPDATA%\InscripcionOPSU\config.txt`:

```
servidor=127.0.0.1
puerto=3306
usuario=root
clave=
basedatos=portal_pnf
```

Las variables de entorno tienen prioridad sobre ese archivo.

---

## Aplicación de escritorio en Visual Basic

Compilar y ejecutar:

```powershell
dotnet build "vb\InscripcionOPSU\InscripcionOPSU.vbproj" -c Debug
.\vb\InscripcionOPSU\bin\Debug\net10.0-windows\InscripcionOPSU.exe
```

O abrir `vb\Comprobantes.sln` (o `Comprobantes.slnx`) en Visual Studio.

### Uso

1. Iniciar MySQL/MariaDB desde XAMPP.
2. Al abrir, la aplicación crea o ajusta el esquema de `portal_pnf` sin borrar
   datos y registra el enlace `inscripcionopsu://`.
3. **Registro e inscripción**: escribir la cédula, completar los datos, elegir
   carrera, **trayecto** y **tipo de ingreso**, y pulsar **Guardar e inscribir**.
4. Al guardar se muestra el código del comprobante y se ofrece abrir la vista
   previa. El botón **Comprobante** (o doble clic en una fila) abre la vista
   previa de cualquier inscripción listada.
5. **Imprimir comprobante**: abre el diálogo de impresora; al confirmar se
   imprime y se registra `IMPRESA`, `impreso_en` e `impresiones + 1`.
6. **Validación OPSU**: seleccionar el archivo oficial, analizarlo, revisar los
   resultados y aplicarlos.
7. **Reportes**: filtrar por trayecto, ingreso y carrera; vista previa,
   impresión o exportación CSV.

### Impresión solicitada desde el portal

1. El comprobante web incluye el botón **Imprimir con Visual Basic**, con el
   enlace `inscripcionopsu://imprimir/INS-2026-000001`.
2. El navegador pide permiso para abrir la aplicación y envía **solo el código**.
3. La aplicación consulta en MySQL el estudiante, la carrera, el trayecto, el
   tipo de ingreso y las asignaturas.
4. Abre la vista previa, imprime en la impresora predeterminada y registra la
   impresión.

Para registrar el enlace manualmente:

```powershell
& .\vb\InscripcionOPSU\bin\Debug\net10.0-windows\InscripcionOPSU.exe --registrar
```

Notas:

- La aplicación debe estar instalada en el mismo equipo del navegador y tener
  acceso a MySQL.
- Si la impresora predeterminada es «Microsoft Print to PDF», Windows pedirá un
  nombre de archivo.
- La web también ofrece **Imprimir en el navegador** como alternativa; esa vía
  no actualiza el estado de la inscripción en la base.

### Autoprueba del escritorio

```powershell
& .\vb\InscripcionOPSU\bin\Debug\net10.0-windows\InscripcionOPSU.exe --selftest
Get-Content "$env:TEMP\opsu-selftest.txt"
```

La autoprueba usa una base temporal (`portal_pnf_prueba`), verifica esquema,
inscripciones por trayecto, códigos, comprobante, impresiones, lectura y
comparación OPSU, filtros y reportes. Al final **elimina la base temporal** y
devuelve `0` si todo pasó.

---

## Base de datos

Base única: `portal_pnf`. El esquema completo y las consultas de referencia
están en [`docs/esquema.sql`](docs/esquema.sql).

| Tabla | Contenido |
|---|---|
| `carreras` | PNF Informática (INF), Veterinaria (VET), Administración (ADM), Agroalimentaria (AGR) |
| `estudiantes` | Datos personales, credenciales, `modalidad` (OPSU/FLOTANTE) y estatus OPSU |
| `asignaturas` | 3 asignaturas por carrera y **trayecto** |
| `inscripciones` | Código único, carrera, **trayecto**, **tipo_ingreso**, fecha, estado e impresiones |
| `inscripcion_asignaturas` | Asignaturas de cada inscripción |
| `opsu_cargas` | Archivos OPSU cargados (hash SHA-256) |
| `opsu_registros` | Filas crudas del archivo OPSU |
| `opsu_comparaciones` | Resultado por cédula y diferencias |
| `bitacora` | Auditoría de inscripciones, cargas, aplicaciones e impresiones |

Estados de una inscripción:

| Estado | Significado |
|---|---|
| `REGISTRADA` | Creada en el portal o en el escritorio, aún sin imprimir |
| `IMPRESA` | El comprobante fue impreso |
| `ANULADA` | Reservado para anulaciones administrativas |

Reglas principales:

- Una sola inscripción por **estudiante + carrera + trayecto** (`UNIQUE`).
- Código de comprobante generado dentro de una transacción.
- `impresiones` es un contador acumulado; `impreso_en` guarda la última impresión.
- `semestre`, `turno` y `condicion` quedan como columnas legado NULLables para
  conservar datos anteriores; el sistema no las usa.

### Respaldo y restauración

Desde phpMyAdmin: seleccionar `portal_pnf` → **Exportar** → formato SQL.

Desde consola:

```powershell
# Respaldo
cmd /c '"C:\xampp\mysql\bin\mysqldump.exe" -u root portal_pnf > "%USERPROFILE%\Desktop\portal_pnf.sql"'

# Restauración (sobrescribe los datos de esa base)
cmd /c '"C:\xampp\mysql\bin\mysql.exe" -u root portal_pnf < "%USERPROFILE%\Desktop\portal_pnf.sql"'
```

---

## Estructura del proyecto

```
.
├─ package.json
├─ server.js                 Arranque del servidor web (MySQL)
├─ src/
│  ├─ app.js                 Rutas, validaciones y renderizado
│  ├─ config-db.js           Configuración de MySQL (DB_HOST, DB_PORT, …)
│  ├─ db.js                  Esquema, migración y consultas MySQL
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
├─ docs/
│  ├─ esquema.sql            Esquema único documentado
│  ├─ README-OPSU.md         Manual de la aplicación de escritorio
│  └─ ejemplo-opsu.csv       Archivo OPSU de ejemplo
└─ vb/
   ├─ Comprobantes.sln       Solución de Visual Studio
   ├─ Comprobantes.slnx      Solución en formato XML
   ├─ InscripcionOPSU/       Sistema de escritorio (sistema único)
   └─ ComprobantesVB/        Versión anterior, solo referencia (no se usa)
```

Archivos principales del escritorio:

```
vb/InscripcionOPSU/
├─ Program.vb                Entrada · --selftest, --leer, --registrar, --imprimir
├─ Configuracion.vb          Conexión MySQL
├─ Esquema.vb                DDL idempotente y catálogos
├─ Datos.vb                  Repositorio MySQL
├─ Modelos.vb                Modelos de datos
├─ Comprobante.vb            Carga y apertura del comprobante
├─ ComprobantePrinter.vb     Dibujo e impresión carta
├─ FormComprobante.vb        Vista previa e impresión
├─ Protocolo.vb              Enlace inscripcionopsu://
├─ OpsuLector.vb             Lectura CSV/XLSX/XLS
├─ OpsuComparador.vb         Comparación con OPSU
├─ Reporte.vb                Listados e impresión
├─ FormPrincipal*.vb         Ventana y pestañas
└─ SelfTest.vb               Autoprueba
```

---

## API interna

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/carreras` | Lista de carreras en JSON |
| GET | `/api/asignaturas?carrera_id=1&trayecto=3` | Asignaturas de una carrera y trayecto |

El resto de rutas son páginas HTML con formularios protegidos por CSRF.

---

## Seguridad implementada

- Contraseñas con `scrypt` + sal aleatoria y comparación en tiempo constante.
- Sesiones en memoria con cookie `HttpOnly`, `SameSite=Lax` y expiración de 8 horas.
- Token CSRF en todos los formularios `POST`.
- Control de propiedad: un estudiante no puede abrir el comprobante de otro.
- Validación doble (cliente y servidor) y consultas SQL parametrizadas.
- Cabeceras `Content-Security-Policy`, `X-Content-Type-Options`,
  `X-Frame-Options` y `Referrer-Policy`.
- Límite de intentos de inicio de sesión por IP (8 intentos / 10 minutos).
- Los errores de base de datos no se muestran al usuario.
- La conexión local de XAMPP usa `root` sin contraseña; en equipos compartidos
  se recomienda un usuario dedicado con permisos solo sobre `portal_pnf`.

---

## Pruebas realizadas

- **Web:** portada, login, panel, formulario por trayecto, API por trayecto,
  comprobante con enlace a Visual Basic y protecciones CSRF/404/permisos.
- **Migración:** 3 estudiantes y 4 inscripciones conservadas con sus códigos
  `INS-2026-000001` a `INS-2026-000004`, ahora con `trayecto` y `tipo_ingreso`.
- **Escritorio:** `--selftest` con 41 comprobaciones (esquema, inscripciones,
  códigos, render del comprobante, impresiones, OPSU, filtros y reportes).
- **Integración:** una inscripción creada en la web se lee desde la aplicación
  VB en la misma base; el comprobante se renderiza e imprime en carta.
- **Protocolo:** `inscripcionopsu://imprimir/…` registrado en `HKEY_CURRENT_USER`
  y enlazado a `InscripcionOPSU.exe`.

---

## Solución de problemas

| Problema | Solución |
|---|---|
| `ECONNREFUSED 127.0.0.1:3306` | Iniciar MySQL/MariaDB desde XAMPP. |
| `Access denied for user 'root'` | Revisar `DB_USER`/`DB_PASSWORD` o el `config.txt` de la app VB. |
| `Unknown database 'portal_pnf'` | Ejecutar `npm run db:migrar` o `npm run db:reset`, o arrancar la web una vez. |
| La app VB no conecta | Verificar MySQL iniciado y `%APPDATA%\InscripcionOPSU\config.txt`. |
| «El puerto 3000 ya está en uso» | Cerrar el proceso o ejecutar `$env:PORT=3001; npm start`. |
| `ER_DUP_ENTRY` al inscribir | Ya existe una inscripción para esa cédula, carrera y trayecto. |
| No aparece la impresora | Instalar una impresora o usar «Microsoft Print to PDF». |
| El navegador no abre la app VB | Ejecutar `InscripcionOPSU.exe --registrar` y aceptar el aviso del navegador. |
| Acentos extraños en la consola | Es la consola de Windows; la base y los archivos usan UTF-8/utf8mb4. |
| Quiero volver a empezar | `npm run db:reset` (borra estudiantes e inscripciones) y luego `npm run demo`. |

---

Proyecto académico de inscripción universitaria · Control de Estudios · PNF.
