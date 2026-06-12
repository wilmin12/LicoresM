# Observaciones Cliente CostCalc 2026-06-12 — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar las 13 observaciones accionables del cliente (PDF `File/CostClac_Comentarios_2026_06_12.pdf`) sobre Calculation Detail y Price Confirmation.

**Architecture:** Cambios frontend en `calculation.html` y `price-confirmation.html`; backend en `CostCalculationsController.cs` y `PriceConfirmationController.cs` para persistencia de estado de price-confirm y generación de Excel/PDF a carpeta configurable.

**Tech Stack:** HTML/JS vanilla + Bootstrap, .NET API (EF Core, SQL Server), ClosedXML (Excel), QuestPDF o HTML→PDF para reportes.

---

## Clasificación de observaciones

| # | Observación | Tipo | Fase |
|---|---|---|---|
| 1 | Quitar botón "Run Calculation" (Edit POs/Base Data ya cubre todo) | UI quick-win | A |
| 2 | Status "Draft" → mostrar "In Calculation" | UI quick-win | A |
| 3 | "â€"" en Warehouse/Forwarder/Ocean Freight/Created By (mojibake de em-dash) | Bug encoding | A |
| 4 | Quitar "+ Add Charge" | UI quick-win | A |
| 5 | Quitar header "Ship Charges (THC, etc.)" | UI quick-win | A |
| 6 | Quitar "No ship charges added yet" | UI quick-win | A |
| 7 | Diferencia entre Qty del PO header (14,568) y Total Qty del Grand Summary (902) | Bug/aclaración | A |
| 8 | Al Confirm: guardar Excel con costos calculados en una carpeta | Feature backend | C |
| 9 | Allowed %: campo extra para distinguir margen 11010 vs 11060 | UI/data | B |
| 10 | Chg Cost/New Price/Old Price/Chg Price/Chg Mrg/New Price(Case) por WH 11010 y 11060 | UI/data | B |
| 11 | Botón "Approve Price Changes" deshabilitado hasta llenar Price change / No price change en TODOS los ítems | Validación | B |
| 12 | Al salir y volver al módulo se pierden los cambios → persistir selección | Bug crítico | B |
| 13 | Al aprobar: generar PDFs (Final Cost Calculation, Approved Price Analysis, Final Price Change Report) + Excel para importar a VIP | Feature backend | C |
| 14 | **FUTURO** — 2 allowed margins (11010/11060), PR01/PR06 calculados con ellos, override manual < allowed → mail al manager + aprobación en módulo | No implementar aún | — |

---

## Fase A — Quick wins en `calculation.html`

### Task A1: Remover Run Calculation, Add Charge y bloque Ship Charges

**Files:** Modify: `frontend/pages/cost-calc/calculation.html`

- [ ] Quitar el botón "Run Calculation" (~línea 449) y el modal `#calcModal` (~línea 187) junto con `prefillModal()` y JS asociado.
- [ ] Quitar el card "Ship Charges" completo (~líneas 111-120), el botón `#btn-add-charge`, la fila "No ship charges added yet." (~línea 317) y las funciones JS de charges que queden huérfanas.
- [ ] Verificar que el cálculo se sigue ejecutando vía "Edit POs / Base Data" → confirmar flujo manual.
- [ ] Commit: `feat(calc-detail): remove Run Calculation and Ship Charges per client feedback Jun-12`

### Task A2: Status "Draft" → "In Calculation"

**Files:** Modify: `frontend/pages/cost-calc/calculation.html` (y `index.html` si muestra el mismo badge)

- [ ] Mapear etiqueta de status: cuando el código sea `DR`, mostrar "In Calculation" (solo display; el código DB no cambia).
- [ ] Revisar `frontend/pages/cost-calc/index.html` y `approve-calculations.html` por el mismo badge y aplicar igual mapeo.
- [ ] Commit: `feat(calc): display status DR as "In Calculation"`

### Task A3: Fix mojibake "â€"" en header

