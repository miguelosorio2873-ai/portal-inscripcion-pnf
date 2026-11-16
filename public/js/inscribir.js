/* Formulario de inscripcion: carga de asignaturas y resumen en vivo */
(() => {
  const form = document.querySelector('.inscripcion-form');
  if (!form) return;

  const carreraSelect = document.getElementById('carrera_id');
  const semestreSelect = document.getElementById('semestre');
  const lista = document.getElementById('lista-asignaturas');
  const contador = document.getElementById('contador-asignaturas');
  const resumenCarrera = document.getElementById('resumen-carrera');
  const resumenSemestre = document.getElementById('resumen-semestre');
  const resumenTurno = document.getElementById('resumen-turno');
  const resumenCondicion = document.getElementById('resumen-condicion');
  const resumenAsignaturas = document.getElementById('resumen-asignaturas');
  const ROMANOS = ['I', 'II', 'III', 'IV', 'V'];

  const textoSeleccionado = (select, sinSeleccion) => {
    if (!select || select.selectedIndex <= 0) return sinSeleccion;
    return select.options[select.selectedIndex].text.trim();
  };

  const actualizarResumen = () => {
    if (!lista) return;
    const marcadas = lista.querySelectorAll('input[name="asignaturas"]:checked');
    const turno = form.querySelector('input[name="turno"]:checked');
    const condicion = form.querySelector('input[name="condicion"]:checked');
    const semestre = Number(semestreSelect?.value || 0);

    if (contador) {
      contador.textContent = `${marcadas.length} seleccionada${marcadas.length === 1 ? '' : 's'}`;
    }
    if (resumenCarrera) resumenCarrera.textContent = textoSeleccionado(carreraSelect, 'Sin seleccionar');
    if (resumenSemestre) {
      resumenSemestre.textContent = semestre ? `Semestre ${ROMANOS[semestre - 1] || semestre}` : 'Sin seleccionar';
    }
    if (resumenTurno) resumenTurno.textContent = turno ? turno.value : 'Sin seleccionar';
    if (resumenCondicion) resumenCondicion.textContent = condicion ? condicion.value : 'Sin seleccionar';
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
    const semestre = semestreSelect?.value;

    if (!carrera || !semestre) {
      mostrarMensaje('Selecciona una carrera y un semestre para ver las asignaturas.');
      actualizarResumen();
      return;
    }

    mostrarMensaje('Cargando asignaturas…');

    try {
      const respuesta = await fetch(
        `/api/asignaturas?carrera_id=${encodeURIComponent(carrera)}&semestre=${encodeURIComponent(semestre)}`,
        { headers: { Accept: 'application/json' } }
      );
      if (!respuesta.ok) throw new Error('Respuesta no valida');

      const datos = await respuesta.json();
      const asignaturas = Array.isArray(datos.asignaturas) ? datos.asignaturas : [];

      if (asignaturas.length === 0) {
        mostrarMensaje('No hay asignaturas registradas para esta carrera y semestre.');
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
  semestreSelect?.addEventListener('change', cargarAsignaturas);
  lista?.addEventListener('change', actualizarResumen);
  form.addEventListener('change', (evento) => {
    if (evento.target.name === 'turno' || evento.target.name === 'condicion') actualizarResumen();
  });

  actualizarResumen();
})();
