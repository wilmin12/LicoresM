-- ============================================================
-- Script  : 65_VendorDepartment_DepartmentCostType.sql
-- Database: LicoresMaduoDB
-- Purpose : Create AB_VENDOR_DEPARTMENT and AB_DEPARTMENT_COST_TYPE
--           tables, register submodules and assign permissions.
-- Run on  : LicoresMaduoDB
-- Safe    : Idempotent (IF NOT EXISTS / MERGE)
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── 1. AB_VENDOR_DEPARTMENT ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'AB_VENDOR_DEPARTMENT' AND type = 'U')
BEGIN
    CREATE TABLE dbo.AB_VENDOR_DEPARTMENT (
        VD_Id         INT           IDENTITY(1,1) NOT NULL,
        VD_VENDOR     NVARCHAR(10)  NOT NULL,
        VD_DEPARTMENT NVARCHAR(50)  NOT NULL,
        IS_Active     BIT           NOT NULL DEFAULT 1,
        Created_At    DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AB_VENDOR_DEPARTMENT PRIMARY KEY (VD_Id)
    );
    PRINT 'Table AB_VENDOR_DEPARTMENT created.';
END
ELSE
    PRINT 'Table AB_VENDOR_DEPARTMENT already exists — skipped.';
GO

-- ── 2. AB_DEPARTMENT_COST_TYPE ─────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'AB_DEPARTMENT_COST_TYPE' AND type = 'U')
BEGIN
    CREATE TABLE dbo.AB_DEPARTMENT_COST_TYPE (
        DCT_Id         INT          IDENTITY(1,1) NOT NULL,
        DCT_DEPARTMENT NVARCHAR(50) NOT NULL,
        DCT_COST_TYPE  NVARCHAR(50) NOT NULL,
        IS_Active      BIT          NOT NULL DEFAULT 1,
        Created_At     DATETIME     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AB_DEPARTMENT_COST_TYPE PRIMARY KEY (DCT_Id)
    );
    PRINT 'Table AB_DEPARTMENT_COST_TYPE created.';
END
ELSE
    PRINT 'Table AB_DEPARTMENT_COST_TYPE already exists — skipped.';
GO

-- ── 3. Register submodules ─────────────────────────────────────────────────────
DECLARE @PurchaseId INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'PURCHASE');

MERGE dbo.LM_Submodules AS tgt
USING (VALUES
    (@PurchaseId, 'AB_VENDOR_DEPARTMENT',   'Vendor / Department',   'AB_VENDOR_DEPARTMENT',   13),
    (@PurchaseId, 'AB_DEPARTMENT_COST_TYPE','Department / Cost Type','AB_DEPARTMENT_COST_TYPE', 14)
) AS src (ModuleId, SubmoduleCode, SubmoduleName, TableName, DisplayOrder)
ON tgt.SubmoduleCode = src.SubmoduleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleId, SubmoduleCode, SubmoduleName, TableName, DisplayOrder)
    VALUES (src.ModuleId, src.SubmoduleCode, src.SubmoduleName, src.TableName, src.DisplayOrder);
GO

PRINT 'Submodules AB_VENDOR_DEPARTMENT and AB_DEPARTMENT_COST_TYPE registered.';
GO

-- ── 4. Assign permissions (SuperAdmin=1, Admin=2: full; User=3: no access) ────
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete, r.CanApprove
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        ('AB_VENDOR_DEPARTMENT',    1, 1,1,1,1,1, 0),
        ('AB_VENDOR_DEPARTMENT',    2, 1,1,1,1,1, 0),
        ('AB_VENDOR_DEPARTMENT',    3, 0,0,0,0,0, 0),
        ('AB_DEPARTMENT_COST_TYPE', 1, 1,1,1,1,1, 0),
        ('AB_DEPARTMENT_COST_TYPE', 2, 1,1,1,1,1, 0),
        ('AB_DEPARTMENT_COST_TYPE', 3, 0,0,0,0,0, 0)
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

PRINT 'Permissions assigned for AB_VENDOR_DEPARTMENT and AB_DEPARTMENT_COST_TYPE.';
GO

-- ── 5. Verify ──────────────────────────────────────────────────────────────────
SELECT
    s.SubmoduleCode, r.RoleName,
    rp.CanAccess, rp.CanRead, rp.CanWrite, rp.CanEdit, rp.CanDelete, rp.CanApprove
FROM dbo.LM_Submodules s
JOIN dbo.LM_RolePermissions rp ON rp.SubmoduleId = s.SubmoduleId
JOIN dbo.LM_Roles r             ON r.RoleId       = rp.RoleId
WHERE s.SubmoduleCode IN ('AB_VENDOR_DEPARTMENT', 'AB_DEPARTMENT_COST_TYPE')
ORDER BY s.SubmoduleCode, r.RoleId;
GO

PRINT 'Script 65 complete.';
GO
