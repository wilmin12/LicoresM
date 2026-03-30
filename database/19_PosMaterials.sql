-- ============================================================
-- 19_PosMaterials.sql
-- Table: POS_MATERIALS (inventory catalog)
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── POS_MATERIALS ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'POS_MATERIALS')
BEGIN
    CREATE TABLE dbo.POS_MATERIALS (
        PM_Id               INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PM_Code             NVARCHAR(20)   NOT NULL,
        PM_Name             NVARCHAR(150)  NOT NULL,
        PM_Category_Code    NVARCHAR(20)   NULL,
        PM_Category_Desc    NVARCHAR(100)  NULL,
        PM_Description      NVARCHAR(300)  NULL,
        PM_Unit             NVARCHAR(20)   NULL,
        PM_Stock_Total      INT            NOT NULL DEFAULT 0,
        PM_Stock_Available  INT            NOT NULL DEFAULT 0,
        PM_Notes            NVARCHAR(300)  NULL,
        IS_Active           BIT            NOT NULL DEFAULT 1,
        Created_At          DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_POS_MATERIALS_Code UNIQUE (PM_Code)
    );
    PRINT 'Table POS_MATERIALS created.';
END
ELSE
    PRINT 'Table POS_MATERIALS already exists.';
GO

-- ── Submodule registration ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleCode = 'ACT_POS_MATERIALS')
BEGIN
    DECLARE @ModuleId INT;
    SELECT @ModuleId = ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'ACTIVITY';

    INSERT INTO dbo.LM_Submodules (ModuleId, SubmoduleCode, SubmoduleName, TableName, IsActive)
    VALUES (@ModuleId, 'ACT_POS_MATERIALS', 'POS Materials', 'POS_MATERIALS', 1);

    DECLARE @SubId INT = SCOPE_IDENTITY();

    -- Roles: 1=SuperAdmin, 2=Admin, 8=Marketing
    INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    VALUES
        (1, @SubId, 1, 1, 1, 1, 1),
        (2, @SubId, 1, 1, 1, 1, 1),
        (8, @SubId, 1, 1, 1, 1, 0);

    PRINT 'Submodule ACT_POS_MATERIALS registered.';
END
ELSE
    PRINT 'Submodule ACT_POS_MATERIALS already registered.';
GO

PRINT 'Script 19_PosMaterials.sql completed.';
GO
