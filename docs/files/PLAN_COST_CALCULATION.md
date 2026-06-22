# Plan: Cost Calculation — Access vs Web (Gap Analysis)

Análisis realizado el 2026-05-24. Basado en ingeniería inversa del programa Access
`CostPrice.accdb` (usuario: test / contraseña: test).

---

## Lo que YA está implementado ✅

- Login / autenticación con roles
- Crear cálculo con POs
- Ejecutar cálculo (Run Calculation)
- FOB Price en ANG
- Flete oceánico distribuido por peso
- Inland freight, Transport, Unloading, Local Handling
- Duties, Econ.Surch, OB por ítem
- Seguro
- Status: Draft → Confirmed → Approved
- Ship Charges (THC, doc fees) distribuidos
- Reporte PDF final (FinalCostReport)
- Tasas de cambio (Currencies)
- Gestión de freight forwarders y quotes

---

## TAREAS PENDIENTES

### 🔴 BLOQUE 1 — Costos por Almacén (Core del sistema)

**T01 — Costo separado por almacén (11060 Duty Free / 11010 Duty Paid / 11020 Store)**
- El Access calcula 3 costos distintos por ítem: sin duties (Duty Free), con duties (Duty Paid), tiendas propias
- Actualmente solo tenemos un `CcpdFinalCost` sin distinción de almacén
- Requiere agregar campos `Cost_11060`, `Cost_11010`, `Cost_11020` al detalle

**T02 — Cantidad por almacén por ítem**
- `CCPD_Qty_11060`, `CCPD_Qty_11010`, `CCPD_Qty_11020` — cuántas cajas van a cada almacén
- En Access se toma del ERP (RANKER); en la versión web necesitamos un form de ingreso

**T03 — Items Bonded (Duty Free) — flag `CCPD_BONDED_Item`**
- Ítems que van 100% al almacén 11060 (sin pagar duties)
- Afecta directamente la fórmula de costo (no se incluyen Duties/Econ.Surch/OB)

---

### 🔴 BLOQUE 2 — Análisis de Precios (Price Analysis)

**T04 — Precios anteriores por ítem (historial del ERP)**
- Leer `RANKER_99T` con costos actuales (`COST01`→`COST10`) y márgenes (`COST99_11010`, `COST99_11060`, `COST99_11020`)
- Agregar al detalle: `Old_FOB`, `Old_Inland`, `Old_Freight`, `Old_Duties`, `Old_Econ_Surch`, `Old_OB`, `Old_Insurance`, `Old_Transport`, `Old_Unloading`

**T05 — Pantalla de Price Analysis (nuevo costo vs precio anterior)**
- Formulario que muestra: costo nuevo calculado, precio de venta anterior, nuevo precio sugerido, impacto en margen
- Toggle "Per Case" / "Per Bottle"
- Equivalente a `Frm_Price_Analysis_Calculation_Sel` + `Frm_Qry_Price_Analysis` del Access

**T06 — Cálculo de Nuevo Precio de Venta Sugerido**
- Fórmula: `New_Price_Case = Round( (Cost_Case × 100) / (100 - Margin_Perc), 2 )`
- Precio por botella = precio_caja / unidades_de_venta
- Separado por almacén (11060 precio Duty Free, 11010/11020 precio normal)

**T07 — Margen por ítem individual (`Item_Margin`)**
- Tabla de márgenes específicos por ítem que sobrescriben el margen de clase
- Pantalla de mantenimiento CRUD

**T08 — Selección de ítems para cambio de precio**
- Flag `CCPD_Price_Change_Sel` por ítem — el usuario decide cuáles cambian precio
- Flag `CCPD_Do_Not_Order_Sel` — artículos que no se ordenarán más

---

### 🔴 BLOQUE 3 — Flujo de Aprobación de Cambios de Precio

**T09 — Umbral configurable de cambio de precio**
- `COMP_PRICE_CHANGE_PERC = 2%` y `COMP_PRICE_CHANGE_AMNT = 2 ANG`
- Si el cambio supera alguno → requiere aprobación de Sales Manager
- Si está debajo → se aprueba automáticamente

**T10 — Pantalla de aprobación de precio (Sales Manager)**
- Sales Manager ve: ítem, precio anterior, precio nuevo, diferencia %, motivo
- Puede aprobar o rechazar con razón (`Cost_Reasons`)
- Equivalente a `Frm_Price_Change_Approval` del Access

