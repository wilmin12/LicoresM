# Vendor CIF CRUD — Design Spec

**Date:** 2026-06-02  
**Feature:** Página de administración para la tabla CC_VENDOR_CIF  
**Module:** Cost Calc  

---

## Goal

Crear una página dentro del módulo Cost Calc que permita ver, agregar y eliminar los vendors que tienen acuerdo de precio CIF (Cost, Insurance & Freight). El acceso está controlado por el sistema de permisos existente, por lo que el administrador puede asignar quién tiene Write/Delete desde la UI de permisos.

---

## Architecture

Tres capas siguiendo los patrones del proyecto:

1. **SQL migration** — registra el submodule `CC_VENDOR_CIF` en `LM_Submodules` y siembra permisos por rol por defecto
2. **API Controller** — `VendorCifController.cs` con GET/POST/DELETE, usando `PermissionService` para Write y Delete
3. **Frontend page** — `vendor-cif.html` con tabla + modal de agregar + modal de confirmación de delete

No se necesita modelo nuevo: `CcVendorCif` ya existe en `ApplicationDbContext.cs` (línea 167) con `DbSet<CcVendorCif> CcVendorCifs`.

---

## Files

| Acción | Archivo | Responsabilidad |
|--------|---------|-----------------|
| CREAR | `database/85_VendorCifSubmodule.sql` | Registrar submodule + sembrar permisos por rol |
| CREAR | `src/LicoresMaduro.API/Controllers/CostCalc/VendorCifController.cs` | GET, POST, DELETE |
| CREAR | `frontend/pages/cost-calc/vendor-cif.html` | Tabla + modal add + modal delete confirm |
| MODIFICAR | `frontend/js/api.js` | Agregar endpoint `vendorCif` |
| MODIFICAR | Sidebar HTML | Agregar enlace "CIF Vendors" en grupo Cost Calc |

---

## Section 1: Database Migration — `85_VendorCifSubmodule.sql`

Registra el submodule en `LM_Submodules` para que aparezca en la pantalla de gestión de permisos:

```sql
-- Register submodule
IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleCode = 'CC_VENDOR_CIF')
BEGIN
    INSERT INTO dbo.LM_Submodules (SubmoduleCode, SubmoduleName, ModuleCode, IsActive)
    VALUES ('CC_VENDOR_CIF', 'CIF Vendors', 'COST_CALC', 1);
END
GO
```

Siembra permisos por defecto (idempotente con MERGE):

| Rol | Access | Read | Write | Edit | Delete | Approve |
|-----|--------|------|-------|------|--------|---------|
| SuperAdmin (1) | 1 | 1 | 1 | 1 | 1 | 1 |
| Admin (2) | 1 | 1 | 1 | 1 | 1 | 1 |
| User (3) | 1 | 1 | 0 | 0 | 0 | 0 |

Los defaults pueden sobreescribirse por usuario desde la UI de permisos (`LM_UserPermissions`).

---

## Section 2: API Controller — `VendorCifController.cs`

**Route:** `api/cost-calc/vendor-cif`  
**Namespace:** `LicoresMaduro.API.Controllers.CostCalc`  
**Dependencies:** `ApplicationDbContext`, `PermissionService`

### Endpoints

#### `GET /api/cost-calc/vendor-cif`
- Autorización: `[Authorize]` — cualquier usuario autenticado
- Devuelve lista ordenada de `VcifVendor` strings
- Respuesta: `ApiResponse<List<string>>`

#### `POST /api/cost-calc/vendor-cif`
- Autorización: `[Authorize]` + check `PermissionService.HasPermissionAsync(User, "CC_VENDOR_CIF", "WRITE")`
- Si no tiene permiso → `403 Forbidden`
- Body: `VendorCifDto { string VendorCode }`
- Validaciones:
  - VendorCode no puede ser vacío/null → `400 BadRequest`
  - Normaliza: `VendorCode.Trim().ToUpper()`
  - Si ya existe → `409 Conflict` con mensaje "Vendor code '{code}' already exists."
- Respuesta éxito: `201 Created` con `ApiResponse<string>`

