# CostCalc Client Comments 2026-06-10 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolver 6 observaciones del cliente sobre el módulo Cost Calculation: quitar columnas, separar roles Confirm/Approve, nueva página de aprobación, y dos bugs de navegación en price-confirmation.

**Architecture:** Cambios en 3 capas: (1) frontend HTML/JS para columnas, botones y nueva página; (2) sidebar.js para nueva entrada de navegación; (3) backend C# para la regla de negocio confirmer ≠ approver. Todos los cambios son independientes y se pueden probar por separado.

**Tech Stack:** Vanilla JS + Bootstrap 5 (frontend), ASP.NET Core 8 + EF Core 8 (backend), SQL Server (datos).

---

## Resumen de Issues (origen: CostClac_Comentarios_2026_06_10.pdf)

| # | Issue | Archivos |
|---|-------|---------|
| T01 | Quitar columnas "Inland Tariff" y "SHP CHGS" del grid y del Grand Summary | `calculation.html` |
| T02 | Bug: price-confirmation abre con `#undefined` cuando viene de "Confirm Prices" | `price-confirmation.html` |
| T03 | Bug: botón "View Calculation" en price-confirmation dice "No calculation ID specified" | `price-confirmation.html` |
| T04 | Regla 4-eyes: quien hizo Confirm NO puede hacer Approve | `CostCalculationsController.cs` + `calculation.html` |
| T05 | Después de Confirm: mostrar mensaje "email enviado al manager" y volver al listado | `calculation.html` |
| T06 | Nueva opción "Approve Cost Calculation" en sidebar — solo calcs CF, solo rol approver | `sidebar.js` + `approve-calculations.html` (nuevo) |

---

## File Map

| Archivo | Acción | Responsabilidad |
|---------|--------|-----------------|
| `frontend/pages/cost-calc/calculation.html` | Modificar | T01 columnas, T04 frontend, T05 post-confirm flow |
| `frontend/pages/cost-calc/price-confirmation.html` | Modificar | T02 URL param, T03 View Calculation fix |
| `frontend/pages/cost-calc/approve-calculations.html` | **Crear** | T06 nueva página Approve Cost Calculation |
| `frontend/js/sidebar.js` | Modificar | T06 agregar entrada de menú |
| `src/LicoresMaduro.API/Controllers/CostCalc/CostCalculationsController.cs` | Modificar | T04 backend guard confirmer ≠ approver |

---

## Task 1 — Quitar columnas "Inland Tariff" y "SHP CHGS"

**Files:**
- Modify: `frontend/pages/cost-calc/calculation.html`

Actualmente, en el grid de detalle PO existen dos columnas con valores siempre 0.00 que el cliente quiere quitar. Hay 4 lugares en el mismo archivo que deben tocarse.

- [ ] **Step 1: Quitar el `<th>` de Inland Tariff y Ship Chgs del header de la tabla**

En `calculation.html` alrededor de las líneas 538–539, eliminar estas dos líneas:
```html
<th class="text-end">Inland Tariff</th>
<th class="text-end">Ship Chgs</th>
```

- [ ] **Step 2: Quitar los `<td>` de datos correspondientes**

En `calculation.html` alrededor de las líneas 501–502, eliminar estas dos líneas:
```html
<td class="text-end">${fmt(eq>0?d.CcpdInlandTariff/eq:0)}</td>
<td class="text-end">${fmt(eq>0?d.CcpdShipCharges/eq:0)}</td>
```

- [ ] **Step 3: Quitar las variables de total y los tiles del Grand Summary**

En `calculation.html` alrededor de las líneas 730–731, eliminar:
```javascript
const totInlandTariff = allDetails.reduce((s,d)=>s+(d.CcpdInlandTariff??0),0);
const totShipChg      = allDetails.reduce((s,d)=>s+(d.CcpdShipCharges??0),0);
```

En `calculation.html` alrededor de las líneas 745–746, eliminar:
```javascript
${tile('Total Inland Tariff', totInlandTariff)}
${tile('Total Ship Chgs', totShipChg)}
```

- [ ] **Step 4: Verificar visualmente**

Abrir `calculation.html?id=64` en el browser. El grid PO debe tener las columnas Insurance, Transport, Unloading, Final Cost sin Inland Tariff ni Ship Chgs. El Grand Summary no debe mostrar esos dos tiles.

