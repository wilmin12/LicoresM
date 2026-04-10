-- ============================================================
-- Script 49 — Add REQ_EMAIL column to REQUESTORS table
-- Sesión: 2026-04-07  QA Remark: requestor email field
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.REQUESTORS') AND name = 'REQ_EMAIL'
)
BEGIN
    ALTER TABLE dbo.REQUESTORS
        ADD REQ_EMAIL NVARCHAR(100) NULL;

    PRINT 'Column REQ_EMAIL added to REQUESTORS.';
END
ELSE
BEGIN
    PRINT 'Column REQ_EMAIL already exists in REQUESTORS.';
END
GO
