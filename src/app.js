/**
 * Aplicacion web del portal de inscripcion universitaria (PNF).
 */
import express from 'express';
import path from 'node:path';
import {
  ROOT_DIR,
  listarCarreras,
  obtenerCarrera,
  listarAsignaturas,
  crearEstudiante,
  estudiantePorId,
  estudiantePorCedula,
  estudiantePorEmail,
  crearInscripcion,
  inscripcionesDeEstudiante,
  obtenerInscripcion,
  asignaturasDeInscripcion,
  contarInscripcionesPorCarrera,
  totalInscripciones,
} from './db.js';
import {
  hashPassword,
  verificarPassword,
  iniciarSesion,
  cerrarSesion,
  middlewareSesion,
  establecerFlash,
  verificarCsrf,
} from './seguridad.js';

/**
 * Modelo unico compartido con la aplicacion de escritorio en Visual Basic:
 * la inscripcion se organiza por TRAYECTO y por TIPO DE INGRESO. No hay turnos.
 */
export const TRAYECTOS = [1, 2, 3, 4, 5];
export const TIPOS_INGRESO = [
  { valor: 'OPSU', etiqueta: 'OPSU', descripcion: 'Asignado por el Sistema Nacional de Ingreso' },
  { valor: 'FLOTANTE', etiqueta: 'Flotante', descripcion: 'Cursa sin pertenecer a la matricula regular del PNF' },
];
const ROMANOS = ['I', 'II', 'III', 'IV', 'V'];

/* -------------------------------------------------------------------------- */
/* Utilidades de formato y validacion                                         */
/* -------------------------------------------------------------------------- */

export function numeroRomano(numero) {
  return ROMANOS[Number(numero) - 1] || String(numero);
}

/** Devuelve la etiqueta legible del tipo de ingreso. */
export function etiquetaTipoIngreso(valor) {
  const encontrado = TIPOS_INGRESO.find((tipo) => tipo.valor === valor);
  return encontrado ? encontrado.etiqueta : 'Flotante';
}

function interpretarFecha(valor) {
  const texto = String(valor);
  // MySQL devuelve fechas locales como "YYYY-MM-DD" o "YYYY-MM-DD HH:MM:SS".
  // Se agrega la hora para que una fecha sin hora no se interprete como UTC.
  if (/^\d{4}-\d{2}-\d{2}$/.test(texto)) return new Date(`${texto}T00:00:00`);
  return new Date(texto.replace(' ', 'T'));
}

export function formatearFecha(valor, incluirHora = true) {
  if (!valor) return '—';
  const fecha = interpretarFecha(valor);
  if (Number.isNaN(fecha.getTime())) return String(valor);
  const dia = String(fecha.getDate()).padStart(2, '0');
  const mes = String(fecha.getMonth() + 1).padStart(2, '0');
  const anio = fecha.getFullYear();
  if (!incluirHora) return `${dia}/${mes}/${anio}`;
  const horas = String(fecha.getHours()).padStart(2, '0');
  const minutos = String(fecha.getMinutes()).padStart(2, '0');
  return `${dia}/${mes}/${anio} ${horas}:${minutos}`;
}

export function fechaLarga(valor) {
  if (!valor) return '—';
  const fecha = interpretarFecha(valor);
  if (Number.isNaN(fecha.getTime())) return String(valor);
  const meses = [
    'enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio',
    'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre',
  ];
  return `${fecha.getDate()} de ${meses[fecha.getMonth()]} de ${fecha.getFullYear()}`;
}

export function normalizarCedula(valor) {
  const limpio = String(valor || '').toUpperCase().replace(/[^VEJ0-9]/g, '');
  const coincidencia = limpio.match(/^([VEJ]?)(\d{5,9})$/);
  if (!coincidencia) return null;
  return `${coincidencia[1] || 'V'}-${coincidencia[2]}`;
}

function esEmailValido(valor) {
  return /^[^\s@]+@[^\s@]+\.[a-zA-Z]{2,}$/.test(String(valor || '').trim());
}

function soloLetras(valor) {
  return /^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ' .-]{2,60}$/.test(String(valor || '').trim());
}

function esFechaValida(valor) {
  if (!valor) return true;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(valor)) return false;
  const fecha = new Date(`${valor}T00:00:00`);
  return !Number.isNaN(fecha.getTime()) && fecha.getFullYear() >= 1940;
}

function texto(valor) {
  return String(valor ?? '').trim();
}

/* -------------------------------------------------------------------------- */
/* Aplicacion                                                                 */
/* -------------------------------------------------------------------------- */

export const app = express();

app.disable('x-powered-by');
app.set('view engine', 'ejs');
app.set('views', path.join(ROOT_DIR, 'views'));