- [ ] **Step 5: Commit**

```bash
git add frontend/pages/cost-calc/calculation.html
git commit -m "feat(cost-calc): remove Inland Tariff and Ship Chgs columns from PO detail grid and summary"
```

---

## Task 2 — Bug: price-confirmation abre con `#undefined`

**Files:**
- Modify: `frontend/pages/cost-calc/price-confirmation.html`

**Causa raíz:** El botón "Confirm Prices" en `calculation.html` (línea 455) abre `price-confirmation.html?id=${c.CcCalcNumber}`. Sin embargo, `price-confirmation.html` **nunca lee** el parámetro `?id=` de la URL. La sección "Calculation Info" intenta mostrar el `calcId` pero éste queda `undefined`.

**Fix:** Leer el `?id=` de la URL al inicio, mostrar la info de esa calc en el header, y pre-filtrar la lista de POs a esa calc específica. Si no hay `?id=`, mostrar todos los POs pendientes (comportamiento actual).

- [ ] **Step 1: Agregar lectura del URL param al inicio del script**

En `price-confirmation.html`, en el bloque `DOMContentLoaded` (alrededor de línea 180), agregar **antes** de `loadPos()`:

```javascript
const _urlParams = new URLSearchParams(window.location.search);
const _urlCalcId = parseInt(_urlParams.get('id'), 10) || null;
```

- [ ] **Step 2: Modificar `loadPos()` para filtrar por calcId si viene de URL**

Localizar la función `loadPos()` (alrededor de línea 187). Cambiar la llamada al API para pasar el filtro:

```javascript
async function loadPos() {
  try {
    const data = await API.costCalc.getPendingPricePos();
    // Si venimos de una calc específica, filtrar solo esa
    const rows = _urlCalcId
      ? data.filter(p => p.CcphCalcNumber === _urlCalcId)
      : data;
    renderL1(rows);
  } catch(e) {
    showToast('Error loading pending POs', 'danger');
  }
}
```

- [ ] **Step 3: Corregir el header "Calculation Info" para no mostrar undefined**

Localizar donde se muestra "Calc No: #undefined" en el HTML o JS. Buscar la referencia al campo `calcId` en la sección de header/Calculation Info del nivel 1. Si el header muestra el calcId del parámetro URL, reemplazar la lógica por:

```javascript
// En la sección que renderiza Calculation Info del nivel 1:
const headerCalcNo = document.getElementById('header-calc-no');
const headerStatus = document.getElementById('header-status');
if (headerCalcNo) headerCalcNo.textContent = _urlCalcId ? `#${_urlCalcId}` : '—';
if (headerStatus) headerStatus.textContent = _urlCalcId ? 'Pending Confirmation' : 'All';
```

Si el header está en el HTML estático con placeholders, encontrar los elementos y actualizar sus valores.

- [ ] **Step 4: Verificar**

1. Desde `calculation.html?id=64`, presionar "Confirm Prices".
2. price-confirmation debe abrir mostrando Calc No: `#64` (no `#undefined`).
3. La lista debe mostrar solo los POs de la calc 64 (si tiene POs pendientes).
4. Sin `?id=` en URL, la página debe seguir mostrando todos los POs pendientes.

- [ ] **Step 5: Commit**

```bash
git add frontend/pages/cost-calc/price-confirmation.html
git commit -m "fix(price-confirm): read ?id= URL param to show correct calc info and filter POs"
```

---

## Task 3 — Bug: "View Calculation" sin calcId

**Files:**
- Modify: `frontend/pages/cost-calc/price-confirmation.html`

**Causa raíz:** El botón "View Calculation" en `price-confirmation.html` navega a `calculation.html` sin pasar el ID de la calc, resultando en "No calculation ID specified".

- [ ] **Step 1: Localizar el botón "View Calculation"**

Buscar en `price-confirmation.html` el botón o link "View Calculation". Debe verse algo como:
```html
<a href="calculation.html" ...>View Calculation</a>
<!-- O alguna variante con href vacío o sin ?id= -->
```

- [ ] **Step 2: Corregir el href para usar el calcId actual**

Si el botón es estático en el HTML, convertirlo a elemento con id y actualizar su href en JS:

