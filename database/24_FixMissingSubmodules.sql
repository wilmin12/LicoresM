-- ============================================================
-- 24_FixMissingSubmodules.sql
-- Registers ALL submodules in LM_Submodules (idempotent).
-- Run this once to fix any database that is missing rows.
-- Safe to re-run: uses MERGE ON SubmoduleCode.
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── Step 1: Ensure all 7 modules exist ───────────────────────────────────────
MERGE dbo.LM_Modules AS tgt
USING (VALUES
    ('Tracking',          'TRACKING',  'fa-shipping-fast', 1),
    ('Freight Forwarder', 'FREIGHT',   'fa-ship',          2),
    ('Cost Calculation',  'COST',      'fa-calculator',    3),
    ('Route Assignment',  'ROUTE',     'fa-route',         4),
    ('Stock Analysis',    'STOCK',     'fa-boxes',         5),
    ('Activity Request',  'ACTIVITY',  'fa-tasks',         6),
    ('Aankoopbon',        'PURCHASE',  'fa-file-invoice',  7)
) AS src (ModuleName, ModuleCode, Icon, DisplayOrder)
ON tgt.ModuleCode = src.ModuleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleName, ModuleCode, Icon, DisplayOrder)
    VALUES (src.ModuleName, src.ModuleCode, src.Icon, src.DisplayOrder);
GO

-- ── Step 2: Register ALL submodules ──────────────────────────────────────────
DECLARE @TrackingId INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'TRACKING');
DECLARE @FreightId  INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'FREIGHT');
DECLARE @CostId     INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'COST');
DECLARE @RouteId    INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'ROUTE');
DECLARE @StockId    INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'STOCK');
DECLARE @ActId      INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'ACTIVITY');
DECLARE @PurchId    INT = (SELECT ModuleId FROM dbo.LM_Modules WHERE ModuleCode = 'PURCHASE');