app.use((req, res, next) => {
  res.setHeader('X-Content-Type-Options', 'nosniff');
  res.setHeader('X-Frame-Options', 'SAMEORIGIN');
  res.setHeader('Referrer-Policy', 'strict-origin-when-cross-origin');
  res.setHeader(
    'Content-Security-Policy',
    "default-src 'self'; style-src 'self'; script-src 'self'; img-src 'self' data:; form-action 'self'; frame-ancestors 'self'"
  );
  next();
});

app.use(express.urlencoded({ extended: false, limit: '100kb' }));
app.use(express.static(path.join(ROOT_DIR, 'public'), { maxAge: 0 }));
app.use(middlewareSesion);

app.use(async (req, res, next) => {
  res.locals.estudiante = null;
  if (req.session.estudianteId) {
    const estudiante = await estudiantePorId(req.session.estudianteId);
    if (estudiante) {
      res.locals.estudiante = estudiante;
    } else {
      req.session.estudianteId = null;
    }
  }
  res.locals.rutaActiva = req.path;
  res.locals.titulo = 'Portal de Inscripción';
  res.locals.formatearFecha = formatearFecha;
  res.locals.fechaLarga = fechaLarga;
  res.locals.numeroRomano = numeroRomano;
  res.locals.etiquetaTipoIngreso = etiquetaTipoIngreso;
  res.locals.trayectos = TRAYECTOS;
  res.locals.tiposIngreso = TIPOS_INGRESO;
  res.locals.anio = new Date().getFullYear();
  next();
});

function requiereEstudiante(req, res, next) {
  if (!res.locals.estudiante) {
    establecerFlash(req, 'error', 'Inicia sesión para continuar con tu inscripción.');
    return res.redirect(`/ingresar?destino=${encodeURIComponent(req.originalUrl)}`);
  }
  next();
}

/* ------------------------------- Portada --------------------------------- */

app.get('/', async (req, res) => {
  const [carreras, total, porCarrera] = await Promise.all([
    listarCarreras(),
    totalInscripciones(),
    contarInscripcionesPorCarrera(),
  ]);
  res.render('index', {
    titulo: 'Inicio',
    carreras,
    total,
    porCarrera,
  });
});

/* ------------------------------ Registro --------------------------------- */

app.get('/registro', (req, res) => {
  if (res.locals.estudiante) return res.redirect('/panel');
  res.render('registro', { titulo: 'Crear cuenta', valores: {}, errores: {} });
});

app.post('/registro', verificarCsrf, async (req, res) => {
  if (res.locals.estudiante) return res.redirect('/panel');

  const valores = {
    cedula: texto(req.body.cedula),
    nombres: texto(req.body.nombres),
    apellidos: texto(req.body.apellidos),
    email: texto(req.body.email).toLowerCase(),
    telefono: texto(req.body.telefono),
    fecha_nacimiento: texto(req.body.fecha_nacimiento),
    sexo: texto(req.body.sexo),
    direccion: texto(req.body.direccion),
  };
  const password = String(req.body.password || '');
  const confirmar = String(req.body.confirmar_password || '');
  const errores = {};

  const cedula = normalizarCedula(valores.cedula);
  if (!cedula) errores.cedula = 'Ingresa una cédula válida (ejemplo: V-12345678).';
  if (!soloLetras(valores.nombres)) errores.nombres = 'Indica tus nombres (solo letras).';
  if (!soloLetras(valores.apellidos)) errores.apellidos = 'Indica tus apellidos (solo letras).';
  if (!esEmailValido(valores.email)) errores.email = 'Ingresa un correo electrónico válido.';
  if (valores.email.length > 190) errores.email = 'El correo no puede superar 190 caracteres.';
  if (valores.telefono && !/^[0-9+()\-\s]{7,20}$/.test(valores.telefono)) {
    errores.telefono = 'Ingresa un teléfono válido.';
  }
  if (!esFechaValida(valores.fecha_nacimiento)) errores.fecha_nacimiento = 'La fecha de nacimiento no es válida.';
  if (valores.sexo && !['F', 'M', 'O'].includes(valores.sexo)) errores.sexo = 'Selecciona una opción válida.';
  if (valores.direccion.length > 255) errores.direccion = 'La dirección no puede superar 255 caracteres.';
  if (password.length < 8) errores.password = 'La contraseña debe tener al menos 8 caracteres.';
  if (password !== confirmar) errores.confirmar_password = 'Las contraseñas no coinciden.';

  if (cedula && (await estudiantePorCedula(cedula))) errores.cedula = 'Ya existe una cuenta registrada con esta cédula.';
  if (!errores.email && (await estudiantePorEmail(valores.email))) {
    errores.email = 'Ya existe una cuenta registrada con este correo.';
  }

  if (Object.keys(errores).length > 0) {
    return res.status(422).render('registro', { titulo: 'Crear cuenta', valores, errores });
  }

  let id;
  try {
    id = await crearEstudiante({
      ...valores,
      cedula,
      password_hash: hashPassword(password),
    });
  } catch (error) {
    if (error.code === 'ER_DUP_ENTRY') {
      const mensaje = String(error.sqlMessage || error.message).toLowerCase();
      if (mensaje.includes('cedula')) {
        errores.cedula = 'Ya existe una cuenta registrada con esta cédula.';
      } else if (mensaje.includes('email')) {
        errores.email = 'Ya existe una cuenta registrada con este correo.';
      } else {
        throw error;
      }
      return res.status(409).render('registro', { titulo: 'Crear cuenta', valores, errores });
    }
    throw error;
  }

  iniciarSesion(res, id);
  establecerFlash(req, 'exito', '¡Cuenta creada! Ya puedes realizar tu inscripción.');
  res.redirect('/panel');
});

