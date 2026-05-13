-- ============================================================
-- 66_AddSelectedLines_And_ExpandColumns.sql
-- 1. Add CCPH_SelectedLines to store specific item selections.
-- 2. Expand Warehouse and Vendor columns to avoid truncation.
-- ============================================================

USE LicoresMaduoDB;
GO

-- 1. Add column for item selection if not exists
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('COST_CALC_PO_HEAD_FIN') AND name = 'CCPH_SelectedLines')
BEGIN
    ALTER TABLE COST_CALC_PO_HEAD_FIN ADD CCPH_SelectedLines NVARCHAR(MAX) NULL;
END
GO

-- 2. Expand Warehouse columns (supporting 10101 and similar formats)
ALTER TABLE COST_CALC_FIN ALTER COLUMN CC_Warehouse NVARCHAR(10);
GO
ALTER TABLE COST_CALC_PO_HEAD_FIN ALTER COLUMN CCPH_WareHouse NVARCHAR(10);
GO
ALTER TABLE COST_CALC_PO_DET_FIN ALTER COLUMN CCPD_Warehouse NVARCHAR(10);
GO

-- 3. Expand Vendor columns
ALTER TABLE COST_CALC_FIN ALTER COLUMN CC_Forwarder_Name NVARCHAR(100);
GO
ALTER TABLE COST_CALC_PO_HEAD_FIN ALTER COLUMN CCPH_VendNo NVARCHAR(20);
GO
ALTER TABLE COST_CALC_PO_HEAD_FIN ALTER COLUMN CCPH_VendName NVARCHAR(100);
GO
