-- ============================================================
-- 32_AankoopbonClosedStatus.sql
-- Adds CLOSED status tracking columns to AB_ORDER_HEADERS
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AB_ORDER_HEADERS') AND name = 'AOH_Closed_By'
)
BEGIN
    ALTER TABLE dbo.AB_ORDER_HEADERS
    ADD AOH_Closed_By       INT          NULL,
        AOH_Closed_By_Name  NVARCHAR(100) NULL,
        AOH_Closed_At       DATETIME      NULL;
    PRINT 'CLOSED status columns added to AB_ORDER_HEADERS.';
END
ELSE
    PRINT 'CLOSED status columns already exist — skipped.';