/* ------------------------------- Ingreso --------------------------------- */

const intentosIngreso = new Map();
const MAX_INTENTOS = 8;
const VENTANA_INTENTOS_MS = 10 * 60 * 1000;

function bloqueadoPorIntentos(ip) {
  const registro = intentosIngreso.get(ip);
  if (!registro) return false;
  if (Date.now() - registro.inicio > VENTANA_INTENTOS_MS) {
    intentosIngreso.delete(ip);
    return false;
  }
  return registro.cantidad >= MAX_INTENTOS;
}

function registrarIntentoFallido(ip) {
  const registro = intentosIngreso.get(ip);
  if (!registro || Date.now() - registro.inicio > VENTANA_INTENTOS_MS) {
    intentosIngreso.set(ip, { cantidad: 1, inicio: Date.now() });
  } else {
    registro.cantidad += 1;
  }
}

app.get('/ingresar', (req, res) => {
  if (res.locals.estudiante) return res.redirect('/panel');
  res.render('ingresar', {
    titulo: 'Iniciar sesión',
    valores: {},
    error: null,
    destino: texto(req.query.destino),
  });
});

app.post('/ingresar', verificarCsrf, async (req, res) => {
  const identificador = texto(req.body.identificador);
  const password = String(req.body.password || '');
  const destino = texto(req.body.destino);
  const renderError = (mensaje) =>
    res.status(401).render('ingresar', {
      titulo: 'Iniciar sesión',
      valores: { identificador },
      error: mensaje,
      destino,
    });

  if (bloqueadoPorIntentos(req.ip)) {
    return res.status(429).render('ingresar', {
      titulo: 'Iniciar sesión',
      valores: { identificador },
      error: 'Demasiados intentos fallidos. Espera unos minutos antes de volver a intentar.',
      destino,
    });
  }

  if (!identificador || !password) return renderError('Completa cédula o correo y contraseña.');

  const estudiante = identificador.includes('@')
    ? await estudiantePorEmail(identificador.toLowerCase())
    : await estudiantePorCedula(normalizarCedula(identificador) || identificador);

  if (!estudiante || !verificarPassword(password, estudiante.password_hash)) {
    registrarIntentoFallido(req.ip);
    return renderError('Las credenciales no son correctas. Verifica tus datos.');
  }

  intentosIngreso.delete(req.ip);
  iniciarSesion(res, estudiante.id);
  establecerFlash(req, 'exito', `Bienvenido(a) de nuevo, ${estudiante.nombres}.`);

  if (destino.startsWith('/') && !destino.startsWith('//')) return res.redirect(destino);
  res.redirect('/panel');
});

app.post('/salir', verificarCsrf, (req, res) => {
  cerrarSesion(req, res);
  res.redirect('/');
});

/* -------------------------------- Panel ---------------------------------- */

app.get('/panel', requiereEstudiante, async (req, res) => {
  const estudiante = res.locals.estudiante;
  res.render('panel', {
    titulo: 'Mi panel',
    inscripciones: await inscripcionesDeEstudiante(estudiante.id),
  });
});

/* ------------------------------ Inscripcion ------------------------------ */

app.get('/inscribir', requiereEstudiante, async (req, res) => {
  const carrera = await obtenerCarrera(req.query.carrera || 0);
  const estudiante = res.locals.estudiante;
  res.render('inscribir', {
    titulo: 'Nueva inscripción',
    carreras: await listarCarreras(),
    valores: {
      carrera_id: carrera ? carrera.id : '',
      trayecto: '',
      tipo_ingreso: estudiante.modalidad === 'OPSU' ? 'OPSU' : 'FLOTANTE',
      observaciones: '',
    },
    errores: {},
    paginaScript: '/js/inscribir.js',
  });
});

