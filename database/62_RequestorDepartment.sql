-- ============================================================
-- Script  : 62_RequestorDepartment.sql
-- Database: LicoresMaduoDB
-- Purpose : Create AB_REQUESTOR_DEPARTMENT table to replace
--           the Vendor/Department linking with Requestor/Department.
--           Register submodule and permissions.
-- Run on  : LicoresMaduoDB
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── 1. Create table ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'AB_REQUESTOR_DEPARTMENT' AND type = 'U')
BEGIN
    CREATE TABLE dbo.AB_REQUESTOR_DEPARTMENT (
        RD_Id        INT           IDENTITY(1,1) NOT NULL,
        RD_REQUESTOR NVARCHAR(100) NOT NULL,
        RD_DEPARTMENT NVARCHAR(50) NOT NULL,
        IS_Active    BIT           NOT NULL DEFAULT 1,
        Created_At   DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AB_REQUESTOR_DEPARTMENT PRIMARY KEY (RD_Id)
    );
    PRINT 'Table AB_REQUESTOR_DEPARTMENT created.';
END
ELSE
    PRINT 'Table AB_REQUESTOR_DEPARTMENT already exists — skipped.';
GO

-- ── 2. Register submodule ──────────────────────────────────────────────────────
DECLARE @PurchaseId INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'PURCHASE');

MERGE dbo.LM_Submodules AS tgt
USING (VALUES
    (@PurchaseId, 'AB_REQUESTOR_DEPARTMENT', 'Requestor / Department', 'AB_REQUESTOR_DEPARTMENT', 12)
) AS src (ModuleId, SubmoduleCode, SubmoduleName, TableName, DisplayOrder)
ON tgt.SubmoduleCode = src.SubmoduleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleId, SubmoduleCode, SubmoduleName, TableName, DisplayOrder)
    VALUES (src.ModuleId, src.SubmoduleCode, src.SubmoduleName, src.TableName, src.DisplayOrder);
GO

PRINT 'Submodule AB_REQUESTOR_DEPARTMENT registered.';
GO

-- ── 3. Assign permissions (SuperAdmin=1, Admin=2 — full; User=3 — no access) ──
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete, r.CanApprove
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        ('AB_REQUESTOR_DEPARTMENT', 1, 1,1,1,1,1, 0),
        ('AB_REQUESTOR_DEPARTMENT', 2, 1,1,1,1,1, 0),
        ('AB_REQUESTOR_DEPARTMENT', 3, 0,0,0,0,0, 0)
    ) AS r(SubmoduleCode, RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete, CanApprove)
    WHERE sm.SubmoduleCode = r.SubmoduleCode
) AS src ON (tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId)
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete, CanApprove)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead, src.CanWrite, src.CanEdit, src.CanDelete, src.CanApprove)
WHEN MATCHED THEN
    UPDATE SET
        CanAccess  = src.CanAccess,  CanRead   = src.CanRead,
        CanWrite   = src.CanWrite,   CanEdit   = src.CanEdit,
        CanDelete  = src.CanDelete,  CanApprove= src.CanApprove;
GO

PRINT 'Permissions assigned for AB_REQUESTOR_DEPARTMENT.';
GO

PRINT 'Script 62 complete.';
GO
