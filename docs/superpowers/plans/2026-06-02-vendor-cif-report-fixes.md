# Vendor CIF Exception + Report Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Activar la excepción de seguro para proveedores CIF, cambiar "AWG" a "XCG" en el reporte imprimible, mostrar precios con 2 decimales y ampliar la columna de descripción.

**Architecture:** Los 4 cambios son independientes entre sí. El backend para CIF ya está implementado — solo falta poblar la tabla `CC_VENDOR_CIF` con los 6 proveedores. Los cambios de AWG/decimales/descripción son puramente en el HTML del frontend (`calculation.html`).

**Tech Stack:** SQL Server (migration script), HTML/JavaScript vanilla (`calculation.html`), A4 landscape print report (`buildReportHTML`).

---

## Archivos a modificar

| Acción | Archivo | Responsabilidad |
|--------|---------|-----------------|
| CREAR  | `database/84_PopulateVendorCif.sql` | Seed idempotente de CC_VENDOR_CIF con 6 vendors |
| EDITAR | `frontend/pages/cost-calc/calculation.html` | AWG→XCG (3 lugares), dec:4→dec:2 (2 cols), description width+wrap |
| EDITAR | `frontend/pages/cost-calc/FinalCostReport-preview.html` | AWG→XCG si aparece en ese archivo |

**Backend sin cambios:** `CostCalculationsController.cs` ya tiene `isCifVendor` y `insurance = 0` para esos vendors (líneas 351-355 y 628-660).

---

## Impactos por cambio

| Cambio | Impacto |
|--------|---------|
| CC_VENDOR_CIF seed | Recálculos futuros de BCP, BM, DWL, EG, LH, ROS → insurance = 0. Cálculos existentes en BD **no se recalculan automáticamente** — el usuario debe abrir y re-guardar. |
| AWG → XCG | Solo cosmético — etiquetas del reporte imprimible. Sin impacto en datos ni lógica. |
| Precios 2 dec. | Solo display. Los datos en BD siguen siendo DECIMAL(18,4). Algunos precios con más decimales aparecerán redondeados en el reporte. |
| Descripción más ancha | Item No. se reduce de 4.5% a 3%, Description sube de 10% a 15%, las 21 columnas de datos pasan de 4.07% a 3.9% cada una — diferencia mínima pero la descripción gana 50% más espacio. |

---

## Task 1: SQL seed — Poblar CC_VENDOR_CIF

**Files:**
- Create: `database/84_PopulateVendorCif.sql`

La tabla `CC_VENDOR_CIF` ya existe (migración 72). Solo falta insertar los 6 vendors del archivo `Vendors_CIF.xlsx`: **BCP, BM, DWL, EG, LH, ROS**.

- [ ] **Step 1: Crear el archivo de migración**

Crear `database/84_PopulateVendorCif.sql` con este contenido:

```sql
-- Populate CC_VENDOR_CIF with vendors who have CIF price agreements
-- CIF = Cost, Insurance and Freight — these vendors already include freight+insurance in their price
-- For CIF vendors, insurance is set to 0 in cost calculations
MERGE dbo.CC_VENDOR_CIF AS target
USING (VALUES
    ('BCP'),
    ('BM'),
    ('DWL'),
    ('EG'),
    ('LH'),
    ('ROS')
) AS source (VCIF_Vendor)
ON target.VCIF_Vendor = source.VCIF_Vendor
WHEN NOT MATCHED THEN
    INSERT (VCIF_Vendor) VALUES (source.VCIF_Vendor);
GO

-- Verify: should return 6 rows
SELECT VCIF_Vendor FROM dbo.CC_VENDOR_CIF ORDER BY VCIF_Vendor;
GO
```

- [ ] **Step 2: Ejecutar el script en la base de datos**

Abre SQL Server Management Studio (o el cliente que uses) y ejecuta `84_PopulateVendorCif.sql` contra la base de datos del proyecto. El `SELECT` final debe retornar exactamente 6 filas: BCP, BM, DWL, EG, LH, ROS.

- [ ] **Step 3: Verificar en la base de datos**

```sql
SELECT VCIF_Vendor FROM dbo.CC_VENDOR_CIF ORDER BY VCIF_Vendor;
-- Expected: BCP, BM, DWL, EG, LH, ROS (6 rows)
```

- [ ] **Step 4: Commit**