HTML (cambiar href vacío a `#`):
```html
<a id="btn-view-calc" href="#" class="btn btn-outline-secondary btn-sm">
  <i class="fas fa-calculator me-1"></i>View Calculation
</a>
```

JS (actualizar el href dinámicamente cuando se cargue la página o cuando se abra un PO):

```javascript
// Función utilitaria para actualizar el botón View Calculation
function updateViewCalcBtn(calcId) {
  const btn = document.getElementById('btn-view-calc');
  if (!btn) return;
  if (calcId) {
    btn.href = `calculation.html?id=${calcId}`;
    btn.classList.remove('disabled');
  } else {
    btn.href = '#';
    btn.classList.add('disabled');
  }
}

// Llamar al inicio con el URL param:
updateViewCalcBtn(_urlCalcId);

// Llamar también cuando se abre un PO (función openPo, línea ~228):
async function openPo(calcId, poNo) {
  currentCalcId = calcId; currentPoNo = poNo;
  updateViewCalcBtn(calcId);  // <-- agregar esta línea
  // ... resto del código existente
}
```

- [ ] **Step 3: Verificar**

1. Abrir `price-confirmation.html?id=64`.
2. El botón "View Calculation" debe estar habilitado y al hacer click, navegar a `calculation.html?id=64`.
3. calculation.html debe abrir correctamente sin el error "No calculation ID specified".

- [ ] **Step 4: Commit**

```bash
git add frontend/pages/cost-calc/price-confirmation.html
git commit -m "fix(price-confirm): wire View Calculation button with correct calcId href"
```

---

## Task 4 — Regla 4-eyes: Confirmer ≠ Approver

**Files:**
- Modify: `src/LicoresMaduro.API/Controllers/CostCalc/CostCalculationsController.cs`
- Modify: `frontend/pages/cost-calc/calculation.html`

**Contexto:** El cliente reporta que quien hizo "Confirm" también puede hacer "Approve", lo que viola el principio de 4-ojos. El `CcphConfirmedBy` (en los PO heads) almacena el nombre del usuario que confirmó. El backend debe bloquearlo y el frontend debe ocultar el botón.

### Sub-task 4A — Guardia en el backend

- [ ] **Step 1: Agregar guardia en el endpoint Approve**

En `CostCalculationsController.cs`, endpoint `Approve` (alrededor de línea 887), **después** del status guard (`if (calc.CcCalcStatus != "CF")`), agregar:

```csharp
// 4-eyes: whoever confirmed cannot approve
var currentUser = User.Identity?.Name ?? string.Empty;
bool confirmedByCurrentUser = calc.PoHeads
    .Any(p => string.Equals(p.CcphConfirmedBy, currentUser, StringComparison.OrdinalIgnoreCase));

if (confirmedByCurrentUser)
    return BadRequest(new { message = "The user who confirmed this calculation cannot approve it. Another user must approve." });
```

- [ ] **Step 2: Verificar respuesta del backend**

Compilar y ejecutar:
```bash
cd src/LicoresMaduro.API
dotnet build
```
Expected: 0 errors.

Probar manualmente: intentar aprobar una calc que el usuario actual confirmó → debe recibir HTTP 400 con `message`.

- [ ] **Step 3: Commit backend**

```bash
git add src/LicoresMaduro.API/Controllers/CostCalc/CostCalculationsController.cs
git commit -m "feat(cost-calc): add 4-eyes guard: confirmer cannot approve same calculation"
```

### Sub-task 4B — Ocultar botón Approve en frontend si el usuario fue el confirmer

- [ ] **Step 4: Agregar campo `CcCalcConfirmedBy` al objeto retornado por la API**

El objeto calc que devuelve `getCalculation(calcId)` actualmente no expone quién confirmó (solo los PO heads lo tienen). Necesitamos exponer ese dato.

**Opción simple (sin migración):** en la respuesta del endpoint `GET /api/cost-calc/calculations/{id}`, incluir el primer `CcphConfirmedBy` no nulo de los PoHeads. Localizar el DTO o el mapeo de la respuesta y agregar:

```csharp
// En el DTO o en el Select del GetCalculation endpoint:
CcCalcConfirmedBy = calc.PoHeads.FirstOrDefault(p => p.CcphConfirmedBy != null)?.CcphConfirmedBy
```

