/**
 * Licores Maduro - Sidebar Manager
 * Builds the navigation menu from user permissions,
 * handles toggle, active state, and collapsible submenus.
 */

// ── Apply saved theme immediately (runs before DOM renders) ──────────────────
(function () {
  var t = localStorage.getItem('lm_theme');
  if (t && t !== 'wine') document.documentElement.setAttribute('data-theme', t);
})();

const Sidebar = (() => {
  // ── Module / submenu definitions ────────────────────────────────────────────
  // hrefs are relative to the frontend/ root; _base is prepended at render time
  const MENU_STRUCTURE = [
    {
      moduleCode: 'TRACKING',
      label:      'Tracking',
      icon:       'fa-shipping-fast',
      children: [
        { code: 'TRACKING_ORDERS',            label: 'Purchase Order Tracking', href: 'pages/tracking/index.html' },
        { code: 'TRACKING_ORDER_STATUS',      label: 'Order Status Codes',      href: 'pages/tracking/order-status.html' },
        { code: 'TRACKING_CONTAINER_TYPES',   label: 'Container Types',          href: 'pages/tracking/container-types.html' },
      ]
    },
    {
      moduleCode: 'FREIGHT',
      label:      'Freight Forwarder',
      icon:       'fa-ship',
      children: [
        { code: 'FF_CURRENCIES',           label: 'Currencies',               href: 'pages/freight/currencies.html' },
        { code: 'FF_COUNTRIES',            label: 'Countries',                href: 'pages/freight/countries.html' },
        { code: 'FF_SUPPLIERS',            label: 'Suppliers',                href: 'pages/freight/suppliers.html' },
        { code: 'FF_LOADTYPES',            label: 'Load Types',               href: 'pages/freight/load-types.html' },
        { code: 'FF_PORT_OF_LOADING',      label: 'Ports of Loading',         href: 'pages/freight/ports.html' },
        { code: 'FF_SHIPPING_LINES',       label: 'Shipping Lines',           href: 'pages/freight/shipping-lines.html' },
        { code: 'FF_SHIPPING_AGENT',       label: 'Shipping Agents',          href: 'pages/freight/shipping-agents.html' },
        { code: 'FF_ROUTES',               label: 'Routes',                   href: 'pages/freight/routes.html' },
        { code: 'FF_CONTAINER_SPECS',      label: 'Container Specs',          href: 'pages/freight/container-specs.html' },
        { code: 'FF_CONTAINER_TYPES',      label: 'Container Types',          href: 'pages/freight/container-types.html' },
        { code: 'FF_ROUTES_BY_SA',         label: 'Routes by Shipping Agent', href: 'pages/freight/routes-by-agents.html' },
        { code: 'FF_REGIONS',              label: 'Regions',                  href: 'pages/freight/regions.html' },
        { code: 'FF_OCEAN_FREIGHT_CHARGE', label: 'Ocean Freight Charges',    href: 'pages/freight/charge-types.html' },
        { code: 'FF_INLAND_FREIGHT_CHARGE',label: 'Inland Freight Charges',   href: 'pages/freight/charge-types.html' },
        { code: 'FF_LCL_CHARGE',           label: 'LCL Charge Types',         href: 'pages/freight/charge-types.html' },
        { code: 'FF_PRICE_TYPE',           label: 'Price Types',              href: 'pages/freight/charge-types.html' },
        { code: 'FF_AMOUNT_TYPE',          label: 'Amount Types',             href: 'pages/freight/charge-types.html' },
        { code: 'FF_CHARGE_ACTION',        label: 'Charge Actions',           href: 'pages/freight/charge-types.html' },
        { code: 'FF_CHARGE_OVER',          label: 'Charge Over',              href: 'pages/freight/charge-types.html' },
        { code: 'FF_CHARGE_PER',           label: 'Charge Per',               href: 'pages/freight/charge-types.html' },
        { code: 'FF_FORWARDERS',           label: 'Freight Forwarders',       href: 'pages/freight/forwarders.html' },
        { code: 'FF_OCEAN_QUOTES',         label: 'Ocean Freight Quotes',     href: 'pages/freight/ocean-freight.html' },
        { code: 'FF_INLAND_QUOTES',        label: 'Inland Freight Quotes',    href: 'pages/freight/inland-freight.html' },
        { code: 'FF_LCL_QUOTES',           label: 'LCL Quotes',               href: 'pages/freight/lcl.html' },
        { code: 'FF_INLAND_ADD_CHARGES',   label: 'Inland Additional Charges',href: 'pages/freight/inland-additional-charges.html' },
        { code: 'FF_QUOTES',               label: 'Applied Quotes',            href: 'pages/freight/quotes.html' },
      ]
    },
    {
      moduleCode: 'COST',
      label:      'Cost Calculation',
      icon:       'fa-calculator',
      children: [
        { code: 'COST_CALCULATIONS',       label: 'Calculations',         href: 'pages/cost-calc/index.html' },
        { code: 'COST_NEW_CALC',          label: 'New Calculation',      href: 'pages/cost-calc/new-calculation.html' },
        { code: 'COST_PO_LOOKUP',         label: 'Purchase Orders',      href: 'pages/cost-calc/purchase-orders.html' },
        { code: 'COST_TARIFF_ITEMS',      label: 'Tariff Items (HS)',    href: 'pages/cost-calc/tariff-items.html' },
        { code: 'COST_GOODS_CLASS',       label: 'Goods Classification', href: 'pages/cost-calc/goods-classification.html' },
        { code: 'COST_ITEM_WEIGHTS',      label: 'Item Weights',         href: 'pages/cost-calc/item-weights.html' },
        { code: 'COST_ALLOWED_MARGINS',   label: 'Allowed Margins',      href: 'pages/cost-calc/allowed-margins.html' },
        { code: 'COST_INLAND_TARIFFS',    label: 'Inland Tariffs',       href: 'pages/cost-calc/inland-tariffs.html' },
      ],
    },
    {
      moduleCode: 'ROUTE',
      label:      'Route Assignment',
      icon:       'fa-route',
      children: [
        { code: 'ROUTE_CUSTOMER_EXT',  label: 'Customer Ext Dimensions', href: 'pages/route/customer-ext.html' },
        { code: 'ROUTE_PRODUCT_EXT',   label: 'Product Ext Dimensions',  href: 'pages/route/product-ext.html' },
        { code: 'ROUTE_BUDGET',        label: 'Budget',                  href: 'pages/route/budget.html' },
        { code: 'ROUTE_REPORTS',       label: 'Reports',                 href: 'pages/route/reports.html' },
        { code: 'ROUTE_DIMENSIONS',    label: 'Dimensions Viewer',       href: 'pages/route/dimensions.html' },
      ]
    },
    {
      moduleCode: 'STOCK',
      label:      'Stock Analysis',
      icon:       'fa-boxes',
      children: [
        { code: 'STOCK_IDEAL_MONTHS',       label: 'Ideal Months Config',    href: 'pages/stock/ideal-months.html' },
        { code: 'STOCK_VENDOR_CONSTRAINTS', label: 'Vendor Constraints',     href: 'pages/stock/upload.html' },
        { code: 'STOCK_SALES_BUDGET',       label: 'Sales Budget',           href: 'pages/stock/upload.html' },
        { code: 'STOCK_ANALYSIS',           label: 'Generate Analysis',      href: 'pages/stock/analysis.html' },
        { code: 'STOCK_ANALYSIS_RESULTS',   label: 'Analysis Results',       href: 'pages/stock/analysis.html' },
      ]
    },
    {
      moduleCode: 'ACTIVITY',
      label:      'Activity Request',
      icon:       'fa-tasks',
      children: [
        { code: 'ACT_MARKETING_CALENDAR',   label: 'Marketing Calendar',     href: 'pages/activity/marketing-calendar.html' },
        { code: 'ACT_ACTIVITY_REQUESTS',    label: 'Activity Requests',      href: 'pages/activity/activity-list.html' },
        { code: 'ACT_POS_MATERIALS',        label: 'POS Materials',          href: 'pages/activity/pos-materials-catalog.html' },
        { code: 'ACT_POS_LEND_OUT',         label: 'POS Lend Out',           href: 'pages/activity/pos-lend-out.html' },
        { code: 'ACT_ACTIVITY_TYPE',        label: 'Activity Types',         href: 'pages/activity/activity-config.html#activityTypes' },
        { code: 'ACT_BUDGET_ACTIVITIES',    label: 'Budget Activities',      href: 'pages/activity/activity-config.html#budgetActivities' },
        { code: 'ACT_DENIAL_REASONS',       label: 'Denial Reasons',         href: 'pages/activity/activity-config.html#denialReasons' },
        { code: 'ACT_ENTERTAINMENT_TYPE',   label: 'Entertainment Types',    href: 'pages/activity/activity-config.html#entertainmentTypes' },
        { code: 'ACT_STATUS_CODES',         label: 'Status Codes',           href: 'pages/activity/activity-config.html#statusCodes' },
        { code: 'ACT_SPONSORING_TYPE',      label: 'Sponsoring Types',       href: 'pages/activity/activity-config.html#sponsoringType' },
        { code: 'ACT_FISCAL_YEARS',         label: 'Fiscal Years',           href: 'pages/activity/pos-finance.html' },
        { code: 'ACT_LICORES_GROUP',        label: 'Licores Group',          href: 'pages/activity/pos-finance.html' },
        { code: 'ACT_POS_CATEGORY',         label: 'POS Category',           href: 'pages/activity/pos-finance.html' },
        { code: 'ACT_POS_LEND_GIVE',        label: 'POS Lend/Give',          href: 'pages/activity/pos-finance.html' },
        { code: 'ACT_POS_MATERIALS_STATUS', label: 'POS Materials Status',   href: 'pages/activity/pos-finance.html' },
        { code: 'ACT_CUSTOMER_NON_CLIENT',  label: 'Customer Non-Client',    href: 'pages/activity/customers.html' },
        { code: 'ACT_CUSTOMER_SALES_GROUP', label: 'Customer Sales Groups',  href: 'pages/activity/customers.html' },
        { code: 'ACT_CUSTOMER_SEGMENT_INFO',label: 'Customer Segments',      href: 'pages/activity/customers.html' },
        { code: 'ACT_CUSTOMER_TARGET_GROUP',label: 'Customer Target Groups', href: 'pages/activity/customers.html' },
        { code: 'ACT_FACILITATORS_INFO',    label: 'Facilitators',           href: 'pages/activity/customers.html' },
        { code: 'ACT_LOCATION_INFO',        label: 'Location Info',          href: 'pages/activity/customers.html' },
        { code: 'ACT_CAT_ADD_SPECS',        label: 'Cat Additional Specs',   href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_APPAREL_TYPE',     label: 'Cat Apparel Types',      href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_BAG_SPECS',        label: 'Cat Bag Specs',          href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_BOTTLES',          label: 'Cat Bottles',            href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_BRAND_SPECIFIC',   label: 'Cat Brand Specific',     href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_CLOTHING_TYPE',    label: 'Cat Clothing Types',     href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_COLORS',           label: 'Cat Colors',             href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_CONTENT',          label: 'Cat Content',            href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_COOLER_CAPACITY',  label: 'Cat Cooler Capacity',    href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_COOLER_MODEL',     label: 'Cat Cooler Model',       href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_COOLER_TYPE',      label: 'Cat Cooler Types',       href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_FILE_NAMES',       label: 'Cat File Names',         href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_GENDER',           label: 'Cat Gender',             href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_GLASS_SERVING',    label: 'Cat Glass Serving',      href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_INSURRANCE',       label: 'Cat Insurance',          href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_LED',              label: 'Cat LED',                href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_MAINT_MONTHS',     label: 'Cat Maintenance Months', href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_MATERIALS',        label: 'Cat Materials',          href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_SHAPES',           label: 'Cat Shapes',             href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_SIZES',            label: 'Cat Sizes',              href: 'pages/activity/product-catalogs.html' },
        { code: 'ACT_CAT_VAP_TYPE',         label: 'Cat VAP Types',          href: 'pages/activity/product-catalogs.html' },
      ]
    },
    {
      moduleCode: 'PURCHASE',
      label:      'Aankoopbon',
      icon:       'fa-file-invoice',
      children: [
        { code: 'AB_AANKOOPBON',        label: 'Aankoopbonnen',       href: 'pages/aankoopbon/orders.html' },
        { code: 'AB_VENDORS',           label: 'Vendors',             href: 'pages/aankoopbon/vendors.html' },
        { code: 'AB_DEPARTMENTS',       label: 'Departments',         href: 'pages/aankoopbon/catalogs.html#departments' },
        { code: 'AB_EENHEDEN',          label: 'Eenheden (Units)',    href: 'pages/aankoopbon/catalogs.html#eenheden' },
        { code: 'AB_RECEIVERS',         label: 'Receivers',           href: 'pages/aankoopbon/catalogs.html#receivers' },
        { code: 'AB_REQUESTORS',        label: 'Requestors',          href: 'pages/aankoopbon/catalogs.html#requestors' },
        { code: 'AB_REQUESTORS_VENDOR', label: 'Requestors / Vendor', href: 'pages/aankoopbon/catalogs.html#requestors-vendor' },
        { code: 'AB_COST_TYPE',         label: 'Cost Types',          href: 'pages/aankoopbon/catalogs.html#cost-types' },
        { code: 'AB_VEHICLE_TYPE',      label: 'Vehicle Types',       href: 'pages/aankoopbon/catalogs.html#vehicle-types' },
        { code: 'AB_VEHICLES',          label: 'Vehicles',            href: 'pages/aankoopbon/catalogs.html#vehicles' },
        { code: 'AB_PRODUCTS_MGT',      label: 'AB Products',         href: 'pages/aankoopbon/products.html' },
      ]
    }
  ];

  // ── Phase 1 visibility filter ──────────────────────────────────────────────
  // Temporary: only show these 3 modules to the client during Phase 1 evaluation.
  // To re-enable all modules, remove the .filter() line in build() below.
  const PHASE1_MODULES = ['PURCHASE', 'FREIGHT', 'COST'];

  // ── Help / Manuals (visible to all authenticated users) ──────────────────────
  const HELP_ITEMS = [
    { label: 'Manual Aankoopbon',       href: 'pages/help/manual-aankoopbon.html',      icon: 'fa-book-open' },
    { label: 'Manual Cost Calculation', href: 'pages/help/manual-costcalculation.html', icon: 'fa-book-open' },
    { label: 'Manual Freight',          href: 'pages/help/manual-freight.html',         icon: 'fa-book-open' },
    { label: 'Manual Plataforma',       href: 'pages/help/manual-platform.html',        icon: 'fa-book-open' },
  ];

  // ── Admin menu items (always shown to SuperAdmin/Admin) ─────────────────────
  const ADMIN_ITEMS = [
    { code: 'SETTINGS_COMPANY',         label: 'Company Settings',              href: 'pages/settings/company.html',               icon: 'fa-building' },
    { code: 'SETTINGS_APPROVERS',       label: 'Module Approvers',              href: 'pages/settings/approvers.html',             icon: 'fa-envelope-circle-check' },
    { code: 'SETTINGS_MAINT_COSTCALC',  label: 'Maintenance Cost Calculation',  href: 'pages/settings/maintenance-costcalc.html',  icon: 'fa-sliders' },
    { label: 'Users',           href: 'pages/users.html',  icon: 'fa-users' },
    { label: 'Roles',           href: 'pages/roles.html',  icon: 'fa-user-shield' },
    { label: 'Email Config',    href: 'pages/email-config.html', icon: 'fa-envelope-open-text', superAdminOnly: true },
    { label: 'Active Users',    href: 'pages/sessions.html',     icon: 'fa-circle-dot',          superAdminOnly: true },
  ];

  // ── State ───────────────────────────────────────────────────────────────────
  let _collapsed = false;
  let _sidebarEl = null;

  // ── Compute base path to frontend/ root from current page location ──────────
  function getBase() {
    const path = window.location.pathname;
    // /…/frontend/pages/module/file.html → two levels below pages → ../../
    if (/\/pages\/[^/]+\/[^/]+$/.test(path)) return '../../';
    // /…/frontend/pages/file.html        → one level below pages → ../
    if (/\/pages\/[^/]+$/.test(path))         return '../';
    // /…/frontend/dashboard.html         → at root              → ''
    return '';
  }

  // ── Build sidebar HTML ───────────────────────────────────────────────────────
  function build(containerId = 'sidebar-nav') {
    const container = document.getElementById(containerId);
    if (!container) return;

    const user = window.Auth?.getCurrentUser();
    if (!user) return;

    const _base        = getBase();
    const isSuperAdmin = user.RoleName === 'SuperAdmin';
    const isAdmin      = isSuperAdmin || user.RoleName === 'Admin';
    const perms        = user.Permissions || [];
    const currentHref  = location.href;

    let html = '';

    // ── Module sections ────────────────────────────────────────────────────────
    const _t = window.I18n ? window.I18n.t.bind(window.I18n) : (k => k);
    html += `<div class="nav-section-label">${_t('Modules')}</div>`;

    MENU_STRUCTURE.filter(m => PHASE1_MODULES.includes(m.moduleCode)).forEach((mod, idx) => {
      // Check module access
      const hasAccess = isSuperAdmin || perms.some(p =>
        p.ModuleCode === mod.moduleCode && p.CanAccess
      );
      if (!hasAccess) return;

      // Filter accessible children
      const visibleChildren = mod.children.filter(ch =>
        isSuperAdmin || perms.some(p => p.SubmoduleCode === ch.code && p.CanAccess)
      );

      const collapseId = `menu-collapse-${idx}`;

      if (visibleChildren.length === 0 && mod.children.length > 0) return; // no children accessible
      if (visibleChildren.length === 0) {
        // Module with no children – render as direct link
        html += `
          <div class="sidebar-item">
            <a class="sidebar-link ${isActive(mod.label, currentHref)}"
               href="#" data-module="${mod.moduleCode}">
              <i class="fas ${mod.icon}"></i>
              <span class="sidebar-text">${_t(mod.label)}</span>
            </a>
          </div>`;
        return;
      }

      // Module with children – collapsible
      const anyChildActive = visibleChildren.some(ch => {
        const pageFile = ch.href.split('#')[0].split('/').pop();
        return pageFile && currentHref.includes(pageFile);
      });
      html += `
        <div class="sidebar-item">
          <a class="sidebar-link ${anyChildActive ? 'active' : ''}"
             data-bs-toggle="collapse"
             data-label="${_t(mod.label)}"
             href="#${collapseId}"
             aria-expanded="${anyChildActive}"
             aria-controls="${collapseId}">
            <i class="fas ${mod.icon}"></i>
            <span class="sidebar-text">${_t(mod.label)}</span>
            <i class="fas fa-chevron-right sidebar-toggle-arrow ms-auto"></i>
          </a>
          <div class="collapse sidebar-submenu ${anyChildActive ? 'show' : ''}" id="${collapseId}">`;

      visibleChildren.forEach(ch => {
        const fullHref = _base + ch.href;
        const pageFile = ch.href.split('#')[0].split('/').pop();
        const active   = currentHref.includes(pageFile) && pageFile !== '';
        html += `
            <a class="sidebar-link ${active ? 'active' : ''}" href="${fullHref}">
              <i class="fas fa-angle-right" style="font-size:.7rem;"></i>
              <span class="sidebar-text">${_t(ch.label)}</span>
            </a>`;
      });

      html += `</div></div>`;
    });

    // ── Help / Manuals section (all users) ────────────────────────────────────
    html += `<div class="nav-section-label mt-2">${_t('Ayuda')}</div>`;
    HELP_ITEMS.forEach(item => {
      const fullHref = _base + item.href;
      const pageFile = item.href.split('/').pop();
      html += `
        <div class="sidebar-item">
          <a class="sidebar-link ${currentHref.includes(pageFile) ? 'active' : ''}"
             data-label="${_t(item.label)}"
             href="${fullHref}">
            <i class="fas ${item.icon}"></i>
            <span class="sidebar-text">${_t(item.label)}</span>
          </a>
        </div>`;
    });

    // ── Administration section ─────────────────────────────────────────────────
    if (isAdmin) {
      html += `<div class="nav-section-label mt-2">${_t('Administration')}</div>`;
      ADMIN_ITEMS.forEach(item => {
        if (item.superAdminOnly && !isSuperAdmin) return;
        const fullHref = _base + item.href;
        const pageFile = item.href.split('/').pop();
        html += `
          <div class="sidebar-item">
            <a class="sidebar-link ${currentHref.includes(pageFile) ? 'active' : ''}"
               data-label="${_t(item.label)}"
               href="${fullHref}">
              <i class="fas ${item.icon}"></i>
              <span class="sidebar-text">${_t(item.label)}</span>
            </a>
          </div>`;
      });
    }

    container.innerHTML = html;
  }

  function isActive(label, href) {
    return href.toLowerCase().includes(label.toLowerCase()) ? 'active' : '';
  }

  // ── Toggle sidebar ──────────────────────────────────────────────────────────
  function init() {
    _sidebarEl = document.getElementById('sidebar');

    // Load collapse state from localStorage
    _collapsed = localStorage.getItem('lm_sidebar_collapsed') === 'true';
    if (_collapsed) _sidebarEl?.classList.add('collapsed');

    // Toggle button
    const toggleBtn = document.getElementById('btn-sidebar-toggle');
    if (toggleBtn) {
      toggleBtn.addEventListener('click', toggle);
    }

    // Mobile overlay click
    const overlay = document.getElementById('sidebar-overlay');
    if (overlay) {
      overlay.addEventListener('click', closeMobile);
    }

    // Build menu
    build();

    // Auto-expand when clicking any nav link while collapsed
    const nav = document.getElementById('sidebar-nav');
    if (nav) {
      nav.addEventListener('click', e => {
        const link = e.target.closest('.sidebar-link');
        if (!link) return;
        if (_collapsed && window.innerWidth > 768) {
          _collapsed = false;
          _sidebarEl?.classList.remove('collapsed');
          localStorage.setItem('lm_sidebar_collapsed', 'false');
        }
      });
    }

    // Inject real-time clock into topbar
    _initClock();

    // Load chat widget
    _initChat();

    // Load notification bell (Admin/SuperAdmin only — guard is inside notifications.js)
    _initNotifications();

    // Load company name + logo dynamically
    loadBranding();

    // Inject theme picker into sidebar footer
    _initThemePicker();
  }

  // ── Theme picker ─────────────────────────────────────────────────────────────
  const THEMES = [
    { id: 'wine',   label: 'Wine',   color: '#A52535' },
    { id: 'navy',   label: 'Navy',   color: '#1E40AF' },
    { id: 'forest', label: 'Forest', color: '#15803D' },
    { id: 'ocean',  label: 'Ocean',  color: '#0E7490' },
  ];

  function _initThemePicker() {
    const footer = document.querySelector('.sidebar-footer');
    if (!footer || document.querySelector('.theme-picker')) return; // already injected

    const picker = document.createElement('div');
    picker.className = 'theme-picker';
    picker.innerHTML = `
      <div class="theme-picker-label">Theme</div>
      <div class="theme-swatches">
        ${THEMES.map(t =>
          `<button class="theme-swatch" data-theme="${t.id}"
             style="background:${t.color};" title="${t.label}"></button>`
        ).join('')}
      </div>`;

    footer.insertBefore(picker, footer.firstChild);
    _markActiveTheme();

    picker.querySelectorAll('.theme-swatch').forEach(btn => {
      btn.addEventListener('click', () => setTheme(btn.dataset.theme));
    });
  }

  function setTheme(name) {
    if (!name || name === 'wine') {
      document.documentElement.removeAttribute('data-theme');
      localStorage.setItem('lm_theme', 'wine');
    } else {
      document.documentElement.setAttribute('data-theme', name);
      localStorage.setItem('lm_theme', name);
    }
    _markActiveTheme();
  }

  function _markActiveTheme() {
    const current = localStorage.getItem('lm_theme') || 'wine';
    document.querySelectorAll('.theme-swatch').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.theme === current);
    });
  }

  function _initChat() {
    if (document.getElementById('chat-widget-bubble')) return; // already loaded
    const base = getBase();
    const s = document.createElement('script');
    s.src = base + 'js/chat.js';
    s.onload = () => { if (window.ChatWidget) ChatWidget.init(); };
    document.head.appendChild(s);
  }

  function _initNotifications() {
    // If already loaded (page included the script manually), just init
    if (window.Notifications) { Notifications.init(); return; }
    const base = getBase();
    const s = document.createElement('script');
    s.src = base + 'js/notifications.js';
    s.onload = () => { if (window.Notifications) Notifications.init(); };
    document.head.appendChild(s);
  }

  function _initClock() {
    const actions = document.querySelector('.topbar-actions, header.topbar .ms-auto');
    if (!actions || document.getElementById('topbar-clock')) return;

    const clock = document.createElement('div');
    clock.id        = 'topbar-clock';
    clock.className = 'topbar-clock d-none d-sm-flex';
    clock.innerHTML =
      '<i class="fas fa-clock"></i>' +
      '<span id="topbar-clock-hm">00:00</span>' +
      '<span class="topbar-clock-seconds" id="topbar-clock-s">:00</span>';
    actions.prepend(clock);

    function _tick() {
      const now = new Date();
      const hm  = now.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });
      const s   = ':' + String(now.getSeconds()).padStart(2, '0');
      const hmEl = document.getElementById('topbar-clock-hm');
      const sEl  = document.getElementById('topbar-clock-s');
      if (hmEl) hmEl.textContent = hm;
      if (sEl)  sEl.textContent  = s;
    }
    _tick();
    setInterval(_tick, 1000);
  }

  function toggle() {
    if (window.innerWidth <= 768) {
      // Mobile: slide in/out
      _sidebarEl?.classList.toggle('mobile-open');
      const overlay = document.getElementById('sidebar-overlay');
      if (overlay) overlay.style.display = _sidebarEl?.classList.contains('mobile-open') ? 'block' : 'none';
    } else {
      // Desktop: collapse/expand
      _collapsed = !_collapsed;
      _sidebarEl?.classList.toggle('collapsed', _collapsed);
      localStorage.setItem('lm_sidebar_collapsed', _collapsed);

      // Close all open submenus when collapsing
      if (_collapsed) {
        _sidebarEl?.querySelectorAll('.sidebar-submenu.show').forEach(el => {
          el.classList.remove('show');
          const trigger = document.querySelector(`[href="#${el.id}"]`);
          if (trigger) trigger.setAttribute('aria-expanded', 'false');
        });
      }
    }
  }

  function closeMobile() {
    _sidebarEl?.classList.remove('mobile-open');
    const overlay = document.getElementById('sidebar-overlay');
    if (overlay) overlay.style.display = 'none';
  }

  function highlightActive() {
    const links = document.querySelectorAll('.sidebar-link[href]');
    const currentUrl = window.location.href;
    links.forEach(link => {
      const href = link.getAttribute('href');
      if (href && href !== '#' && currentUrl.includes(href)) {
        link.classList.add('active');
        // Expand parent collapse
        const parent = link.closest('.collapse');
        if (parent) {
          parent.classList.add('show');
          const trigger = document.querySelector(`[href="#${parent.id}"]`);
          if (trigger) trigger.setAttribute('aria-expanded', 'true');
        }
      }
    });
  }

  // ── Branding: load company name + logo from API and apply to sidebar ────────
  const BRANDING_KEY = 'lm_company_branding';

  async function loadBranding() {
    try {
      // Use cached value first for fast render, then refresh in background
      const cached = sessionStorage.getItem(BRANDING_KEY);
      if (cached) _applyBranding(JSON.parse(cached));

      const res  = await API.system.getCompanySettings();
      const data = res?.Data || res;
      if (!data) return;
      const branding = { name: data.CsCompanyName, logo: data.CsLogoUrl };
      sessionStorage.setItem(BRANDING_KEY, JSON.stringify(branding));
      _applyBranding(branding);
    } catch (_) {
      // Non-critical — silently ignore if company settings not yet configured
    }
  }

  function _applyBranding({ name, logo } = {}) {
    // Company name — target by class (used in all sidebar headers)
    document.querySelectorAll('.sidebar-logo-text').forEach(el => {
      if (name) el.textContent = name.toUpperCase();
    });

    // Logo — target by class (used in all sidebar headers)
    if (logo) {
      document.querySelectorAll('.sidebar-logo-img').forEach(el => {
        el.src = logo + '?t=' + Date.now();
        el.style.display = '';
      });
    }

    // Page title
    if (name) document.title = document.title.replace('Licores Maduro', name);
  }

  function refreshBranding() {
    sessionStorage.removeItem(BRANDING_KEY);
    loadBranding();
  }

  // ── Public ──────────────────────────────────────────────────────────────────
  return { init, build, toggle, highlightActive, loadBranding, refreshBranding, setTheme };
})();

window.Sidebar = Sidebar;
