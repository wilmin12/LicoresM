-- ============================================================
-- 20_PosLendOut.sql
-- Tables: POS_LEND_OUT (header) + POS_LEND_OUT_ITEMS (lines)
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── POS_LEND_OUT ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'POS_LEND_OUT')
BEGIN
    CREATE TABLE dbo.POS_LEND_OUT (
        PLO_Id               INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PLO_Number           NVARCHAR(20)   NOT NULL,
        PLO_Year             INT            NOT NULL,
        PLO_Status           NVARCHAR(20)   NOT NULL DEFAULT 'DRAFT',   -- DRAFT, LENT, RETURNED, PARTIAL
        PLO_Date             DATE           NULL,
        PLO_Expected_Return  DATE           NULL,
        PLO_Actual_Return    DATE           NULL,
        PLO_Client_Code      NVARCHAR(20)   NULL,
        PLO_Client_Name      NVARCHAR(150)  NULL,
        PLO_Contact_Name     NVARCHAR(150)  NULL,
        PLO_Contact_Phone    NVARCHAR(50)   NULL,
        PLO_Notes            NVARCHAR(300)  NULL,
        PLO_Created_By_Id    INT            NULL,
        PLO_Created_By_Name  NVARCHAR(150)  NULL,
        IS_Active            BIT            NOT NULL DEFAULT 1,
        Created_At           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_POS_LEND_OUT_Number UNIQUE (PLO_Number)
    );
    PRINT 'Table POS_LEND_OUT created.';
END
ELSE
    PRINT 'Table POS_LEND_OUT already exists.';
GO

-- ── POS_LEND_OUT_ITEMS ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'POS_LEND_OUT_ITEMS')
BEGIN
    CREATE TABLE dbo.POS_LEND_OUT_ITEMS (
        PLOI_Id                INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PLOI_PLO_Id            INT            NOT NULL,
        PLOI_PM_Code           NVARCHAR(20)   NULL,
        PLOI_PM_Name           NVARCHAR(150)  NULL,
        PLOI_Quantity_Lent     INT            NOT NULL DEFAULT 0,
        PLOI_Quantity_Returned INT            NOT NULL DEFAULT 0,
        PLOI_Notes             NVARCHAR(300)  NULL,
        IS_Active              BIT            NOT NULL DEFAULT 1,
        Created_At             DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_PLOI_PLO FOREIGN KEY (PLOI_PLO_Id)
            REFERENCES dbo.POS_LEND_OUT(PLO_Id)
    );
    PRINT 'Table POS_LEND_OUT_ITEMS created.';
END
ELSE
    PRINT 'Table POS_LEND_OUT_ITEMS already exists.';
GO

-- ── Submodule registration ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleCode = 'ACT_POS_LEND_OUT')
BEGIN
    DECLARE @ModuleId INT;
    SELECT @ModuleId = ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'ACTIVITY';

    INSERT INTO dbo.LM_Submodules (ModuleId, SubmoduleCode, SubmoduleName, TableName, IsActive)
    VALUES (@ModuleId, 'ACT_POS_LEND_OUT', 'POS Lend Out', 'POS_LEND_OUT', 1);

    DECLARE @SubId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    VALUES
        (1, @SubId, 1, 1, 1, 1, 1),
        (2, @SubId, 1, 1, 1, 1, 1),
        (8, @SubId, 1, 1, 1, 1, 0);

    PRINT 'Submodule ACT_POS_LEND_OUT registered.';
END
ELSE
    PRINT 'Submodule ACT_POS_LEND_OUT already registered.';
GO

PRINT 'Script 20_PosLendOut.sql completed.';
GO
