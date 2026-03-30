-- ============================================================
-- 03_WebManagedTables.sql
-- Seeds Modules and Submodules for all application modules
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── Modules seed ─────────────────────────────────────────────
MERGE LM_Modules AS target
USING (VALUES
    ('Tracking',          'TRACKING',  'fa-shipping-fast', 1),
    ('Freight Forwarder', 'FREIGHT',   'fa-ship',          2),
    ('Cost Calculation',  'COST',      'fa-calculator',    3),
    ('Route Assignment',  'ROUTE',     'fa-route',         4),
    ('Stock Analysis',    'STOCK',     'fa-boxes',         5),
    ('Activity Request',  'ACTIVITY',  'fa-tasks',         6),
    ('Aankoopbon',        'PURCHASE',  'fa-file-invoice',  7)
) AS source (ModuleName, ModuleCode, Icon, DisplayOrder)
ON target.ModuleCode = source.ModuleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleName, ModuleCode, Icon, DisplayOrder)
    VALUES (source.ModuleName, source.ModuleCode, source.Icon, source.DisplayOrder);
GO

-- ── Submodules seed ──────────────────────────────────────────
DECLARE @TrackingId  INT = (SELECT ModuleId FROM LM_Modules WHERE ModuleCode = 'TRACKING');
DECLARE @FreightId   INT = (SELECT ModuleId FROM LM_Modules WHERE ModuleCode = 'FREIGHT');
DECLARE @CostId      INT = (SELECT ModuleId FROM LM_Modules WHERE ModuleCode = 'COST');
DECLARE @RouteId     INT = (SELECT ModuleId FROM LM_Modules WHERE ModuleCode = 'ROUTE');
DECLARE @StockId     INT = (SELECT ModuleId FROM LM_Modules WHERE ModuleCode = 'STOCK');
DECLARE @ActivityId  INT = (SELECT ModuleId FROM LM_Modules WHERE ModuleCode = 'ACTIVITY');
DECLARE @PurchaseId  INT = (SELECT ModuleId FROM LM_Modules WHERE ModuleCode = 'PURCHASE');

