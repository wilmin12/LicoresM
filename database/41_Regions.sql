-- ============================================================
-- 41 – REGIONS catalog table (generic, not inland-only)
--      + expand FQER_REGION column to match REG_Code length
-- ============================================================

SET NOCOUNT ON;

-- ── 1. Create REGIONS table ───────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'REGIONS')
BEGIN
    CREATE TABLE dbo.REGIONS (
        REG_Id      INT           NOT NULL IDENTITY(1,1),
        REG_Code    NVARCHAR(50)  NOT NULL,
        REG_Name    NVARCHAR(50)  NOT NULL,
        REG_Country NVARCHAR(100) NULL,
        IsActive    BIT           NOT NULL DEFAULT 1,
        CreatedAt   DATETIME2     NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_REGIONS      PRIMARY KEY CLUSTERED (REG_Id),
        CONSTRAINT UQ_REGIONS_Code UNIQUE (REG_Code)
    );
    PRINT 'Created REGIONS table.';
END
GO

-- ── 2. Expand FQER_REGION column (was NVARCHAR(20)) ──────
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'FF_QUOTE_INL_REGION'
      AND COLUMN_NAME = 'FQER_REGION'
      AND CHARACTER_MAXIMUM_LENGTH < 50
)
BEGIN
    ALTER TABLE FF_QUOTE_INL_REGION
        ALTER COLUMN FQER_REGION NVARCHAR(50) NOT NULL;
    PRINT 'Expanded FQER_REGION to NVARCHAR(50).';
END
GO

-- ── 3. Register module permission (idempotent) ────────────
DECLARE @FreightId INT = (SELECT ModuleId FROM LM_Modules WHERE ModuleCode = 'FREIGHT');
IF @FreightId IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM LM_Submodules WHERE SubmoduleCode = 'FF_REGIONS')
BEGIN
    INSERT INTO LM_Submodules (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (@FreightId, 'Regions', 'FF_REGIONS', 'REGIONS', 19);
    PRINT 'FF_REGIONS sub-module registered.';
END
GO
