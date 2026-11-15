/**
 * Configuracion de la conexion MySQL/MariaDB compartida por el portal web
 * y la aplicacion de escritorio en Visual Basic.
 *
 * Los valores se pueden cambiar con variables de entorno:
 *   DB_HOST, DB_PORT, DB_USER, DB_PASSWORD, DB_NAME
 *
 * Por defecto se usa la instalacion local de XAMPP:
 *   root sin contrasena, 127.0.0.1:3306, base portal_pnf
 */

const NOMBRE_POR_DEFECTO = 'portal_pnf';

function texto(valor) {
  return typeof valor === 'string' ? valor.trim() : '';
}

const nombre = texto(process.env.DB_NAME) || NOMBRE_POR_DEFECTO;

// El nombre se interpola en CREATE DATABASE, por eso solo se permiten
// caracteres seguros de identificador SQL.
if (!/^[A-Za-z0-9_]+$/.test(nombre)) {
  throw new Error(
    `El nombre de la base de datos "${nombre}" no es valido. Usa solo letras, numeros y guiones bajos.`
  );
}

const puerto = Number(process.env.DB_PORT || 3306);
if (!Number.isInteger(puerto) || puerto < 1 || puerto > 65535) {
  throw new Error(`El puerto de MySQL "${process.env.DB_PORT}" no es valido.`);
}

export const DB_NAME = nombre;

export const DB_SETTINGS = Object.freeze({
  host: texto(process.env.DB_HOST) || '127.0.0.1',
  port: puerto,
  user: texto(process.env.DB_USER) || 'root',
  password: process.env.DB_PASSWORD ?? '',
});
