-- ============================================================
-- Script  : 60_AddItemFobPricesSubmodule.sql
-- Database: LicoresMaduoDB
-- Purpose : Register COST_ITEM_FOB_PRICES submodule and set
--           permissions (Admin only, User = no access).
-- Run on  : LicoresMaduoDB
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── 1. Resolve COST module ID ─────────────────────────────────────────────────
DECLARE @CostId INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'COST');

-- ── 2. Register submodule ─────────────────────────────────────────────────────
MERGE dbo.LM_Submodules AS tgt
USING (VALUES
    (@CostId, 'Item FOB Prices', 'COST_ITEM_FOB_PRICES', NULL, 9)
) AS src (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
ON tgt.SubmoduleCode = src.SubmoduleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (src.ModuleId, src.SubmoduleName, src.SubmoduleCode, src.TableName, src.DisplayOrder);
GO

-- ── 3. Permissions: SuperAdmin(1) + Admin(2) = full, User(3) = no access ──────
DECLARE @SmId INT = (SELECT SubmoduleId FROM dbo.LM_Submodules WHERE SubmoduleCode = 'COST_ITEM_FOB_PRICES');

MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM (VALUES
        (1, 1,1,1,1,1),   -- SuperAdmin: full
        (2, 1,1,1,1,1),   -- Admin:      full
        (3, 0,0,0,0,0)    -- User:       no access
    ) AS r(RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    CROSS JOIN (SELECT @SmId AS SubmoduleId) AS sm
) AS src ON (tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId)
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead, src.CanWrite, src.CanEdit, src.CanDelete)
WHEN MATCHED THEN
    UPDATE SET
        CanAccess = src.CanAccess,
        CanRead   = src.CanRead,
        CanWrite  = src.CanWrite,
        CanEdit   = src.CanEdit,
        CanDelete = src.CanDelete;
GO

PRINT 'COST_ITEM_FOB_PRICES submodule and permissions registered.';
GO
