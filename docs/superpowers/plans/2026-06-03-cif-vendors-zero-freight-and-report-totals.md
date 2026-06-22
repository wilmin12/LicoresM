# Plan: Vendors CIF sin fletes/seguro + Totales del reporte (CostClac_Comentarios_2026_06_03.pdf)

**Fecha:** 2026-06-03
**Fuente de requerimientos:** `File/CostClac_Comentarios_2026_06_03.pdf` (1 página, 2 observaciones)
**Relación:** independiente del plan `2026-06-03-price-confirmation-vip-margins.md` (PDF 03A). Ambos tocan `CostCalculationsController.Calculate` — coordinar si se ejecutan en paralelo.

---

## Phase 0 — Descubrimiento (COMPLETADO)

### Observación 1 del PDF — Vendors CIF

> "Si el Vendor code in CIF_Vendors es igual que el vendor en el Purchase Order, NO debes calcular: Inland Freight, Ocean Freight, Insurance. Deben permanecer 0."

**Estado actual en `CostCalculationsController.cs` (método `Calculate`, línea 322+):**

| Componente | Estado | Evidencia |
|---|---|---|
| `isCifVendor` flag | ✅ Ya existe | Línea 455: `cifVendors.Contains(poVendor)` cargado de `CcVendorCifs` (línea 352) |
| Insurance = 0 para CIF | ✅ Ya implementado | Líneas 645-648: `if (isCifVendor) { insurance = 0; }` y línea 630 excluye CIF del header insurance |
| **Ocean Freight = 0 para CIF** | ❌ **FALTA** | Línea 527: `freight = poFreight * lineProp` — se calcula SIEMPRE (poFreight viene de línea 428: `calc.CcFreight * rate * poWeightProp`) |
| **Inland Freight = 0 para CIF** | ❌ **FALTA** | Línea 528: `inland = (poHead.CcphInlandFreight ?? 0) * rate * lineProp` — se calcula SIEMPRE |

**Efectos en cascada (verificados):**
- Inland Tariff (línea 550): base = `(netFobTot + inland + freight) * ItRate` → con inland/freight=0 la base queda solo netFobTot. Consecuencia correcta del requerimiento (el arancel se calcula sobre lo que efectivamente cuesta).
- Final cost (línea 663): suma `lc.Inland + lc.Freight` → quedarán 0 automáticamente.
- Header totals (línea 812): `poHead.CcphFreight = poFreight` → para PO CIF debe guardarse 0, no el valor distribuido.

**Decisión de diseño pendiente (pregunta al cliente):**
El ocean freight del contenedor se distribuye por peso entre los POs (`poWeightProp = pesoPO / totalWeight`, línea 427). Si un PO es CIF y su flete se fuerza a 0, ¿la porción del flete que le tocaba:
- (a) se pierde (el total del flete del calc no se distribuye completo), o
- (b) se redistribuye a los POs no-CIF (excluir el peso de POs CIF del denominador `totalWeight`)?

El PDF solo dice "deben permanecer 0" → **implementación inicial: opción (a), solo poner 0** (mínima, reversible). Confirmar (b) con el cliente.

### Observación 2 del PDF — Total "Total INV/P" sin espacio en la hoja

> "No hay espacio para el total de 'Total INV/P' debajo en la hoja. Quizás puedes poner un total arriba y el otro total abajo, después de nuevo arriba y abajo."

**Estado actual:** es el **Print Report** dentro de `frontend/pages/cost-calc/calculation.html`:
- Tabla "Item Detail — Cost per Case in XCG" (líneas 924-947): `table-layout:fixed`, fuente 8px, ~20 columnas
- Totales solo en `tfoot` (línea 942: `FTL('Totals') + itemFoot`), columnas muy angostas → los números largos del total (ej. Total Inv/P con miles) no caben
- La columna existe como `{key:'totalInvP', label:'Total Inv/P'}` (línea 817)
- Helpers de celdas: `FTD`/`FTL` (líneas 842-843), `THL`/`THR` (840-841)

**Solución pedida por el cliente:** alternar los totales — la mitad de las columnas muestran su total en una fila ARRIBA de la tabla (tras el thead) y la otra mitad ABAJO (tfoot), intercalado (arriba, abajo, arriba, abajo...), para que cada número tenga el doble de ancho disponible (puede invadir la celda vacía vecina).

