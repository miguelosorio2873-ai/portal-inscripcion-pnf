/* Impresión del comprobante: Visual Basic o navegador */
(() => {
  const botonNavegador = document.getElementById('boton-imprimir');
  if (botonNavegador) {
    botonNavegador.addEventListener('click', () => window.print());
  }

  // ?imprimir=1 resalta la opción de Visual Basic, pero no abre el diálogo del navegador.
  const comprobante = document.getElementById('comprobante');
  const botonVb = document.getElementById('boton-imprimir-vb');
  if (comprobante && comprobante.dataset.autoImprimir === '1' && botonVb) {
    window.addEventListener('load', () => {
      botonVb.classList.add('resaltado');
      botonVb.focus();
      window.setTimeout(() => botonVb.classList.remove('resaltado'), 2600);
    });
  }
})();