**Files:** Modify: `frontend/pages/cost-calc/calculation.html`

- [ ] Los campos Warehouse/Forwarder/Ocean Freight/Created By muestran `â€"` (em-dash UTF-8 mal codificado usado como placeholder). Reemplazar el literal por `—` correcto o `-`, y poblar los campos con valores reales (nombre del warehouse/forwarder en vez de quedar vacío) cuando existan en la respuesta del API.
- [ ] Grep global por `â€` en frontend y corregir todas las ocurrencias.
- [ ] Commit: `fix(calc): replace mojibake placeholders and show real header values`

### Task A4: Aclarar/corregir los dos Qty

**Files:** Investigate/Modify: `frontend/pages/cost-calc/calculation.html` (líneas 525 y 730)

- [ ] Investigar: header PO usa `po.CcphTotQty` (probable botellas), Grand Summary usa `effQty(d)` (probable cajas/cases). Confirmar contra datos (#62: 14,568 vs 902).
- [ ] Unificar criterio: etiquetar claramente ("Qty (bottles)" vs "Total Cases") o usar la misma unidad en ambos. Documentar la decisión en el commit.
- [ ] Commit: `fix(calc): clarify bottle vs case quantity labels`

---

## Fase B — Price Confirmation (`price-confirmation.html` + API)

### Task B1: Persistir estado del módulo (bug crítico #12)

**Files:**
- Modify: `frontend/pages/cost-calc/price-confirmation.html`
- Modify: `src/LicoresMaduro.API/Controllers/CostCalc/PriceConfirmationController.cs`
- Verify: tabla de detalle price-confirm (columnas flag PriceChange/NoPriceChange, NewPrice, NewMargin) — agregar migración `database/8X_...sql` si faltan columnas.

- [ ] Confirmar qué guarda hoy `saveL3()` y qué endpoint usa; identificar por qué al recargar se pierden flags Price change/No price change y valores editados.
- [ ] Backend: asegurar que el endpoint de save persiste por ítem: flag (1/0/null), new price, new margin, reason code, price change date.
- [ ] Frontend: al cargar la página, hidratar los radios/inputs desde la respuesta del API (no resetear a vacío).
- [ ] Agregar auto-save (al cambiar flag o al salir del Level 3 con "Save & Back") para que nada dependa solo del Approve final.
- [ ] Probar: marcar 2 ítems, salir del módulo, volver a entrar → los flags y precios siguen.
- [ ] Commit: `fix(price-confirm): persist per-item flags and edits across sessions`

### Task B2: Validación del botón Approve (#11)

**Files:** Modify: `frontend/pages/cost-calc/price-confirmation.html`; Modify: `PriceConfirmationController.cs` (validación servidor)

- [ ] Frontend: deshabilitar "APPROVE PRICE CHANGES AND SEND EMAIL" mientras exista algún ítem sin flag Price change/No price change. Recalcular en cada cambio. Tooltip: "Mark Price change / No price change on all items first".
- [ ] Backend: en `ApprovePo`, devolver 400 si algún detalle no tiene flag (defensa en profundidad).
- [ ] Commit: `feat(price-confirm): require all items flagged before approve`

### Task B3: Allowed % y campos por warehouse 11010/11060 (#9, #10)

**Files:**
- Modify: `frontend/pages/cost-calc/price-confirmation.html`
- Modify: `PriceConfirmationController.cs` + DTOs
- Posible migración: columnas Allowed%/Chg Cost/New-Old Price/Chg Price/Chg Mrg/New Price Case por WH.

- [ ] Hoy la grilla muestra una sola columna ALLOWED% y una fila de valores aunque WH lista 11010/11060. Replicar el patrón del Access (screenshot pág. 2): dos sub-filas por ítem, una por warehouse, cada una con su Allowed%, Actual%, New Cost, Old Cost, Chg Cost, New Price, Old Price, Chg Price, Chg Mrg, New Price(Case).
- [ ] Backend: exponer los valores por warehouse en el detalle (verificar si el servicio PriceCalc ya calcula por WH; si no, extender; ver memoria `project_price_confirm_empty_columns.md` — hay capas que no persisten).
- [ ] Nota: los **allowed margins separados** como dato maestro son el ítem FUTURO (#14); aquí solo se distingue visual/estructuralmente 11010 vs 11060 con los datos disponibles.
- [ ] Commit: `feat(price-confirm): per-warehouse (11010/11060) rows for allowed% and price fields`

---

## Fase C — Generación de archivos (Excel/PDF) (#8, #13)

> Requiere decidir con el usuario: carpeta destino (config en SystemConfig/appsettings, p.ej. `\\server\CostCalc\{calcNo}\`) y librería PDF. Hay ejemplos del cliente en `File/` (Final Cost Calculation, Approved Price Analysis, Final Price Change Report, Vendors_CIF.xlsx).

### Task C1: Excel de costos al Confirm (#8)

**Files:**
- Modify: `src/LicoresMaduro.API/Controllers/CostCalc/CostCalculationsController.cs` (endpoint Confirm)
- Create: `src/LicoresMaduro.API/Services/CostCalcExportService.cs`
- Modify: `src/LicoresMaduro.API/appsettings.json` (ruta carpeta export)

- [ ] Agregar paquete ClosedXML; crear servicio que genere un .xlsx con las líneas calculadas (mismas columnas de la grilla: Item, Desc, UC, UM, FC Price, FOB, fletes, duties, eco surch, OB tax, final cost...).
- [ ] Hook en Confirm: tras persistir, generar `CostCalc_{CalcNo}_{yyyyMMdd}.xlsx` en la carpeta configurada (fire-and-forget con log de error, no bloquear el confirm).
- [ ] Commit: `feat(cost-calc): export costs Excel on confirm`

### Task C2: Reportes PDF + Excel VIP al Approve (#13)

**Files:**
- Modify: `PriceConfirmationController.cs` (endpoint ApprovePo)
- Create: `src/LicoresMaduro.API/Services/PriceReportService.cs`

- [ ] Generar en la carpeta del calc, al aprobar: (a) Final Cost Calculation PDF, (b) Approved Price Analysis by Sales Manager PDF (marcando qué precios cambiaron), (c) Final Price Change Report PDF por producto cambiado, (d) Excel con cambios para importar a VIP (formato según attachment del cliente — pedir el layout exacto de Vendors_CIF.xlsx / VIP import).
- [ ] Reutilizar `buildReportHTML` del frontend como base del layout o renderizar server-side; decidir librería (QuestPDF recomendado).
- [ ] Responder al cliente: "Sale un reporte con los cambios de precio?" → sí, será el (c).
- [ ] Commit: `feat(price-confirm): generate approval PDFs and VIP import Excel`

---

## Fase D — Futuro (NO implementar ahora, #14)

Cuando los allowed margins de 11010 y 11060 estén listos:
- Dos allowed margins maestros (11010/11060); PR01 y PR06 calculados con esos márgenes.
- Override manual del margen; si queda por debajo del allowed → email al manager.
- Manager entra al módulo y aprueba el cambio para continuar (workflow de aprobación de excepción).

---

## Respuestas al cliente (no son código)

1. **Diferencia "Edit POs/Base Data" vs "Run Calculation":** el primero edita datos base y recalcula; el segundo solo dispara el recálculo. Se elimina "Run Calculation" por redundante (Task A1).
2. **Códigos en Warehouse/Forwarder/etc.:** era un placeholder mal codificado (`â€"` = guion largo corrupto), se corrige en Task A3.
3. **¿Sale un reporte con los cambios de precio?** Sí — Final Price Change Report (Task C2).

## Self-Review
- Cobertura: 14 observaciones mapeadas (13 accionables + 1 futura documentada en Fase D). ✔
- Pendientes de confirmación con usuario: carpeta destino de exports, layout exacto del Excel VIP, librería PDF.
