-- ============================================================
-- Script 31: Add Quotation PDF Path to AB_ORDER_HEADERS
-- ============================================================
USE LicoresMaduoDB;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AB_ORDER_HEADERS') AND name = 'AOH_Quotation_PDF_Path'
)
BEGIN
    ALTER TABLE dbo.AB_ORDER_HEADERS
        ADD AOH_Quotation_PDF_Path NVARCHAR(500) NULL;
    PRINT 'Column AOH_Quotation_PDF_Path added to AB_ORDER_HEADERS.';
END
ELSE
    PRINT 'Column AOH_Quotation_PDF_Path already exists.';
GO

PRINT 'Script 31 complete.';
