# Vendor CIF CRUD — Design Spec

**Date:** 2026-06-02
**Feature:** Pagina de administracion para la tabla CC_VENDOR_CIF
**Module:** Cost Calc

---

## Goal

Crear una pagina dentro del modulo Cost Calc que permita ver, agregar y eliminar los vendors que tienen acuerdo de precio CIF (Cost, Insurance & Freight). El acceso esta controlado por el sistema de permisos existente (`Auth.hasPermission`), por lo que el administrador puede asignar quien tiene Write/Delete desde la UI de permisos sin cambiar codigo.

---

## Architecture

Tres capas siguiendo los patrones del proyecto:

1. **SQL migration** — registra el submodule `COST_VENDOR_CIF` en `LM_Submodules` y siembra permisos por rol por defecto
2. **API Controller** — `VendorCifController.cs` con GET/POST/DELETE, usando `PermissionService` para Write y Delete
3. **Frontend page** — `vendor-cif.html` con tabla + modal de agregar + modal de confirmacion de delete

No se necesita modelo nuevo: `CcVendorCif` ya existe en `ApplicationDbContext.cs` (linea 167) con `DbSet<CcVendorCif> CcVendorCifs`.

---

## Files

| Accion | Archivo | Responsabilidad |
|--------|---------|-----------------|
| CREAR | `database/85_VendorCifSubmodule.sql` | Registrar submodule + sembrar permisos por rol |
| CREAR | `src/LicoresMaduro.API/Controllers/CostCalc/VendorCifController.cs` | GET, POST, DELETE |
| CREAR | `frontend/pages/cost-calc/vendor-cif.html` | Tabla + modal add + modal delete confirm |
| MODIFICAR | `frontend/js/api.js` | Agregar endpoint `vendorCif` |
| MODIFICAR | Sidebar HTML | Agregar enlace "CIF Vendors" en grupo Cost Calc |

---

## Section 1: Database Migration — `85_VendorCifSubmodule.sql`

Registra el submodule en `LM_Submodules` para que aparezca en la pantalla de gestion de permisos.

**Submodule code:** `COST_VENDOR_CIF`
**Nombre visible:** `CIF Vendors`
**Modulo padre:** Cost Calc (ModuleCode = `'COST'`, igual que `COST_ITEM_WEIGHTS`, `COST_ALLOWED_MARGINS`)

**Estructura de LM_Submodules:** `(ModuleId INT FK, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)`

```sql
USE LicoresMaduoDB;
GO

DECLARE @CostId INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'COST');

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleCode = 'COST_VENDOR_CIF')
BEGIN
    INSERT INTO dbo.LM_Submodules (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (@CostId, 'CIF Vendors', 'COST_VENDOR_CIF', 'CC_VENDOR_CIF', 9);
END
GO

DECLARE @SmId INT = (SELECT SubmoduleId FROM dbo.LM_Submodules WHERE SubmoduleCode = 'COST_VENDOR_CIF');

MERGE dbo.LM_RolePermissions AS tgt
USING (VALUES
    (1, @SmId, 1,1,1,1,1,0),  -- SuperAdmin: full
    (2, @SmId, 1,1,1,1,1,0),  -- Admin: full
    (3, @SmId, 1,1,0,0,0,0)   -- User: read-only
) AS src (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete, CanApprove)
ON tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete, CanApprove)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead,
            src.CanWrite, src.CanEdit, src.CanDelete, src.CanApprove);
GO
```

**Matriz de permisos por defecto:**

| Rol | Access | Read | Write | Edit | Delete |
|-----|--------|------|-------|------|--------|
| SuperAdmin (1) | si | si | si | si | si |
| Admin (2) | si | si | si | si | si |
| User (3) | si | si | no | no | no |

Los defaults pueden sobreescribirse por usuario desde la UI de permisos (`LM_UserPermissions`).

---

## Section 2: API Controller — `VendorCifController.cs`

**Route:** `api/cost-calc/vendor-cif`
**Namespace:** `LicoresMaduro.API.Controllers.CostCalc`
**Dependencies:** `ApplicationDbContext`, `PermissionService`

### Endpoints

#### `GET /api/cost-calc/vendor-cif`
- Autorizacion: `[Authorize]` — cualquier usuario autenticado
- Devuelve lista ordenada de `VcifVendor` strings
- Respuesta: `ApiResponse<List<string>>`

#### `POST /api/cost-calc/vendor-cif`
- Autorizacion: `[Authorize]` + check `PermissionService.HasPermissionAsync(User, "COST_VENDOR_CIF", "WRITE")`
- Si no tiene permiso: `403 Forbidden`
- Body: `VendorCifDto { string VendorCode }`
- Validaciones:
  - VendorCode no puede ser vacio/null: `400 BadRequest`
  - Normaliza: `VendorCode.Trim().ToUpper()`
  - Si ya existe: `409 Conflict` con mensaje "Vendor code '{code}' already exists."