MERGE dbo.LM_Submodules AS tgt
USING (VALUES
    -- ── Tracking ──────────────────────────────────────────────────────────────
    (@TrackingId, 'Purchase Order Tracking',  'TRACKING_ORDERS',             'TRACKING_ORDERS',            1),
    (@TrackingId, 'Order Status Codes',       'TRACKING_ORDER_STATUS',        'ORDER_STATUS',               2),
    (@TrackingId, 'Container Types',          'TRACKING_CONTAINER_TYPES',     'TRACKING_CONTAINER_TYPES',   3),

    -- ── Freight Forwarder ──────────────────────────────────────────────────────
    (@FreightId,  'Currencies',               'FF_CURRENCIES',                'CURRENCIES',                 1),
    (@FreightId,  'Load Types',               'FF_LOADTYPES',                 'LOADTYPES',                  2),
    (@FreightId,  'Ports of Loading',         'FF_PORT_OF_LOADING',           'PORT_OF_LOADING',            3),
    (@FreightId,  'Shipping Lines',           'FF_SHIPPING_LINES',            'SHIPPING_LINES',             4),
    (@FreightId,  'Shipping Agents',          'FF_SHIPPING_AGENT',            'SHIPPING_AGENT',             5),
    (@FreightId,  'Routes',                   'FF_ROUTES',                    'ROUTES',                     6),
    (@FreightId,  'Container Specs',          'FF_CONTAINER_SPECS',           'CONTAINER_SPECS',            7),
    (@FreightId,  'Container Types',          'FF_CONTAINER_TYPES',           'CONTAINER_TYPES',            8),
    (@FreightId,  'Routes by Shipping Agent', 'FF_ROUTES_BY_SA',              'ROUTES_BY_SHIPPING_AGENTS',  9),
    (@FreightId,  'Ocean Freight Charges',    'FF_OCEAN_FREIGHT_CHARGE',      'FF_OCEAN_FREIGHT_PORT_SLINE_CHARGES', 10),
    (@FreightId,  'Inland Freight Charges',   'FF_INLAND_FREIGHT_CHARGE',     'FF_INLAND_FREIGHT_REGION_TYPE_DET',   11),
    (@FreightId,  'LCL Charge Types',         'FF_LCL_CHARGE',                'LCL_CHARGE_TYPES',           12),
    (@FreightId,  'Price Types',              'FF_PRICE_TYPE',                'PRICE_TYPE',                 13),
    (@FreightId,  'Amount Types',             'FF_AMOUNT_TYPE',               'AMOUNT_TYPE',                14),
    (@FreightId,  'Charge Actions',           'FF_CHARGE_ACTION',             'CHARGE_ACTION',              15),
    (@FreightId,  'Charge Over',              'FF_CHARGE_OVER',               'CHARGE_OVER',                16),
    (@FreightId,  'Charge Per',               'FF_CHARGE_PER',                'CHARGE_PER',                 17),
    (@FreightId,  'Freight Forwarders',       'FF_FORWARDERS',                'FREIGHT_FORWARDERS',         18),
    (@FreightId,  'Ocean Freight Quotes',     'FF_OCEAN_QUOTES',              'FF_OCEAN_FREIGHT_HEADER',    19),
    (@FreightId,  'Inland Freight Quotes',    'FF_INLAND_QUOTES',             'FF_INLAND_FREIGHT_HEADER',   20),
    (@FreightId,  'LCL Quotes',               'FF_LCL_QUOTES',                'FF_LCL_HEADER',              21),
    (@FreightId,  'Inland Additional Charges','FF_INLAND_ADD_CHARGES',        'FF_INLAND_ADDITIONAL_CHARGES',22),
    (@FreightId,  'Countries',                'FF_COUNTRIES',                 'COUNTRIES',                  23),
    (@FreightId,  'Suppliers',                'FF_SUPPLIERS',                 'SUPPLIERT',                  24),

    -- ── Cost Calculation ───────────────────────────────────────────────────────
    (@CostId,     'Calculations',             'COST_CALCULATIONS',            'COST_CALC_FIN',              1),
    (@CostId,     'New Calculation',          'COST_NEW_CALC',                'COST_CALC_FIN',              2),
    (@CostId,     'Purchase Orders',          'COST_PO_LOOKUP',               NULL,                         3),

    -- ── Route Assignment ───────────────────────────────────────────────────────
    (@RouteId,    'Customer Ext Dimensions',  'ROUTE_CUSTOMER_EXT',           'ROUTE_CUSTOMER_EXT',         1),
    (@RouteId,    'Product Ext Dimensions',   'ROUTE_PRODUCT_EXT',            'ROUTE_PRODUCT_EXT',          2),
    (@RouteId,    'Budget',                   'ROUTE_BUDGET',                 'ROUTE_BUDGET',               3),
    (@RouteId,    'Reports',                  'ROUTE_REPORTS',                NULL,                         4),
    (@RouteId,    'Dimensions Viewer',        'ROUTE_DIMENSIONS',             NULL,                         5),

    -- ── Stock Analysis ─────────────────────────────────────────────────────────
    (@StockId,    'Ideal Months Config',      'STOCK_IDEAL_MONTHS',           'STOCK_IDEAL_MONTHS',         1),
    (@StockId,    'Vendor Constraints',       'STOCK_VENDOR_CONSTRAINTS',     'STOCK_VENDOR_CONSTRAINTS',   2),
    (@StockId,    'Sales Budget',             'STOCK_SALES_BUDGET',           'STOCK_SALES_BUDGET',         3),
    (@StockId,    'Generate Analysis',        'STOCK_ANALYSIS',               NULL,                         4),
    (@StockId,    'Analysis Results',         'STOCK_ANALYSIS_RESULTS',       'STOCK_ANALYSIS_RESULT',      5),

    -- ── Activity Request — operational pages ───────────────────────────────────
    (@ActId,      'Marketing Calendar',       'ACT_MARKETING_CALENDAR',       'MARKETING_CALENDAR',         1),
    (@ActId,      'Activity Requests',        'ACT_ACTIVITY_REQUESTS',        'ACTIVITY_REQUESTS',          2),
    (@ActId,      'POS Materials',            'ACT_POS_MATERIALS',            'POS_MATERIALS_INVENTORY',    3),
    (@ActId,      'POS Lend Out',             'ACT_POS_LEND_OUT',             'POS_LEND_OUT',               4),
    -- Activity Request — configuration catalogs
    (@ActId,      'Activity Types',           'ACT_ACTIVITY_TYPE',            'ACTIVITY_TYPE',              5),
    (@ActId,      'Budget Activities',        'ACT_BUDGET_ACTIVITIES',        'BUDGET_ACTIVITIES',          6),
    (@ActId,      'Status Codes',             'ACT_STATUS_CODES',             'STATUS_CODES',               7),
    (@ActId,      'Denial Reasons',           'ACT_DENIAL_REASONS',           'DENIAL_REASONS',             8),
    (@ActId,      'Sponsoring Types',         'ACT_SPONSORING_TYPE',          'SPONSORING_TYPE',            9),
    (@ActId,      'Entertainment Types',      'ACT_ENTERTAINMENT_TYPE',       'ENTERTAINMENT_TYPE',         10),
    (@ActId,      'Fiscal Years',             'ACT_FISCAL_YEARS',             'FISCAL_YEARS',               11),
    (@ActId,      'Licores Group',            'ACT_LICORES_GROUP',            'LICORES_GROUP',              12),
    (@ActId,      'Location Info',            'ACT_LOCATION_INFO',            'LOCATION_INFO',              13),
    (@ActId,      'POS Category',             'ACT_POS_CATEGORY',             'POS_CATEGORY',               14),
    (@ActId,      'POS Lend/Give',            'ACT_POS_LEND_GIVE',            'POS_LEND_GIVE',              15),
    (@ActId,      'POS Materials Status',     'ACT_POS_MATERIALS_STATUS',     'POS_MATERIALS_STATUS',       16),
    (@ActId,      'Customer Non-Client',      'ACT_CUSTOMER_NON_CLIENT',      'CUSTOMER_NON_CLIENT',        17),
    (@ActId,      'Customer Sales Groups',    'ACT_CUSTOMER_SALES_GROUP',     'CUSTOMER_SALES_GROUP',       18),
    (@ActId,      'Customer Segments',        'ACT_CUSTOMER_SEGMENT_INFO',    'CUSTOMER_SEGMENT_INFO',      19),
    (@ActId,      'Customer Target Groups',   'ACT_CUSTOMER_TARGET_GROUP',    'CUSTOMER_TARGET_GROUP',      20),
    (@ActId,      'Facilitators',             'ACT_FACILITATORS_INFO',        'FACILITATORS_INFO',          21),
    -- Activity Request — product catalogs
    (@ActId,      'Cat Additional Specs',     'ACT_CAT_ADD_SPECS',            'CAT_ADD_SPECS',              22),
    (@ActId,      'Cat Apparel Types',        'ACT_CAT_APPAREL_TYPE',         'CAT_APPAREL_TYPE',           23),
    (@ActId,      'Cat Bag Specs',            'ACT_CAT_BAG_SPECS',            'CAT_BAG_SPECS',              24),
    (@ActId,      'Cat Bottles',              'ACT_CAT_BOTTLES',              'CAT_BOTTLES',                25),
    (@ActId,      'Cat Brand Specific',       'ACT_CAT_BRAND_SPECIFIC',       'CAT_BRAND_SPECIFIC',         26),
    (@ActId,      'Cat Clothing Types',       'ACT_CAT_CLOTHING_TYPE',        'CAT_CLOTHING_TYPE',          27),
    (@ActId,      'Cat Colors',               'ACT_CAT_COLORS',               'CAT_COLORS',                 28),
    (@ActId,      'Cat Content',              'ACT_CAT_CONTENT',              'CAT_CONTENT',                29),
    (@ActId,      'Cat Cooler Capacity',      'ACT_CAT_COOLER_CAPACITY',      'CAT_COOLER_CAPACITY',        30),
    (@ActId,      'Cat Cooler Model',         'ACT_CAT_COOLER_MODEL',         'CAT_COOLER_MODEL',           31),
    (@ActId,      'Cat Cooler Types',         'ACT_CAT_COOLER_TYPE',          'CAT_COOLER_TYPE',            32),
    (@ActId,      'Cat File Names',           'ACT_CAT_FILE_NAMES',           'CAT_FILE_NAMES',             33),
    (@ActId,      'Cat Gender',               'ACT_CAT_GENDER',               'CAT_GENDER',                 34),
    (@ActId,      'Cat Glass Serving',        'ACT_CAT_GLASS_SERVING',        'CAT_GLASS_SERVING',          35),
    (@ActId,      'Cat Insurance',            'ACT_CAT_INSURRANCE',           'CAT_INSURRANCE',             36),
    (@ActId,      'Cat LED',                  'ACT_CAT_LED',                  'CAT_LED',                    37),
    (@ActId,      'Cat Maintenance Months',   'ACT_CAT_MAINT_MONTHS',         'CAT_MAINT_MONTHS',           38),
    (@ActId,      'Cat Materials',            'ACT_CAT_MATERIALS',            'CAT_MATERIALS',              39),
    (@ActId,      'Cat Shapes',               'ACT_CAT_SHAPES',               'CAT_SHAPES',                 40),
    (@ActId,      'Cat Sizes',                'ACT_CAT_SIZES',                'CAT_SIZES',                  41),
    (@ActId,      'Cat VAP Types',            'ACT_CAT_VAP_TYPE',             'CAT_VAP_TYPE',               42),

    -- ── Aankoopbon ─────────────────────────────────────────────────────────────
    (@PurchId,    'Aankoopbon Orders',         'AB_AANKOOPBON',                'AB_ORDER_HEADERS',           0),
    (@PurchId,    'Vendors',                  'AB_VENDORS',                   'VENDORS',                    1),
    (@PurchId,    'Departments',              'AB_DEPARTMENTS',               'DEPARTMENTS',                2),
    (@PurchId,    'Eenheden (Units)',          'AB_EENHEDEN',                  'EENHEDEN',                   3),
    (@PurchId,    'Receivers',                'AB_RECEIVERS',                 'RECEIVERS',                  4),
    (@PurchId,    'Requestors',               'AB_REQUESTORS',                'REQUESTORS',                 5),
    (@PurchId,    'Requestors / Vendor',      'AB_REQUESTORS_VENDOR',         'REQUESTORS_VENDOR',          6),
    (@PurchId,    'Cost Types',               'AB_COST_TYPE',                 'COST_TYPE',                  7),
    (@PurchId,    'Vehicle Types',            'AB_VEHICLE_TYPE',              'VEHICLE_TYPE',               8),
    (@PurchId,    'Vehicles',                 'AB_VEHICLES',                  'VEHICLES',                   9),
    (@PurchId,    'AB Products',              'AB_PRODUCTS_MGT',              'AB_PRODUCTS',                10)
) AS src (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
ON tgt.SubmoduleCode = src.SubmoduleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (src.ModuleId, src.SubmoduleName, src.SubmoduleCode, src.TableName, src.DisplayOrder);
GO

-- ── Step 3: Seed LM_RolePermissions for ALL submodules ───────────────────────
-- GROUP A: Operational pages
--   SuperAdmin/Admin: full (1,1,1,1,1)  |  User: Access+Read+Edit (1,1,0,1,0)
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        (1, 1,1,1,1,1),
        (2, 1,1,1,1,1),
        (3, 1,1,0,1,0)
    ) AS r(RoleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    WHERE sm.SubmoduleCode IN (
        'TRACKING_ORDERS',
        'ACT_MARKETING_CALENDAR',
        'ACT_ACTIVITY_REQUESTS',
        'ACT_POS_MATERIALS',
        'ACT_POS_LEND_OUT',
        'AB_AANKOOPBON'
    )
) AS src ON (tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId)
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead, src.CanWrite, src.CanEdit, src.CanDelete);
GO

-- GROUP B: Read-only for User
--   SuperAdmin/Admin: full  |  User: Access+Read (1,1,0,0,0)
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        (1, 1,1,1,1,1),
        (2, 1,1,1,1,1),
        (3, 1,1,0,0,0)
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

-- GROUP C: Admin/SuperAdmin only — User gets no access (0,0,0,0,0)
MERGE dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           r.CanAccess, r.CanRead, r.CanWrite, r.CanEdit, r.CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN (VALUES
        (1, 1,1,1,1,1),
        (2, 1,1,1,1,1),
        (3, 0,0,0,0,0)
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
        -- Activity config
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

-- ── Verify ────────────────────────────────────────────────────────────────────
SELECT
    m.ModuleName,
    COUNT(DISTINCT s.SubmoduleId) AS Submodules,
    COUNT(rp.PermissionId)        AS PermissionRows
FROM dbo.LM_Modules m
JOIN dbo.LM_Submodules s ON s.ModuleId = m.ModuleId
LEFT JOIN dbo.LM_RolePermissions rp ON rp.SubmoduleId = s.SubmoduleId
GROUP BY m.ModuleName
ORDER BY m.ModuleName;
GO

PRINT 'Script 24_FixMissingSubmodules.sql completed.';
GO
