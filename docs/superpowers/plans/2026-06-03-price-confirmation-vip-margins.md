# Plan: Confirmación de Precios con Márgenes VIP (CostClac_Comentarios_2026_06_03A.pdf)

**Fecha:** 2026-06-03
**Fuente de requerimientos:** `File/CostClac_Comentarios_2026_06_03A.pdf` (3 páginas)
**Estado:** PENDIENTE DE APROBACIÓN — contiene 1 prerequisito de datos abierto (ver Phase 1)

---

## Phase 0 — Descubrimiento (COMPLETADO)

### Qué YA EXISTE (no re-implementar)

| Requerimiento del PDF | Estado | Evidencia |
|---|---|---|
| Botón de confirmación dentro del módulo | ✅ Existe | `CostCalculationsController.cs:838` — `PATCH {id}/confirm` (DR→CF) y `:884` Approve (CF→AP) |
| Email a la persona de "Maintenance Cost Calculation" | ✅ Existe | `CostCalculationsController.cs:918-921` — `SendManagerConfirmEmailAsync` usa `CompEmailConfirmMngr` |
| Guardar reporte PDF en "Cost Calculation Path" | ✅ Existe | `CostCalculationsController.cs:925,1217,1440` — `GenerateCostCalcPdfAsync` guarda en `CompPathCostCalc` |
| Store Percentages (Retail/Alliance/Norsa) | ✅ Existe | `SystemConfigController.cs:65-67` — `CompStoreRetailPerc`, `CompStoreAlliancePerc`, `CompStoreNorsaPerc` |
| Página de confirmación de precios | ⚠️ Versión básica | `frontend/pages/cost-calc/price-confirmation.html` — solo edita UN selling price por ítem; no muestra PR01–PR11 |
| Endpoint confirmar precios | ⚠️ Versión básica | `CostCalculationsController.cs:931` — `PATCH {id}/confirm-prices` recibe `List<ConfirmPriceItemDto>(PoNo, ItemNo, SellingPrice)` — un solo precio |
| Datos para lista de POs del manager (vendor, invoice, cases, amount, weight, inl. frght) | ✅ Existen | `ApplicationDbContext.cs:2140-2175` — `CcCalcPoHead` tiene `CcphInvNumber`, `CcphInvDate`, `CcphTotQty`, `CcphTotAmount`, `CcphWeight`, `CcphInlandFreight` |
| Costos viejos VIP (componentes) | ✅ Existen | `DhwDbContext.cs:317-330` — `DhwRanker952`: Cost01=FOB, Cost02=Inland Frt, Cost03=Ocean Frt, Cost04=Local Hdl, Cost05=Duties, Cost06=Eco Surch, Cost07=OB Tax, Cost08=Insurance, Cost09=Transport, Cost10=Unloading |

### Qué FALTA / HAY QUE CORREGIR

| # | Gap | Detalle |
|---|---|---|
| G1 | **Precios viejos VIP (PR01–PR11) sin fuente de datos** | No existe ninguna entidad/tabla con la lista de precios de VIP. `DhwItemT` (DhwDbContext.cs:508) no tiene precios; los scripts de `Datawarehouse/` tampoco. **PREREQUISITO ABIERTO** |
| G2 | **El selling price se calcula con AllowedMargins, NO con el margen VIP existente** | `CostCalculationsController.cs:667-677` usa `CcAllowedMargins` con fallback 0.3. El PDF exige calcular el margen viejo desde VIP y aplicarlo |
| G3 | **No existe matriz de precios PR01–PR11** | `CcCalcPoDetail` solo tiene `CcpdSellingPrice` (singular). Falta tabla para new/old price, new/old margin por cada lista de precios |
| G4 | **No existe estado de PO "pendiente de confirmación de precios"** | `CcphStatus` solo maneja DR/CF/AP. El manager debe ver SOLO los POs que tiene que confirmar |
| G5 | **No existe catálogo de Reason Codes para cambios de precio** | Screenshot pág. 2: "Reason Code: 01 — Product costs did not change". Solo existe `DenialReasons` para ActivityRequest (otro módulo) |
| G6 | **UI de manager incompleta** | Falta el flujo de 3 niveles: lista POs pendientes → productos del PO con análisis de precios → detalle por producto con CASE PRICES PR01–PR11, Price Change/No Price Change, edición bidireccional precio↔margen, reason code, fecha |
| G7 | **No hay email final tras aprobar cambios de precio** | Screenshot: botón "APPROVE PRICE CHANGES AND SEND EMAIL". Existe `CompEmailPriceChangesFinance` en config pero no se usa para este flujo |

### Reglas de negocio del PDF (especificación exacta)

