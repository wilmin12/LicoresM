/**
 * Licores Maduro - API Client
 * Fetch-based HTTP client with JWT token handling.
 */

const API = (() => {
  // ── Configuration ──────────────────────────────────────────────────────────
  const BASE_URL = ''; // API serves the frontend — same origin, no absolute URL needed

  // ── Token management ───────────────────────────────────────────────────────
  function getToken()          { return sessionStorage.getItem('lm_token'); }
  function setToken(t)         { sessionStorage.setItem('lm_token', t); }
  function removeToken()       { sessionStorage.removeItem('lm_token'); }
  function getUser()           { return JSON.parse(sessionStorage.getItem('lm_user') || 'null'); }
  function setUser(u)          { sessionStorage.setItem('lm_user', JSON.stringify(u)); }
  function removeUser()        { sessionStorage.removeItem('lm_user'); }

  // ── Base fetch wrapper ──────────────────────────────────────────────────────
  async function request(method, path, body = null, extraHeaders = {}) {
    const token = getToken();
    const headers = {
      'Content-Type': 'application/json',
      ...extraHeaders
    };

    if (token) headers['Authorization'] = `Bearer ${token}`;

    const options = { method, headers };
    if (body !== null) options.body = JSON.stringify(body);

    let response;
    try {
      response = await fetch(`${BASE_URL}${path}`, options);
    } catch (err) {
      throw new Error(`Network error: ${err.message}`);
    }

    // Handle 401 – only redirect to login if the token is actually expired or missing
    if (response.status === 401) {
      const isLoginPage = window.location.pathname.endsWith('index.html') ||
                          window.location.pathname.endsWith('/');
      if (!isLoginPage) {
        const token = getToken();
        let shouldRedirect = !token; // no token → redirect

        if (token && !shouldRedirect) {
          try {
            const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
            shouldRedirect = Date.now() >= payload.exp * 1000; // token expired → redirect
          } catch {
            shouldRedirect = true; // can't decode → redirect to be safe
          }
        }

        if (shouldRedirect) {
          removeToken();
          removeUser();
          window.location.href = '/index.html';
          throw new Error('Session expired. Please log in again.');
        }
        // Token is valid – 401 is a role/permission issue, not session expiry.
        // Fall through so the error is shown on the page without forcing logout.
      }
      // On login page: fall through so the real server message is shown
    }

    let data;
    try {
      data = await response.json();
    } catch {
      data = null;
    }

    if (!response.ok) {
      const msg = data?.Errors?.[0]
               ?? data?.Message
               ?? data?.title
               ?? `HTTP ${response.status}`;
      throw new Error(msg);
    }

    return data;
  }

  // ── HTTP methods ────────────────────────────────────────────────────────────
  const get    = (path)           => request('GET',    path);
  const post   = (path, body)     => request('POST',   path, body);
  const put    = (path, body)     => request('PUT',    path, body);
  const del    = (path)           => request('DELETE', path);
  const patch  = (path, body)     => request('PATCH',  path, body);

  // ── Auth ────────────────────────────────────────────────────────────────────
  async function login(username, password) {
    const data = await post('/api/auth/login', { Username: username, Password: password });
    if (data?.Success && data.Data) {
      setToken(data.Data.Token);
      setUser(data.Data.User);
    }
    return data;
  }

  async function logout() {
    try { await post('/api/auth/logout', {}); } catch { /* ignore */ }
    removeToken();
    removeUser();
  }

  async function getMe()          { return get('/api/auth/me'); }

  // ── Users ───────────────────────────────────────────────────────────────────
  const users = {
    getAll:        ()           => get('/api/users'),
    getById:       (id)         => get(`/api/users/${id}`),
    create:        (dto)        => post('/api/users', dto),
    update:        (id, dto)    => put(`/api/users/${id}`, dto),
    changePassword:(id, dto)    => put(`/api/users/${id}/password`, dto),
    toggleStatus:  (id)         => put(`/api/users/${id}/toggle-status`, {}),
    delete:        (id)         => del(`/api/users/${id}`),
    getPermissions:  (id)        => get(`/api/users/${id}/permissions`),
    setPermissions:  (id, perms) => put(`/api/users/${id}/permissions`, perms),
    clearPermissions:(id)        => del(`/api/users/${id}/permissions`),
    uploadAvatar:  (id, formData) => {
      const token = getToken();
      return fetch(`${BASE_URL}/api/users/${id}/avatar`, {
        method: 'POST',
        headers: token ? { 'Authorization': `Bearer ${token}` } : {},
        body: formData
      }).then(async r => {
        const data = await r.json();
        if (!r.ok) throw new Error(data?.Message || 'Upload failed.');
        return data;
      });
    }
  };

  // ── Roles ───────────────────────────────────────────────────────────────────
  const roles = {
    getAll:          ()          => get('/api/roles'),
    getById:         (id)        => get(`/api/roles/${id}`),
    create:          (dto)       => post('/api/roles', dto),
    update:          (id, dto)   => put(`/api/roles/${id}`, dto),
    toggleStatus:    (id)        => patch(`/api/roles/${id}/toggle-status`, {}),
    delete:          (id)        => del(`/api/roles/${id}`),
    getPermissions:  (id)        => get(`/api/roles/${id}/permissions`),
    setPermissions:  (id, perms) => put(`/api/roles/${id}/permissions`, perms),
    copy:            (id, dto)   => post(`/api/roles/${id}/copy`, dto)
  };

  // ── Generic CRUD helper ─────────────────────────────────────────────────────
  /**
   * Build a CRUD object for any catalog endpoint.
   * @param {string} prefix  e.g. '/api/freight/currencies'
   */
  function catalog(prefix) {
    return {
      getAll:   (params = {}) => {
        const qs = new URLSearchParams(params).toString();
        return get(`${prefix}${qs ? '?' + qs : ''}`);
      },
      getById:  (id)       => get(`${prefix}/${id}`),
      create:   (dto)      => post(prefix, dto),
      update:   (id, dto)  => put(`${prefix}/${id}`, dto),
      toggle:   (id)       => patch(`${prefix}/${id}/toggle`, {}),
      delete:   (id)       => del(`${prefix}/${id}`)
    };
  }

  // ── Specific catalogs ───────────────────────────────────────────────────────
  const catalogs = {
    // Tracking
    orderStatus:           catalog('/api/tracking/order-status'),
    trackingContainerTypes: catalog('/api/tracking/container-types'),

    // Freight Forwarder
    currencies:        catalog('/api/freight/currencies'),
    loadTypes:         catalog('/api/freight/load-types'),
    portsOfLoading:    catalog('/api/freight/ports'),
    shippingLines:     catalog('/api/freight/shipping-lines'),
    shippingAgents:    catalog('/api/freight/shipping-agents'),
    routes:            catalog('/api/freight/routes'),
    containerSpecs:    catalog('/api/freight/container-specs'),
    containerTypes:    catalog('/api/freight/container-types'),
    routesBySA:        catalog('/api/freight/routes-by-agents'),
    oceanFreight:      catalog('/api/freight/ocean-charge-types'),
    inlandFreight:     catalog('/api/freight/inland-charge-types'),
    regions:           catalog('/api/freight/regions'),
    lclCharge:         catalog('/api/freight/lcl-charge-types'),
    priceTypes:        catalog('/api/freight/price-types'),
    amountTypes:       catalog('/api/freight/amount-types'),
    chargeActions:     catalog('/api/freight/charge-actions'),
    chargeOvers:       catalog('/api/freight/charge-overs'),
    chargePers:        catalog('/api/freight/charge-pers'),

    // Freight Forwarder - Main entities & quotes
    freightForwarders: catalog('/api/freight/forwarders'),
    countries:         catalog('/api/freight/countries'),
    suppliers:         catalog('/api/freight/suppliers'),

    // Aankoopbon
    vendors:           catalog('/api/aankoopbon/vendors'),
    departments:       catalog('/api/aankoopbon/departments'),
    eenheden:          catalog('/api/aankoopbon/eenheden'),
    receivers:         catalog('/api/aankoopbon/receivers'),
    requestors:        catalog('/api/aankoopbon/requestors'),
    requestorsVendor:  catalog('/api/aankoopbon/requestors-vendor'),
    costTypes:         catalog('/api/aankoopbon/cost-types'),
    vehicleTypes:      catalog('/api/aankoopbon/vehicle-types'),
    vehicles:          catalog('/api/aankoopbon/vehicles'),
    abProducts:        catalog('/api/aankoopbon/products'),
    abProductItemLookup: (code) => get(`/api/aankoopbon/products/item-lookup?code=${encodeURIComponent(code)}`),

    // Activity Request
    activityTypes:     catalog('/api/activity/activity-types'),
    budgetActivities:  catalog('/api/activity/budget-activities'),
    denialReasons:     catalog('/api/activity/denial-reasons'),
    entertainmentTypes:catalog('/api/activity/entertainment-types'),
    fiscalYears:       catalog('/api/activity/fiscal-years'),
    licoresGroup:      catalog('/api/activity/licores-group'),
    locationInfo:      catalog('/api/activity/location-info'),
    posCategory:       catalog('/api/activity/pos-category'),
    statusCodes:       catalog('/api/activity/status-codes'),
    facilitators:      catalog('/api/activity/facilitators-info'),
    customerNonClient: catalog('/api/activity/customer-non-client'),
    customerSalesGroup:catalog('/api/activity/customer-sales-group'),
    customerSegment:   catalog('/api/activity/customer-segment-info'),
    customerTarget:    catalog('/api/activity/customer-target-group'),
    sponsoringType:    catalog('/api/activity/sponsoring-type'),
    posLendGive:       catalog('/api/activity/pos-lend-give'),
    posMaterialsStatus:catalog('/api/activity/pos-materials-status'),
    posMaterials:      catalog('/api/activity/pos-materials'),
    posLendOut:        catalog('/api/activity/pos-lend-out'),
    marketingCalendar: catalog('/api/activity/marketing-calendar'),
    activityRequests:  catalog('/api/activity/requests'),

    // Cost Calculation
    costCalcCalculations: catalog('/api/cost-calc/calculations'),
  };

  // ── Freight Forwarder Quotes ─────────────────────────────────────────────────
  const freightQuotes = {
    // Ocean Freight
    getOceanHeaders:      (params = {}) => { const qs = new URLSearchParams(params).toString(); return get(`/api/freight/ocean/headers${qs ? '?' + qs : ''}`); },
    getOceanHeader:       (id)          => get(`/api/freight/ocean/headers/${id}`),
    getOceanNextNumber:   ()            => get('/api/freight/ocean/headers/next-number'),
    createOceanHeader:    (dto)         => post('/api/freight/ocean/headers', dto),
    updateOceanHeader:    (id, dto)     => put(`/api/freight/ocean/headers/${id}`, dto),
    deleteOceanHeader:    (id)          => del(`/api/freight/ocean/headers/${id}`),
    copyOceanHeader:      (id)          => post(`/api/freight/ocean/headers/${id}/copy`, {}),
    addOceanPort:         (hId, dto)    => post(`/api/freight/ocean/headers/${hId}/ports`, dto),
    updateOceanPort:      (id, dto)     => put(`/api/freight/ocean/ports/${id}`, dto),
    deleteOceanPort:      (id)          => del(`/api/freight/ocean/ports/${id}`),
    addOceanSLine:        (pId, dto)    => post(`/api/freight/ocean/ports/${pId}/slines`, dto),
    updateOceanSLine:     (id, dto)     => put(`/api/freight/ocean/slines/${id}`, dto),
    deleteOceanSLine:     (id)          => del(`/api/freight/ocean/slines/${id}`),
    getOceanCharges:      (slId)        => get(`/api/freight/ocean/slines/${slId}/charges`),
    addOceanCharge:       (slId, dto)   => post(`/api/freight/ocean/slines/${slId}/charges`, dto),
    updateOceanCharge:    (id, dto)     => put(`/api/freight/ocean/charges/${id}`, dto),
    deleteOceanCharge:    (id)          => del(`/api/freight/ocean/charges/${id}`),
    getOceanPortCharges:  (portId)      => get(`/api/freight/ocean/ports/${portId}/port-charges`),
    addOceanPortCharge:   (portId, dto) => post(`/api/freight/ocean/ports/${portId}/port-charges`, dto),
    updateOceanPortCharge:(id, dto)     => put(`/api/freight/ocean/port-charges/${id}`, dto),
    deleteOceanPortCharge:(id)          => del(`/api/freight/ocean/port-charges/${id}`),

    // Inland Freight
    getInlandHeaders:     (params = {}) => { const qs = new URLSearchParams(params).toString(); return get(`/api/freight/inland/headers${qs ? '?' + qs : ''}`); },
    getInlandHeader:      (id)          => get(`/api/freight/inland/headers/${id}`),
    createInlandHeader:   (dto)         => post('/api/freight/inland/headers', dto),
    updateInlandHeader:   (id, dto)     => put(`/api/freight/inland/headers/${id}`, dto),
    deleteInlandHeader:   (id)          => del(`/api/freight/inland/headers/${id}`),
    addInlandRegion:      (hId, dto)    => post(`/api/freight/inland/headers/${hId}/regions`, dto),
    updateInlandRegion:   (id, dto)     => put(`/api/freight/inland/regions/${id}`, dto),
    deleteInlandRegion:   (id)          => del(`/api/freight/inland/regions/${id}`),
    addInlandRType:       (rId, dto)    => post(`/api/freight/inland/regions/${rId}/types`, dto),
    updateInlandRType:    (id, dto)     => put(`/api/freight/inland/region-types/${id}`, dto),
    deleteInlandRType:    (id)          => del(`/api/freight/inland/region-types/${id}`),
    getInlandDetails:     (rtId)        => get(`/api/freight/inland/region-types/${rtId}/details`),
    addInlandDetail:      (rtId, dto)   => post(`/api/freight/inland/region-types/${rtId}/details`, dto),
    updateInlandDetail:   (id, dto)     => put(`/api/freight/inland/details/${id}`, dto),
    deleteInlandDetail:   (id)          => del(`/api/freight/inland/details/${id}`),

    // LCL
    getLclHeaders:        (params = {}) => { const qs = new URLSearchParams(params).toString(); return get(`/api/freight/lcl/headers${qs ? '?' + qs : ''}`); },
    getLclHeader:         (id)          => get(`/api/freight/lcl/headers/${id}`),
    createLclHeader:      (dto)         => post('/api/freight/lcl/headers', dto),
    updateLclHeader:      (id, dto)     => put(`/api/freight/lcl/headers/${id}`, dto),
    deleteLclHeader:      (id)          => del(`/api/freight/lcl/headers/${id}`),
    addLclPort:           (hId, dto)    => post(`/api/freight/lcl/headers/${hId}/ports`, dto),
    updateLclPort:        (id, dto)     => put(`/api/freight/lcl/ports/${id}`, dto),
    deleteLclPort:        (id)          => del(`/api/freight/lcl/ports/${id}`),
    addLclPortType:       (pId, dto)    => post(`/api/freight/lcl/ports/${pId}/types`, dto),
    updateLclPortType:    (id, dto)     => put(`/api/freight/lcl/port-types/${id}`, dto),
    deleteLclPortType:    (id)          => del(`/api/freight/lcl/port-types/${id}`),
    getLclDetails:        (ptId)        => get(`/api/freight/lcl/port-types/${ptId}/details`),
    addLclDetail:         (ptId, dto)   => post(`/api/freight/lcl/port-types/${ptId}/details`, dto),
    updateLclDetail:      (id, dto)     => put(`/api/freight/lcl/details/${id}`, dto),
    deleteLclDetail:      (id)          => del(`/api/freight/lcl/details/${id}`),

    // Inland Additional Charges
    getInlandAdditional:   (params = {}) => { const qs = new URLSearchParams(params).toString(); return get(`/api/freight/inland-additional-charges${qs ? '?' + qs : ''}`); },
    getInlandAdditionalById: (id)        => get(`/api/freight/inland-additional-charges/${id}`),
    createInlandAdditional: (dto)        => post('/api/freight/inland-additional-charges', dto),
    updateInlandAdditional: (id, dto)    => put(`/api/freight/inland-additional-charges/${id}`, dto),
    deleteInlandAdditional: (id)         => del(`/api/freight/inland-additional-charges/${id}`)
  };

  // ── Tracking ────────────────────────────────────────────────────────────────
  const tracking = {
    previewVip:   (poNo)        => get(`/api/tracking/orders/preview-vip?poNo=${encodeURIComponent(poNo)}`),
    searchVip:    (q)           => get(`/api/tracking/orders/search-vip?q=${encodeURIComponent(q)}`),
    getOrders:    (params = {}) => { const qs = new URLSearchParams(params).toString(); return get(`/api/tracking/orders${qs ? '?' + qs : ''}`); },
    getOrder:     (id)          => get(`/api/tracking/orders/${id}`),
    createOrder:  (dto)         => post('/api/tracking/orders', dto),
    updateOrder:  (id, dto)     => put(`/api/tracking/orders/${id}`, dto),
    deleteOrder:  (id)          => del(`/api/tracking/orders/${id}`),
    getHistory:   (id)          => get(`/api/tracking/orders/${id}/history`),
    getProducts:  (id)          => get(`/api/tracking/orders/${id}/products`),
    syncVip:        (id)           => post(`/api/tracking/orders/${id}/sync-vip`, {}),
    autoImportVip:  ()             => post('/api/tracking/orders/auto-import-vip', {}),
    closeOrder:     (id)           => post(`/api/tracking/orders/${id}/close`, {}),
    reopenOrder:    (id)           => post(`/api/tracking/orders/${id}/reopen`, {}),
    getByContainer: (containerNo)  => get(`/api/tracking/orders/by-container/${encodeURIComponent(containerNo)}`)
  };

  // ── Cost Calculation ────────────────────────────────────────────────────────
  const costCalc = {
    // Purchase Orders (from DHW_DATABASE)
    getPurchaseOrders: (params = {}) => {
      const qs = new URLSearchParams(params).toString();
      return get(`/api/cost-calc/purchase-orders${qs ? '?' + qs : ''}`);
    },
    getPurchaseOrder: (warehouse, poNo) => get(`/api/cost-calc/purchase-orders/${warehouse}/${poNo}`),
    getSystemConfig:  ()               => get('/api/cost-calc/system-config'),
    // Calculations
    getCalculations:  (params = {}) => {
      const qs = new URLSearchParams(params).toString();
      return get(`/api/cost-calc/calculations${qs ? '?' + qs : ''}`);
    },
    getCalculation:   (id)          => get(`/api/cost-calc/calculations/${id}`),
    createCalculation: (dto)        => post('/api/cost-calc/calculations', dto),
    runCalculation:   (id, dto)     => post(`/api/cost-calc/calculations/${id}/calculate`, dto),
    confirmCalculation: (id)        => patch(`/api/cost-calc/calculations/${id}/confirm`, {}),
    approveCalculation: (id)        => patch(`/api/cost-calc/calculations/${id}/approve`, {}),
    deleteCalculation:  (id)        => del(`/api/cost-calc/calculations/${id}`),
    // Tariff Items (HS codes)
    getTariffItems:         (params = {}) => { const qs = new URLSearchParams(params).toString(); return get(`/api/cost-calc/tariff-items${qs ? '?' + qs : ''}`); },
    createTariffItem:       (dto)         => post('/api/cost-calc/tariff-items', dto),
    updateTariffItem:       (id, dto)     => put(`/api/cost-calc/tariff-items/${id}`, dto),
    deleteTariffItem:       (id)          => del(`/api/cost-calc/tariff-items/${id}`),
    // Goods Classification (item → HS code)
    getGoodsClassifications:    (params = {}) => { const qs = new URLSearchParams(params).toString(); return get(`/api/cost-calc/goods-classification${qs ? '?' + qs : ''}`); },
    createGoodsClassification:  (dto)         => post('/api/cost-calc/goods-classification', dto),
    updateGoodsClassification:  (id, dto)     => put(`/api/cost-calc/goods-classification/${id}`, dto),
    deleteGoodsClassification:  (id)          => del(`/api/cost-calc/goods-classification/${id}`),
    // Item Weights
    getItemWeights:    (params = {}) => { const qs = new URLSearchParams(params).toString(); return get(`/api/cost-calc/item-weights${qs ? '?' + qs : ''}`); },
    createItemWeight:  (dto)         => post('/api/cost-calc/item-weights', dto),
    updateItemWeight:  (id, dto)     => put(`/api/cost-calc/item-weights/${id}`, dto),
    deleteItemWeight:  (id)          => del(`/api/cost-calc/item-weights/${id}`),
    // Allowed Margins
    getAllowedMargins:   (params = {}) => { const qs = new URLSearchParams(params).toString(); return get(`/api/cost-calc/allowed-margins${qs ? '?' + qs : ''}`); },
    createAllowedMargin: (dto)        => post('/api/cost-calc/allowed-margins', dto),
    updateAllowedMargin: (id, dto)    => put(`/api/cost-calc/allowed-margins/${id}`, dto),
    deleteAllowedMargin: (id)         => del(`/api/cost-calc/allowed-margins/${id}`),
    // Inland Tariffs
    getInlandTariffs:   (params = {}) => { const qs = new URLSearchParams(params).toString(); return get(`/api/cost-calc/inland-tariffs${qs ? '?' + qs : ''}`); },
    createInlandTariff: (dto)         => post('/api/cost-calc/inland-tariffs', dto),
    updateInlandTariff: (id, dto)     => put(`/api/cost-calc/inland-tariffs/${id}`, dto),
    deleteInlandTariff: (id)          => del(`/api/cost-calc/inland-tariffs/${id}`),
    // Ship Charges (nested under calculation)
    getShipCharges:   (calcId)        => get(`/api/cost-calc/calculations/${calcId}/ship-charges`),
    createShipCharge: (calcId, dto)   => post(`/api/cost-calc/calculations/${calcId}/ship-charges`, dto),
    updateShipCharge: (calcId, id, dto) => put(`/api/cost-calc/calculations/${calcId}/ship-charges/${id}`, dto),
    deleteShipCharge: (calcId, id)    => del(`/api/cost-calc/calculations/${calcId}/ship-charges/${id}`)
  };

  // ── Route Assignment ─────────────────────────────────────────────────────────
  const routeAssignment = {
    // Dimension management
    customerExt: catalog('/api/route/customer-ext'),
    productExt:  catalog('/api/route/product-ext'),
    budget:      catalog('/api/route/budget'),

    // Reports
    reports: {
      measures:         (params) => get(`/api/route/reports/measures?${new URLSearchParams(params)}`),
      visitSchedule:    (params) => get(`/api/route/reports/visit-schedule?${new URLSearchParams(params)}`),
      deliverySchedule: (params) => get(`/api/route/reports/delivery-schedule?${new URLSearchParams(params)}`),
      zeroSales:        (params) => get(`/api/route/reports/zero-sales?${new URLSearchParams(params)}`),
      commission:       (params) => get(`/api/route/reports/commission?${new URLSearchParams(params)}`),
    },

    // Dimension lookups
    dimensions: {
      customers: (params) => get(`/api/route/dimensions/customers?${new URLSearchParams(params)}`),
      products:  (params) => get(`/api/route/dimensions/products?${new URLSearchParams(params)}`),
    }
  };

  // ── Stock Analysis ───────────────────────────────────────────────────────────
  const stockAnalysis = {
    idealMonths:        catalog('/api/stock/ideal-months'),
    vendorConstraints:  catalog('/api/stock/vendor-constraints'),
    salesBudget:        catalog('/api/stock/sales-budget'),

    bulkImportVendorConstraints: (rows) => post('/api/stock/vendor-constraints/bulk', rows),
    bulkImportSalesBudget:       (rows) => post('/api/stock/sales-budget/bulk', rows),

    generate:            (body)   => post('/api/stock/analysis/generate', body),
    getResults:          (params) => get(`/api/stock/analysis/results?${new URLSearchParams(params)}`),
    getSummary:          (params) => get(`/api/stock/analysis/summary?${new URLSearchParams(params)}`),
    getAvailablePeriods: ()       => get('/api/stock/analysis/available-periods'),
  };

  // ── Sessions ─────────────────────────────────────────────────────────────────
  const sessions = {
    getActive:  ()          => get('/api/sessions/active'),
    forceClose: (id)        => del(`/api/sessions/${id}`)
  };

  // ── Email Config ─────────────────────────────────────────────────────────────
  const emailConfig = {
    get:       ()        => get('/api/system/email-config'),
    save:      (dto)     => put('/api/system/email-config', dto),
    test:      (to)      => post('/api/system/email-config/test', { To: to })
  };

  // ── Applied Freight Quotes ───────────────────────────────────────────────────
  const appliedQuotes = {
    getQuotes:       (params = {})   => { const clean = Object.fromEntries(Object.entries(params).filter(([,v]) => v != null && v !== '')); const qs = new URLSearchParams(clean).toString(); return get(`/api/freight/quotes${qs ? '?' + qs : ''}`); },
    getQuote:        (id)            => get(`/api/freight/quotes/${id}`),
    getNextNumber:   ()              => get('/api/freight/quotes/next-number'),
    createQuote:     (dto)           => post('/api/freight/quotes', dto),
    updateQuote:     (id, dto)       => put(`/api/freight/quotes/${id}`, dto),
    deleteQuote:     (id)            => del(`/api/freight/quotes/${id}`),
    // Ocean ports (level 1)
    addOceanPort:      (qId, dto)    => post(`/api/freight/quotes/${qId}/ocean-ports`, dto),
    updateOceanPort:   (id, dto)     => put(`/api/freight/quotes/ocean-ports/${id}`, dto),
    deleteOceanPort:   (id)          => del(`/api/freight/quotes/ocean-ports/${id}`),
    // Ocean shipping lines (level 2)
    addOceanSLine:     (pId, dto)    => post(`/api/freight/quotes/ocean-ports/${pId}/slines`, dto),
    updateOceanSLine:  (id, dto)     => put(`/api/freight/quotes/ocean-slines/${id}`, dto),
    deleteOceanSLine:  (id)          => del(`/api/freight/quotes/ocean-slines/${id}`),
    // Ocean charges (level 3 — under SLine)
    addOceanCharge:    (slId, dto)   => post(`/api/freight/quotes/ocean-slines/${slId}/charges`, dto),
    updateOceanCharge: (id, dto)     => put(`/api/freight/quotes/ocean-charges/${id}`, dto),
    deleteOceanCharge: (id)          => del(`/api/freight/quotes/ocean-charges/${id}`),
    // Inland regions (level 1)
    addInlandRegion:    (qId, dto)   => post(`/api/freight/quotes/${qId}/inland-regions`, dto),
    deleteInlandRegion: (id)         => del(`/api/freight/quotes/inland-regions/${id}`),
    // Inland region types (level 2)
    addInlandRegionType:    (rId, dto) => post(`/api/freight/quotes/inland-regions/${rId}/types`, dto),
    updateInlandRegionType: (id, dto)  => put(`/api/freight/quotes/inland-region-types/${id}`, dto),
    deleteInlandRegionType: (id)       => del(`/api/freight/quotes/inland-region-types/${id}`),
    // Inland region type details / escalonamiento (level 3)
    addInlandDetail:    (tId, dto)   => post(`/api/freight/quotes/inland-region-types/${tId}/details`, dto),
    updateInlandDetail: (id, dto)    => put(`/api/freight/quotes/inland-details/${id}`, dto),
    deleteInlandDetail: (id)         => del(`/api/freight/quotes/inland-details/${id}`),
    // Port additional charges
    addPortAdd:    (qId, dto)        => post(`/api/freight/quotes/${qId}/port-adds`, dto),
    updatePortAdd: (id, dto)         => put(`/api/freight/quotes/port-adds/${id}`, dto),
    deletePortAdd: (id)              => del(`/api/freight/quotes/port-adds/${id}`),
    // LCL ports
    addLclPort:        (qId, dto)    => post(`/api/freight/quotes/${qId}/lcl-ports`, dto),
    updateLclPort:     (id, dto)     => put(`/api/freight/quotes/lcl-ports/${id}`, dto),
    deleteLclPort:     (id)          => del(`/api/freight/quotes/lcl-ports/${id}`),
    // LCL port types (shipping)
    addLclPortType:    (pId, dto)    => post(`/api/freight/quotes/lcl-ports/${pId}/types`, dto),
    updateLclPortType: (id, dto)     => put(`/api/freight/quotes/lcl-port-types/${id}`, dto),
    deleteLclPortType: (id)          => del(`/api/freight/quotes/lcl-port-types/${id}`),
    // LCL port type details
    addLclDetail:      (tId, dto)    => post(`/api/freight/quotes/lcl-port-types/${tId}/details`, dto),
    updateLclDetail:   (id, dto)     => put(`/api/freight/quotes/lcl-details/${id}`, dto),
    deleteLclDetail:   (id)          => del(`/api/freight/quotes/lcl-details/${id}`)
  };

  // ── System ──────────────────────────────────────────────────────────────────
  const system = {
    getCompanySettings:    ()        => get('/api/system/company-settings'),
    updateCompanySettings: (dto)     => put('/api/system/company-settings', dto),
    getModuleApprovers:    ()        => get('/api/system/module-approvers'),
    updateModuleApprover:  (key, dto) => put(`/api/system/module-approvers/${key}`, dto),
    uploadLogo:            (file)    => {
      const token = getToken();
      const form  = new FormData();
      form.append('file', file);
      return fetch(`${BASE_URL}/api/system/company-settings/logo`, {
        method:  'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body:    form
      }).then(async r => {
        const json = await r.json().catch(() => ({}));
        if (!r.ok) throw new Error(json?.Message || json?.message || `Upload failed (${r.status})`);
        return json;
      });
    }
  };

  // ── Public API ──────────────────────────────────────────────────────────────
  return {
    // Core
    get, post, put, del, patch,
    // Auth
    login, logout, getMe,
    getToken, setToken, removeToken,
    getUser, setUser, removeUser,
    // Resources
    users, roles, catalogs, catalog, costCalc, tracking, freightQuotes, appliedQuotes, routeAssignment, stockAnalysis,
    emailConfig, sessions, system,
    // Config
    BASE_URL
  };
})();

// Make available globally
window.API = API;

/**
 * BtnLoader — maneja el estado loading/ready de un botón.
 *
 * Uso:
 *   BtnLoader.start(btn);          // activa spinner, deshabilita
 *   BtnLoader.stop(btn);           // restaura texto, habilita
 *   BtnLoader.wrap(btn, asyncFn);  // auto start/stop alrededor de una promesa
 */
window.BtnLoader = (() => {
  function start(btn) {
    if (!btn) return;
    if (!btn.dataset.originalHtml) {
      btn.dataset.originalHtml = btn.innerHTML;
    }
    btn.classList.add('btn-loading');
    btn.disabled = true;
    if (!btn.querySelector('.btn-text')) {
      btn.innerHTML = `<span class="btn-text">${btn.innerHTML}</span>`;
    }
  }

  function stop(btn) {
    if (!btn) return;
    btn.classList.remove('btn-loading');
    btn.disabled = false;
    if (btn.dataset.originalHtml) {
      btn.innerHTML = btn.dataset.originalHtml;
      delete btn.dataset.originalHtml;
    }
  }

  async function wrap(btn, asyncFn) {
    start(btn);
    try {
      return await asyncFn();
    } finally {
      stop(btn);
    }
  }

  return { start, stop, wrap };
})();
