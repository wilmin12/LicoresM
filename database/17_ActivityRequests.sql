-- ============================================================
-- 17_ActivityRequests.sql
-- Creates ACTIVITY_REQUESTS main table and registers submodule
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── Table ─────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ACTIVITY_REQUESTS')
BEGIN
    CREATE TABLE dbo.ACTIVITY_REQUESTS (
        AR_Id                   INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AR_Number               NVARCHAR(20)   NOT NULL,          -- AR-2026-0001
        AR_Year                 INT            NOT NULL,
        AR_Status               NVARCHAR(20)   NOT NULL DEFAULT 'DRAFT',
                                                                   -- DRAFT | PENDING | APPROVED
                                                                   -- INPROCESS | READY | INVOICED | DENIED
        AR_Supplier_Code        NVARCHAR(5)    NULL,
        AR_Supplier_Name        NVARCHAR(100)  NULL,
        AR_Brand                NVARCHAR(100)  NULL,
        AR_Activity_Type_Code   NVARCHAR(20)   NULL,
        AR_Activity_Type_Desc   NVARCHAR(100)  NULL,
        AR_Description          NVARCHAR(500)  NULL,
        AR_Start_Date           DATE           NULL,
        AR_End_Date             DATE           NULL,
        AR_Location_Code        NVARCHAR(20)   NULL,
        AR_Location_Name        NVARCHAR(100)  NULL,
        AR_Budget               DECIMAL(18,2)  NULL,
        AR_Segment_Code         NVARCHAR(20)   NULL,
        AR_Target_Group_Code    NVARCHAR(20)   NULL,
        AR_Sales_Group_Code     NVARCHAR(20)   NULL,
        AR_Non_Client_Code      NVARCHAR(20)   NULL,
        AR_Non_Client_Name      NVARCHAR(100)  NULL,
        AR_Facilitator_Code     NVARCHAR(20)   NULL,
        AR_Facilitator_Name     NVARCHAR(100)  NULL,
        AR_Sponsoring_Type_Code NVARCHAR(20)   NULL,
        AR_Entertainment_Type_Code NVARCHAR(20) NULL,
        AR_Notes                NVARCHAR(1000) NULL,
        -- Audit
        AR_Created_By           INT            NULL,
        AR_Created_By_Name      NVARCHAR(100)  NULL,
        AR_Approved_By          INT            NULL,
        AR_Approved_By_Name     NVARCHAR(100)  NULL,
        AR_Approved_At          DATETIME2      NULL,
        AR_Denied_By            INT            NULL,
        AR_Denied_By_Name       NVARCHAR(100)  NULL,
        AR_Denied_At            DATETIME2      NULL,
        AR_Denial_Reason        NVARCHAR(500)  NULL,
        IS_Active               BIT            NOT NULL DEFAULT 1,
        Created_At              DATETIME2      NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'Table ACTIVITY_REQUESTS created.';
END
ELSE
    PRINT 'Table ACTIVITY_REQUESTS already exists.';
GO

-- ── Unique index on AR_Number ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_ActivityRequests_Number' AND object_id = OBJECT_ID('ACTIVITY_REQUESTS'))
    CREATE UNIQUE INDEX UQ_ActivityRequests_Number ON dbo.ACTIVITY_REQUESTS (AR_Number);
GO

-- ── Register submodule + permissions ─────────────────────────────────────────
DECLARE @ModuleId INT;
SELECT @ModuleId = ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'ACTIVITY';

IF @ModuleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleCode = 'ACT_ACTIVITY_REQUESTS')
    BEGIN
        INSERT INTO dbo.LM_Submodules (ModuleId, SubmoduleCode, SubmoduleName, TableName, DisplayOrder, IsActive)
        VALUES (@ModuleId, 'ACT_ACTIVITY_REQUESTS', 'Activity Requests', 'ACTIVITY_REQUESTS', 2, 1);
        PRINT 'Submodule ACT_ACTIVITY_REQUESTS inserted.';
    END

    DECLARE @SmId INT;
    SELECT @SmId = SubmoduleId FROM dbo.LM_Submodules WHERE SubmoduleCode = 'ACT_ACTIVITY_REQUESTS';

    INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    SELECT r.RoleId, @SmId, 1, 1, 1, 1, 1
    FROM dbo.LM_Roles r
    WHERE r.RoleId IN (1, 2, 8)
      AND NOT EXISTS (
          SELECT 1 FROM dbo.LM_RolePermissions p
          WHERE p.RoleId = r.RoleId AND p.SubmoduleId = @SmId
      );
    PRINT 'Permissions granted for Activity Requests.';
END
ELSE
    PRINT 'WARNING: ACTIVITY module not found.';
GO

PRINT 'Script 17_ActivityRequests.sql completed.';
GO