#### `DELETE /api/cost-calc/vendor-cif/{code}`
- Autorización: `[Authorize]` + check `PermissionService.HasPermissionAsync(User, "CC_VENDOR_CIF", "DELETE")`
- Si no tiene permiso → `403 Forbidden`
- `code` se normaliza: `.Trim().ToUpper()`
- Si no existe → `404 NotFound`
- Respuesta éxito: `200 OK` con `ApiResponse.Ok("Deleted.")`

### DTO

```csharp
public record VendorCifDto(string VendorCode);
```

---

## Section 3: Frontend Page — `vendor-cif.html`

Sigue el patrón de `item-weights.html` y `load-types.html`.

### Estructura

```
[Header]
  Título: "CIF Vendors"
  Subtítulo: "Vendors with Cost, Insurance & Freight price agreements"
  Botón "+ Add Vendor" (visible solo si CanWrite = true)

[Tabla]
  Columnas: # | Vendor Code | Actions
  Actions: botón delete (ícono fa-trash, rojo) — visible solo si CanDelete = true
  Si la lista está vacía: mensaje "No CIF vendors registered."

[Modal: Add Vendor]
  Título: "Add CIF Vendor"
  Input: "Vendor Code" (text, maxlength=20, auto-uppercase)
  Botones: Cancel | Save

[Modal: Confirm Delete]
  Título: "Confirm Delete"
  Mensaje: "Are you sure you want to remove vendor {code} from the CIF list?
            This will affect future cost calculations for this vendor."
  Botones: Cancel | Delete (btn-danger)
```

### Lógica de permisos en frontend

Al cargar la página:
1. Llama `GET /api/auth/my-permissions?submodule=CC_VENDOR_CIF`
2. Si `CanWrite = false` → oculta botón "+ Add Vendor"
3. Si `CanDelete = false` → oculta columna Actions (o la celda de delete por fila)
4. Si `CanAccess = false` → redirige a `/unauthorized`

### Flujo de operaciones

**Cargar lista:** `GET /api/cost-calc/vendor-cif` → renderiza tabla

**Agregar:**
1. Click "+ Add Vendor" → abre modal Add
2. Usuario ingresa código → click Save
3. `POST /api/cost-calc/vendor-cif` con `{VendorCode: input.value.toUpperCase().trim()}`
4. Éxito → cierra modal + recarga tabla + toast "Vendor added."
5. Error 409 → muestra inline "Vendor code already exists." (no cierra modal)
6. Error 403 → toast "You don't have permission."

**Eliminar:**
1. Click delete icon en fila → abre modal Confirm Delete con el código
2. Click "Delete" → `DELETE /api/cost-calc/vendor-cif/{code}`
3. Éxito → cierra modal + recarga tabla + toast "Vendor removed."
4. Error 403 → toast "You don't have permission."

### `api.js` — nuevo endpoint

```javascript
vendorCif: {
    getAll:  ()     => apiFetch('cost-calc/vendor-cif'),
    create:  (dto)  => apiFetch('cost-calc/vendor-cif', { method: 'POST', body: JSON.stringify(dto) }),
    remove:  (code) => apiFetch(`cost-calc/vendor-cif/${encodeURIComponent(code)}`, { method: 'DELETE' }),
}
```

### Sidebar

Agregar enlace dentro del grupo Cost Calc en el sidebar HTML (junto a Item Weights, Allowed Margins, etc.):
```html
<a class="sidebar-link" href="/pages/cost-calc/vendor-cif.html"
   data-submodule="CC_VENDOR_CIF">
  <i class="fas fa-store-slash me-2"></i>CIF Vendors
</a>
```
El sidebar ya oculta automáticamente los links si `CanAccess = false`.

---

## Error Handling

| Scenario | Backend | Frontend |
|----------|---------|----------|
| Vendor code vacío | 400 BadRequest | Validación HTML5 `required` |
| Código duplicado | 409 Conflict | Mensaje inline en modal |
| No existe al eliminar | 404 NotFound | Toast "Not found" |
| Sin permiso Write/Delete | 403 Forbidden | Toast "No permission" |
| Error de red | — | Toast genérico de error |

---

## Out of Scope

- Edit/Update: la tabla solo tiene PK (vendor code) — no hay campos editables. Add + Delete es suficiente.
- Historial de cambios / audit log
- Paginación (la lista de vendors CIF es pequeña, < 50 registros típicamente)
- Página equivalente para CC_VENDOR_FREIGHT_WEIGHT (puede hacerse en el futuro con el mismo patrón)
