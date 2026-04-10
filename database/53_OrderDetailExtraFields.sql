-- ============================================================
-- Script 53: Add Additional Description and Cost Type
--            to AB_ORDER_DETAILS
-- Date: 2026-04-09
-- ============================================================

ALTER TABLE AB_ORDER_DETAILS
    ADD AOD_Additional_Desc NVARCHAR(255) NULL,
        AOD_Cost_Type       NVARCHAR(50)  NULL;

GO

PRINT 'Script 53 executed: AOD_Additional_Desc and AOD_Cost_Type added to AB_ORDER_DETAILS.';
