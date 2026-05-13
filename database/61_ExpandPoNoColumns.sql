-- ============================================================
-- Script  : 61_ExpandPoNoColumns.sql
-- Database: LicoresMaduoDB
-- Purpose : Expand CCPH_LMPoNo and CCPD_LMPoNo from NVARCHAR(10)
--           to NVARCHAR(15) to support PO numbers longer than 10 chars.
-- Run on  : LicoresMaduoDB
-- ============================================================

USE LicoresMaduoDB;
GO

-- Drop both FK constraints that reference these columns
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_COST_CALC_PO_DET_FIN_HEAD'
           AND parent_object_id = OBJECT_ID('dbo.COST_CALC_PO_DET_FIN'))
    ALTER TABLE dbo.COST_CALC_PO_DET_FIN DROP CONSTRAINT FK_COST_CALC_PO_DET_FIN_HEAD;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CCPD_PoHead'
           AND parent_object_id = OBJECT_ID('dbo.COST_CALC_PO_DET_FIN'))
    ALTER TABLE dbo.COST_CALC_PO_DET_FIN DROP CONSTRAINT FK_CCPD_PoHead;
GO

-- Drop PK on COST_CALC_PO_HEAD_FIN (PK includes CCPH_LMPoNo)
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_COST_CALC_PO_HEAD_FIN'
           AND parent_object_id = OBJECT_ID('dbo.COST_CALC_PO_HEAD_FIN'))
    ALTER TABLE dbo.COST_CALC_PO_HEAD_FIN DROP CONSTRAINT PK_COST_CALC_PO_HEAD_FIN;
GO

-- Drop PK on COST_CALC_PO_DET_FIN (PK includes CCPD_LMPoNo)
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_COST_CALC_PO_DET_FIN'
           AND parent_object_id = OBJECT_ID('dbo.COST_CALC_PO_DET_FIN'))
    ALTER TABLE dbo.COST_CALC_PO_DET_FIN DROP CONSTRAINT PK_COST_CALC_PO_DET_FIN;
GO

-- Expand columns
ALTER TABLE dbo.COST_CALC_PO_HEAD_FIN ALTER COLUMN CCPH_LMPoNo NVARCHAR(15) NOT NULL;
GO
ALTER TABLE dbo.COST_CALC_PO_DET_FIN  ALTER COLUMN CCPD_LMPoNo NVARCHAR(15) NOT NULL;
GO

-- Re-add PKs
ALTER TABLE dbo.COST_CALC_PO_HEAD_FIN
    ADD CONSTRAINT PK_COST_CALC_PO_HEAD_FIN PRIMARY KEY (CCPH_Calc_Number, CCPH_LMPoNo);
GO

ALTER TABLE dbo.COST_CALC_PO_DET_FIN
    ADD CONSTRAINT PK_COST_CALC_PO_DET_FIN PRIMARY KEY (CCPD_Calc_Number, CCPD_LMPoNo, CCPD_Item_No);
GO

-- Re-add FK
ALTER TABLE dbo.COST_CALC_PO_DET_FIN
    ADD CONSTRAINT FK_CCPD_PoHead
    FOREIGN KEY (CCPD_Calc_Number, CCPD_LMPoNo)
    REFERENCES dbo.COST_CALC_PO_HEAD_FIN (CCPH_Calc_Number, CCPH_LMPoNo);
GO

PRINT 'CCPH_LMPoNo and CCPD_LMPoNo expanded to NVARCHAR(15) successfully.';
GO
