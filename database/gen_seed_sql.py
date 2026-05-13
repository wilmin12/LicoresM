"""
Genera los scripts SQL de seed para:
  58_SeedTariffItems.sql    -> CC_TARIFF_ITEMS    (LicoresMaduoDB) desde GS_2012.xlsx
  57_SeedGoodsClassification.sql -> CC_GOODS_CLASSIFICATION (LicoresMaduoDB) desde goodsclassification.xls

EJECUTAR 58 ANTES QUE 57 (FK dependency).
"""
import os, sys

BASE   = r"C:\Users\wilmi\OneDrive\Desktop\Proyecto Licores Maduro"
GC_XLS = os.path.join(BASE, "CostCalculation_Wilmin", "CostCalculation_Wilmin", "Downloads", "goodsclassification.xls")
GS_XLSX= os.path.join(BASE, "CostCalculation_Wilmin", "CostCalculation_Wilmin", "Downloads", "GS_2012.xlsx")
OUT_58 = os.path.join(BASE, "database", "58_SeedTariffItems.sql")
OUT_57 = os.path.join(BASE, "database", "57_SeedGoodsClassification.sql")

# ---------------------------------------------------------------------------
def esc(s):
    """Escapa una cadena para T-SQL."""
    if s is None or str(s).strip() == '':
        return 'NULL'
    return "'" + str(s).strip().replace("'", "''") + "'"

def to_rate(val):
    """Convierte porcentaje (5 → 0.05). Blank/None → 0."""
    if val is None or str(val).strip() == '':
        return 0.0
    try:
        return float(val) / 100.0
    except Exception:
        return 0.0

def safe_str_int(val):
    """Convierte float de xlrd a int string: 22030000.0 → '22030000'."""
    s = str(val).strip()
    if s.endswith('.0') and s[:-2].isdigit():
        return s[:-2]
    return s

# ---------------------------------------------------------------------------
# 58 — CC_TARIFF_ITEMS desde GS_2012.xlsx
# Col 0: GOEDERENCODE (6-char HS base)
# Col 1: TARIEF PRECISIE 1 (2-char sub)
# Col 5: GOEDEREN BESCHRIJVING
# Col 8: INVOERRECHTEN          → TI_Duty_Rate
# Col 12: ACCIJNS OP GEDISTILLEERD → TI_Econ_Rate
# Col 13: OB                    → TI_OB_Rate
# ---------------------------------------------------------------------------
print("Leyendo GS_2012.xlsx ...")
try:
    import openpyxl
except ImportError:
    print("ERROR: falta 'openpyxl'. Ejecuta: pip install openpyxl")
    sys.exit(1)

wb  = openpyxl.load_workbook(GS_XLSX, read_only=True, data_only=True)
ws  = wb.active
rows = list(ws.iter_rows(values_only=True))
wb.close()

header = rows[0]
seen_hs = set()
lines58 = []
lines58.append("-- ============================================================")
lines58.append("-- 58_SeedTariffItems.sql")
lines58.append("-- Seed CC_TARIFF_ITEMS desde GS_2012.xlsx (aranceles Suriname)")
lines58.append("-- Ejecutar ANTES de 57_SeedGoodsClassification.sql")
lines58.append("-- ============================================================")
lines58.append("USE LicoresMaduoDB;")
lines58.append("GO")
lines58.append("SET NOCOUNT ON;")
lines58.append("")

skipped = 0
for row in rows[1:]:
    code = str(row[0]).strip() if row[0] is not None else ''
    tp1  = str(row[1]).strip() if row[1] is not None else '00'
    # Quitar decimales: '00.0' → '00', '01' → '01'
    code = safe_str_int(code).zfill(6)
    tp1  = safe_str_int(tp1).zfill(2)

    hs = code + tp1   # 8 chars
    if not code or hs in seen_hs:
        skipped += 1
        continue
    seen_hs.add(hs)

    # Col 2=TAR_DSC, Col 5=TAR_T01(duty), Col 9=TAR_T06(econ/spirits), Col 10=TAR_T07(OB)
    descr = str(row[2]).strip()[:200] if row[2] is not None else ''
    duty  = to_rate(row[5])
    econ  = to_rate(row[9])
    ob    = to_rate(row[10])

    lines58.append(
        f"INSERT INTO CC_TARIFF_ITEMS (TI_HS_Code, TI_Description, TI_Duty_Rate, TI_Econ_Rate, TI_OB_Rate, IS_Active, Created_At)\n"
        f"SELECT {esc(hs)}, {esc(descr)}, {duty:.6f}, {econ:.6f}, {ob:.6f}, 1, GETDATE()\n"
        f"WHERE NOT EXISTS (SELECT 1 FROM CC_TARIFF_ITEMS WHERE TI_HS_Code = {esc(hs)});"
    )

with open(OUT_58, 'w', encoding='utf-8') as f:
    f.write('\n'.join(lines58))

inserted58 = len(seen_hs)
print(f"  {inserted58} rows unicos -> {OUT_58}  (saltados duplicados: {skipped})")

# ---------------------------------------------------------------------------
# 57 — CC_GOODS_CLASSIFICATION desde goodsclassification.xls
# Col 0: citemno  → GC_Item_Code
# Col 1: cdescrip → GC_Item_Descr
# Col 3: chtcode  → GC_HS_Code  (FK a TI_HS_Code)
# ---------------------------------------------------------------------------
print("Leyendo goodsclassification.xls ...")
try:
    import xlrd
except ImportError:
    print("ERROR: falta 'xlrd'. Ejecuta: pip install xlrd")
    sys.exit(1)

wb2 = xlrd.open_workbook(GC_XLS)
ws2 = wb2.sheet_by_index(0)

lines57 = []
lines57.append("-- ============================================================")
lines57.append("-- 57_SeedGoodsClassification.sql")
lines57.append("-- Seed CC_GOODS_CLASSIFICATION desde goodsclassification.xls")
lines57.append("-- Ejecutar DESPUES de 58_SeedTariffItems.sql")
lines57.append("-- ============================================================")
lines57.append("USE LicoresMaduoDB;")
lines57.append("GO")
lines57.append("SET NOCOUNT ON;")
lines57.append("")

skip57 = 0
inserted57 = 0
for i in range(1, ws2.nrows):
    row = ws2.row_values(i)
    item_code = safe_str_int(str(row[0]).strip()) if row[0] else ''
    item_descr= str(row[1]).strip()[:100]          if row[1] else ''
    hs_raw    = str(row[3]).strip()                if row[3] else ''

    if not item_code or not hs_raw:
        skip57 += 1
        continue

    hs_code = safe_str_int(hs_raw).zfill(8)

    lines57.append(
        f"INSERT INTO CC_GOODS_CLASSIFICATION (GC_Item_Code, GC_Item_Descr, GC_HS_Code, IS_Active, Created_At)\n"
        f"SELECT {esc(item_code)}, {esc(item_descr)}, {esc(hs_code)}, 1, GETDATE()\n"
        f"WHERE NOT EXISTS (SELECT 1 FROM CC_GOODS_CLASSIFICATION WHERE GC_Item_Code = {esc(item_code)})\n"
        f"  AND EXISTS     (SELECT 1 FROM CC_TARIFF_ITEMS WHERE TI_HS_Code = {esc(hs_code)});"
    )
    inserted57 += 1

with open(OUT_57, 'w', encoding='utf-8') as f:
    f.write('\n'.join(lines57))

print(f"  {inserted57} rows -> {OUT_57}  (saltados blancos: {skip57})")
print("Listo. Ejecuta 58 primero, luego 57.")