**Costos por almacén (aplica a viejo VIP vía RANKER_952 y nuevo vía CcCalcPoDetail):**
- `Costo_11010` y `Costo_11020` = suma de TODOS los componentes de costo
- `Costo_11060` = suma de todos EXCEPTO Duties, Economic Surcharge y Tax OB
  - Viejo (Ranker952): Cost01+02+03+04+08+09+10 (excluye Cost05, Cost06, Cost07)
  - Nuevo (CcCalcPoDetail): cadena de costos excluyendo `CcpdDuties`, `CcpdEconSurch`, `CcpdOb`

**Margen viejo (margen sobre precio, redondeado a 2 decimales):**
```
Margin PRxx = Round( ((PrecioViejo_PRxx − CostoViejo) / PrecioViejo_PRxx) * 100, 2 )
```
- PR01 usa `Costo_11010` viejo
- PR06, PR07, PR08, PR09, PR11 usan `Costo_11060` viejo

**Precios nuevos (margen sobre precio ⇒ P = C / (1 − M/100)):**
- `PR01 = CostoNuevo_11010 / (1 − MarginPR01/100)`
- `PR06/07/08/09/11 = CostoNuevo_11060 / (1 − MarginPRxx/100)`
- `PR03 = PR01 * (1 + CompStoreNorsaPerc/100)` (STORE NORSA)
- `PR04 = PR01 * (1 + CompStoreRetailPerc/100)` (STORE RETAIL)
- `PR05 = PR01 * (1 + CompStoreAlliancePerc/100)` (STORE ALLIANCE)
- `PR10 = PR01 * 0.90` (descuento 10%)
- No existe PR02. Etiquetas (screenshot pág. 3): PR01 WHOLESALE, PR03 STORE NORSA, PR04 STORE RETAIL, PR05 STORE ALLIANCE, PR06 BONDED, PR07 SPECIAL BONDED, PR08 GWC_MANG_ESP, PR09 BONDED YU HUA, PR10 BBB DUTY PAID, PR11 BBB BONDED

**Flujo del manager:**
1. Al enviarse el email de confirmación (en Approve), los POs pasan a estado "pendiente de confirmación de precios" → el manager ve SOLO esos POs
2. Seleccionar PO → muestra productos con New Cost/Old Cost (11010 y 11060), Allowed vs Actual Margin%, New Price Calc, Old Price, Changes
3. Seleccionar producto → muestra TODOS los precios calculados PR01–PR11 (new price, new margin, old price, old margin)
4. Marcar "Price Change" o "No Price Change" por producto
5. Puede editar precio (recalcula margen) o margen (recalcula precio)
6. Seleccionar Reason Code + Price Change Date
7. "Approve Price Changes and Send Email" → guarda, cambia estado, envía email

### APIs/patrones permitidos (verificados en el código)

- EF Core con `ApplicationDbContext` (`_db`) + `DhwDbContext` (`_dhw`) — patrón dual ya usado en `GetById` (CostCalculationsController.cs:46)
- Side effects asíncronos: patrón fire-and-forget `_ = MethodAsync(..., CancellationToken.None)` con datos pre-cargados (DbContext no es thread-safe) — copiar de `Confirm` (línea 862)
- Emails: `LmEmailConfig` + patrón de `SendManagerConfirmEmailAsync` (línea 1057)
- Permisos: `PermissionService` con módulo `COST_CALCULATIONS` (acciones EDIT/APPROVE ya existen)
- Frontend: HTML + Bootstrap 5.3.2 + `js/api.js` (objeto `API.costCalc`) + `auth.js` + `sidebar.js`; tema vino `#6b2929`
- Migraciones: scripts SQL numerados en `database/` (último visto: `60_AddItemFobPricesSubmodule.sql`)

### Anti-patrones (NO hacer)

- NO inventar campos en `DhwRanker952`/`DhwItemT` — solo existen los listados arriba
- NO usar `CcAllowedMargins` para CALCULAR el precio nuevo (solo como referencia visual Allowed vs Actual)
- NO modificar el flujo DR→CF→AP existente; el nuevo estado es ADICIONAL y a nivel de PO
- NO llamar al DbContext dentro de tareas fire-and-forget sin pre-cargar datos
- NO eliminar `CcpdSellingPrice` ni el endpoint `confirm-prices` actual sin migrar la página que lo usa

---

## Phase 1 — Fuente de datos de precios viejos VIP (PREREQUISITO)

**Bloqueante para Phases 3-5. Requiere confirmación del cliente/DBA.**

### Qué implementar
1. **Confirmar con el cliente** la tabla/query de VIP que contiene la lista de precios PR01–PR11 por ítem (probablemente otro query RANKER del DHW_DATABASE, como RANKER_952 para costos). Datos necesarios: `Item`, `Pr01, Pr03, Pr04, Pr05, Pr06, Pr07, Pr08, Pr09, Pr10, Pr11` (precio por caja).
2. Agregar entidad `DhwItemPrices` en `DhwDbContext.cs` siguiendo EXACTAMENTE el patrón de `DhwRanker952` (líneas 317-330): clase POCO + `DbSet` + mapeo en `OnModelCreating`.
3. Agregar consulta de costos viejos: ya existe `Ranker952` — reusar.

