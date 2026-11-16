/**
 * Crea estudiantes e inscripciones de ejemplo para probar el portal
 * y el sistema de escritorio en Visual Basic.
 *
 * Uso: npm run demo
 */
import {
  DB_NAME,
  listarCarreras,
  listarAsignaturas,
  crearEstudiante,
  estudiantePorCedula,
  crearInscripcion,
} from '../src/db.js';
import { hashPassword } from '../src/seguridad.js';

const CARRERAS = Object.fromEntries((await listarCarreras()).map((c) => [c.codigo, c]));

const estudiantes = [
  {
    cedula: 'V-12345678',
    nombres: 'María',
    apellidos: 'Pérez González',
    email: 'demo@pnf.edu.ve',
    telefono: '0414-1234567',
    fecha_nacimiento: '2003-05-18',
    sexo: 'F',
    direccion: 'Av. Universidad, sector Centro, Barquisimeto',
    modalidad: 'OPSU',
    inscripciones: [
      { carrera: 'INF', trayecto: 3, tipoIngreso: 'OPSU', observaciones: 'Estudiante de ejemplo para demostración.' },
      { carrera: 'AGR', trayecto: 1, tipoIngreso: 'FLOTANTE', observaciones: null },
    ],
  },
  {
    cedula: 'V-87654321',
    nombres: 'José',
    apellidos: 'Rodríguez',
    email: 'jose@pnf.edu.ve',
    telefono: '0424-7654321',
    fecha_nacimiento: '2001-11-02',
    sexo: 'M',
    direccion: 'Calle 5 con Av. Bolívar, Barquisimeto',
    modalidad: 'OPSU',
    inscripciones: [
      { carrera: 'VET', trayecto: 2, tipoIngreso: 'OPSU', observaciones: 'Segunda inscripción de ejemplo.' },
    ],
  },
];

const PASSWORD = 'Demo1234';
let creados = 0;

for (const datos of estudiantes) {
  const existente = await estudiantePorCedula(datos.cedula);
  const id = existente
    ? existente.id
    : await crearEstudiante({ ...datos, password_hash: hashPassword(PASSWORD) });

  if (!existente) creados += 1;

  for (const solicitud of datos.inscripciones) {
    const carrera = CARRERAS[solicitud.carrera];
    const asignaturas = await listarAsignaturas(carrera.id, solicitud.trayecto);
    try {
      const codigo = await crearInscripcion({
        estudianteId: id,
        carreraId: carrera.id,
        trayecto: solicitud.trayecto,
        tipoIngreso: solicitud.tipoIngreso,
        asignaturaIds: asignaturas.map((a) => a.id),
        observaciones: solicitud.observaciones,
      });
      console.log(`  ${datos.cedula}  ${solicitud.carrera} trayecto ${solicitud.trayecto}  ${codigo}`);
    } catch (error) {
      if (error.code === 'ER_DUP_ENTRY') {
        console.log(`  ${datos.cedula}  ${solicitud.carrera} trayecto ${solicitud.trayecto}  (ya existía)`);
      } else {
        throw error;
      }
    }
  }
}

console.log('');
console.log(`Base de datos: MySQL ${DB_NAME}`);
console.log(`Estudiantes nuevos: ${creados}`);
console.log('Credenciales de ejemplo:  demo@pnf.edu.ve  /  Demo1234');
console.log('                          V-12345678        /  Demo1234');
