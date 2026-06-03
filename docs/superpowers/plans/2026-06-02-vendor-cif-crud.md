# Vendor CIF CRUD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Crear la pagina de administracion de CC_VENDOR_CIF dentro del modulo Cost Calc, con permisos por rol asignables desde la UI.

**Architecture:** SQL migration registra el submodule en LM_Submodules; el controlador VendorCifController usa IPermissionService para Write/Delete; el frontend vendor-cif.html sigue el patron de item-weights.html con modales Bootstrap y oculta botones segun Auth.hasPermission.

**Tech Stack:** ASP.NET Core 8, Entity Framework Core, Bootstrap 5.3.2, FontAwesome 6.5.0, JS vanilla.

---

## Files

| Accion | Archivo |
|--------|---------|
| CREAR | `database/85_VendorCifSubmodule.sql` |
| CREAR | `src/LicoresMaduro.API/Controllers/CostCalc/VendorCifController.cs` |
| CREAR | `frontend/pages/cost-calc/vendor-cif.html` |
| MODIFICAR | `frontend/js/api.js` |
| MODIFICAR | `frontend/js/sidebar.js` |

---

## Task 1: SQL Migration — Registrar submodule y permisos

**Files:**
- Create: `database/85_VendorCifSubmodule.sql`

- [ ] **Step 1: Crear el archivo SQL**

Crear `database/85_VendorCifSubmodule.sql` con este contenido exacto:

```sql
USE LicoresMaduoDB;
GO

-- Register COST_VENDOR_CIF submodule so it appears in the permissions management UI
DECLARE @CostId INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'COST');

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleCode = 'COST_VENDOR_CIF')
BEGIN
    INSERT INTO dbo.LM_Submodules (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (@CostId, 'CIF Vendors', 'COST_VENDOR_CIF', 'CC_VENDOR_CIF', 9);
END
GO

-- Seed default role permissions (idempotent MERGE)
DECLARE @SmId INT = (SELECT SubmoduleId FROM dbo.LM_Submodules WHERE SubmoduleCode = 'COST_VENDOR_CIF');

MERGE dbo.LM_RolePermissions AS tgt
USING (VALUES
    (1, @SmId, 1,1,1,1,1,0),
    (2, @SmId, 1,1,1,1,1,0),
    (3, @SmId, 1,1,0,0,0,0)
) AS src (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete, CanApprove)
ON tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete, CanApprove)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead,
            src.CanWrite, src.CanEdit, src.CanDelete, src.CanApprove);
GO

-- Verify
SELECT sm.SubmoduleCode, sm.SubmoduleName, r.RoleId,
       r.CanAccess, r.CanRead, r.CanWrite, r.CanDelete
FROM dbo.LM_Submodules sm
JOIN dbo.LM_RolePermissions r ON r.SubmoduleId = sm.SubmoduleId
WHERE sm.SubmoduleCode = 'COST_VENDOR_CIF'
ORDER BY r.RoleId;
GO
```

- [ ] **Step 2: Ejecutar el script en SQL Server**

Abrir SQL Server Management Studio, conectar a la base de datos LicoresMaduoDB y ejecutar el archivo `85_VendorCifSubmodule.sql`. El SELECT final debe devolver 3 filas (una por rol: 1, 2, 3).

- [ ] **Step 3: Verificar resultado esperado**

```
SubmoduleCode    SubmoduleName  RoleId  CanAccess  CanRead  CanWrite  CanDelete
COST_VENDOR_CIF  CIF Vendors    1       1          1        1         1
COST_VENDOR_CIF  CIF Vendors    2       1          1        1         1
COST_VENDOR_CIF  CIF Vendors    3       1          1        0         0
```

- [ ] **Step 4: Commit**

```bash
git add database/85_VendorCifSubmodule.sql
git commit -m "feat(cost-calc): register COST_VENDOR_CIF submodule and seed role permissions"
```

---

## Task 2: API Controller — VendorCifController.cs

**Files:**
- Create: `src/LicoresMaduro.API/Controllers/CostCalc/VendorCifController.cs`

- [ ] **Step 1: Crear el controlador**

Crear `src/LicoresMaduro.API/Controllers/CostCalc/VendorCifController.cs` con este contenido:

