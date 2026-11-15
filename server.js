/**
 * Arranque del servidor web.
 */
import app from './src/app.js';
import { DB_NAME, pool } from './src/db.js';

const PUERTO = Number(process.env.PORT) || 3000;

const servidor = app.listen(PUERTO, () => {
  console.log('');
  console.log('  Portal de Inscripción · Programas Nacionales de Formación');
  console.log(`  Servidor web:  http://localhost:${PUERTO}`);
  console.log(`  Base de datos: MySQL · ${DB_NAME}`);
  console.log('  Detener:       Ctrl + C');
  console.log('');
});

servidor.on('error', (error) => {
  if (error.code === 'EADDRINUSE') {
    console.error(`\n  El puerto ${PUERTO} ya está en uso. Cierra el proceso que lo ocupa o define otro puerto:`);
    console.error(`  PowerShell:  $env:PORT=3001; npm start\n`);
    process.exit(1);
  }
  throw error;
});

function cerrar() {
  servidor.close(async () => {
    try {
      await pool.end();
    } catch {
      /* el pool ya estaba cerrado */
    }
    process.exit(0);
  });
}

process.on('SIGINT', cerrar);
process.on('SIGTERM', cerrar);
