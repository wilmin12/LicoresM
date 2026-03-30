-- ============================================================
-- 16_MarketingCalendar.sql
-- Creates MARKETING_CALENDAR table and registers submodule
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── Table ─────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MARKETING_CALENDAR')
BEGIN
    CREATE TABLE dbo.MARKETING_CALENDAR (
        MC_Id            INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MC_Year          INT            NOT NULL,
        MC_Supplier_Code NVARCHAR(5)    NULL,           -- SUPPLIERT.SUPPLIER code
        MC_Supplier_Name NVARCHAR(100)  NULL,
        MC_Brand         NVARCHAR(100)  NOT NULL,
        MC_Budget        DECIMAL(18,2)  NULL,
        MC_Month1        NVARCHAR(300)  NULL,           -- Activities planned per month
        MC_Month2        NVARCHAR(300)  NULL,
        MC_Month3        NVARCHAR(300)  NULL,
        MC_Month4        NVARCHAR(300)  NULL,
        MC_Month5        NVARCHAR(300)  NULL,
        MC_Month6        NVARCHAR(300)  NULL,
        MC_Month7        NVARCHAR(300)  NULL,
        MC_Month8        NVARCHAR(300)  NULL,
        MC_Month9        NVARCHAR(300)  NULL,
        MC_Month10       NVARCHAR(300)  NULL,
        MC_Month11       NVARCHAR(300)  NULL,
        MC_Month12       NVARCHAR(300)  NULL,
        MC_Notes         NVARCHAR(500)  NULL,
        IS_Active        BIT            NOT NULL DEFAULT 1,
        Created_At       DATETIME2      NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'Table MARKETING_CALENDAR created.';
END
ELSE
    PRINT 'Table MARKETING_CALENDAR already exists.';
GO

-- ── Unique index: one row per Supplier+Brand+Year ─────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_MarketingCalendar_SupplierBrandYear' AND object_id = OBJECT_ID('MARKETING_CALENDAR'))
    CREATE UNIQUE INDEX UQ_MarketingCalendar_SupplierBrandYear
        ON dbo.MARKETING_CALENDAR (MC_Year, MC_Supplier_Code, MC_Brand)
        WHERE IS_Active = 1;
GO

-- ── Register submodule + permissions ─────────────────────────────────────────
DECLARE @ModuleId INT;
SELECT @ModuleId = ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'ACTIVITY';

IF @ModuleId IS NOT NULL
BEGIN
    -- Submodule
    IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleCode = 'ACT_MARKETING_CALENDAR')
    BEGIN
        INSERT INTO dbo.LM_Submodules (ModuleId, SubmoduleCode, SubmoduleName, TableName, DisplayOrder, IsActive)
        VALUES (@ModuleId, 'ACT_MARKETING_CALENDAR', 'Marketing Calendar',
                'MARKETING_CALENDAR', 1, 1);
        PRINT 'Submodule ACT_MARKETING_CALENDAR inserted.';
    END

    -- Permissions for roles 1 (SuperAdmin), 2 (Admin), 8 (Marketing)
    DECLARE @SmId INT;
    SELECT @SmId = SubmoduleId FROM dbo.LM_Submodules WHERE SubmoduleCode = 'ACT_MARKETING_CALENDAR';

    INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    SELECT r.RoleId, @SmId, 1, 1, 1, 1, 1
    FROM dbo.LM_Roles r
    WHERE r.RoleId IN (1, 2, 8)
      AND NOT EXISTS (
          SELECT 1 FROM dbo.LM_RolePermissions p
          WHERE p.RoleId = r.RoleId AND p.SubmoduleId = @SmId
      );
    PRINT 'Permissions granted for Marketing Calendar.';
END
ELSE
    PRINT 'WARNING: ACTIVITY module not found. Submodule not registered.';
GO

PRINT 'Script 16_MarketingCalendar.sql completed.';
GO
