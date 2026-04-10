-- ============================================================
-- Script 55: Ensure all Aankoopbon submodules exist in
--            LM_Submodules and have permissions for all roles.
-- Safe to run multiple times (idempotent).
-- Date: 2026-04-09
-- ============================================================

USE LicoresMaduoDB;
GO

DECLARE @PurchaseId INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'PURCHASE');

-- ── 1. Insert missing submodules ──────────────────────────────────────────────
MERGE dbo.LM_Submodules AS tgt
USING (VALUES
    (@PurchaseId, 'AB_AANKOOPBON',        'Aankoopbon Orders',        'AB_ORDER_HEADERS',  1),
    (@PurchaseId, 'AB_VENDORS',           'Vendors',                  'VENDORS',           2),
    (@PurchaseId, 'AB_PRODUCTS_MGT',      'AB Products',              'AB_PRODUCTS',       3),
    (@PurchaseId, 'AB_DEPARTMENTS',       'Departments',              'AB_DEPARTMENTS',    4),
    (@PurchaseId, 'AB_EENHEDEN',          'Eenheden (Units)',         'AB_EENHEDEN',       5),
    (@PurchaseId, 'AB_RECEIVERS',         'Receivers',                'AB_RECEIVERS',      6),
    (@PurchaseId, 'AB_REQUESTORS',        'Requestors',               'AB_REQUESTORS',     7),
    (@PurchaseId, 'AB_REQUESTORS_VENDOR', 'Requestors / Vendor',      'AB_REQUESTORS',     8),
    (@PurchaseId, 'AB_COST_TYPE',         'Cost Types',               'AB_COST_TYPE',      9),
    (@PurchaseId, 'AB_VEHICLE_TYPE',      'Vehicle Types',            'AB_VEHICLE_TYPE',  10),
    (@PurchaseId, 'AB_VEHICLES',          'Vehicles',                 'AB_VEHICLES',      11)
) AS src (ModuleId, SubmoduleCode, SubmoduleName, TableName, DisplayOrder)
ON tgt.SubmoduleCode = src.SubmoduleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleId, SubmoduleCode, SubmoduleName, TableName, DisplayOrder)
    VALUES (src.ModuleId, src.SubmoduleCode, src.SubmoduleName, src.TableName, src.DisplayOrder);
GO

PRINT 'Submodules verified/inserted.';
GO

-- ── 2. Assign permissions for all roles ──────────────────────────────────────
--   AB_AANKOOPBON  → SuperAdmin/Admin: full  | User: access+read+write+edit (no delete) | CanApprove: Admin+SuperAdmin only
--   AB catalogs    → SuperAdmin/Admin: full  | User: no access
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete, r.CanApprove
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        -- AB_AANKOOPBON: operational module
        ('AB_AANKOOPBON', 1, 1,1,1,1,1, 1),   -- SuperAdmin: full + approve
        ('AB_AANKOOPBON', 2, 1,1,1,1,1, 1),   -- Admin:      full + approve
        ('AB_AANKOOPBON', 3, 1,1,1,1,0, 0),   -- User:       access but no delete/approve
        -- Catalogs: admin-only
        ('AB_VENDORS',           1, 1,1,1,1,1, 0),
        ('AB_VENDORS',           2, 1,1,1,1,1, 0),
        ('AB_VENDORS',           3, 0,0,0,0,0, 0),
        ('AB_PRODUCTS_MGT',      1, 1,1,1,1,1, 0),
        ('AB_PRODUCTS_MGT',      2, 1,1,1,1,1, 0),
        ('AB_PRODUCTS_MGT',      3, 0,0,0,0,0, 0),
        ('AB_DEPARTMENTS',       1, 1,1,1,1,1, 0),
        ('AB_DEPARTMENTS',       2, 1,1,1,1,1, 0),
        ('AB_DEPARTMENTS',       3, 0,0,0,0,0, 0),
        ('AB_EENHEDEN',          1, 1,1,1,1,1, 0),
        ('AB_EENHEDEN',          2, 1,1,1,1,1, 0),
        ('AB_EENHEDEN',          3, 0,0,0,0,0, 0),
        ('AB_RECEIVERS',         1, 1,1,1,1,1, 0),
        ('AB_RECEIVERS',         2, 1,1,1,1,1, 0),
        ('AB_RECEIVERS',         3, 0,0,0,0,0, 0),
        ('AB_REQUESTORS',        1, 1,1,1,1,1, 0),
        ('AB_REQUESTORS',        2, 1,1,1,1,1, 0),
        ('AB_REQUESTORS',        3, 0,0,0,0,0, 0),
        ('AB_REQUESTORS_VENDOR', 1, 1,1,1,1,1, 0),
        ('AB_REQUESTORS_VENDOR', 2, 1,1,1,1,1, 0),
        ('AB_REQUESTORS_VENDOR', 3, 0,0,0,0,0, 0),
        ('AB_COST_TYPE',         1, 1,1,1,1,1, 0),
        ('AB_COST_TYPE',         2, 1,1,1,1,1, 0),
        ('AB_COST_TYPE',         3, 0,0,0,0,0, 0),
        ('AB_VEHICLE_TYPE',      1, 1,1,1,1,1, 0),
        ('AB_VEHICLE_TYPE',      2, 1,1,1,1,1, 0),
        ('AB_VEHICLE_TYPE',      3, 0,0,0,0,0, 0),
        ('AB_VEHICLES',          1, 1,1,1,1,1, 0),
        ('AB_VEHICLES',          2, 1,1,1,1,1, 0),
        ('AB_VEHICLES',          3, 0,0,0,0,0, 0)
    ) AS r(SubmoduleCode, RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete, CanApprove)
    WHERE sm.SubmoduleCode = r.SubmoduleCode
) AS src ON (tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId)
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete, CanApprove)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead, src.CanWrite, src.CanEdit, src.CanDelete, src.CanApprove)
WHEN MATCHED THEN
    UPDATE SET
        CanAccess  = src.CanAccess,
        CanRead    = src.CanRead,
        CanWrite   = src.CanWrite,
        CanEdit    = src.CanEdit,
        CanDelete  = src.CanDelete,
        CanApprove = src.CanApprove;
GO

PRINT 'Permissions assigned for all Aankoopbon submodules.';
GO

-- ── 3. Verify ─────────────────────────────────────────────────────────────────
SELECT
    s.SubmoduleCode,
    r.RoleName,
    rp.CanAccess, rp.CanRead, rp.CanWrite, rp.CanEdit, rp.CanDelete, rp.CanApprove
FROM dbo.LM_Submodules s
JOIN dbo.LM_RolePermissions rp ON rp.SubmoduleId = s.SubmoduleId
JOIN dbo.LM_Roles r             ON r.RoleId       = rp.RoleId
WHERE s.SubmoduleCode LIKE 'AB_%'
ORDER BY s.SubmoduleCode, r.RoleId;
GO

PRINT 'Script 55 complete.';
GO
