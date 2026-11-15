/**
 * Elimina la base de datos MySQL del portal y la recrea con las carreras y
 * asignaturas iniciales. Uso: npm run db:reset
 *
 * ATENCION: borra todas las inscripciones y estudiantes de la base indicada
 * en DB_NAME (por defecto portal_pnf). El archivo SQLite de respaldo no se toca.
 */
import mysql from 'mysql2/promise';
import { DB_NAME, DB_SETTINGS } from '../src/config-db.js';

const administrador = await mysql.createConnection(DB_SETTINGS);
try {
  await administrador.query(`DROP DATABASE IF EXISTS \`${DB_NAME}\``);
  await administrador.query(
    `CREATE DATABASE \`${DB_NAME}\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci`
  );
} finally {
  await administrador.end();
}

const modulo = await import('../src/db.js');
await modulo.pool.end();

console.log('Base de datos MySQL recreada correctamente.');
console.log(`Base: ${DB_NAME}`);
console.log('Carreras: PNF Informática, PNF Veterinaria, PNF Administración, PNF Agroalimentaria.');
