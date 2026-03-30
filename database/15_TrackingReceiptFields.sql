-- ============================================================
-- 15_TrackingReceiptFields.sql
-- Adds Goods Receipt Validation fields to TRACKING_ORDERS
-- ============================================================

USE LicoresMaduoDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TRACKING_ORDERS') AND name = 'TR_Receipt_Status')
    ALTER TABLE dbo.TRACKING_ORDERS ADD TR_Receipt_Status NVARCHAR(10) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TRACKING_ORDERS') AND name = 'TR_Qty_Shortage')
    ALTER TABLE dbo.TRACKING_ORDERS ADD TR_Qty_Shortage DECIMAL(18,4) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TRACKING_ORDERS') AND name = 'TR_Qty_Damages')
    ALTER TABLE dbo.TRACKING_ORDERS ADD TR_Qty_Damages DECIMAL(18,4) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TRACKING_ORDERS') AND name = 'TR_Receipt_Comments')
    ALTER TABLE dbo.TRACKING_ORDERS ADD TR_Receipt_Comments NVARCHAR(500) NULL;

PRINT 'Receipt fields added to TRACKING_ORDERS.';
GO
