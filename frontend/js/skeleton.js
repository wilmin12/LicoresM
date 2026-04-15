/**
 * skeleton.js — Licores Maduro
 * Utilidades para mostrar/ocultar skeleton loaders en tablas y KPI cards.
 */

const Skeleton = (() => {

  /**
   * Muestra filas skeleton en un <tbody>.
   * @param {string|HTMLElement} tbodyOrSelector
   * @param {number} cols   — número de columnas visibles
   * @param {number} rows   — filas a mostrar (default 5)
   */
  function showTable(tbodyOrSelector, cols, rows = 5) {
    const tbody = typeof tbodyOrSelector === 'string'
      ? document.querySelector(tbodyOrSelector)
      : tbodyOrSelector;
    if (!tbody) return;

    const widths = ['skeleton-w-90', 'skeleton-w-70', 'skeleton-w-55', 'skeleton-w-40', 'skeleton-w-25'];
    let html = '';
    for (let r = 0; r < rows; r++) {
      html += '<tr class="skeleton-row">';
      for (let c = 0; c < cols; c++) {
        const w = widths[(r + c) % widths.length];
        html += `<td><span class="skeleton ${w}"></span></td>`;
      }
      html += '</tr>';
    }
    tbody.innerHTML = html;
  }

  /**
   * Limpia el skeleton de un tbody (para reemplazar con datos reales).
   * @param {string|HTMLElement} tbodyOrSelector
   */
  function hideTable(tbodyOrSelector) {
    const tbody = typeof tbodyOrSelector === 'string'
      ? document.querySelector(tbodyOrSelector)
      : tbodyOrSelector;
    if (!tbody) return;
    tbody.innerHTML = '';
  }

  /**
   * Muestra skeleton dentro de un contenedor de KPI card.
   * El contenedor debe tener elementos con clases: .kpi-title, .kpi-value, .kpi-sub
   * @param {string|HTMLElement} containerOrSelector
   */
  function showKpi(containerOrSelector) {
    const el = typeof containerOrSelector === 'string'
      ? document.querySelector(containerOrSelector)
      : containerOrSelector;
    if (!el) return;

    el.dataset.skeletonOriginal = el.innerHTML;
    el.innerHTML = `
      <div class="skeleton-kpi">
        <div class="skeleton skeleton-title"></div>
        <div class="skeleton skeleton-value"></div>
        <div class="skeleton skeleton-sub"></div>
      </div>`;
  }

  /**
   * Restaura el contenido original de un KPI card.
   * @param {string|HTMLElement} containerOrSelector
   */
  function hideKpi(containerOrSelector) {
    const el = typeof containerOrSelector === 'string'
      ? document.querySelector(containerOrSelector)
      : containerOrSelector;
    if (!el || !el.dataset.skeletonOriginal) return;
    el.innerHTML = el.dataset.skeletonOriginal;
    delete el.dataset.skeletonOriginal;
  }

  /**
   * Muestra un empty state dentro de un tbody (fila colspan completa).
   * @param {string|HTMLElement} tbodyOrSelector
   * @param {number} cols        — número de columnas para el colspan
   * @param {object} opts
   * @param {string} opts.icon   — clase FA, ej: 'fa-box-open'
   * @param {string} opts.title  — texto principal
   * @param {string} opts.sub    — texto secundario (opcional)
   * @param {string} opts.actionHtml — HTML de botón/acción (opcional)
   */
  function showEmpty(tbodyOrSelector, cols, {
    icon = 'fa-inbox',
    title = 'No hay resultados',
    sub = '',
    actionHtml = ''
  } = {}) {
    const tbody = typeof tbodyOrSelector === 'string'
      ? document.querySelector(tbodyOrSelector)
      : tbodyOrSelector;
    if (!tbody) return;

    tbody.innerHTML = `
      <tr>
        <td colspan="${cols}">
          <div class="empty-state">
            <div class="empty-state-icon"><i class="fas ${icon}"></i></div>
            <p class="empty-state-title">${title}</p>
            ${sub ? `<p class="empty-state-subtitle">${sub}</p>` : ''}
            ${actionHtml ? `<div class="empty-state-action">${actionHtml}</div>` : ''}
          </div>
        </td>
      </tr>`;
  }

  return { showTable, hideTable, showKpi, hideKpi, showEmpty };
})();