-- Tracking submodules
MERGE LM_Submodules AS target
USING (VALUES
    (@TrackingId, 'Purchase Order Tracking',  'TRACKING_ORDERS',            'TRACKING_ORDERS',            1),
    (@TrackingId, 'Order Status Codes',       'TRACKING_ORDER_STATUS',       'ORDER_STATUS',               2),
    (@TrackingId, 'Container Types',          'TRACKING_CONTAINER_TYPES',    'TRACKING_CONTAINER_TYPES',   3),

    -- Freight submodules
    (@FreightId,  'Currencies',               'FF_CURRENCIES',               'CURRENCIES',                 1),
    (@FreightId,  'Load Types',               'FF_LOADTYPES',                'LOADTYPES',                  2),
    (@FreightId,  'Ports of Loading',         'FF_PORT_OF_LOADING',          'PORT_OF_LOADING',            3),
    (@FreightId,  'Shipping Lines',           'FF_SHIPPING_LINES',           'SHIPPING_LINES',             4),
    (@FreightId,  'Shipping Agents',          'FF_SHIPPING_AGENT',           'SHIPPING_AGENT',             5),
    (@FreightId,  'Routes',                   'FF_ROUTES',                   'ROUTES',                     6),
    (@FreightId,  'Container Specs',          'FF_CONTAINER_SPECS',          'CONTAINER_SPECS',            7),
    (@FreightId,  'Container Types',          'FF_CONTAINER_TYPES',          'CONTAINER_TYPES',            8),
    (@FreightId,  'Routes by Shipping Agent', 'FF_ROUTES_BY_SA',             'ROUTES_BY_SHIPPING_AGENTS',  9),
    (@FreightId,  'Ocean Freight Charges',    'FF_OCEAN_FREIGHT_CHARGE',     'FF_OCEAN_FREIGHT_PORT_SLINE_CHARGES', 10),
    (@FreightId,  'Inland Freight Charges',   'FF_INLAND_FREIGHT_CHARGE',    'FF_INLAND_FREIGHT_REGION_TYPE_DET',   11),
    (@FreightId,  'LCL Charge Types',         'FF_LCL_CHARGE',               'LCL_CHARGE_TYPES',           12),
    (@FreightId,  'Price Types',              'FF_PRICE_TYPE',               'PRICE_TYPE',                 13),
    (@FreightId,  'Amount Types',             'FF_AMOUNT_TYPE',              'AMOUNT_TYPE',                14),
    (@FreightId,  'Charge Actions',           'FF_CHARGE_ACTION',            'CHARGE_ACTION',              15),
    (@FreightId,  'Charge Over',              'FF_CHARGE_OVER',              'CHARGE_OVER',                16),
    (@FreightId,  'Charge Per',               'FF_CHARGE_PER',               'CHARGE_PER',                 17),
    (@FreightId,  'Freight Forwarders',       'FF_FORWARDERS',               'FREIGHT_FORWARDERS',         18),
    (@FreightId,  'Ocean Freight Quotes',     'FF_OCEAN_QUOTES',             'FF_OCEAN_FREIGHT_HEADER',    19),
    (@FreightId,  'Inland Freight Quotes',    'FF_INLAND_QUOTES',            'FF_INLAND_FREIGHT_HEADER',   20),
    (@FreightId,  'LCL Quotes',               'FF_LCL_QUOTES',               'FF_LCL_HEADER',              21),
    (@FreightId,  'Inland Additional Charges','FF_INLAND_ADD_CHARGES',       'FF_INLAND_ADDITIONAL_CHARGES',22),

    -- Cost Calculation submodules
    (@CostId,     'Calculations',             'COST_CALCULATIONS',           'COST_CALC_FIN',              1),
    (@CostId,     'New Calculation',          'COST_NEW_CALC',               'COST_CALC_FIN',              2),
    (@CostId,     'Purchase Orders',          'COST_PO_LOOKUP',              NULL,                         3),

    -- Route Assignment submodules
    (@RouteId,    'Customer Ext Dimensions',  'ROUTE_CUSTOMER_EXT',          'ROUTE_CUSTOMER_EXT',         1),
    (@RouteId,    'Product Ext Dimensions',   'ROUTE_PRODUCT_EXT',           'ROUTE_PRODUCT_EXT',          2),
    (@RouteId,    'Budget',                   'ROUTE_BUDGET',                'ROUTE_BUDGET',               3),
    (@RouteId,    'Reports',                  'ROUTE_REPORTS',               NULL,                         4),
    (@RouteId,    'Dimensions Viewer',        'ROUTE_DIMENSIONS',            NULL,                         5),

    -- Stock Analysis submodules
    (@StockId,    'Ideal Months Config',      'STOCK_IDEAL_MONTHS',          'STOCK_IDEAL_MONTHS',         1),
    (@StockId,    'Vendor Constraints',       'STOCK_VENDOR_CONSTRAINTS',    'STOCK_VENDOR_CONSTRAINTS',   2),
    (@StockId,    'Sales Budget',             'STOCK_SALES_BUDGET',          'STOCK_SALES_BUDGET',         3),
    (@StockId,    'Generate Analysis',        'STOCK_ANALYSIS',              NULL,                         4),
    (@StockId,    'Analysis Results',         'STOCK_ANALYSIS_RESULTS',      'STOCK_ANALYSIS_RESULT',      5),

    -- Activity Request submodules
    (@ActivityId, 'Activity Types',           'ACT_ACTIVITY_TYPE',           'ACTIVITY_TYPE',              1),
    (@ActivityId, 'Budget Activities',        'ACT_BUDGET_ACTIVITIES',       'BUDGET_ACTIVITIES',          2),
    (@ActivityId, 'Denial Reasons',           'ACT_DENIAL_REASONS',          'DENIAL_REASONS',             3),
    (@ActivityId, 'Entertainment Types',      'ACT_ENTERTAINMENT_TYPE',      'ENTERTAINMENT_TYPE',         4),
    (@ActivityId, 'Status Codes',             'ACT_STATUS_CODES',            'STATUS_CODES',               5),
    (@ActivityId, 'Sponsoring Types',         'ACT_SPONSORING_TYPE',         'SPONSORING_TYPE',            6),
    (@ActivityId, 'Fiscal Years',             'ACT_FISCAL_YEARS',            'FISCAL_YEARS',               7),
    (@ActivityId, 'Licores Group',            'ACT_LICORES_GROUP',           'LICORES_GROUP',              8),
    (@ActivityId, 'POS Category',             'ACT_POS_CATEGORY',            'POS_CATEGORY',               9),
    (@ActivityId, 'POS Lend/Give',            'ACT_POS_LEND_GIVE',           'POS_LEND_GIVE',              10),
    (@ActivityId, 'POS Materials Status',     'ACT_POS_MATERIALS_STATUS',    'POS_MATERIALS_STATUS',       11),
    (@ActivityId, 'Customer Non-Client',      'ACT_CUSTOMER_NON_CLIENT',     'CUSTOMER_NON_CLIENT',        12),
    (@ActivityId, 'Customer Sales Groups',    'ACT_CUSTOMER_SALES_GROUP',    'CUSTOMER_SALES_GROUP',       13),
    (@ActivityId, 'Customer Segments',        'ACT_CUSTOMER_SEGMENT_INFO',   'CUSTOMER_SEGMENT_INFO',      14),
    (@ActivityId, 'Customer Target Groups',   'ACT_CUSTOMER_TARGET_GROUP',   'CUSTOMER_TARGET_GROUP',      15),
    (@ActivityId, 'Facilitators',             'ACT_FACILITATORS_INFO',       'FACILITATORS_INFO',          16),
    (@ActivityId, 'Location Info',            'ACT_LOCATION_INFO',           'LOCATION_INFO',              17),
    (@ActivityId, 'Cat Additional Specs',     'ACT_CAT_ADD_SPECS',           'CAT_ADD_SPECS',              18),
    (@ActivityId, 'Cat Apparel Types',        'ACT_CAT_APPAREL_TYPE',        'CAT_APPAREL_TYPE',           19),
    (@ActivityId, 'Cat Bag Specs',            'ACT_CAT_BAG_SPECS',           'CAT_BAG_SPECS',              20),
    (@ActivityId, 'Cat Bottles',              'ACT_CAT_BOTTLES',             'CAT_BOTTLES',                21),
    (@ActivityId, 'Cat Brand Specific',       'ACT_CAT_BRAND_SPECIFIC',      'CAT_BRAND_SPECIFIC',         22),
    (@ActivityId, 'Cat Clothing Types',       'ACT_CAT_CLOTHING_TYPE',       'CAT_CLOTHING_TYPE',          23),
    (@ActivityId, 'Cat Colors',               'ACT_CAT_COLORS',              'CAT_COLORS',                 24),
    (@ActivityId, 'Cat Content',              'ACT_CAT_CONTENT',             'CAT_CONTENT',                25),
    (@ActivityId, 'Cat Cooler Capacity',      'ACT_CAT_COOLER_CAPACITY',     'CAT_COOLER_CAPACITY',        26),
    (@ActivityId, 'Cat Cooler Model',         'ACT_CAT_COOLER_MODEL',        'CAT_COOLER_MODEL',           27),
    (@ActivityId, 'Cat Cooler Types',         'ACT_CAT_COOLER_TYPE',         'CAT_COOLER_TYPE',            28),
    (@ActivityId, 'Cat File Names',           'ACT_CAT_FILE_NAMES',          'CAT_FILE_NAMES',             29),
    (@ActivityId, 'Cat Gender',               'ACT_CAT_GENDER',              'CAT_GENDER',                 30),
    (@ActivityId, 'Cat Glass Serving',        'ACT_CAT_GLASS_SERVING',       'CAT_GLASS_SERVING',          31),
    (@ActivityId, 'Cat Insurance',            'ACT_CAT_INSURRANCE',          'CAT_INSURRANCE',             32),
    (@ActivityId, 'Cat LED',                  'ACT_CAT_LED',                 'CAT_LED',                    33),
    (@ActivityId, 'Cat Maintenance Months',   'ACT_CAT_MAINT_MONTHS',        'CAT_MAINT_MONTHS',           34),
    (@ActivityId, 'Cat Materials',            'ACT_CAT_MATERIALS',           'CAT_MATERIALS',              35),
    (@ActivityId, 'Cat Shapes',               'ACT_CAT_SHAPES',              'CAT_SHAPES',                 36),
    (@ActivityId, 'Cat Sizes',                'ACT_CAT_SIZES',               'CAT_SIZES',                  37),
    (@ActivityId, 'Cat VAP Types',            'ACT_CAT_VAP_TYPE',            'CAT_VAP_TYPE',               38),

    -- Aankoopbon submodules
    (@PurchaseId, 'Vendors',                  'AB_VENDORS',                  'VENDORS',                    1),
    (@PurchaseId, 'Departments',              'AB_DEPARTMENTS',              'DEPARTMENTS',                2),
    (@PurchaseId, 'Eenheden (Units)',          'AB_EENHEDEN',                 'EENHEDEN',                   3),
    (@PurchaseId, 'Receivers',                'AB_RECEIVERS',                'RECEIVERS',                  4),
    (@PurchaseId, 'Requestors',               'AB_REQUESTORS',               'REQUESTORS',                 5),
    (@PurchaseId, 'Requestors / Vendor',      'AB_REQUESTORS_VENDOR',        'REQUESTORS_VENDOR',          6),
    (@PurchaseId, 'Cost Types',               'AB_COST_TYPE',                'COST_TYPE',                  7),
    (@PurchaseId, 'Vehicle Types',            'AB_VEHICLE_TYPE',             'VEHICLE_TYPE',               8),
    (@PurchaseId, 'Vehicles',                 'AB_VEHICLES',                 'VEHICLES',                   9),
    (@PurchaseId, 'AB Products',              'AB_PRODUCTS_MGT',             'AB_PRODUCTS',                10)
) AS source (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
ON target.SubmoduleCode = source.SubmoduleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (source.ModuleId, source.SubmoduleName, source.SubmoduleCode, source.TableName, source.DisplayOrder);
GO