Si el endpoint devuelve el objeto entidad directamente, agregar una propiedad calculada al DTO de respuesta.

- [ ] **Step 5: Usar `CcCalcConfirmedBy` en el frontend para ocultar el botón Approve**

En `calculation.html`, en la función que renderiza los botones del header (alrededor de línea 420–455), la línea actual es:
```javascript
const isAdmin = user?.RoleName==='SuperAdmin' || user?.RoleName==='Admin' ||
                Auth.hasPermission('COST_CALCULATIONS','CanApprove');
```

Después de obtener `c` (el objeto calc) y `user` (el usuario actual), agregar:

```javascript
const currentUserName = user?.UserName ?? user?.Email ?? '';
const confirmedByMe   = c.CcCalcConfirmedBy &&
    c.CcCalcConfirmedBy.toLowerCase() === currentUserName.toLowerCase();

// Reemplazar la condición del botón Approve (línea ~451):
// ANTES: if(c.CcStatus==='CF'&&isAdmin)
// DESPUÉS:
if(c.CcStatus==='CF' && isAdmin && !confirmedByMe) {
  btns+=` <button class="btn btn-success btn-sm" onclick="approveCalc()">
            <i class="fas fa-thumbs-up me-1"></i>Approve
          </button>`;
}
```

- [ ] **Step 6: Mostrar mensaje informativo cuando el usuario no puede aprobar por 4-eyes**

Justo después del bloque anterior, agregar:

```javascript
if(c.CcStatus==='CF' && isAdmin && confirmedByMe) {
  btns+=` <span class="badge bg-secondary ms-1" title="You confirmed this calculation; another user must approve.">
            <i class="fas fa-lock me-1"></i>Awaiting Approver
          </span>`;
}
```

- [ ] **Step 7: También manejar el error 400 del backend en approveCalc()**

En la función que maneja el click del btn-approve-confirm (línea ~829), capturar el error 400:

```javascript
document.getElementById('btn-approve-confirm').addEventListener('click', async () => {
  bootstrap.Modal.getInstance(document.getElementById('modal-approve'))?.hide();
  try {
    await API.costCalc.approveCalculation(calcId);
    showToast('Calculation approved.','success');
    await loadCalc();
  } catch(err) {
    const msg = err?.response?.data?.message || err?.message || 'Approval failed.';
    showToast(msg, 'danger');
  }
});
```

- [ ] **Step 8: Verificar**

1. Confirmar una calc con usuario A → usuario A no debe ver el botón Approve, debe ver badge "Awaiting Approver".
2. Usar usuario B (con rol approver) → debe ver el botón Approve y poder aprobarlo.
3. Si usuario A intenta aprobar vía API directa → debe recibir 400.

- [ ] **Step 9: Commit frontend 4-eyes**

```bash
git add frontend/pages/cost-calc/calculation.html
git commit -m "feat(cost-calc): hide Approve button for user who confirmed (4-eyes rule)"
```

---

## Task 5 — Post-Confirm: mensaje "email enviado al manager" + redirect

**Files:**
- Modify: `frontend/pages/cost-calc/calculation.html`

**Contexto:** El cliente dice: "Después de Confirm, hay que salir un email al manager quien debe approve y debe cerrar este modulo." El backend ya envía el email (`NotifyCostCalcConfirmedAsync` en línea 868). El frontend solo necesita mostrar un mensaje y redirigir al listado.

- [ ] **Step 1: Localizar el handler del confirm en el frontend**

En `calculation.html` alrededor de la línea 810:
```javascript
try{await API.costCalc.confirmCalculation(calcId);showToast('Calculation confirmed.','success');await loadCalc();}
```

- [ ] **Step 2: Reemplazar por mensaje específico + redirect**

```javascript
try {
  await API.costCalc.confirmCalculation(calcId);
  // Mostrar mensaje y redirigir — no recargar la página
  showToast(
    'Calculation confirmed. An email notification has been sent to the manager for approval.',
    'success',
    5000  // duración más larga para que se lea
  );
  setTimeout(() => { window.location.href = 'index.html'; }, 3000);
} catch(err) {
  showToast(err?.message || 'Confirm failed.', 'danger');
}
```

