-- ============================================================
-- 14_TrackingAddVendorBrand.sql
-- Adds TR_Vendor_Brand column to TRACKING_ORDERS
-- ============================================================

USE LicoresMaduoDB;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('TRACKING_ORDERS') AND name = 'TR_Vendor_Brand'
)
BEGIN
    ALTER TABLE dbo.TRACKING_ORDERS
        ADD TR_Vendor_Brand NVARCHAR(100) NULL;
    PRINT 'Column TR_Vendor_Brand added to TRACKING_ORDERS.';
END
ELSE
BEGIN
    PRINT 'Column TR_Vendor_Brand already exists.';
END
GO
