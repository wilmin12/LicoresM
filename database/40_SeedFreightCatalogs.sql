-- ============================================================
-- 40 – Seed freight catalogs from Access source
--      AMOUNT_TYPE + INLAND_FREIGHT_CHARGE_TYPES
-- ============================================================

SET NOCOUNT ON;
GO

-- ── 1. AMOUNT_TYPE ────────────────────────────────────────
MERGE AMOUNT_TYPE AS tgt
USING (VALUES
    ('T', 'Total Amount'),
    ('U', 'Unit Price')
) AS src (AT_CODE, AT_DESCRIPTION)
ON tgt.AT_CODE = src.AT_CODE
WHEN NOT MATCHED BY TARGET THEN
    INSERT (AT_CODE, AT_DESCRIPTION, IsActive, CreatedAt)
    VALUES (src.AT_CODE, src.AT_DESCRIPTION, 1, GETUTCDATE())
WHEN MATCHED THEN
    UPDATE SET AT_DESCRIPTION = src.AT_DESCRIPTION;

PRINT CONCAT('AMOUNT_TYPE seed complete. Rows affected: ', @@ROWCOUNT);
GO

-- ── 2. INLAND_FREIGHT_CHARGE_TYPES ───────────────────────
MERGE INLAND_FREIGHT_CHARGE_TYPES AS tgt
USING (VALUES
    ('BLISS',  'Bill of Lading Issuing'),
    ('CONSFE', 'Consolidation Fee'),
    ('CONSXT', 'Consolidation Extra Chg'),
    ('DTHC',   'Dest. Local Charges'),
    ('EXPCUS', 'Export Custom Inspect.'),
    ('FUEL',   'Fuel Charges'),
    ('IFC',    'Inland Freight Charges'),
    ('THC',    'Origin THC')
) AS src (IFCT_CODE, IFCT_DESCRIPTION)
ON tgt.IFCT_CODE = src.IFCT_CODE
WHEN NOT MATCHED BY TARGET THEN
    INSERT (IFCT_CODE, IFCT_DESCRIPTION, IsActive, CreatedAt)
    VALUES (src.IFCT_CODE, src.IFCT_DESCRIPTION, 1, GETUTCDATE())
WHEN MATCHED THEN
    UPDATE SET IFCT_DESCRIPTION = src.IFCT_DESCRIPTION;

PRINT CONCAT('INLAND_FREIGHT_CHARGE_TYPES seed complete. Rows affected: ', @@ROWCOUNT);
GO
