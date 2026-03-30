-- ============================================================
-- 23_AllModulePermissions.sql
-- Registers LM_RolePermissions for ALL submodules.
-- Roles seeded in 02_AuthTables.sql:
--   1 = SuperAdmin  2 = Admin  3 = User
--
-- Permission matrix per submodule group:
--   SuperAdmin (1) : full access everywhere  (1,1,1,1,1)
--   Admin      (2) : full access everywhere  (1,1,1,1,1)
--   User       (3) : see notes per section
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── Helper: insert permissions only if not already present ───────────────────
-- We use a MERGE per submodule group to keep it idempotent.
-- Each MERGE injects rows for roles 1, 2, 3 per matching submodule code.

-- ── GROUP A: Operational pages  ───────────────────────────────────────────────
--   User gets: Access=1, Read=1, Write=0, Edit=1, Delete=0
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        (1, 1,1,1,1,1),   -- SuperAdmin: full
        (2, 1,1,1,1,1),   -- Admin:      full
        (3, 1,1,0,1,0)    -- User:       access + read + edit
    ) AS r(RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    WHERE sm.SubmoduleCode IN (
        'TRACKING_ORDERS',
        'ACT_MARKETING_CALENDAR',
        'ACT_ACTIVITY_REQUESTS',
        'ACT_POS_MATERIALS',
        'ACT_POS_LEND_OUT'
    )
) AS src ON (tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId)
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead, src.CanWrite, src.CanEdit, src.CanDelete);
GO

-- ── GROUP B: Read-only pages for User ─────────────────────────────────────────
--   User gets: Access=1, Read=1, Write=0, Edit=0, Delete=0
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        (1, 1,1,1,1,1),
        (2, 1,1,1,1,1),
        (3, 1,1,0,0,0)   -- User: read-only
    ) AS r(RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    WHERE sm.SubmoduleCode IN (
        'FF_OCEAN_QUOTES',
        'FF_INLAND_QUOTES',
        'FF_LCL_QUOTES',
        'FF_INLAND_ADD_CHARGES',
        'COST_CALCULATIONS',
        'COST_NEW_CALC',
        'COST_PO_LOOKUP',
        'ROUTE_REPORTS',
        'ROUTE_DIMENSIONS',
        'STOCK_ANALYSIS',
        'STOCK_ANALYSIS_RESULTS'
    )
) AS src ON (tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId)
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead, src.CanWrite, src.CanEdit, src.CanDelete);
GO

-- ── GROUP C: Config/Catalog pages — SuperAdmin + Admin only ──────────────────
--   User gets: no access (0,0,0,0,0)
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        (1, 1,1,1,1,1),
        (2, 1,1,1,1,1),
        (3, 0,0,0,0,0)   -- User: no access
    ) AS r(RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    WHERE sm.SubmoduleCode IN (
        -- Tracking catalogs
        'TRACKING_ORDER_STATUS',
        'TRACKING_CONTAINER_TYPES',
        -- Freight catalogs
        'FF_FORWARDERS',
        'FF_CURRENCIES',
        'FF_COUNTRIES',
        'FF_SUPPLIERS',
        'FF_LOADTYPES',
        'FF_PORT_OF_LOADING',
        'FF_SHIPPING_LINES',
        'FF_SHIPPING_AGENT',
        'FF_ROUTES',
        'FF_CONTAINER_SPECS',
        'FF_CONTAINER_TYPES',
        'FF_ROUTES_BY_SA',
        'FF_OCEAN_FREIGHT_CHARGE',
        'FF_INLAND_FREIGHT_CHARGE',
        'FF_LCL_CHARGE',
        'FF_PRICE_TYPE',
        'FF_AMOUNT_TYPE',
        'FF_CHARGE_ACTION',
        'FF_CHARGE_OVER',
        'FF_CHARGE_PER',
        -- Route config
        'ROUTE_CUSTOMER_EXT',
        'ROUTE_PRODUCT_EXT',
        'ROUTE_BUDGET',
        -- Stock config
        'STOCK_IDEAL_MONTHS',
        'STOCK_VENDOR_CONSTRAINTS',
        'STOCK_SALES_BUDGET',
        -- Activity config catalogs
        'ACT_ACTIVITY_TYPE',
        'ACT_BUDGET_ACTIVITIES',
        'ACT_STATUS_CODES',
        'ACT_DENIAL_REASONS',
        'ACT_SPONSORING_TYPE',
        'ACT_ENTERTAINMENT_TYPE',
        'ACT_FISCAL_YEARS',
        'ACT_LICORES_GROUP',
        'ACT_LOCATION_INFO',
        'ACT_POS_CATEGORY',
        'ACT_POS_LEND_GIVE',
        'ACT_POS_MATERIALS_STATUS',
        'ACT_CUSTOMER_NON_CLIENT',
        'ACT_CUSTOMER_SALES_GROUP',
        'ACT_CUSTOMER_SEGMENT_INFO',
        'ACT_CUSTOMER_TARGET_GROUP',
        'ACT_FACILITATORS_INFO',
        -- Activity product catalogs
        'ACT_CAT_ADD_SPECS',
        'ACT_CAT_APPAREL_TYPE',
        'ACT_CAT_BAG_SPECS',
        'ACT_CAT_BOTTLES',
        'ACT_CAT_BRAND_SPECIFIC',
        'ACT_CAT_CLOTHING_TYPE',
        'ACT_CAT_COLORS',
        'ACT_CAT_CONTENT',
        'ACT_CAT_COOLER_CAPACITY',
        'ACT_CAT_COOLER_MODEL',
        'ACT_CAT_COOLER_TYPE',
        'ACT_CAT_FILE_NAMES',
        'ACT_CAT_GENDER',
        'ACT_CAT_GLASS_SERVING',
        'ACT_CAT_INSURRANCE',
        'ACT_CAT_LED',
        'ACT_CAT_MAINT_MONTHS',
        'ACT_CAT_MATERIALS',
        'ACT_CAT_SHAPES',
        'ACT_CAT_SIZES',
        'ACT_CAT_VAP_TYPE',
        -- Aankoopbon catalogs
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

PRINT 'Script 23_AllModulePermissions.sql completed. All submodules now have role permission rows.';
GO