**T11 — Razones de rechazo/no cambio (`Cost_Reasons`)**
- Tabla configurable: "No price change", "Market competition", etc.
- Pantalla CRUD de mantenimiento

**T12 — Número secuencial de Price Change Document**
- Auto-incremental por año: `PC-2026-001`, `PC-2026-002`, etc.
- Campos en config: `COMP_PRICE_CHANGE_YEAR`, `COMP_PRICE_CHANGE_SEQ`

---

### 🟠 BLOQUE 4 — Versiones del Cálculo (Pre / Normal / Final)

**T13 — Versión PRE (borrador antes de confirmar la orden)**
- Análisis de precios ANTES de colocar la orden de compra
- Flujo: Pre Price Analysis → Sales Manager → aprobación → colocar orden
- Tablas: `Cost_Calc_Pre`, `Cost_Calc_PO_Head_Pre`, `Cost_Calc_PO_Det_Pre`

**T14 — Versión FINAL (post-recepción de mercancía)**
- Re-cálculo con datos reales al llegar el contenedor (fecha real, costos definitivos)
- Genera archivos definitivos para el ERP
- Tablas: `Cost_Calc_Fin`, `Cost_Calc_PO_Head_Fin`, `Cost_Calc_PO_Det_Fin`

---

### 🟠 BLOQUE 5 — Cálculo de Accijns (Duties Aduaneros)

**T15 — Clasificación HandelsBenaming (categorías aduaneras)**
- Tabla que agrupa ítems por categoría aduanera neerlandesa/antillana
- Campos: código HandelsBenaming, descripción, HS Code, unidades/caja, mL/botella
- Los duties se calculan primero por categoría (totales litros + valor), luego se distribuyen al ítem
- Pantalla de mantenimiento

**T16 — Factor Litros diferenciado por tipo de producto**
- Beer: Factor = 1 (mL de oz × 29.5735)
- Wine: Factor = 1 (mL directo)
- Liquor: Factor = 1.2 (multiplicador para accijns)
- Tabla `Item_Liter_Factor` para sobreescribir por ítem
- Agregar `CCPD_Liter_Factor` y `CCPD_Tot_Liters` al cálculo

**T17 — Tasas de arancel por categoría (Tariff Rates)**
- `ntarrif01` a `ntarrif06` de goodsclassification:
  - T01: IVB (impuesto sobre volumen de bebidas)
  - T02: Recargo económico %
  - T03: Licencia por HL
  - T04: Accijns cerveza por litro
  - T05: Accijns cigarrillos
  - T06: Accijns destilados por litro

---

### 🟠 BLOQUE 6 — Mercancía Gratis (Free Goods)

**T18 — Campo `CCPD_Free_Goods` en detalle de ítem**
- Cajas gratis que vienen con el pedido
- Los costos se distribuyen sobre `OrdQty + Free_Goods`; el FOB total usa solo `OrdQty`

---

### 🟡 BLOQUE 7 — Precios por Tienda y Porcentajes

**T19 — Porcentajes de precio por punto de venta (`Item_Store_Percentage`)**
- Retail %, Norsa %, Alliance % por ítem
- Defaults en config: Retail=10%, Alliance=5%, Norsa=15%
- Pantalla CRUD de mantenimiento

**T20 — Precios alternativos (códigos 03/04/05/07/08/09/10/11)**
- Precios para múltiples canales de distribución
- Definir cuáles aplican a Licores Maduro

**T21 — Makutu Basiko**
- Ítems con precio especial reducido para el programa "Makutu Basiko"
- Tabla `Item_Makutu_Basiko` con ítems marcados
- Afecta el precio de venta final

---

### 🟡 BLOQUE 8 — Flags de Artículos

**T22 — Flag `CCPD_NEW_Item` (artículo nuevo sin precio anterior)**
- Se marca automáticamente si el ítem no tiene historial en el ERP
- En la UI se visualiza diferente (sin columnas de comparación)

**T23 — Flag `CCPD_Miniature`**
- Afecta el cálculo de litros y posiblemente el margen

**T24 — Peso por ítem (`Item_Weight`)**
- Si el proveedor no reporta peso, se toma de esta tabla maestra
- Tabla: `IW_ItemCode`, `IW_Wght_Kilo`
- Tabla `Vendor_Freight_Weight` — proveedores que SÍ reportan peso por ítem
- Pantalla CRUD de mantenimiento