> **Nota:** Si `showToast` no acepta duración como tercer parámetro, verificar la firma de la función y adaptar. Si usa la duración de Bootstrap Toast, buscar el parámetro `delay`.

- [ ] **Step 3: Verificar**

1. Confirmar una calc → aparece toast con mensaje sobre el email al manager.
2. Después de ~3 segundos, la página redirige automáticamente a `index.html`.
3. La calc aparece en la lista con status "Confirmed".

- [ ] **Step 4: Commit**

```bash
git add frontend/pages/cost-calc/calculation.html
git commit -m "feat(cost-calc): show email notification message and redirect to list after Confirm"
```

---

## Task 6 — Nueva página "Approve Cost Calculation"

**Files:**
- Modify: `frontend/js/sidebar.js`
- Create: `frontend/pages/cost-calc/approve-calculations.html`

**Contexto:** El cliente quiere una nueva opción en el sidebar que muestre **solo** las calcs con status "Confirmed" (`CF`) y que solo sea accesible a usuarios con rol approver. Esta página es distinta de `index.html` (que muestra todos los status).

### Sub-task 6A — Agregar entrada al sidebar

- [ ] **Step 1: Agregar ítem de menú en sidebar.js**

En `frontend/js/sidebar.js`, en la sección Cost Calculation (alrededor de línea 61–70), agregar después de `COST_CALCULATIONS`:

```javascript
{ code: 'COST_APPROVE_CALC', label: 'Approve Cost Calculation', href: 'pages/cost-calc/approve-calculations.html' },
```

El bloque debe quedar:
```javascript
{ code: 'COST_CALCULATIONS',   label: 'Calculations',              href: 'pages/cost-calc/index.html' },
{ code: 'COST_APPROVE_CALC',   label: 'Approve Cost Calculation',  href: 'pages/cost-calc/approve-calculations.html' },
{ code: 'COST_NEW_CALC',       label: 'New Calculation',           href: 'pages/cost-calc/new-calculation.html' },
```

> **Nota:** El sidebar renderiza ítems basado en permisos del usuario. Si el backend no tiene el permission code `COST_APPROVE_CALC` registrado, el ítem no aparecerá. Verificar en la tabla de permisos si existe o si se debe agregar. Por ahora, si no existe, usar `COST_CALCULATIONS` como fallback y controlar el acceso en la página misma.

- [ ] **Step 2: Commit sidebar**

```bash
git add frontend/js/sidebar.js
git commit -m "feat(sidebar): add Approve Cost Calculation navigation entry"
```

### Sub-task 6B — Crear la página approve-calculations.html

- [ ] **Step 3: Crear `frontend/pages/cost-calc/approve-calculations.html`**

La página debe:
- Verificar que el usuario tiene rol approver (si no, mostrar acceso denegado)
- Cargar solo calcs con status `CF` vía `API.costCalc.getCalculations()` filtrado por status
- Por cada calc, verificar que el usuario actual NO fue el que confirmó (4-eyes) → si fue el confirmer, mostrar como "no disponible para ti"
- Mostrar columnas: #, Calc No., Date, Warehouse, Forwarder, Currency, POs, Confirmed By, Status
- Botón "Approve" por fila (solo si el usuario no fue el confirmer)
- Botón "View" para ir a `calculation.html?id=N`

Crear el archivo con el siguiente contenido:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <meta name="viewport" content="width=device-width,initial-scale=1"/>
  <title>Approve Cost Calculations - Licores Maduro</title>
  <link rel="stylesheet" href="../../css/bootstrap.min.css"/>
  <link rel="stylesheet" href="../../css/app.css"/>
  <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"/>