```bash
git add database/84_PopulateVendorCif.sql
git commit -m "feat(cost-calc): seed CC_VENDOR_CIF with 6 CIF vendors (BCP,BM,DWL,EG,LH,ROS)"
```

---

## Task 2: AWG → XCG en el reporte imprimible

**Files:**
- Modify: `frontend/pages/cost-calc/calculation.html` (líneas 904, 916, 923)
- Check+Modify: `frontend/pages/cost-calc/FinalCostReport-preview.html` (si contiene AWG)

El string "AWG" (Florin arubano) aparece 3 veces en `buildReportHTML` dentro de `calculation.html`. Debe cambiar a "XCG" (Florin de las Antillas Neerlandesas / Curaçao guilder).

- [ ] **Step 1: Cambiar las 3 ocurrencias en calculation.html**

Las líneas exactas a cambiar en `calculation.html`:

**Línea 904** — encabezado del reporte:
```
ANTES: Import Cost Report — AWG
DESPUÉS: Import Cost Report — XCG
```

**Línea 916** — título sección totales:
```
ANTES: Cost Components Totals in AWG
DESPUÉS: Cost Components Totals in XCG
```

**Línea 923** — título sección item detail:
```
ANTES: Item Detail — Cost per Case in AWG
DESPUÉS: Item Detail — Cost per Case in XCG
```

Usar replace_all=false y cambiar cada ocurrencia con su contexto exacto:

Para línea 904:
```
old: Import Cost Report — AWG
new: Import Cost Report — XCG
```

Para línea 916:
```
old: Cost Components Totals in AWG
new: Cost Components Totals in XCG
```

Para línea 923:
```
old: Item Detail — Cost per Case in AWG
new: Item Detail — Cost per Case in XCG
```

- [ ] **Step 2: Verificar FinalCostReport-preview.html**

Abrir `frontend/pages/cost-calc/FinalCostReport-preview.html` y buscar "AWG". Si existe, reemplazar por "XCG" con el mismo criterio.

- [ ] **Step 3: Verificar que no queden instancias de AWG en el reporte**

```bash
grep -n "AWG" frontend/pages/cost-calc/calculation.html
# Expected: 0 resultados (o solo en comentarios/código no relacionado con el reporte)
grep -n "AWG" frontend/pages/cost-calc/FinalCostReport-preview.html
# Expected: 0 resultados
```

- [ ] **Step 4: Commit**

```bash
git add frontend/pages/cost-calc/calculation.html frontend/pages/cost-calc/FinalCostReport-preview.html
git commit -m "fix(cost-calc-report): change currency label AWG to XCG in print report"
```

---

## Task 3: Precios con 2 decimales en el reporte

**Files:**
- Modify: `frontend/pages/cost-calc/calculation.html` (líneas 816-817 en `ITEM_COLS`, y líneas 457, 461 en la tabla de detalle)

Actualmente `U/P` y `F/C Price` en el reporte usan `dec:4`. El usuario pide 2 decimales.

- [ ] **Step 1: Cambiar dec en ITEM_COLS del reporte (línea 816)**

En `calculation.html`, alrededor de línea 816, la definición de `ITEM_COLS`:

```javascript
// ANTES (línea 816):
{key:'uc',label:'U/C',dec:0,g:'order'},{key:'up',label:'U/P',dec:4,g:'order'},{key:'ord',label:'Ord.',dec:0,g:'order'},

// DESPUÉS:
{key:'uc',label:'U/C',dec:0,g:'order'},{key:'up',label:'U/P',dec:2,g:'order'},{key:'ord',label:'Ord.',dec:0,g:'order'},
```

- [ ] **Step 2: Cambiar dec de F/C Price (línea 817)**

```javascript
// ANTES (línea 817):
{key:'fcPrice',label:'F/C Price',dec:4,g:'order'},{key:'totalInvP',label:'Total Inv/P',dec:2,g:'order'},

// DESPUÉS:
{key:'fcPrice',label:'F/C Price',dec:2,g:'order'},{key:'totalInvP',label:'Total Inv/P',dec:2,g:'order'},
```

- [ ] **Step 3: Cambiar decimales en la tabla de detalle principal (líneas 457, 461)**

También en la pantalla de detalle (fuera del reporte imprimible), los precios FOB usan 4 decimales:

