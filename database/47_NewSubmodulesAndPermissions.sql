-- ============================================================
-- Script 47: New Submodules & Permissions
-- Registers submodules created after script 23:
--   · 5 new Cost Calc catalog pages
--   · SETTINGS module + Company Settings submodule
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── 1. Add SETTINGS module (if not already present) ──────────────────────────
MERGE dbo.LM_Modules AS tgt
USING (VALUES
    ('Settings', 'SETTINGS', 'fa-sliders', 99)
) AS src (ModuleName, ModuleCode, Icon, DisplayOrder)
ON tgt.ModuleCode = src.ModuleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleName, ModuleCode, Icon, DisplayOrder)
    VALUES (src.ModuleName, src.ModuleCode, src.Icon, src.DisplayOrder);
GO

-- ── 2. Resolve module IDs ─────────────────────────────────────────────────────
DECLARE @CostId     INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'COST');
DECLARE @SettingsId INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'SETTINGS');

-- ── 3. Register new submodules ────────────────────────────────────────────────
MERGE dbo.LM_Submodules AS tgt
USING (VALUES
    -- Cost Calc catalog pages (display order continues from 3)
    (@CostId, 'Tariff Items (HS)',    'COST_TARIFF_ITEMS',    NULL, 4),
    (@CostId, 'Goods Classification', 'COST_GOODS_CLASS',     NULL, 5),
    (@CostId, 'Item Weights',         'COST_ITEM_WEIGHTS',    NULL, 6),
    (@CostId, 'Allowed Margins',      'COST_ALLOWED_MARGINS', NULL, 7),
    (@CostId, 'Inland Tariffs',       'COST_INLAND_TARIFFS',  NULL, 8),
    -- Settings
    (@SettingsId, 'Company Settings', 'SETTINGS_COMPANY',     'COMPANY_SETTINGS', 1)
) AS src (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
ON tgt.SubmoduleCode = src.SubmoduleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (src.ModuleId, src.SubmoduleName, src.SubmoduleCode, src.TableName, src.DisplayOrder);
GO

-- ── 4. Permissions: new COST catalogs → Admin only, User = no access ─────────
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        (1, 1,1,1,1,1),   -- SuperAdmin: full
        (2, 1,1,1,1,1),   -- Admin:      full
        (3, 0,0,0,0,0)    -- User:       no access
    ) AS r(RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    WHERE sm.SubmoduleCode IN (
        'COST_TARIFF_ITEMS',
        'COST_GOODS_CLASS',
        'COST_ITEM_WEIGHTS',
        'COST_ALLOWED_MARGINS',
        'COST_INLAND_TARIFFS'
    )
) AS src ON (tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId)
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead, src.CanWrite, src.CanEdit, src.CanDelete);
GO

-- ── 5. Permissions: Company Settings → SuperAdmin + Admin only ────────────────
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        (1, 1,1,1,1,1),   -- SuperAdmin: full
        (2, 1,1,1,1,1),   -- Admin:      full
        (3, 0,0,0,0,0)    -- User:       no access
    ) AS r(RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    WHERE sm.SubmoduleCode = 'SETTINGS_COMPANY'
) AS src ON (tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId)
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead, src.CanWrite, src.CanEdit, src.CanDelete);
GO

PRINT 'Script 47 completed: 5 COST submodules + SETTINGS module/submodule registered with permissions.';
GO