- Respuesta exito: `201 Created` con `ApiResponse<string>`

#### `DELETE /api/cost-calc/vendor-cif/{code}`
- Autorizacion: `[Authorize]` + check `PermissionService.HasPermissionAsync(User, "COST_VENDOR_CIF", "DELETE")`
- Si no tiene permiso: `403 Forbidden`
- `code` se normaliza: `.Trim().ToUpper()`
- Si no existe: `404 NotFound`
- Respuesta exito: `200 OK` con `ApiResponse.Ok("Deleted.")`

### DTO

```csharp
public record VendorCifDto(string VendorCode);
```

---

## Section 3: Frontend Page — `vendor-cif.html`

Sigue el patron de `item-weights.html` y `load-types.html`.

### Permisos en frontend

Los permisos vienen en el JWT — NO se hace un fetch adicional. Se usan las funciones existentes de `auth.js`:

```javascript
const canWrite  = Auth.hasPermission('COST_VENDOR_CIF', 'CanWrite');
const canDelete = Auth.hasPermission('COST_VENDOR_CIF', 'CanDelete');
const canAccess = Auth.hasPermission('COST_VENDOR_CIF', 'CanAccess');
```

- Si `canAccess = false`: redirigir a pagina de acceso denegado
- Si `canWrite = false`: ocultar boton "+ Add Vendor"
- Si `canDelete = false`: ocultar columna Actions (o celda de delete por fila)

### Estructura

```
[Header]
  Titulo: "CIF Vendors"
  Subtitulo: "Vendors with Cost, Insurance & Freight price agreements"
  Boton "+ Add Vendor" (visible solo si canWrite = true)

[Tabla]
  Columnas: # | Vendor Code | Actions
  Actions: boton delete (icono fa-trash, rojo) — visible solo si canDelete = true
  Si la lista esta vacia: "No CIF vendors registered."

[Modal: Add Vendor]
  Titulo: "Add CIF Vendor"
  Input: "Vendor Code" (text, maxlength=20, auto-uppercase on input)
  Botones: Cancel | Save

[Modal: Confirm Delete]
  Titulo: "Confirm Delete"
  Mensaje: "Are you sure you want to remove vendor {code} from the CIF list?
            This will affect future cost calculations for this vendor."
  Botones: Cancel | Delete (btn-danger)
```

### Flujo de operaciones

**Cargar lista:** `GET /api/cost-calc/vendor-cif` al inicializar la pagina

**Agregar:**
1. Click "+ Add Vendor" abre modal Add
2. Usuario ingresa codigo, click Save
3. `POST /api/cost-calc/vendor-cif` con `{ VendorCode: input.value }`
4. Exito: cierra modal + recarga tabla + showToast("Vendor added.", "success")
5. Error 409: muestra inline "Vendor code already exists." (modal no se cierra)
6. Error 403: showToast("You don't have permission.", "danger")

**Eliminar:**
1. Click delete icon en fila abre modal Confirm Delete con el codigo
2. Click "Delete": `DELETE /api/cost-calc/vendor-cif/{code}`
3. Exito: cierra modal + recarga tabla + showToast("Vendor removed.", "success")
4. Error 403: showToast("You don't have permission.", "danger")

### `api.js` — nuevo endpoint a agregar

```javascript
vendorCif: {
    getAll: ()     => get('/api/cost-calc/vendor-cif'),
    create: (dto)  => post('/api/cost-calc/vendor-cif', dto),
    remove: (code) => del(`/api/cost-calc/vendor-cif/${encodeURIComponent(code)}`),
},
```

### Sidebar — enlace a agregar

Dentro del grupo Cost Calc, junto a Item Weights y Allowed Margins:

```html
<a class="sidebar-link" href="/pages/cost-calc/vendor-cif.html"
   data-permission="COST_VENDOR_CIF">
  <i class="fas fa-store-slash me-2"></i>CIF Vendors
</a>
```

---

## Section 4: Error Handling

| Escenario | Backend | Frontend |
|-----------|---------|----------|
| Vendor code vacio | 400 BadRequest | Validacion HTML5 `required` |
| Codigo duplicado | 409 Conflict | Mensaje inline en modal (no cierra) |
| No existe al eliminar | 404 NotFound | Toast "Not found" |
| Sin permiso Write/Delete | 403 Forbidden | Toast "No permission" |
| Error de red | — | Toast error generico |

---

## Out of Scope

- Edit/Update: la tabla solo tiene PK (vendor code) — no hay campos editables. Add + Delete es suficiente.
- Historial de cambios / audit log
- Paginacion (la lista de vendors CIF es pequeña, menos de 50 registros tipicamente)
- Pagina equivalente para CC_VENDOR_FREIGHT_WEIGHT (puede hacerse en el futuro con el mismo patron)
