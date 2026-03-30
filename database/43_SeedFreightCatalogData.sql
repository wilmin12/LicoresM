-- ============================================================
-- 43 – Seed freight catalog tables from Excel source files
--      CHARGES_PER · CHARGE_ACTION · CHARGE_OVER
--      CURRENCIES  · ROUTES_BY_SHIPPING_AGENTS
-- ============================================================

SET NOCOUNT ON;

-- ── 1. CHARGE_PER ─────────────────────────────────────────────
MERGE CHARGE_PER AS tgt
USING (VALUES
    ('C',     'Per Container'),
    ('CR',    'Per Carton'),
    ('O',     'Per Order')
) AS src (CP_CODE, CP_DESCRIPTION)
ON tgt.CP_CODE = src.CP_CODE
WHEN NOT MATCHED BY TARGET THEN
    INSERT (CP_CODE, CP_DESCRIPTION, IsActive, CreatedAt)
    VALUES (src.CP_CODE, src.CP_DESCRIPTION, 1, GETUTCDATE())
WHEN MATCHED THEN
    UPDATE SET CP_DESCRIPTION = src.CP_DESCRIPTION;

PRINT CONCAT('CHARGE_PER seed complete. Rows affected: ', @@ROWCOUNT);
GO

-- ── 2. CHARGE_ACTION ──────────────────────────────────────────
MERGE CHARGE_ACTION AS tgt
USING (VALUES
    ('-', 'Subtract'),
    ('%', 'Percentage'),
    ('+', 'Add'),
    ('X', 'Multiply')
) AS src (CA_CODE, CA_DESCRIPTION)
ON tgt.CA_CODE = src.CA_CODE
WHEN NOT MATCHED BY TARGET THEN
    INSERT (CA_CODE, CA_DESCRIPTION, IsActive, CreatedAt)
    VALUES (src.CA_CODE, src.CA_DESCRIPTION, 1, GETUTCDATE())
WHEN MATCHED THEN
    UPDATE SET CA_DESCRIPTION = src.CA_DESCRIPTION;

PRINT CONCAT('CHARGE_ACTION seed complete. Rows affected: ', @@ROWCOUNT);
GO

-- ── 3. CHARGE_OVER ────────────────────────────────────────────
MERGE CHARGE_OVER AS tgt
USING (VALUES
    ('CASES',  'Cases'),
    ('CFR',    'Cost and Freight'),
    ('CONSF',  'Consolidation Fee'),
    ('M3',     'Cubic Meters'),
    ('PURORD', 'Purchase Orders')
) AS src (CO_CODE, CO_DESCRIPTION)
ON tgt.CO_CODE = src.CO_CODE
WHEN NOT MATCHED BY TARGET THEN
    INSERT (CO_CODE, CO_DESCRIPTION, IsActive, CreatedAt)
    VALUES (src.CO_CODE, src.CO_DESCRIPTION, 1, GETUTCDATE())
WHEN MATCHED THEN
    UPDATE SET CO_DESCRIPTION = src.CO_DESCRIPTION;

PRINT CONCAT('CHARGE_OVER seed complete. Rows affected: ', @@ROWCOUNT);
GO

-- ── 4. CURRENCIES ─────────────────────────────────────────────
MERGE CURRENCIES AS tgt
USING (VALUES
    ('AWG', 'ARUBAN GUILDERS',    0.97,    1.0),
    ('CAD', 'CANADIAN DOLLAR',    NULL,    1.44),
    ('CNY', 'CHINA YUAN',         0.0,     0.21529),
    ('EUR', 'EURO',               2.0612,  2.0612),
    ('GBP', 'ENGELAND POUND',     2.76,    2.76),
    ('JPY', 'JAPAN YEN',          0.023093,0.023093),
    ('USD', 'US DOLLAR',          1.82,    1.82),
    ('XCG', 'CARIBBEAN GUILDERS', 1.0,     1.0)
) AS src (CUR_CODE, CUR_DESCRIPTION, CUR_BNK_PURCHASE_RATE, CUR_CUSTOMS_RATE)
ON tgt.CUR_CODE = src.CUR_CODE
WHEN NOT MATCHED BY TARGET THEN
    INSERT (CUR_CODE, CUR_DESCRIPTION, CUR_BNK_PURCHASE_RATE, CUR_CUSTOMS_RATE, IsActive, CreatedAt)
    VALUES (src.CUR_CODE, src.CUR_DESCRIPTION, src.CUR_BNK_PURCHASE_RATE, src.CUR_CUSTOMS_RATE, 1, GETUTCDATE())
WHEN MATCHED THEN
    UPDATE SET
        CUR_DESCRIPTION      = src.CUR_DESCRIPTION,
        CUR_BNK_PURCHASE_RATE = src.CUR_BNK_PURCHASE_RATE,
        CUR_CUSTOMS_RATE     = src.CUR_CUSTOMS_RATE;

PRINT CONCAT('CURRENCIES seed complete. Rows affected: ', @@ROWCOUNT);
GO

-- ── 5. ROUTES_BY_SHIPPING_AGENTS ──────────────────────────────
MERGE ROUTES_BY_SHIPPING_AGENTS AS tgt
USING (VALUES
    ('FRBAS',  'SSL HAPAG', 'CARTAGENA', 30),
    ('LIVORN', 'SSL HAPAG', 'KINGSTON',  28)
) AS src (RSA_PORT, RSA_SHIPPING_AGENT, RSA_ROUTE, RSA_DAYS)
ON  tgt.RSA_PORT           = src.RSA_PORT
AND tgt.RSA_SHIPPING_AGENT = src.RSA_SHIPPING_AGENT
AND tgt.RSA_ROUTE          = src.RSA_ROUTE
WHEN NOT MATCHED BY TARGET THEN
    INSERT (RSA_PORT, RSA_SHIPPING_AGENT, RSA_ROUTE, RSA_DAYS)
    VALUES (src.RSA_PORT, src.RSA_SHIPPING_AGENT, src.RSA_ROUTE, src.RSA_DAYS)
WHEN MATCHED THEN
    UPDATE SET RSA_DAYS = src.RSA_DAYS;

PRINT CONCAT('ROUTES_BY_SHIPPING_AGENTS seed complete. Rows affected: ', @@ROWCOUNT);
GO