```csharp
using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/vendor-cif")]
[Authorize]
[Produces("application/json")]
public sealed class VendorCifController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPermissionService   _permissions;

    public VendorCifController(ApplicationDbContext db, IPermissionService permissions)
    { _db = db; _permissions = permissions; }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var data = await _db.CcVendorCifs
            .AsNoTracking()
            .OrderBy(x => x.VcifVendor)
            .Select(x => x.VcifVendor)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<string>>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VendorCifDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "COST_VENDOR_CIF", "WRITE", ct))
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.VendorCode))
            return BadRequest(ApiResponse.Fail("Vendor code is required."));

        var code = dto.VendorCode.Trim().ToUpper();

        if (await _db.CcVendorCifs.AnyAsync(x => x.VcifVendor == code, ct))
            return Conflict(ApiResponse.Fail($"Vendor code '{code}' already exists."));

        _db.CcVendorCifs.Add(new CcVendorCif { VcifVendor = code });
        await _db.SaveChangesAsync(ct);
        return StatusCode(201, ApiResponse<string>.Ok(code, "Created."));
    }

    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(string code, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "COST_VENDOR_CIF", "DELETE", ct))
            return Forbid();

        var normalized = code.Trim().ToUpper();
        var item = await _db.CcVendorCifs
            .FirstOrDefaultAsync(x => x.VcifVendor == normalized, ct);
        if (item is null)
            return NotFound(ApiResponse.Fail($"Vendor '{normalized}' not found."));

        _db.CcVendorCifs.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }
}

public record VendorCifDto(string VendorCode);
```

- [ ] **Step 2: Verificar que compila**

```bash
cd "C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\src\LicoresMaduro.API"
dotnet build
```

Expected: `Build succeeded. 0 Warning(s). 0 Error(s).`

Si hay errores de namespace en `IPermissionService`, verificar que el using `LicoresMaduro.API.Services` sea correcto buscando donde esta declarado en el proyecto.

- [ ] **Step 3: Commit**

```bash
git add src/LicoresMaduro.API/Controllers/CostCalc/VendorCifController.cs
git commit -m "feat(cost-calc): add VendorCifController with GET/POST/DELETE and permission checks"
```

---

## Task 3: api.js — Agregar metodos vendorCif

**Files:**
- Modify: `frontend/js/api.js`

El objeto `costCalc` en api.js termina con:

```javascript
    deleteShipCharge: (calcId, id)    => del(`/api/cost-calc/calculations/${calcId}/ship-charges/${id}`)
  };
```

- [ ] **Step 1: Agregar los 3 metodos al objeto costCalc**

Reemplazar la ultima linea de costCalc (antes del cierre `};`) para agregar los metodos de vendorCif:

```javascript
    deleteShipCharge: (calcId, id)    => del(`/api/cost-calc/calculations/${calcId}/ship-charges/${id}`),
    // Vendor CIF
    getVendorCifs:   ()     => get('/api/cost-calc/vendor-cif'),
    createVendorCif: (dto)  => post('/api/cost-calc/vendor-cif', dto),
    deleteVendorCif: (code) => del(`/api/cost-calc/vendor-cif/${encodeURIComponent(code)}`)
  };
```

Usar el Edit tool con `old_string` = la linea exacta `    deleteShipCharge: (calcId, id)    => del(\`/api/cost-calc/calculations/${calcId}/ship-charges/${id}\`)` y `new_string` el bloque de arriba.

- [ ] **Step 2: Verificar que los metodos existen**

```bash
grep -n "getVendorCifs\|createVendorCif\|deleteVendorCif" frontend/js/api.js
```

Expected: 3 lineas con los nuevos metodos.

- [ ] **Step 3: Commit**

```bash
git add frontend/js/api.js
git commit -m "feat(cost-calc): add vendorCif API methods to api.js"
```

---

## Task 4: sidebar.js — Agregar enlace CIF Vendors

**Files:**
- Modify: `frontend/js/sidebar.js`

El bloque Cost Calc en sidebar.js termina con:

```javascript
        { code: 'COST_PRICE_CONFIRM', label: 'Price Confirmation', href: 'pages/cost-calc/price-confirmation.html' },
      ],
    },
```

