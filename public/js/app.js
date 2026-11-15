/* Comportamiento general del portal */
(() => {
  // Oculta los avisos de exito despues de unos segundos.
  const avisos = document.querySelectorAll('.aviso');
  avisos.forEach((aviso) => {
    setTimeout(() => {
      aviso.classList.add('aviso-oculto');
      setTimeout(() => aviso.remove(), 500);
    }, 8000);
  });

  // Evita envios duplicados del mismo formulario.
  document.querySelectorAll('form').forEach((form) => {
    form.addEventListener('submit', () => {
      const boton = form.querySelector('button[type="submit"]');
      if (!boton) return;
      setTimeout(() => {
        boton.disabled = true;
        boton.style.opacity = '0.6';
        boton.style.cursor = 'wait';
      }, 0);
    });
  });
})();
