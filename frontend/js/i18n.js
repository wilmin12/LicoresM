/**
 * Licores Maduro - Internationalization (i18n)
 * Supports English (en) and Spanish (es).
 * Usage:
 *   I18n.t('Save')           → 'Guardar'  (when lang = es)
 *   I18n.setLang('es')       → switch & re-render
 *   <span data-i18n="Save">  → auto-translated on apply()
 */
const I18n = (() => {

  // ── Translation dictionary (English keys → Spanish values) ──────────────────
  const ES = {
    // ── Navigation / Sidebar ──────────────────────────────────────────────────
    'Modules':                    'Módulos',
    'Administration':             'Administración',
    'Tracking':                   'Seguimiento',
    'Freight Forwarder':          'Agente de Carga',
    'Cost Calculation':           'Cálculo de Costos',
    'Route Assignment':           'Asignación de Rutas',
    'Stock Analysis':             'Análisis de Stock',
    'Activity Request':           'Solicitud de Actividad',
    'Aankoopbon':                 'Orden de Compra',

    // ── Tracking ──────────────────────────────────────────────────────────────
    'Order Status':               'Estado de Orden',

    // ── Freight ───────────────────────────────────────────────────────────────
    'Currencies':                 'Monedas',
    'Load Types':                 'Tipos de Carga',
    'Ports of Loading':           'Puertos de Embarque',
    'Shipping Lines':             'Líneas Navieras',
    'Shipping Agents':            'Agentes Navieros',
    'Routes':                     'Rutas',
    'Container Specs':            'Espec. de Contenedor',
    'Container Types':            'Tipos de Contenedor',
    'Routes by Shipping Agent':   'Rutas por Agente Naviero',
    'Ocean Freight Charges':      'Cargos Flete Marítimo',
    'Inland Freight Charges':     'Cargos Flete Terrestre',
    'LCL Charge Types':           'Tipos de Cargo LCL',
    'Price Types':                'Tipos de Precio',
    'Amount Types':               'Tipos de Monto',
    'Charge Actions':             'Acciones de Cargo',
    'Charge Over':                'Cargo Sobre',
    'Charge Per':                 'Cargo Por',

    // ── Activity ──────────────────────────────────────────────────────────────
    'Activity Types':             'Tipos de Actividad',
    'Budget Activities':          'Actividades de Presupuesto',
    'Denial Reasons':             'Razones de Negación',
    'Entertainment Types':        'Tipos de Entretenimiento',
    'Status Codes':               'Códigos de Estado',
    'Sponsoring Types':           'Tipos de Patrocinio',
    'Fiscal Years':               'Años Fiscales',
    'Licores Group':              'Grupo Licores',
    'POS Category':               'Categoría POS',
    'POS Lend/Give':              'POS Préstamo/Donación',
    'POS Materials Status':       'Estado Material POS',
    'Customer Non-Client':        'Cliente No-Cliente',
    'Customer Sales Groups':      'Grupos de Venta',
    'Customer Segments':          'Segmentos de Cliente',
    'Customer Target Groups':     'Grupos Objetivo',
    'Facilitators':               'Facilitadores',
    'Location Info':              'Info de Ubicación',

    // ── Product Catalogs ──────────────────────────────────────────────────────
    'Cat Additional Specs':       'Especif. Adicionales',
    'Cat Apparel Types':          'Tipos de Ropa',
    'Cat Bag Specs':              'Especif. de Bolsa',
    'Cat Bottles':                'Botellas',
    'Cat Brand Specific':         'Específico de Marca',
    'Cat Clothing Types':         'Tipos de Vestimenta',
    'Cat Colors':                 'Colores',
    'Cat Content':                'Contenido',
    'Cat Cooler Capacity':        'Capacidad de Cooler',
    'Cat Cooler Model':           'Modelo de Cooler',
    'Cat Cooler Types':           'Tipos de Cooler',
    'Cat File Names':             'Nombres de Archivo',
    'Cat Gender':                 'Género',
    'Cat Glass Serving':          'Tipo de Copa',
    'Cat Insurance':              'Seguro',
    'Cat LED':                    'LED',
    'Cat Maintenance Months':     'Meses de Mantenimiento',
    'Cat Materials':              'Materiales',
    'Cat Shapes':                 'Formas',
    'Cat Sizes':                  'Tallas',
    'Cat VAP Types':              'Tipos VAP',

    // ── Aankoopbon ────────────────────────────────────────────────────────────
    'Vendors':                    'Proveedores',
    'Departments':                'Departamentos',
    'Eenheden (Units)':           'Unidades',
    'Receivers':                  'Receptores',
    'Requestors':                 'Solicitantes',
    'Requestors / Vendor':        'Solicitantes / Proveedor',
    'Cost Types':                 'Tipos de Costo',
    'Vehicle Types':              'Tipos de Vehículo',
    'Vehicles':                   'Vehículos',
    'AB Products':                'Productos AB',

    // ── Admin ─────────────────────────────────────────────────────────────────
    'Users':                      'Usuarios',
    'Roles':                      'Roles',

    // ── Common UI ─────────────────────────────────────────────────────────────
    'New':                        'Nuevo',
    'Save':                       'Guardar',
    'Cancel':                     'Cancelar',
    'Edit':                       'Editar',
    'Delete':                     'Eliminar',
    'Search...':                  'Buscar...',
    'Search vendors...':          'Buscar proveedores...',
    'Search currencies...':       'Buscar monedas...',
    'Loading...':                 'Cargando...',
    'Active':                     'Activo',
    'Inactive':                   'Inactivo',
    'Actions':                    'Acciones',
    'Status':                     'Estado',
    'Logout':                     'Cerrar Sesión',
    'Home':                       'Inicio',
    'Name':                       'Nombre',
    'Code':                       'Código',
    'Description':                'Descripción',
    'Created':                    'Creado',
    'Yes':                        'Sí',
    'No':                         'No',
    'No records found.':          'No se encontraron registros.',
    'records':                    'registros',
    'record':                     'registro',
    'Page':                       'Página',
    'of':                         'de',
    'Toggle':                     'Cambiar estado',

    // ── Login page ────────────────────────────────────────────────────────────
    'Sign In':                    'Iniciar Sesión',
    'Signing in...':              'Iniciando sesión...',
    'Username':                   'Usuario',
    'Password':                   'Contraseña',
    'Enter your username':        'Ingrese su usuario',
    'Enter your password':        'Ingrese su contraseña',
    'Secured connection':         'Conexión segura',

    // ── Vendors page ──────────────────────────────────────────────────────────
    'Vendor':                     'Proveedor',
    'New Vendor':                 'Nuevo Proveedor',
    'Edit Vendor':                'Editar Proveedor',
    'Manage supplier and vendor records.': 'Gestionar registros de proveedores y suplidores.',
    'Address':                    'Dirección',
    'Phone':                      'Teléfono',
    'Email':                      'Correo Electrónico',
    'Contact Person':             'Persona de Contacto',
    'Currency':                   'Moneda',
    'Cash Vendor':                'Proveedor en Efectivo',
    'Cash':                       'Efectivo',
    'Contact':                    'Contacto',

    // ── Freight pages ─────────────────────────────────────────────────────────
    'Currency Code':              'Código de Moneda',
    'Currency Name':              'Nombre de Moneda',
    'Exchange Rate':              'Tipo de Cambio',
    'Port Code':                  'Código de Puerto',
    'Port Name':                  'Nombre de Puerto',
    'Country':                    'País',
    'Agent Code':                 'Código de Agente',
    'Agent Name':                 'Nombre de Agente',
    'Route Code':                 'Código de Ruta',
    'Route Name':                 'Nombre de Ruta',
    'Origin':                     'Origen',
    'Destination':                'Destino',

    // ── Breadcrumbs ───────────────────────────────────────────────────────────
    'Freight Forwarder':          'Agente de Carga',
    'Activity Request':           'Solicitud de Actividad',
    'Product Catalogs':           'Catálogos de Producto',
    'Charge Types':               'Tipos de Cargo',
    'Customers':                  'Clientes',
    'POS & Finance':              'POS y Finanzas',
    'Activity Config':            'Config. de Actividad',

    // ── Table headers (generic) ───────────────────────────────────────────────
    '#':                          '#',
    'Label':                      'Etiqueta',
    'Type':                       'Tipo',
    'Value':                      'Valor',
    'Notes':                      'Notas',
    'Rate':                       'Tasa',

    // ── Page titles & subtitles ────────────────────────────────────────────────
    'Dashboard':                  'Panel de Control',
    'Routes by Shipping Agent':   'Rutas por Agente',
    'AB Products':                'Productos AB',
    'Activity Configuration':     'Configuración de Actividad',
    'Customer Data':              'Datos de Clientes',
    'POS & Finance':              'POS y Finanzas',
    'Product Catalogs':           'Catálogos de Productos',
    'Order Status':               'Estado de Orden',
    'Freight Forwarder':          'Agente de Carga',
    'Aankoopbon':                 'Orden de Compra',

    // Subtitles
    'Manage currency codes and exchange rates.': 'Gestionar códigos de moneda y tipos de cambio.',
    'Manage load types for shipments.':          'Gestionar tipos de carga para envíos.',
    'Manage port of loading catalog.':           'Gestionar catálogo de puertos de embarque.',
    'Manage shipping line catalog.':             'Gestionar catálogo de líneas navieras.',
    'Manage shipping agent catalog.':            'Gestionar catálogo de agentes navieros.',
    'Manage route catalog.':                     'Gestionar catálogo de rutas.',
    'Manage container specification catalog.':   'Gestionar catálogo de especificaciones de contenedor.',
    'Manage container type catalog.':            'Gestionar catálogo de tipos de contenedor.',
    'Manage routes by shipping agent.':          'Gestionar rutas por agente naviero.',
    'Manage charge type catalogs.':              'Gestionar catálogos de tipos de cargo.',
    'Manage the Aankoopbon product catalog.':    'Gestionar el catálogo de productos.',
    'Manage activity types, budget activities, status codes, denial reasons, sponsoring and entertainment types.': 'Gestionar tipos de actividad, actividades de presupuesto, códigos de estado, razones de negación, tipos de patrocinio y entretenimiento.',
    'Manage non-clients, sales groups, segments, target groups, facilitators and locations.': 'Gestionar no-clientes, grupos de venta, segmentos, grupos objetivo, facilitadores y ubicaciones.',
    'Manage POS categories, lend/give, materials status, Licores Group and Fiscal Years.': 'Gestionar categorías POS, préstamo/donación, estado de materiales, Grupo Licores y Años Fiscales.',
    'Manage all CAT_ product attribute catalogs.': 'Gestionar todos los catálogos de atributos de productos.',
    'Manage system users and their access.':     'Gestionar usuarios del sistema y su acceso.',
    'Manage roles and permissions.':             'Gestionar roles y permisos.',
    'Manage order status codes.':                'Gestionar códigos de estado de órdenes.',

    // ── Table headers (additional) ─────────────────────────────────────────────
    'Bank Purchase Rate':         'Tasa de Compra',
    'Customs Rate':               'Tasa Aduanal',
    'Port Code':                  'Código de Puerto',
    'Port Name':                  'Nombre de Puerto',
    'Country':                    'País',
    'Transit Days':               'Días de Tránsito',
    'Cost Type':                  'Tipo de Costo',
    'Unit':                       'Unidad',
    'Unit Qty':                   'Cant. Unidad',
    'Item Code':                  'Código de Artículo',
    'License Plate':              'Placa',
    'Model':                      'Modelo',
    'Vehicle Type':               'Tipo de Vehículo',
    'Requestor':                  'Solicitante',
    'Year':                       'Año',
    'Start Date':                 'Fecha Inicio',
    'End Date':                   'Fecha Fin',
    'ID Doc':                     'Doc. ID',
    'Username':                   'Usuario',
    'Full Name':                  'Nombre Completo',
    'Role':                       'Rol',
    'Last Login':                 'Último Acceso',
    'Permissions':                'Permisos',
    'Prefix':                     'Prefijo',
    'Display Text':               'Texto de Pantalla',
    'Billing Descr':              'Descr. Facturación',
    'Containers':                 'Contenedores',
    'Cases':                      'Cajas',
    'Weight (kg)':                'Peso (kg)',
    'Port':                       'Puerto',
    'Route':                      'Ruta',
    'Days':                       'Días',
    'Shipping Agent':             'Agente Naviero',

    // ── Buttons / actions ──────────────────────────────────────────────────────
    'New Currency':               'Nueva Moneda',
    'New Vendor':                 'Nuevo Proveedor',
    'New Product':                'Nuevo Producto',

    // ── Form labels ────────────────────────────────────────────────────────────
    'Address':                    'Dirección',
    'Phone':                      'Teléfono',
    'KVK Number':                 'Número KVK',
    'CRIB Number':                'Número CRIB',
    'Conversion Factor':          'Factor de Conversión',
    'Unit Code':                  'Código de Unidad',

    // ── Tracking page ─────────────────────────────────────────────────────────
    'Purchase Order Tracking':    'Seguimiento de Órdenes de Compra',
    'Track and manage the end-to-end status of purchase orders from origin to warehouse.':
                                  'Rastree y gestione el estado de las órdenes de compra desde el origen hasta el almacén.',
    'Total Orders':               'Total Órdenes',
    'In VIP':                     'En VIP',
    'In Transit':                 'En Tránsito',
    'At Customs':                 'En Aduana',
    'Overdue':                    'Vencidas',
    'Closed':                     'Cerradas',
    'All Status':                 'Todos los Estados',
    'All Warehouses':             'Todos los Almacenes',
    'Beer':                       'Cerveza',
    'Wine':                       'Vino',
    'Export':                     'Exportar',
    'Add PO':                     'Agregar PO',
    'Tracking Orders':            'Órdenes de Seguimiento',
    'PO Number':                  'No. de PO',
    'Warehouse':                  'Almacén',
    'Supplier':                   'Proveedor',
    'Container No.':              'No. Contenedor',
    'Req. ETA':                   'ETA Sol.',
    'Est. Arrival':               'Llegada Est.',
    'Days Over':                  'Días Vencidos',
    'Last Update':                'Últ. Actualización',
    'Add PO for Tracking':        'Agregar PO al Seguimiento',
    'Initial Status':             'Estado Inicial',
    'Type to search VIP POs...':  'Escriba para buscar POs en VIP...',
    'Freight forwarder name':     'Nombre del agente de carga',
    'Optional comments...':       'Comentarios opcionales...',
    'Add & Track':                'Agregar y Rastrear',
    'Search PO number, supplier, container...': 'Buscar PO, proveedor, contenedor...',
    'new order(s) imported from VIP': 'nueva(s) orden(es) importada(s) de VIP',

    // ── Tracking page — warehouse filter names ─────────────────────────────────
    'All':                            'Todos',
    'Duty Paid':                      'Arancel Pagado',
    'Store':                          'Depósito',
    'Duty Free':                      'Libre de Impuestos',
    'Comments':                       'Comentarios',

    // ── Help / User Manuals ───────────────────────────────────────────────────
    'Ayuda':                              'Ayuda',
    'Manual Aankoopbon':                  'Manual Aankoopbon',
    'Manual Cost Calculation':            'Manual Cálculo de Costos',
    'Manual Freight':                     'Manual Agente de Carga',
    'Manual — Aankoopbonnen':             'Manual — Aankoopbonnen',
    'Manual — Cost Calculation':          'Manual — Cálculo de Costos',
    'Manual — Freight Forwarder':         'Manual — Agente de Carga',

    // Aankoopbon manual sections
    'What is Aankoopbonnen?':             '¿Qué es Aankoopbonnen?',
    'Roles and Permissions':              'Roles y Permisos',
    'Process Flow':                       'Flujo del Proceso',
    'Create an Order':                    'Crear una Orden',
    'Send for Approval':                  'Enviar a Aprobación',
    'Approve or Reject':                  'Aprobar o Rechazar',
    'Register Invoice':                   'Registrar Factura',
    'Close the Order':                    'Cerrar la Orden',
    'Resubmit after Rejection':           'Reenviar tras un Rechazo',
    'Generate PDF':                       'Generar PDF',
    'Support Catalogs':                   'Catálogos de Soporte',
    'Form Fields':                        'Campos del Formulario',
    'Frequently Asked Questions':         'Preguntas Frecuentes',

    // Cost Calculation manual sections
    'What is Cost Calculation?':          '¿Qué es Cost Calculation?',
    'Create a New Calculation':           'Crear una Calculación Nueva',
    'Run or Re-run the Calculation':      'Ejecutar o Re-ejecutar el Cálculo',
    'Additional Charges (Ship Charges)':  'Cargos Adicionales (Ship Charges)',
    'Confirm the Calculation':            'Confirmar la Calculación',
    'Approve':                            'Aprobar',
    'Landed Cost Formula':                'Fórmula del Costo Landed',
    'Support Catalogs':                   'Catálogos de Soporte',

    // Freight manual sections
    'What is Freight Forwarder?':         '¿Qué es Freight Forwarder?',
    'Module Structure':                   'Estructura del Módulo',
    'Base Catalogs':                      'Catálogos Base',
    'Manage Freight Forwarders':          'Gestionar Freight Forwarders',
    'Ocean Freight Quotes (FCL)':         'Cotizaciones Ocean Freight (FCL)',
    'LCL Quotes':                         'Cotizaciones LCL',
    'Inland Freight Quotes':              'Cotizaciones Inland Freight',
    'Inland Additional Charges':          'Cargos Adicionales Inland',
    'Applied Quotes':                     'Cotizaciones Aplicadas',
    'Charge Types':                       'Tipos de Cargos',

    // Platform & Administration manual
    'Manual Plataforma':                       'Manual Plataforma',
    'Manual — Plataforma & Administración':    'Manual — Plataforma & Administración',
    'Platform Overview':                       'Visión General de la Plataforma',
    'Roles and Admin Access':                  'Roles y Acceso de Administración',
    'Company Settings':                        'Configuración de la Empresa',
    'Email Configuration':                     'Configuración de Correo',
    'Module Approvers':                        'Aprobadores por Módulo',
    'User Management':                         'Gestión de Usuarios',
    'Role Management':                         'Gestión de Roles y Permisos',
    'Active Sessions':                         'Sesiones Activas',
    'Color Themes':                            'Temas de Color',
    'Language EN/ES':                          'Idioma (EN / ES)',
    'Login and Session':                       'Login y Sesión',

    // ── Dynamic JS text ────────────────────────────────────────────────────────
    'Toggle this record?':        '¿Cambiar estado de este registro?',
    'Toggle status?':             '¿Cambiar estado?',
    'Updated.':                   'Actualizado.',
    'Created.':                   'Creado.',
    'Status updated.':            'Estado actualizado.',
    'Currency updated.':          'Moneda actualizada.',
    'Currency created.':          'Moneda creada.',
  };

  // ── State ──────────────────────────────────────────────────────────────────
  let _lang = localStorage.getItem('lm_lang') || 'en';

  // ── Core functions ─────────────────────────────────────────────────────────
  function t(key) {
    if (!key) return key;
    if (_lang === 'en') return key;
    return ES[key] ?? key;
  }

  function getLang() { return _lang; }

  function setLang(lang) {
    _lang = lang;
    localStorage.setItem('lm_lang', lang);
    apply();
    // Rebuild sidebar (re-translates menu labels)
    if (window.Sidebar) Sidebar.build();
    // Update switcher button states
    document.querySelectorAll('.lang-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.lang === lang);
    });
  }

  // Apply translations to all [data-i18n] elements in the DOM
  function apply() {
    // 1. Explicit [data-i18n] tagged elements (existing logic)
    document.querySelectorAll('[data-i18n]').forEach(el => {
      const key = el.getAttribute('data-i18n');
      const val = t(key);
      if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA') {
        el.placeholder = val;
      } else {
        el.textContent = val;
      }
    });
    document.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
      el.placeholder = t(el.getAttribute('data-i18n-placeholder'));
    });
    document.querySelectorAll('[data-i18n-title]').forEach(el => {
      el.title = t(el.getAttribute('data-i18n-title'));
    });

    // 2. Auto-translate pure-text structural elements
    // Save original EN text as data-i18n-key on first call, then translate
    document.querySelectorAll(
      '.page-title, .page-subtitle, .breadcrumb-item.active, thead th, #tab-title'
    ).forEach(el => {
      if (el.children.length > 0) return; // skip elements with child tags
      if (!el.dataset.i18nKey) el.dataset.i18nKey = el.textContent.trim();
      const tr = t(el.dataset.i18nKey);
      if (tr) el.textContent = tr;
    });

    // 3. Elements with mixed content (icon + text node) — translate the text node only
    document.querySelectorAll('.topbar-title, .modal-title, #btn-new, #btn-save, #btn-logout').forEach(el => {
      // Find the trailing text node
      const textNodes = [...el.childNodes].filter(n => n.nodeType === 3 && n.textContent.trim());
      textNodes.forEach(tn => {
        if (!tn._i18nKey) tn._i18nKey = tn.textContent.trim();
        const tr = t(tn._i18nKey);
        if (tr) tn.textContent = tn.textContent.replace(tn._i18nKey, tr);
      });
    });

    // 4. Form labels — may contain a required-asterisk span; translate text node only
    document.querySelectorAll('.form-label').forEach(el => {
      const tn = [...el.childNodes].find(n => n.nodeType === 3 && n.textContent.trim());
      if (!tn) return;
      if (!tn._i18nKey) tn._i18nKey = tn.textContent.trim();
      const tr = t(tn._i18nKey);
      if (tr) tn.textContent = tn.textContent.replace(tn._i18nKey, tr);
    });

    // 5. Cancel / Close modal buttons (pure text)
    document.querySelectorAll('[data-bs-dismiss="modal"]:not(.btn-close)').forEach(el => {
      if (el.children.length > 0) return;
      if (!el.dataset.i18nKey) el.dataset.i18nKey = el.textContent.trim();
      const tr = t(el.dataset.i18nKey);
      if (tr) el.textContent = tr;
    });

    // 6. Search input placeholders
    document.querySelectorAll('input[placeholder]').forEach(el => {
      if (!el._i18nPlaceholder) el._i18nPlaceholder = el.placeholder.trim();
      const tr = t(el._i18nPlaceholder);
      if (tr) el.placeholder = tr;
    });

    // 7. Active/Inactive badges — these are regenerated by renderTable but
    //    if already in DOM (e.g. after render then lang change) translate them
    document.querySelectorAll('.badge-active, .badge-inactive').forEach(el => {
      if (!el.dataset.i18nKey) el.dataset.i18nKey = el.textContent.trim();
      const tr = t(el.dataset.i18nKey);
      if (tr) el.textContent = tr;
    });
  }

  // Inject language switcher into the topbar (called once on init)
  function injectSwitcher() {
    if (document.getElementById('lang-switcher')) return;
    const topbar = document.querySelector('.topbar .ms-auto');
    if (!topbar) return;

    const wrap = document.createElement('div');
    wrap.id = 'lang-switcher';
    wrap.style.cssText = 'display:flex;align-items:center;gap:2px;';
    wrap.innerHTML = `
      <button class="lang-btn btn btn-sm ${_lang==='en'?'btn-wine':'btn-outline-secondary'}"
              data-lang="en" onclick="I18n.setLang('en')"
              title="English" style="padding:2px 8px;font-size:.75rem;font-weight:600;">EN</button>
      <button class="lang-btn btn btn-sm ${_lang==='es'?'btn-wine':'btn-outline-secondary'}"
              data-lang="es" onclick="I18n.setLang('es')"
              title="Español" style="padding:2px 8px;font-size:.75rem;font-weight:600;">ES</button>`;
    topbar.insertBefore(wrap, topbar.firstChild);
  }

  function init() {
    _lang = localStorage.getItem('lm_lang') || 'en';
    // Wait for DOM then inject and apply
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', () => { injectSwitcher(); apply(); });
    } else {
      injectSwitcher(); apply();
    }
  }

  return { t, getLang, setLang, apply, init, injectSwitcher };
})();

window.I18n = I18n;
I18n.init();