app.post('/inscribir', requiereEstudiante, verificarCsrf, async (req, res) => {
  const estudiante = res.locals.estudiante;
  const carreras = await listarCarreras();
  const valores = {
    carrera_id: texto(req.body.carrera_id),
    trayecto: texto(req.body.trayecto),
    tipo_ingreso: texto(req.body.tipo_ingreso).toUpperCase(),
    observaciones: texto(req.body.observaciones).slice(0, 500),
  };
  const errores = {};

  const carrera = await obtenerCarrera(valores.carrera_id);
  const trayecto = Number(valores.trayecto);
  const tipoIngreso = TIPOS_INGRESO.some((tipo) => tipo.valor === valores.tipo_ingreso)
    ? valores.tipo_ingreso
    : null;

  if (!carrera) errores.carrera_id = 'Selecciona una carrera.';
  if (!Number.isInteger(trayecto) || !TRAYECTOS.includes(trayecto)) {
    errores.trayecto = 'Selecciona un trayecto válido.';
  }
  if (!tipoIngreso) errores.tipo_ingreso = 'Indica el tipo de ingreso (OPSU o flotante).';

  const disponibles = carrera && TRAYECTOS.includes(trayecto)
    ? await listarAsignaturas(carrera.id, trayecto)
    : [];
  const idsDisponibles = new Set(disponibles.map((a) => a.id));
  const seleccionadas = (Array.isArray(req.body.asignaturas)
    ? req.body.asignaturas
    : req.body.asignaturas
      ? [req.body.asignaturas]
      : []
  )
    .map((id) => Number(id))
    .filter((id) => idsDisponibles.has(id));

  if (disponibles.length > 0 && seleccionadas.length === 0) {
    errores.asignaturas = 'Selecciona al menos una asignatura del trayecto.';
  }

  const renderFormulario = (estado = 422) =>
    res.status(estado).render('inscribir', {
      titulo: 'Nueva inscripción',
      carreras,
      valores: { ...valores, asignaturas: seleccionadas, disponibles },
      errores,
      paginaScript: '/js/inscribir.js',
    });

  if (Object.keys(errores).length > 0) return renderFormulario();

  let codigo;
  try {
    codigo = await crearInscripcion({
      estudianteId: estudiante.id,
      carreraId: carrera.id,
      trayecto,
      tipoIngreso,
      asignaturaIds: seleccionadas,
      observaciones: valores.observaciones,
    });
  } catch (error) {
    if (error.code === 'ER_DUP_ENTRY') {
      errores.general =
        'Ya tienes una inscripción activa para esta carrera y trayecto. Revisa tu panel o comunícate con Control de Estudios.';
      return renderFormulario(409);
    }
    throw error;
  }

  establecerFlash(req, 'exito', `Inscripción registrada. Tu comprobante es ${codigo}.`);
  res.redirect(`/comprobante/${encodeURIComponent(codigo)}`);
});

/* ------------------------------ Comprobante ------------------------------ */

app.get('/comprobante/:codigo', requiereEstudiante, async (req, res, next) => {
  const inscripcion = await obtenerInscripcion(req.params.codigo);
  if (!inscripcion) return next();

  if (inscripcion.estudiante_id !== res.locals.estudiante.id) {
    return res.status(403).render('error', {
      titulo: 'Acceso restringido',
      mensaje: 'Este comprobante pertenece a otro estudiante.',
    });
  }

  res.render('comprobante', {
    titulo: `Comprobante ${inscripcion.codigo}`,
    inscripcion,
    asignaturas: await asignaturasDeInscripcion(inscripcion.id),
    autoImprimir: req.query.imprimir === '1',
    paginaScript: '/js/comprobante.js',
  });
});

/* --------------------------------- API ----------------------------------- */

app.get('/api/asignaturas', async (req, res) => {
  const carrera = await obtenerCarrera(req.query.carrera_id);
  const trayecto = Number(req.query.trayecto);
  if (!carrera || !TRAYECTOS.includes(trayecto)) {
    return res.status(400).json({ error: 'Carrera o trayecto no válido.' });
  }
  res.json({ asignaturas: await listarAsignaturas(carrera.id, trayecto) });
});

app.get('/api/carreras', async (req, res) => {
  res.json({ carreras: await listarCarreras() });
});

/* ------------------------------- Errores --------------------------------- */

app.use((req, res) => {
  res.status(404).render('404', { titulo: 'Página no encontrada' });
});

app.use((error, req, res, next) => {
  console.error('[error]', error);
  if (res.headersSent) return next(error);
  res.status(500).render('error', {
    titulo: 'Error del servidor',
    mensaje: 'Ocurrió un problema inesperado. Intenta de nuevo en unos momentos.',
  });
});

export default app;
