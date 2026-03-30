-- ============================================================
-- Script 30: Fix Aankoopbon role permissions
--
-- Problems fixed:
--   1. AB_AANKOOPBON submodule missing from 24_FixMissingSubmodules
--      (was only in 28_AankoopbonOrders, roles 1+2 only)
--   2. Role 3 (User) has no permissions on AB_AANKOOPBON
--      → sidebar hides "Aankoopbonnen" for regular users
--
-- Permission design:
--   AB_AANKOOPBON  → Group A (operational): User gets CanAccess+CanRead+CanWrite+CanEdit
--   AB_* catalogs  → Group C (config):      User gets no access (admin-only)
-- ============================================================
USE LicoresMaduoDB;
GO

-- ── Step 1: Ensure AB_AANKOOPBON is registered in LM_Submodules ──────────────
-- (28_AankoopbonOrders already inserts it, this is idempotent safety)
IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleCode = 'AB_AANKOOPBON')
BEGIN
    INSERT INTO dbo.LM_Submodules (ModuleId, SubmoduleCode, SubmoduleName, TableName, IsActive)
    SELECT m.ModuleId, 'AB_AANKOOPBON', 'Aankoopbon Orders', 'AB_ORDER_HEADERS', 1
    FROM   dbo.LM_Modules m WHERE m.ModuleCode = 'PURCHASE';
    PRINT 'AB_AANKOOPBON submodule inserted.';
END
ELSE
    PRINT 'AB_AANKOOPBON already exists.';
GO

-- ── Step 2: Assign permissions for all 3 roles on AB_AANKOOPBON ──────────────
-- SuperAdmin(1): full  Admin(2): full  User(3): Access+Read+Write+Edit
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        (1, 1,1,1,1,1),   -- SuperAdmin: full
        (2, 1,1,1,1,1),   -- Admin:      full
        (3, 1,1,1,1,0)    -- User:       access + read + write + edit (no delete)
    ) AS r(RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    WHERE sm.SubmoduleCode = 'AB_AANKOOPBON'
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

-- ── Step 3: Ensure all AB catalog submodules also have role 3 row (no access) ─
-- If script 24 wasn't run yet, these rows might be missing for role 3.
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        (1, 1,1,1,1,1),
        (2, 1,1,1,1,1),
        (3, 0,0,0,0,0)    -- User: no access to catalog management
    ) AS r(RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    WHERE sm.SubmoduleCode IN (
        'AB_VENDORS',
        'AB_PRODUCTS_MGT',
        'AB_DEPARTMENTS',
        'AB_EENHEDEN',
        'AB_RECEIVERS',
        'AB_REQUESTORS',
        'AB_REQUESTORS_VENDOR',
        'AB_COST_TYPE',
        'AB_VEHICLE_TYPE',
        'AB_VEHICLES'
    )
) AS src ON (tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId)
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead, src.CanWrite, src.CanEdit, src.CanDelete);
GO

-- ── Verify result ─────────────────────────────────────────────────────────────
SELECT
    s.SubmoduleCode,
    s.SubmoduleName,
    r.RoleName,
    rp.CanAccess, rp.CanRead, rp.CanWrite, rp.CanEdit, rp.CanDelete
FROM dbo.LM_Submodules s
JOIN dbo.LM_RolePermissions rp ON rp.SubmoduleId = s.SubmoduleId
JOIN dbo.LM_Roles r             ON r.RoleId       = rp.RoleId
WHERE s.SubmoduleCode LIKE 'AB_%'
ORDER BY s.SubmoduleCode, r.RoleId;
GO

PRINT 'Script 30 complete.';
GO