```javascript
// ANTES (línea 457):
<td class="text-end">${fmt(d.CcpdFobPriceUsd,4)}</td>

// DESPUÉS:
<td class="text-end">${fmt(d.CcpdFobPriceUsd,2)}</td>
```

```javascript
// ANTES (línea 461):
<td class="text-end">${fmt(d.CcpdFobPrice,4)}</td>

// DESPUÉS:
<td class="text-end">${fmt(d.CcpdFobPrice,2)}</td>
```

> **Nota:** Si el usuario prefiere mantener 4 decimales en la pantalla de detalle (no en el reporte), se puede omitir el Step 3 y solo aplicar Steps 1-2.

- [ ] **Step 4: Verificar que no queden dec:4 en zonas de precios**

```bash
grep -n "dec:4" frontend/pages/cost-calc/calculation.html
# Expected: 0 resultados dentro de ITEM_COLS
```

- [ ] **Step 5: Commit**

```bash
git add frontend/pages/cost-calc/calculation.html
git commit -m "fix(cost-calc-report): display prices with 2 decimal places (U/P and F/C Price)"
```

---

## Task 4: Descripción — más espacio en el reporte

**Files:**
- Modify: `frontend/pages/cost-calc/calculation.html` (líneas 858 y 925)

Actualmente:
- Item No.: `4.5%`, Description: `10%`, 21 columnas de datos comparten `85.5%` (= 4.07% c/u)

Con el cambio:
- Item No.: `3%`, Description: `15%`, 21 columnas comparten `82%` (= 3.9% c/u)

Adicionalmente se elimina `white-space:nowrap` de la celda de descripción para que el texto pueda hacer wrap.

- [ ] **Step 1: Actualizar el colgroup (línea 925)**

```html
<!-- ANTES (línea 925): -->
<colgroup><col style="width:4.5%"/><col style="width:10%"/>${ITEM_COLS.map(function(){return '<col style="width:'+(85.5/ITEM_COLS.length)+'%"/>';}).join('')}</colgroup>

<!-- DESPUÉS: -->
<colgroup><col style="width:3%"/><col style="width:15%"/>${ITEM_COLS.map(function(){return '<col style="width:'+(82/ITEM_COLS.length)+'%"/>';}).join('')}</colgroup>
```

- [ ] **Step 2: Actualizar el estilo de la celda de descripción (línea 858)**

```html
<!-- ANTES (línea 858): -->
<td style="padding:3px 6px;font-size:8px;color:#334155;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;border:1px solid #d1d5db;" title="${esc(d.description)}">${esc(d.description)}</td>

<!-- DESPUÉS: -->
<td style="padding:3px 6px;font-size:8px;color:#334155;word-break:break-word;white-space:normal;border:1px solid #d1d5db;" title="${esc(d.description)}">${esc(d.description)}</td>
```

- [ ] **Step 3: Verificar visualmente en el navegador**

1. Abre `calculation.html` en el browser → navega a un cálculo con descripciones largas
2. Haz clic en "Print" / "Reporte Final"
3. Confirma que la columna Description muestra más texto y hace wrap si es necesario
4. Confirma que Item No. column sigue legible a 3% de ancho

- [ ] **Step 4: Commit**

```bash
git add frontend/pages/cost-calc/calculation.html
git commit -m "fix(cost-calc-report): widen description column (10%->15%) and enable text wrap"
```

---

## Notas adicionales post-implementación

### Cálculos existentes con vendors CIF

Después de ejecutar la migración 84, los cálculos **ya guardados** en la base de datos para BCP, BM, DWL, EG, LH, ROS todavía tienen el valor de `CCPD_Insurance` calculado con seguro. Para reflejar el valor correcto (insurance = 0):

1. El usuario debe abrir cada cálculo afectado en la pantalla de Cost Calculation
2. Hacer clic en "Create & Calculate" (o el botón de recalcular)
3. El sistema borrará los registros existentes y recalculará con insurance = 0

No hay migración automática de los datos históricos — el recálculo es manual.

### Management UI para CC_VENDOR_CIF (opcional, fuera de scope)

Si en el futuro se necesita agregar/quitar vendors CIF desde la UI web (sin ejecutar SQL), se puede crear una pantalla similar a la configuración de `CC_VENDOR_FREIGHT_WEIGHT`. No es parte de este plan.
