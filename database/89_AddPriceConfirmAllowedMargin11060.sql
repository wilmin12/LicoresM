USE LicoresMaduoDB;
GO

-- Separate allowed margin for warehouse 11060 (client feedback 2026-06-12).
-- CCPC_AllowedMargin keeps the 11010 value.
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'CC_PRICE_CONFIRMATION' AND COLUMN_NAME = 'CCPC_AllowedMargin11060')
BEGIN
    ALTER TABLE dbo.CC_PRICE_CONFIRMATION ADD CCPC_AllowedMargin11060 DECIMAL(18,4) NULL;
END
GO