- [ ] **Step 1: Agregar entrada COST_VENDOR_CIF**

Reemplazar el final del bloque Cost Calc para agregar el nuevo enlace antes del cierre `],`:

```javascript
        { code: 'COST_PRICE_CONFIRM', label: 'Price Confirmation',   href: 'pages/cost-calc/price-confirmation.html' },
        { code: 'COST_VENDOR_CIF',    label: 'CIF Vendors',          href: 'pages/cost-calc/vendor-cif.html' },
      ],
    },
```

- [ ] **Step 2: Verificar**

```bash
grep -n "COST_VENDOR_CIF" frontend/js/sidebar.js
```

Expected: 1 linea con el nuevo entry.

- [ ] **Step 3: Commit**

```bash
git add frontend/js/sidebar.js
git commit -m "feat(cost-calc): add CIF Vendors link to Cost Calc sidebar section"
```

---

## Task 5: Frontend page — vendor-cif.html

**Files:**
- Create: `frontend/pages/cost-calc/vendor-cif.html`

- [ ] **Step 1: Crear el archivo HTML**

Crear `frontend/pages/cost-calc/vendor-cif.html` con este contenido:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/><meta name="viewport" content="width=device-width, initial-scale=1.0"/>
  <title>CIF Vendors - Licores Maduro</title>
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css"/>
  <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css"/>
  <link rel="stylesheet" href="../../css/main.css"/>
</head>
<body>
<div id="sidebar-overlay" style="display:none;position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:999;"></div>
<div class="app-wrapper">
  <nav id="sidebar" class="sidebar">
    <div class="sidebar-header"><img src="../../img/LogoLicores.png" alt="Licores Maduro" class="sidebar-logo-img"/><span class="sidebar-logo-text">LICORES MADURO</span></div>
    <div id="sidebar-nav" class="sidebar-nav"></div>
    <div class="sidebar-footer"><div class="sidebar-user"><div class="sidebar-avatar" id="sidebar-user-initials">LM</div><div class="sidebar-text"><div id="sidebar-user-name" style="font-weight:600;">-</div></div></div></div>
  </nav>
  <div class="main-area">
    <header class="topbar">
      <button id="btn-sidebar-toggle" class="btn-sidebar-toggle"><i class="fas fa-bars"></i></button>
      <span class="topbar-title"><i class="fas fa-store-slash me-2 text-wine"></i>CIF Vendors</span>
      <div class="ms-auto d-flex align-items-center gap-2">
        <div class="dropdown">
          <button class="btn btn-outline-secondary btn-sm dropdown-toggle d-flex align-items-center gap-2" type="button" data-bs-toggle="dropdown">
            <span class="sidebar-avatar" id="topbar-user-initials" style="width:28px;height:28px;font-size:.75rem;">LM</span>
            <span id="topbar-user-name" class="d-none d-md-inline">-</span>
          </button>
          <ul class="dropdown-menu dropdown-menu-end shadow-sm">
            <li><div class="dropdown-header"><strong id="topbar-user-fullname">User</strong><br/><small class="text-muted" id="topbar-user-role">Role</small></div></li>
            <li><hr class="dropdown-divider"/></li>
            <li><a class="dropdown-item text-danger" href="#" id="btn-logout"><i class="fas fa-sign-out-alt me-2"></i>Logout</a></li>
          </ul>
        </div>
      </div>
    </header>
    <main class="page-content">
      <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
          <li class="breadcrumb-item"><a href="../../dashboard.html"><i class="fas fa-home me-1"></i>Home</a></li>
          <li class="breadcrumb-item"><a href="index.html">Cost Calculations</a></li>
          <li class="breadcrumb-item active">CIF Vendors</li>
        </ol>
      </nav>
      <div class="page-title">CIF Vendors</div>
      <div class="page-subtitle">Vendors with Cost, Insurance &amp; Freight price agreements. Insurance is not calculated for these vendors in cost calculations.</div>

      <div class="card mb-3">
        <div class="card-body d-flex align-items-center gap-2 py-2">
          <button id="btn-add" class="btn btn-wine btn-sm ms-auto" style="display:none;"><i class="fas fa-plus me-1"></i>Add Vendor</button>
        </div>
      </div>

      <div class="card">
        <div class="card-header d-flex align-items-center justify-content-between">
          <span><i class="fas fa-table me-2"></i>CIF Vendors</span>
          <span class="badge" style="background:var(--primary);color:#fff;" id="total-count">0 records</span>
        </div>
        <div class="card-body p-0">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Vendor Code</th>
                  <th id="actions-th" class="text-center" style="display:none;">Actions</th>
                </tr>
              </thead>
              <tbody id="data-tbody">
                <tr><td colspan="3" class="text-center py-4 text-muted"><i class="fas fa-spinner fa-spin me-2"></i>Loading...</td></tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </main>
  </div>
