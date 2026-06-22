USE LicoresMaduoDB;
GO

-- Drop old constraint (only allowed DR, CF, AP — missing PC and PD)
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_CCPH_Status'
      AND parent_object_id = OBJECT_ID('dbo.COST_CALC_PO_HEAD_FIN')
)
    ALTER TABLE dbo.COST_CALC_PO_HEAD_FIN DROP CONSTRAINT CK_CCPH_Status;
GO

-- Recreate with full lifecycle: DR=Draft, CF=Confirmed, AP=Approved, PC=Price Confirm pending, PD=Price Determined
ALTER TABLE dbo.COST_CALC_PO_HEAD_FIN ADD CONSTRAINT CK_CCPH_Status
    CHECK (CCPH_Status IN ('DR','CF','AP','PC','PD'));
GO