### Verificación
- [ ] `dotnet build` sin errores
- [ ] Endpoint de prueba o test que lea 1 ítem conocido y devuelva sus 10 precios
- [ ] Grep: `DhwItemPrices` registrado en `DhwDbContext` con `ToTable`/`HasNoKey` igual que Ranker952

### Anti-pattern guards
- NO asumir nombres de columnas de VIP — esperar el nombre real de la tabla/vista
- Campos legacy pueden venir como varchar (gotcha conocido de RANKER_560) — validar tipos

---

## Phase 2 — Modelo de datos: matriz de precios, reason codes, estado PO

### Qué implementar
1. **Script SQL** `database/61_PriceConfirmation.sql`:
   - Tabla `CC_PRICE_CONFIRMATION`: CalcNumber, LmPoNo, ItemNo + por cada lista (01,03,04,05,06,07,08,09,10,11): NewPrice, NewMargin, OldPrice, OldMargin + NewCost11010, NewCost11060, OldCost11010, OldCost11060, PriceChangeFlag (bit), ReasonCode, PriceChangeDate, ApprovedBy, ApprovedAt
   - Tabla `CC_PRICE_CHANGE_REASONS`: ReasonCode (char 2), Description, Active — seed: `01 = Product costs did not change` (+ los que indique el cliente)
   - `CcphStatus` nuevo valor `'PC'` (Pending price Confirmation) — solo datos, sin cambio de schema
2. **Entidades EF** en `ApplicationDbContext.cs` junto a `CcCalcPoDetail` (línea 2178): `CcPriceConfirmation`, `CcPriceChangeReason`, siguiendo el naming `Ccpd*`/`Ccph*` existente.

### Documentación de referencia
- Patrón de entidad: `ApplicationDbContext.cs:2140-2175` (`CcCalcPoHead`)
- Patrón de script SQL: `database/59_CreateItemFobPrices.sql` y `60_AddItemFobPricesSubmodule.sql`

### Verificación
- [ ] Script SQL corre sin errores en BD de desarrollo
- [ ] `dotnet build` OK
- [ ] Grep: entidades registradas en `OnModelCreating`

---

## Phase 3 — Backend: motor de cálculo de precios + endpoints del manager

### Qué implementar
1. **`PriceCalculationService`** (nuevo, en `Services/`) con la especificación EXACTA de Phase 0 "Reglas de negocio":
   - `ComputeOldCosts(item)` desde `Ranker952` (11010/11020 = todos; 11060 = excluye Cost05/06/07)
   - `ComputeNewCosts(CcCalcPoDetail)` (11060 excluye `CcpdDuties`, `CcpdEconSurch`, `CcpdOb`)
   - `ComputeOldMargins(oldPrices, oldCosts)` — fórmula Round(((P−C)/P)*100, 2)
   - `ComputeNewPrices(newCosts, oldMargins, storePercentages)` — PR01/06/07/08/09/11 por margen; PR03/04/05 desde PR01 + %; PR10 = PR01*0.90
   - Guard: si PrecioViejo = 0 o margen ≥ 100 → precio nuevo = null y flag de advertencia (no dividir por cero)
2. **Modificar `Approve`** (`CostCalculationsController.cs:884`): tras pasar a AP, ejecutar el motor, persistir la matriz en `CC_PRICE_CONFIRMATION` y poner `CcphStatus='PC'` en los POs ANTES de enviar el email al manager (requerimiento: "al mandar el email, hay que cambiar el status").
3. **Endpoints nuevos** en `CostCalculationsController` (o controller dedicado `PriceConfirmationController`):
   - `GET pending-price-confirmations` → POs con `CcphStatus='PC'` (columnas del screenshot: PO No, Calc No, Vendor, Curr, Rate, Invoice Nr/Date, Cases, Amount, Weight, Inl. Frght — todas en `CcCalcPoHead`)
   - `GET price-confirmations/{calcId}/{poNo}` → productos con matriz completa
   - `POST price-confirmations/recalc` → dado precio nuevo recalcula margen, o dado margen recalcula precio (mismas fórmulas, server-side)
   - `POST price-confirmations/{calcId}/{poNo}/approve` → guarda flags/reason/fecha, `CcphStatus='PD'` (price done) o similar, email a `CompEmailPriceChangesFinance` (fire-and-forget con datos pre-cargados), actualiza `CcpdSellingPrice` con PR01 para compatibilidad
   - `GET price-change-reasons` → catálogo

