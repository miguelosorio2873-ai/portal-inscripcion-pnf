/**
 * Seguridad: hash de contrasenas, sesiones y proteccion CSRF.
 */
import { randomBytes, scryptSync, timingSafeEqual } from 'node:crypto';

const COOKIE_SESION = 'pnf_sid';
const DURACION_SESION_MS = 1000 * 60 * 60 * 8; // 8 horas
const sesiones = new Map();

/* ------------------------------ Contrasenas ------------------------------ */

const LARGO_HASH = 64;

export function hashPassword(password) {
  const sal = randomBytes(16).toString('hex');
  const hash = scryptSync(password, sal, LARGO_HASH).toString('hex');
  return `scrypt$${sal}$${hash}`;
}

export function verificarPassword(password, guardado) {
  const [algoritmo, sal, hash] = String(guardado || '').split('$');
  if (algoritmo !== 'scrypt' || !sal || !hash) return false;
  const calculado = scryptSync(password, sal, LARGO_HASH);
  const esperado = Buffer.from(hash, 'hex');
  if (calculado.length !== esperado.length) return false;
  return timingSafeEqual(calculado, esperado);
}

/* -------------------------------- Sesiones ------------------------------- */

function leerCookies(encabezado) {
  const cookies = {};
  for (const parte of String(encabezado || '').split(';')) {
    const indice = parte.indexOf('=');
    if (indice === -1) continue;
    cookies[parte.slice(0, indice).trim()] = decodeURIComponent(parte.slice(indice + 1).trim());
  }
  return cookies;
}

function crearSesion(res) {
  const id = randomBytes(32).toString('hex');
  const datos = {
    id,
    estudianteId: null,
    csrf: randomBytes(24).toString('hex'),
    flash: null,
    creada: Date.now(),
  };
  sesiones.set(id, datos);
  res.cookie(COOKIE_SESION, id, {
    httpOnly: true,
    sameSite: 'lax',
    maxAge: DURACION_SESION_MS,
    path: '/',
  });
  return datos;
}

export function iniciarSesion(res, estudianteId) {
  const sesion = crearSesion(res);
  sesion.estudianteId = Number(estudianteId);
  return sesion;
}

export function cerrarSesion(req, res) {
  if (req.session) sesiones.delete(req.session.id);
  res.clearCookie(COOKIE_SESION, { path: '/' });
}

export function sesionActual(req) {
  return req.session ?? null;
}

export function middlewareSesion(req, res, next) {
  const cookies = leerCookies(req.headers.cookie);
  const id = cookies[COOKIE_SESION];
  let sesion = id ? sesiones.get(id) : null;

  if (sesion && Date.now() - sesion.creada > DURACION_SESION_MS) {
    sesiones.delete(sesion.id);
    sesion = null;
  }

  if (!sesion) sesion = crearSesion(res);

  req.session = sesion;
  res.locals.csrfToken = sesion.csrf;
  res.locals.flash = sesion.flash;
  sesion.flash = null;
  next();
}

export function establecerFlash(req, tipo, texto) {
  if (req.session) req.session.flash = { tipo, texto };
}

export function verificarCsrf(req, res, next) {
  const enviado = String(req.body?._csrf || req.headers['x-csrf-token'] || '');
  if (!req.session || !enviado || enviado !== req.session.csrf) {
    return res.status(403).render('error', {
      titulo: 'Sesion expirada',
      mensaje: 'El formulario expiro o la sesion no es valida. Vuelve a cargar la pagina e intentalo de nuevo.',
    });
  }
  next();
}

// Limpieza periodica de sesiones vencidas.
const limpieza = setInterval(() => {
  const ahora = Date.now();
  for (const [id, sesion] of sesiones) {
    if (ahora - sesion.creada > DURACION_SESION_MS) sesiones.delete(id);
  }
}, 15 * 60 * 1000);
limpieza.unref();
