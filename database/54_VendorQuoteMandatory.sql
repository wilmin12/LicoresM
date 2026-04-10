-- ============================================================
-- Script 54: Add Quote Mandatory flag to VENDORS table
-- Date: 2026-04-09
-- ============================================================

ALTER TABLE VENDORS
    ADD VND_Quote_Mandatory BIT NOT NULL DEFAULT 0;

GO

PRINT 'Script 54 executed: VND_Quote_Mandatory added to VENDORS.';