### Documentación de referencia
- Patrón dual-context: `GetById` (CostCalculationsController.cs:46-70)
- Patrón email: `SendManagerConfirmEmailAsync` (línea 1057-1108)
- Patrón fire-and-forget seguro: `Confirm` (líneas 845-862)

### Verificación
- [ ] `dotnet build` OK
- [ ] Test manual con calc conocida: comparar márgenes/precios contra cálculo a mano de las fórmulas del PDF (ej. página 3: Old Price 618.00, Old Margin 42.70 con Cost 11010 = 354.11 → verificar Round(((618−354.11)/618)*100,2) = 42.70 ✓)
- [ ] El manager NO ve POs en estado DR/CF/AP/PD — solo PC

### Anti-pattern guards
- Margen es SOBRE PRECIO (P−C)/P, NO sobre costo — no usar markup
- Redondeo del margen a 2 decimales ANTES de usarlo para el precio nuevo (así lo define el PDF)

---

## Phase 4 — Frontend: UI del manager en 3 niveles

### Qué implementar
Rediseñar `frontend/pages/cost-calc/price-confirmation.html` (3 vistas en la misma página, patrón ya usado: lista→detalle):

1. **Nivel 1 — POs pendientes** (screenshot pág. 2 arriba): tabla con PO Number, Calc Number, Vendor, Vendor Name, Curr, Rate, Invoice Nr, Invoice Date, Cases, Amount, Act. Wght, Inl. Frght. Solo estado PC.
2. **Nivel 2 — Análisis de precios del PO** (screenshot pág. 2 abajo): por producto, filas 11010/11060 con FOB F/C-FOB ANG, Allowed Margin% vs Actual Margin%, New Cost, Old Cost, Change Cost, New Price Calc, Old Price, Change Price, Change MRG, New Price (Case). Botón "Show All Prices" por producto. Al pie: Reason Code (dropdown del catálogo), Price Change Date, botón "APPROVE PRICE CHANGES AND SEND EMAIL".
3. **Nivel 3 — Case Prices del producto** (screenshot pág. 3): columnas PR01 WHOLESALE … PR11 BBB BONDED; filas New Price (editable), New Margin (editable), Old Price, Old Margin. Checkboxes "PRICE CHANGE" / "NO PRICE CHANGE" (mutuamente excluyentes). Editar precio → recalcula margen y viceversa (vía endpoint recalc o JS con la MISMA fórmula).
4. Agregar métodos a `js/api.js` bajo `API.costCalc` siguiendo el patrón existente.
5. Mantenimiento de Reason Codes en `pages/settings/maintenance-costcalc.html` (sección nueva, patrón de las secciones existentes).

### Documentación de referencia
- Página actual: `frontend/pages/cost-calc/price-confirmation.html` (lista de calcs AP + tabla editable — reusar layout/estilos)
- Estilos: tema vino `#6b2929`, badges de estado DR=gris/CF=azul/AP=verde (agregar PC=warning/amarillo)
- API client: `frontend/js/api.js`

### Verificación
- [ ] Flujo completo en navegador: lista PC → seleccionar PO → productos → producto → editar precio/margen → reason → approve
- [ ] Al aprobar, el PO desaparece de la lista del manager
- [ ] Edición bidireccional: cambiar margen actualiza precio y viceversa, con redondeo a 2 decimales

---

## Phase 5 — Verificación final end-to-end

1. **Flujo completo:** Crear calc → Calculate → Confirm (CF) → Approve (AP) → verificar: PDF guardado en `CompPathCostCalc`, email a `CompEmailConfirmMngr`, POs en estado PC, matriz PR01–PR11 persistida.
2. **Manager:** login con rol manager → ve solo POs PC → aprueba cambios → email a `CompEmailPriceChangesFinance` → estado final.
3. **Validación numérica:** verificar contra los valores reales de los screenshots del PDF (Calc #420, orden 1069/25/RC001, ítem 202088: Old Cost 11010=354.11, 11060=136.66, PR01 Old=618.00, Margin=42.70).
4. **Anti-patrones:** grep de `marginPerc ?? 0.3` para confirmar que el flujo nuevo no depende del default; grep de fire-and-forget sin pre-carga.
5. **Regresión:** el flujo viejo de `confirm-prices` (selling price simple) sigue funcionando o fue migrado conscientemente.

---

## Preguntas abiertas para el cliente

1. **(G1 — bloqueante)** ¿Cuál es la tabla/query de VIP con los precios PR01–PR11 por ítem? ¿Se agrega al DHW_DATABASE como otro RANKER?
2. ¿Qué reason codes adicionales al `01 — Product costs did not change` deben existir?
3. Tras aprobar precios, ¿el email final va solo a `CompEmailPriceChangesFinance` o también a otros?
4. ¿Los precios PR son por CAJA (case) — como sugiere "CASE PRICES" del screenshot — y hay que mostrar también por botella ("Bottle" aparece en pág. 2)?