</div>

<!-- Add Vendor Modal -->
<div class="modal fade" id="addModal" tabindex="-1">
  <div class="modal-dialog modal-sm">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Add CIF Vendor</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <div class="mb-2">
          <label class="form-label">Vendor Code <span class="text-danger">*</span></label>
          <input type="text" id="f-vendor-code" class="form-control form-control-sm" maxlength="20" placeholder="e.g. BCP"/>
          <div id="f-vendor-error" class="text-danger small mt-1" style="display:none;"></div>
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancel</button>
        <button type="button" class="btn btn-wine" id="btn-save"><i class="fas fa-save me-1"></i>Save</button>
      </div>
    </div>
  </div>
</div>

<!-- Delete Confirm Modal -->
<div class="modal fade" id="deleteModal" tabindex="-1">
  <div class="modal-dialog modal-sm">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title text-danger"><i class="fas fa-exclamation-triangle me-2"></i>Confirm Delete</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <p>Are you sure you want to remove vendor <strong id="delete-code-label"></strong> from the CIF list?</p>
        <p class="text-muted small mb-0">This will affect future cost calculations for this vendor. Insurance will be calculated normally again.</p>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancel</button>
        <button type="button" class="btn btn-danger" id="btn-confirm-delete"><i class="fas fa-trash me-1"></i>Delete</button>
      </div>
    </div>
  </div>
</div>

<div class="toast-container position-fixed bottom-0 end-0 p-3" style="z-index:9999;">
  <div id="toast" class="toast align-items-center text-white border-0" role="alert">
    <div class="d-flex"><div class="toast-body" id="toast-msg"></div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>
  </div>
</div>

