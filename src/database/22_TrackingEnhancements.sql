-- ============================================================
-- 22_TrackingEnhancements.sql
-- Tracking module improvements:
--   #2  Actual Delivery Date (real arrival vs naviera ETA)
--   #6  Close / Lock tracking (closed records are read-only)
-- ============================================================

USE LicoresMaduoDB;
GO

-- #2 ── Actual Delivery Date ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TRACKING_ORDERS') AND name = 'TR_Actual_Delivery_Date')
    ALTER TABLE dbo.TRACKING_ORDERS ADD TR_Actual_Delivery_Date DATE NULL;
PRINT 'Column TR_Actual_Delivery_Date added (or already exists).';
GO

-- #6 ── Close / Lock columns ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TRACKING_ORDERS') AND name = 'TR_Is_Closed')
    ALTER TABLE dbo.TRACKING_ORDERS ADD TR_Is_Closed BIT NOT NULL DEFAULT 0;
PRINT 'Column TR_Is_Closed added (or already exists).';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TRACKING_ORDERS') AND name = 'TR_Closed_At')
    ALTER TABLE dbo.TRACKING_ORDERS ADD TR_Closed_At DATETIME NULL;
PRINT 'Column TR_Closed_At added (or already exists).';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TRACKING_ORDERS') AND name = 'TR_Closed_By')
    ALTER TABLE dbo.TRACKING_ORDERS ADD TR_Closed_By NVARCHAR(50) NULL;
PRINT 'Column TR_Closed_By added (or already exists).';
GO

PRINT 'Script 22_TrackingEnhancements.sql completed.';
GO