</head>
<body>
<div id="sidebar-container"></div>
<div class="main-content">
  <div id="topbar-container"></div>
  <div class="content-wrapper p-4">

    <!-- Breadcrumb -->
    <nav aria-label="breadcrumb" class="mb-3">
      <ol class="breadcrumb">
        <li class="breadcrumb-item"><a href="index.html">Cost Calculations</a></li>
        <li class="breadcrumb-item active">Approve Cost Calculations</li>
      </ol>
    </nav>

    <div class="d-flex justify-content-between align-items-center mb-3">
      <div>
        <h4 class="fw-bold mb-0">Approve Cost Calculations</h4>
        <div class="text-muted small">Calculations pending approval — Status: Confirmed</div>
      </div>
    </div>

    <!-- Access denied panel (hidden by default) -->
    <div id="access-denied" class="alert alert-danger d-none">
      <i class="fas fa-lock me-2"></i>
      <strong>Access Denied.</strong> Only users with the Approver role can access this page.
    </div>

    <!-- Table -->
    <div id="table-container">
      <div class="card shadow-sm">
        <div class="card-body p-0">
          <div class="d-flex justify-content-between align-items-center px-3 py-2 border-bottom">
            <span class="fw-semibold text-muted small" id="rec-count">Loading...</span>
          </div>
          <table class="table table-hover table-sm mb-0" id="tbl-approve">
            <thead class="table-light">
              <tr>
                <th>#</th>
                <th>Calc No.</th>
                <th>Date</th>
                <th>Warehouse</th>
                <th>Forwarder</th>
                <th>Currency</th>
                <th class="text-center">POs</th>
                <th>Confirmed By</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody id="tbl-body">
              <tr><td colspan="10" class="text-center py-4 text-muted">Loading...</td></tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

  </div><!-- content-wrapper -->
</div><!-- main-content -->

<!-- Approve Confirmation Modal -->
<div class="modal fade" id="modal-approve" tabindex="-1">
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content">
      <div class="modal-header border-0 pb-0">
        <h5 class="fw-bold mb-0"><i class="fas fa-thumbs-up me-2 text-success"></i>Approve Calculation?</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <p class="mb-1">Are you sure you want to approve <strong id="modal-calc-label"></strong>?</p>
        <p class="text-muted small">This will run the price calculation and send confirmation emails.</p>
      </div>
      <div class="modal-footer border-0 pt-0">
        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancel</button>
        <button type="button" class="btn btn-success px-4 fw-semibold" id="btn-approve-confirm">
          <i class="fas fa-thumbs-up me-2"></i>Approve
        </button>
      </div>
    </div>
  </div>
</div>

<!-- Toast -->
<div class="position-fixed bottom-0 end-0 p-3" style="z-index:9999">
  <div id="toast-msg" class="toast align-items-center" role="alert">
    <div class="d-flex">
      <div class="toast-body fw-semibold" id="toast-text"></div>
      <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast"></button>
    </div>
  </div>
</div>