<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
<script src="../../js/i18n.js"></script>
<script src="../../js/api.js"></script>
<script src="../../js/auth.js"></script>
<script src="../../js/sidebar.js"></script>
<script>
  let allItems = [];
  let addModal, deleteModal;
  let _pendingDeleteCode = null;
  let canWrite = false, canDelete = false;

  document.addEventListener('DOMContentLoaded', async () => {
    if (!Auth.requireAuth()) return;
    Auth.startExpiryWatcher();
    Auth.populateUserUI();
    Sidebar.init();
    document.getElementById('btn-logout').addEventListener('click', e => { e.preventDefault(); Auth.logout(); });

    canWrite  = Auth.hasPermission('COST_VENDOR_CIF', 'CanWrite');
    canDelete = Auth.hasPermission('COST_VENDOR_CIF', 'CanDelete');

    addModal    = new bootstrap.Modal(document.getElementById('addModal'));
    deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));

    if (canWrite)  document.getElementById('btn-add').style.display = '';
    if (canDelete) document.getElementById('actions-th').style.display = '';

    document.getElementById('btn-add').addEventListener('click', openAdd);
    document.getElementById('btn-save').addEventListener('click', save);
    document.getElementById('btn-confirm-delete').addEventListener('click', confirmDelete);
    document.getElementById('f-vendor-code').addEventListener('input', e => {
      e.target.value = e.target.value.toUpperCase();
    });

    await loadData();
  });

  async function loadData() {
    document.getElementById('data-tbody').innerHTML =
      '<tr><td colspan="3" class="text-center py-4 text-muted"><i class="fas fa-spinner fa-spin me-2"></i>Loading...</td></tr>';
    try {
      const res = await API.costCalc.getVendorCifs();
      allItems = res?.Data || res || [];
      renderTable();
    } catch (err) { showToast(err.message || 'Error loading data.', 'danger'); }
  }

  function renderTable() {
    const tbody = document.getElementById('data-tbody');
    const cols = canDelete ? 3 : 2;
    document.getElementById('total-count').textContent = allItems.length + ' records';
    if (!allItems.length) {
      tbody.innerHTML = `<tr><td colspan="${cols}" class="text-center py-4 text-muted">No CIF vendors registered.</td></tr>`;
      return;
    }
    tbody.innerHTML = allItems.map((code, i) => `<tr>
      <td>${i + 1}</td>
      <td><strong>${esc(code)}</strong></td>
      ${canDelete ? `<td class="text-center">
        <button class="btn-action delete" onclick="askDelete('${esc(code)}')" title="Remove vendor">
          <i class="fas fa-trash"></i>
        </button>
      </td>` : ''}
    </tr>`).join('');
  }

  function openAdd() {
    document.getElementById('f-vendor-code').value = '';
    document.getElementById('f-vendor-error').style.display = 'none';
    addModal.show();
    setTimeout(() => document.getElementById('f-vendor-code').focus(), 300);
  }

  async function save() {
    const code = document.getElementById('f-vendor-code').value.trim();
    const errEl = document.getElementById('f-vendor-error');
    errEl.style.display = 'none';
    if (!code) { errEl.textContent = 'Vendor code is required.'; errEl.style.display = ''; return; }
    const btn = document.getElementById('btn-save');
    btn.disabled = true;
    try {
      await API.costCalc.createVendorCif({ VendorCode: code });
      addModal.hide();
      showToast('Vendor added successfully.', 'success');
      await loadData();
    } catch (err) {
      const msg = err.message || '';
      if (err.status === 409 || msg.toLowerCase().includes('already exists')) {
        errEl.textContent = `Vendor code "${code}" already exists.`;
        errEl.style.display = '';
      } else {
        showToast(msg || 'Error saving vendor.', 'danger');
      }
    } finally { btn.disabled = false; }
  }

  function askDelete(code) {
    _pendingDeleteCode = code;
    document.getElementById('delete-code-label').textContent = code;
    deleteModal.show();
  }

  async function confirmDelete() {
    if (!_pendingDeleteCode) return;
    const code = _pendingDeleteCode;
    _pendingDeleteCode = null;
    deleteModal.hide();
    try {
      await API.costCalc.deleteVendorCif(code);
      showToast('Vendor removed successfully.', 'success');
      await loadData();
    } catch (err) { showToast(err.message || 'Error deleting vendor.', 'danger'); }
  }

  function showToast(msg, type = 'success') {
    const el = document.getElementById('toast');
    el.className = `toast align-items-center text-white border-0 bg-${type === 'success' ? 'success' : 'danger'}`;
    document.getElementById('toast-msg').textContent = msg;
    new bootstrap.Toast(el, { delay: 3500 }).show();
  }

  function esc(s) {
    return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }
</script>
</body>
</html>
```

- [ ] **Step 2: Verificar que el archivo existe**

```bash
Test-Path "frontend/pages/cost-calc/vendor-cif.html"
```

Expected: `True`

- [ ] **Step 3: Commit**

```bash
git add frontend/pages/cost-calc/vendor-cif.html
git commit -m "feat(cost-calc): add vendor-cif.html page with add/delete modals and permission checks"
```

---

## Spec Self-Review Checklist

- [x] SQL migration registra submodule con ModuleId (no ModuleCode) — correcto
- [x] Submodule code es COST_VENDOR_CIF (patron COST_*) — correcto
- [x] Controller usa IPermissionService para WRITE y DELETE — permite asignacion por usuario desde UI
- [x] GET es publico para cualquier usuario autenticado
- [x] Frontend oculta btn-add si !canWrite, oculta actions-th si !canDelete
- [x] Modal de delete Bootstrap (no confirm()) — segun spec
- [x] Input auto-uppercase en frontend + Trim().ToUpper() en backend
- [x] 409 Conflict mostrado inline en el modal (no cierra el modal)
- [x] api.js metodos siguen el patron existente (get/post/del funciones locales)
- [x] sidebar.js entry sigue el formato de objeto {code, label, href}
- [x] Vendor CIF que ya esta en CC_VENDOR_CIF (migration 84) sigue funcionando