### Anti-patrones (NO hacer)

- NO tocar la lógica de Insurance CIF existente (líneas 630, 645-648) — ya cumple
- NO cambiar la distribución de freight para POs no-CIF
- NO alterar el cálculo de Duties/Econ/OB — el PDF no los menciona
- NO romper la tabla "Cost Totals per PO" (línea 918-921) — la observación es sobre Item Detail

---

## Phase 1 — Backend: Inland/Ocean Freight = 0 para vendors CIF

### Qué implementar
En `CostCalculationsController.cs`, Pass 1 del `Calculate` (líneas 524-532):

```csharp
decimal freight = isCifVendor ? 0 : poFreight * lineProp;
decimal inland  = isCifVendor ? 0 : (poHead.CcphInlandFreight ?? 0) * (decimal)(poHead.CcphCurrRate ?? dto.CurrRate ?? 1) * lineProp;
```

Y al actualizar header totals (línea ~812): guardar `poHead.CcphFreight = isCifVendor ? 0 : poFreight;` (y revisar si `CcphInlandFreight` debe limpiarse o conservar el valor ingresado como referencia — conservarlo, ya que el detalle es quien manda; documentar con comentario).

### Documentación de referencia
- Patrón existente de la misma regla: Insurance CIF en líneas 645-648 (copiar el estilo `if (isCifVendor)`)
- Carga de `cifVendors`: línea 352

### Verificación
- [ ] `dotnet build` OK
- [ ] Calc de prueba con PO de vendor CIF (ej. LH — Hillebrand, marcado en el screenshot): `CcpdInlandFreight = 0`, `CcpdFreight = 0`, `CcpdInsurance = 0` en todos los detalles
- [ ] Mismo calc con vendor NO CIF: valores ≠ 0 (sin regresión)
- [ ] Inland Tariff del PO CIF calculado solo sobre netFobTot
- [ ] Final cost del PO CIF = FOB + LH + Duties + Econ + OB + InlandTariff + Transport + Unloading + ShipChg (sin fletes ni seguro)

---

## Phase 2 — Frontend: totales arriba/abajo en el Print Report

### Qué implementar
En `calculation.html`, tabla Item Detail del print report (líneas 924-947):

1. Construir DOS filas de totales a partir de `ITEM_COLS`/`itemFoot` (línea 873):
   - Fila superior (insertada como última fila del `thead` o primera del `tbody` con estilo de total): totales de las columnas en posición PAR
   - Fila inferior (`tfoot` actual): totales de las columnas en posición IMPAR
   - En cada fila, las celdas sin total quedan vacías → el número vecino puede usar `white-space:nowrap; overflow:visible` para extenderse
2. Mantener las filas existentes "Total Actual VIP" / "Total Real Cost" (líneas 944-945) en el tfoot, sin cambios.
3. Reusar helpers `FTD`/`FTL` — no introducir estilos nuevos fuera del patrón inline existente.

### Verificación
- [ ] Print Report en navegador (botón "Print Report" del calc #64 u otro con varios ítems): el total de Total Inv/P se lee completo, sin recorte
- [ ] Vista previa de impresión (Ctrl+P) — la hoja no se desborda
- [ ] Las filas VIP/Real Cost siguen intactas

---

## Phase 3 — Verificación final

1. Calc end-to-end con mezcla de POs (uno CIF, uno normal) → revisar pantalla + Print Report + PDF generado en `CompPathCostCalc` (el PDF del backend usa los mismos detalles, heredará los 0 automáticamente).
2. Grep de regresión: `isCifVendor` debe aparecer ahora en 4 sitios (flag, header insurance, insurance per-line, freight/inland).
3. Confirmar con el cliente la pregunta abierta de redistribución del ocean freight (opción a vs b).

---

## Preguntas abiertas para el cliente

1. Cuando un PO es CIF, ¿su porción del ocean freight del contenedor se redistribuye a los demás POs del cálculo, o simplemente no se aplica? (implementamos "no se aplica"; cambiar a redistribución es 1 línea: excluir peso CIF de `totalWeight`)
2. ¿El valor de Inland Freight ingresado en el header del PO CIF debe bloquearse/ocultarse en la pantalla de captura (new-calculation.html), o solo ignorarse en el cálculo?