<script src="../../js/bootstrap.bundle.min.js"></script>
<script src="../../js/auth.js"></script>
<script src="../../js/sidebar.js"></script>
<script src="../../js/topbar.js"></script>
<script src="../../js/api.js"></script>
<script>
  let pendingApproveCalcId = null;

  function showToast(msg, type='success') {
    const el = document.getElementById('toast-msg');
    const txt = document.getElementById('toast-text');
    el.className = `toast align-items-center text-bg-${type} border-0`;
    txt.textContent = msg;
    bootstrap.Toast.getOrCreateInstance(el, {delay:4000}).show();
  }

  async function loadCalcs() {
    const user = Auth.getUser();
    const isApprover = user?.RoleName === 'SuperAdmin' || user?.RoleName === 'Admin' ||
                       Auth.hasPermission('COST_CALCULATIONS', 'CanApprove');

    if (!isApprover) {
      document.getElementById('access-denied').classList.remove('d-none');
      document.getElementById('table-container').classList.add('d-none');
      return;
    }

    try {
      const data = await API.costCalc.getCalculations();
      // Only CF (Confirmed) calculations
      const cfCalcs = data.filter(c => c.CcCalcStatus === 'CF');

      document.getElementById('rec-count').textContent = `${cfCalcs.length} record${cfCalcs.length!==1?'s':''} pending approval`;

      const currentUserName = (user?.UserName ?? user?.Email ?? '').toLowerCase();
      const tbody = document.getElementById('tbl-body');

      if (!cfCalcs.length) {
        tbody.innerHTML = '<tr><td colspan="10" class="text-center py-4 text-muted">No calculations pending approval.</td></tr>';
        return;
      }

      tbody.innerHTML = cfCalcs.map((c, i) => {
        // Determine who confirmed (from CcCalcConfirmedBy or first PO head confirmedBy)
        const confirmedBy = c.CcCalcConfirmedBy || '—';
        const confirmedByMe = confirmedBy.toLowerCase() === currentUserName;

        const viewBtn = `<a href="calculation.html?id=${c.CcCalcNumber}" class="btn btn-outline-secondary btn-sm me-1" title="View">
                           <i class="fas fa-eye"></i>
                         </a>`;

        const approveBtn = confirmedByMe
          ? `<span class="badge bg-secondary" title="You confirmed this calculation — another user must approve.">
               <i class="fas fa-lock me-1"></i>Awaiting Other Approver
             </span>`
          : `<button class="btn btn-success btn-sm" onclick="openApproveModal(${c.CcCalcNumber}, '${c.CcCalcNumber}')">
               <i class="fas fa-thumbs-up me-1"></i>Approve
             </button>`;

        return `<tr>
          <td>${i+1}</td>
          <td><strong>#${c.CcCalcNumber}</strong></td>
          <td>${c.CcCalcDate ? c.CcCalcDate.substring(0,10) : '—'}</td>
          <td>${c.WarehouseName || c.CcCalcWarehouse || '—'}</td>
          <td>${c.ForwarderName || '—'}</td>
          <td>${c.CurrencyCode || '—'}</td>
          <td class="text-center">${c.TotalPos ?? '—'}</td>
          <td>${confirmedBy}</td>
          <td><span class="badge bg-warning text-dark">Confirmed</span></td>
          <td class="text-nowrap">${viewBtn} ${approveBtn}</td>
        </tr>`;
      }).join('');

    } catch(e) {
      showToast('Error loading calculations: ' + (e?.message || e), 'danger');
    }
  }

  function openApproveModal(calcId, calcLabel) {
    pendingApproveCalcId = calcId;
    document.getElementById('modal-calc-label').textContent = `Calculation #${calcLabel}`;
    new bootstrap.Modal(document.getElementById('modal-approve')).show();
  }

  document.getElementById('btn-approve-confirm').addEventListener('click', async () => {
    bootstrap.Modal.getInstance(document.getElementById('modal-approve'))?.hide();
    if (!pendingApproveCalcId) return;
    try {
      await API.costCalc.approveCalculation(pendingApproveCalcId);
      showToast(`Calculation #${pendingApproveCalcId} approved successfully.`, 'success');
      await loadCalcs();
    } catch(err) {
      const msg = err?.response?.data?.message || err?.message || 'Approval failed.';
      showToast(msg, 'danger');
    } finally {
      pendingApproveCalcId = null;
    }
  });

  document.addEventListener('DOMContentLoaded', () => {
    Auth.requireAuth();
    Sidebar.init();
    Topbar.init();
    loadCalcs();
  });
</script>
</body>
</html>
```

- [ ] **Step 4: Verificar la nueva página**

1. Navegar al sidebar → "Approve Cost Calculation".
2. Con usuario sin rol approver: debe mostrar el panel "Access Denied".
3. Con usuario approver: debe mostrar la tabla con calcs CF.
4. Calcs que el usuario confirmó deben mostrar badge "Awaiting Other Approver".
5. Calcs que otro confirmó deben mostrar botón "Approve".
6. Presionar Approve → modal de confirmación → approbar → la calc desaparece de la lista (cambia a AP).

- [ ] **Step 5: Commit**

```bash
git add frontend/pages/cost-calc/approve-calculations.html
git commit -m "feat(cost-calc): add Approve Cost Calculations page for approver role (CF status only)"
```

---

## Self-Review — Spec Coverage

| Observación del cliente | Tarea | Cubierta |
|------------------------|-------|----------|
| Quitar columnas "Inland Tariff" y "SHP CHGS" | T01 | ✅ |
| Confirmer no puede hacer Approve | T04 | ✅ |
| Después de Confirm: email al manager + cerrar módulo | T05 | ✅ |
| Nueva opción "Approve Cost Calculation" en sidebar | T06 | ✅ |
| Bug: price-confirmation abre con `#undefined` | T02 | ✅ |
| Bug: "View Calculation" sin calcId | T03 | ✅ |

**Dependencias entre tareas:**
- T02 debe hacerse antes de T03 (T03 usa `_urlCalcId` definido en T02).
- T04A (backend) y T04B (frontend) son independientes entre sí pero forman la misma feature.
- T05 y T06 son independientes de todo.

**Orden recomendado:** T01 → T02 → T03 → T04A → T04B → T05 → T06
