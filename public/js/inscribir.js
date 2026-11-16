/* Formulario de inscripcion: carga de asignaturas y resumen en vivo */
(() => {
  const form = document.querySelector('.inscripcion-form');
  if (!form) return;

  const carreraSelect = document.getElementById('carrera_id');
  const trayectoSelect = document.getElementById('trayecto');
  const lista = document.getElementById('lista-asignaturas');
  const contador = document.getElementById('contador-asignaturas');
  const resumenCarrera = document.getElementById('resumen-carrera');
  const resumenTrayecto = document.getElementById('resumen-trayecto');
  const resumenIngreso = document.getElementById('resumen-ingreso');
  const resumenAsignaturas = document.getElementById('resumen-asignaturas');
  const ROMANOS = ['I', 'II', 'III', 'IV', 'V'];

  const textoSeleccionado = (select, sinSeleccion) => {
    if (!select || select.selectedIndex <= 0) return sinSeleccion;
    return select.options[select.selectedIndex].text.trim();
  };

  const actualizarResumen = () => {
    if (!lista) return;
    const marcadas = lista.querySelectorAll('input[name="asignaturas"]:checked');
    const ingreso = form.querySelector('input[name="tipo_ingreso"]:checked');

    if (contador) {
      contador.textContent = `${marcadas.length} seleccionada${marcadas.length === 1 ? '' : 's'}`;
    }
    if (resumenCarrera) resumenCarrera.textContent = textoSeleccionado(carreraSelect, 'Sin seleccionar');
    if (resumenTrayecto) resumenTrayecto.textContent = textoSeleccionado(trayectoSelect, 'Sin seleccionar');
    if (resumenIngreso) resumenIngreso.textContent = ingreso ? ingreso.parentElement.textContent.trim() : 'Sin seleccionar';
    if (resumenAsignaturas) resumenAsignaturas.textContent = String(marcadas.length);
  };

  const crearOpcion = (asignatura) => {
    const label = document.createElement('label');
    label.className = 'opcion-asignatura';

    const input = document.createElement('input');
    input.type = 'checkbox';
    input.name = 'asignaturas';
    input.value = asignatura.id;
    input.checked = true;

    const span = document.createElement('span');
    const nombre = document.createElement('strong');
    nombre.textContent = asignatura.nombre;
    const codigo = document.createElement('small');
    codigo.textContent = asignatura.codigo;
    span.append(nombre, codigo);

    label.append(input, span);
    return label;
  };

  const mostrarMensaje = (texto, esError = false) => {
    if (!lista) return;
    lista.replaceChildren();
    const mensaje = document.createElement('p');
    mensaje.className = esError ? 'error-campo' : 'asignaturas-vacio';
    mensaje.textContent = texto;
    lista.append(mensaje);
  };

  async function cargarAsignaturas() {
    const carrera = carreraSelect?.value;
    const trayecto = trayectoSelect?.value;

    if (!carrera || !trayecto) {
      mostrarMensaje('Selecciona una carrera y un trayecto para ver las asignaturas.');
      actualizarResumen();
      return;
    }

    mostrarMensaje('Cargando asignaturas…');

    try {
      const respuesta = await fetch(
        `/api/asignaturas?carrera_id=${encodeURIComponent(carrera)}&trayecto=${encodeURIComponent(trayecto)}`,
        { headers: { Accept: 'application/json' } }
      );
      if (!respuesta.ok) throw new Error('Respuesta no valida');

      const datos = await respuesta.json();
      const asignaturas = Array.isArray(datos.asignaturas) ? datos.asignaturas : [];

      if (asignaturas.length === 0) {
        mostrarMensaje('No hay asignaturas registradas para esta carrera y trayecto.');
      } else {
        if (lista) {
          lista.replaceChildren(...asignaturas.map(crearOpcion));
        }
      }
    } catch {
      mostrarMensaje('No fue posible cargar las asignaturas. Recarga la página e intenta de nuevo.', true);
    }

    actualizarResumen();
  }

  carreraSelect?.addEventListener('change', cargarAsignaturas);
  trayectoSelect?.addEventListener('change', cargarAsignaturas);
  lista?.addEventListener('change', actualizarResumen);
  form.addEventListener('change', (evento) => {
    if (evento.target.name === 'tipo_ingreso') actualizarResumen();
  });

  actualizarResumen();
})();