---

### 🟡 BLOQUE 9 — MIS (Management Information System)

**T25 — MIS: Historial de componentes de costo por ítem**
- Ver evolución de cada componente de costo en el tiempo por ítem
- Comparar FOB, Freight, Duties, etc. entre múltiples importaciones

**T26 — MIS: Análisis por Freight Forwarder**
- Comparar costos, tiempos, contenedores por forwarder

**T27 — MIS: Análisis por Proveedor**
- Historial de importaciones, costos, evolución por proveedor

**T28 — Consulta histórica de cálculos (Inquiry Mode)**
- Ver cualquier cálculo pasado en modo solo lectura con todos sus detalles

---

### 🟡 BLOQUE 10 — Integración ERP / Exportación

**T29 — Generación de archivo de costos para ERP (Change Wizard)**
- Al aprobar el cálculo final → CSV/XLSX con `COST01`→`COST10` por ítem
- Formato para importar en VIP/RANKER

**T30 — Generación de archivo de cambios de precio para ERP**
- CSV/XLSX con nuevos precios de venta por ítem y canal
- Numeración: `PC-YYYY-NNN`

**T31 — Importación / sincronización de datos del ERP**
- poheader, poptrs, loadsheet, vendors, RANKER_99T, goodsclassification
- Mecanismo de sincronización con VIP/RANKER para datos de referencia

---

### 🟡 BLOQUE 11 — Configuración del Sistema

**T32 — Pantalla de parámetros de empresa (`Cost_Company`)**
- Tasa de seguro, costos fijos (transport, unloading, local handling por contenedor)
- Umbrales de cambio de precio
- Márgenes por defecto
- Correos para notificaciones
- OZ factor (29.5735), Liter multiplier (1.2)

**T33 — Almacén por defecto por ítem (`Item_Default_Warehouse`)**
- Algunos ítems siempre van a un almacén específico
- Pantalla CRUD de mantenimiento

---

### 🔵 BLOQUE 12 — Notificaciones / Emails

**T34 — Email automático al Sales Manager para aprobación de precio**
- Trigger: cambio de precio supera el umbral configurado
- Adjunta reporte PDF con análisis de precios

**T35 — Email a Brand Managers con cambios de precio aprobados**
- Al finalizar el proceso de aprobación

**T36 — Email a Finanzas con reporte final**
- "Licores Maduro Price Analysis Approval Finance"

---

## Resumen

| Prioridad | Tareas | Descripción |
|---|---|---|
| 🔴 Crítico | T01–T12 | Costos por almacén, Price Analysis, Aprobación de precios |
| 🟠 Alto | T13–T18 | Versiones Pre/Final, Accijns, Free Goods |
| 🟡 Medio | T19–T33 | Tiendas, Flags, MIS, ERP export, Configuración |
| 🔵 Bajo | T34–T36 | Emails automáticos |

**Total: 36 tareas**

---

## Fórmulas Clave del Access (Referencia)

### Costo Final por Almacén
```
Cost_11060 = FOB + Inland_Freight + Freight + Local_Handling + Insurance + Transport + Unloading
Cost_11010 = FOB + Inland_Freight + Freight + Local_Handling + Insurance + Transport + Unloading + Duties + Econ_Surch + OB
Cost_11020 = (igual que 11010)
```

### Distribución de Flete al PO (por peso)
```
CCPH_Freight = Round( (CC_Freight / CC_TotWeight * CCPH_Weight * CC_CurrRate), 2 )
```

### Distribución de Componentes al Ítem
```
CCPD_Freight = (CCPD_Weight / CCPH_TotWeight * CCPH_Freight) / (OrdQty + FreeGoods)
```

### FOB Price
```
CCPD_FOB_Price = CCPD_Cost * CCPH_CurrRate
```

### Total Litros (para Accijns)
```
CCPD_Tot_Liters = Round( (OrdQty + FreeGoods) * UnitCase * LiterS * Liter_Factor, 2 )
```

### Nuevo Precio de Venta
```
New_Price_Case = Round( (Cost_Case * 100) / (100 - Margin_Perc), 2 )
New_Price_Btl  = New_Price_Case / Selling_Units
```

### Seguro
```
CCPD_Insurance = (FOB + Inland + Freight + Local + Duties + Econ_Surch + OB) * 1.1 * 0.005 * 1.07
```
*(COMP_INSURANCE = 0.00871)*
