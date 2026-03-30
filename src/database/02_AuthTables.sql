-- ============================================================
-- Licores Maduro - Auth & Security Tables
-- Script: 02_AuthTables.sql
-- Description: Creates authentication/authorization tables
--              and seeds initial data
-- ============================================================

USE LicoresMaduoDB;
GO

-- ============================================================
-- TABLE: LM_Roles
-- ============================================================
IF OBJECT_ID('dbo.LM_Roles', 'U') IS NOT NULL DROP TABLE dbo.LM_Roles;
GO

CREATE TABLE dbo.LM_Roles (
    RoleId      INT             NOT NULL IDENTITY(1,1),
    RoleName    NVARCHAR(50)    NOT NULL,
    Description NVARCHAR(200)   NULL,
    IsActive    BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_LM_Roles PRIMARY KEY CLUSTERED (RoleId),
    CONSTRAINT UQ_LM_Roles_RoleName UNIQUE (RoleName)
);
GO

-- ============================================================
-- TABLE: LM_Users
-- ============================================================
IF OBJECT_ID('dbo.LM_Users', 'U') IS NOT NULL DROP TABLE dbo.LM_Users;
GO

CREATE TABLE dbo.LM_Users (
    UserId       INT             NOT NULL IDENTITY(1,1),
    Username     NVARCHAR(50)    NOT NULL,
    PasswordHash NVARCHAR(256)   NOT NULL,
    Email        NVARCHAR(100)   NOT NULL,
    FullName     NVARCHAR(100)   NOT NULL,
    IsActive     BIT             NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2       NOT NULL DEFAULT GETDATE(),
    LastLogin    DATETIME2       NULL,
    RoleId       INT             NOT NULL,
    CONSTRAINT PK_LM_Users PRIMARY KEY CLUSTERED (UserId),
    CONSTRAINT UQ_LM_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_LM_Users_Email UNIQUE (Email),
    CONSTRAINT FK_LM_Users_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.LM_Roles(RoleId)
);
GO

CREATE INDEX IX_LM_Users_RoleId ON dbo.LM_Users (RoleId);
CREATE INDEX IX_LM_Users_IsActive ON dbo.LM_Users (IsActive);
GO

-- ============================================================
-- TABLE: LM_Modules
-- ============================================================
IF OBJECT_ID('dbo.LM_Modules', 'U') IS NOT NULL DROP TABLE dbo.LM_Modules;
GO

CREATE TABLE dbo.LM_Modules (
    ModuleId     INT             NOT NULL IDENTITY(1,1),
    ModuleName   NVARCHAR(100)   NOT NULL,
    ModuleCode   NVARCHAR(20)    NOT NULL,
    Icon         NVARCHAR(50)    NULL,
    DisplayOrder INT             NOT NULL DEFAULT 0,
    IsActive     BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_LM_Modules PRIMARY KEY CLUSTERED (ModuleId),
    CONSTRAINT UQ_LM_Modules_ModuleCode UNIQUE (ModuleCode)
);
GO

-- ============================================================
-- TABLE: LM_Submodules
-- ============================================================
IF OBJECT_ID('dbo.LM_Submodules', 'U') IS NOT NULL DROP TABLE dbo.LM_Submodules;
GO

CREATE TABLE dbo.LM_Submodules (
    SubmoduleId    INT             NOT NULL IDENTITY(1,1),
    ModuleId       INT             NOT NULL,
    SubmoduleName  NVARCHAR(100)   NOT NULL,
    SubmoduleCode  NVARCHAR(50)    NOT NULL,
    TableName      NVARCHAR(100)   NULL,
    DisplayOrder   INT             NOT NULL DEFAULT 0,
    IsActive       BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_LM_Submodules PRIMARY KEY CLUSTERED (SubmoduleId),
    CONSTRAINT FK_LM_Submodules_ModuleId FOREIGN KEY (ModuleId) REFERENCES dbo.LM_Modules(ModuleId)
);
GO

CREATE INDEX IX_LM_Submodules_ModuleId ON dbo.LM_Submodules (ModuleId);
GO

-- ============================================================
-- TABLE: LM_RolePermissions
-- ============================================================
IF OBJECT_ID('dbo.LM_RolePermissions', 'U') IS NOT NULL DROP TABLE dbo.LM_RolePermissions;
GO

CREATE TABLE dbo.LM_RolePermissions (
    PermissionId  INT   NOT NULL IDENTITY(1,1),
    RoleId        INT   NOT NULL,
    SubmoduleId   INT   NOT NULL,
    CanAccess     BIT   NOT NULL DEFAULT 0,
    CanRead       BIT   NOT NULL DEFAULT 0,
    CanWrite      BIT   NOT NULL DEFAULT 0,
    CanEdit       BIT   NOT NULL DEFAULT 0,
    CanDelete     BIT   NOT NULL DEFAULT 0,
    CONSTRAINT PK_LM_RolePermissions PRIMARY KEY CLUSTERED (PermissionId),
    CONSTRAINT FK_LM_RolePermissions_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.LM_Roles(RoleId),
    CONSTRAINT FK_LM_RolePermissions_SubmoduleId FOREIGN KEY (SubmoduleId) REFERENCES dbo.LM_Submodules(SubmoduleId),
    CONSTRAINT UQ_LM_RolePermissions UNIQUE (RoleId, SubmoduleId)
);
GO

CREATE INDEX IX_LM_RolePermissions_RoleId ON dbo.LM_RolePermissions (RoleId);
CREATE INDEX IX_LM_RolePermissions_SubmoduleId ON dbo.LM_RolePermissions (SubmoduleId);
GO

-- ============================================================
-- TABLE: LM_AuditLog
-- ============================================================
IF OBJECT_ID('dbo.LM_AuditLog', 'U') IS NOT NULL DROP TABLE dbo.LM_AuditLog;
GO

CREATE TABLE dbo.LM_AuditLog (
    LogId      BIGINT          NOT NULL IDENTITY(1,1),
    UserId     INT             NULL,
    Action     NVARCHAR(50)    NOT NULL,
    TableName  NVARCHAR(100)   NOT NULL,
    RecordId   NVARCHAR(50)    NULL,
    OldValues  NVARCHAR(MAX)   NULL,
    NewValues  NVARCHAR(MAX)   NULL,
    CreatedAt  DATETIME2       NOT NULL DEFAULT GETDATE(),
    IpAddress  NVARCHAR(50)    NULL,
    CONSTRAINT PK_LM_AuditLog PRIMARY KEY CLUSTERED (LogId)
);
GO

CREATE INDEX IX_LM_AuditLog_UserId    ON dbo.LM_AuditLog (UserId);
CREATE INDEX IX_LM_AuditLog_TableName ON dbo.LM_AuditLog (TableName);
CREATE INDEX IX_LM_AuditLog_CreatedAt ON dbo.LM_AuditLog (CreatedAt DESC);
GO

-- ============================================================
-- SEED DATA: Roles
-- ============================================================
SET IDENTITY_INSERT dbo.LM_Roles ON;
INSERT INTO dbo.LM_Roles (RoleId, RoleName, Description, IsActive) VALUES
(1, 'SuperAdmin',      'Full system access - unrestricted',                    1),
(2, 'Admin',           'Administrative access to all modules',                 1),
(3, 'TrackingManager', 'Manage Tracking module',                               1),
(4, 'FreightManager',  'Manage Freight Forwarder module',                      1),
(5, 'CostManager',     'Manage Cost Calculation module',                       1),
(6, 'ActivityManager', 'Manage Activity Request module',                       1),
(7, 'PurchaseManager', 'Manage Aankoopbon (Purchase) module',                  1),
(8, 'ReadOnly',        'Read-only access to permitted modules',                1);
SET IDENTITY_INSERT dbo.LM_Roles OFF;
GO

-- ============================================================
-- SEED DATA: Default SuperAdmin User
-- Password: Admin@123  (BCrypt hash placeholder - replace on first run)
-- ============================================================
SET IDENTITY_INSERT dbo.LM_Users ON;
INSERT INTO dbo.LM_Users (UserId, Username, PasswordHash, Email, FullName, IsActive, CreatedAt, RoleId) VALUES
(1, 'admin',
 '$2a$12$PLACEHOLDER_BCRYPT_HASH_REPLACE_ON_FIRST_RUN_XXXXXXXXXX',
 'admin@licoresmaduro.com',
 'System Administrator',
 1,
 GETDATE(),
 1);
SET IDENTITY_INSERT dbo.LM_Users OFF;
GO

-- ============================================================
-- SEED DATA: Modules
-- ============================================================
SET IDENTITY_INSERT dbo.LM_Modules ON;
INSERT INTO dbo.LM_Modules (ModuleId, ModuleName, ModuleCode, Icon, DisplayOrder, IsActive) VALUES
(1, 'Tracking',           'TRACKING',  'fa-shipping-fast', 1, 1),
(2, 'Freight Forwarder',  'FREIGHT',   'fa-ship',          2, 1),
(3, 'Cost Calculation',   'COST',      'fa-calculator',    3, 1),
(4, 'Route Assignment',   'ROUTE',     'fa-route',         4, 1),
(5, 'Stock Analysis',     'STOCK',     'fa-boxes',         5, 1),
(6, 'Activity Request',   'ACTIVITY',  'fa-tasks',         6, 1),
(7, 'Aankoopbon',         'PURCHASE',  'fa-file-invoice',  7, 1);
SET IDENTITY_INSERT dbo.LM_Modules OFF;
GO

-- ============================================================
-- SEED DATA: Submodules (66 total web-managed tables)
-- ============================================================
SET IDENTITY_INSERT dbo.LM_Submodules ON;

-- MODULE 1: Tracking (1 table)
INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive) VALUES
(1,  1, 'Order Status',                  'TRACKING_ORDER_STATUS',        'ORDER_STATUS',                  1,  1);

-- MODULE 2: Freight Forwarder (17 tables)
INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive) VALUES
(2,  2, 'Currencies',                    'FF_CURRENCIES',                'CURRENCIES',                    1,  1),
(3,  2, 'Load Types',                    'FF_LOADTYPES',                 'LOADTYPES',                     2,  1),
(4,  2, 'Port of Loading',               'FF_PORT_OF_LOADING',           'PORT_OF_LOADING',               3,  1),
(5,  2, 'Shipping Lines',                'FF_SHIPPING_LINES',            'SHIPPING_LINES',                4,  1),
(6,  2, 'Shipping Agents',               'FF_SHIPPING_AGENT',            'SHIPPING_AGENT',                5,  1),
(7,  2, 'Routes',                        'FF_ROUTES',                    'ROUTES',                        6,  1),
(8,  2, 'Container Specs',               'FF_CONTAINER_SPECS',           'CONTAINER_SPECS',               7,  1),
(9,  2, 'Container Types',               'FF_CONTAINER_TYPES',           'CONTAINER_TYPES',               8,  1),
(10, 2, 'Routes by Shipping Agents',     'FF_ROUTES_BY_SA',              'ROUTES_BY_SHIPPING_AGENTS',     9,  1),
(11, 2, 'Ocean Freight Charge Types',    'FF_OCEAN_FREIGHT_CHARGE',      'OCEAN_FREIGHT_CHARGE_TYPES',    10, 1),
(12, 2, 'Inland Freight Charge Types',   'FF_INLAND_FREIGHT_CHARGE',     'INLAND_FREIGHT_CHARGE_TYPES',   11, 1),
(13, 2, 'LCL Charge Types',              'FF_LCL_CHARGE',                'LCL_CHARGE_TYPES',              12, 1),
(14, 2, 'Price Types',                   'FF_PRICE_TYPE',                'PRICE_TYPE',                    13, 1),
(15, 2, 'Amount Types',                  'FF_AMOUNT_TYPE',               'AMOUNT_TYPE',                   14, 1),
(16, 2, 'Charge Actions',                'FF_CHARGE_ACTION',             'CHARGE_ACTION',                 15, 1),
(17, 2, 'Charge Over',                   'FF_CHARGE_OVER',               'CHARGE_OVER',                   16, 1),
(18, 2, 'Charge Per',                    'FF_CHARGE_PER',                'CHARGE_PER',                    17, 1);

-- MODULE 6: Activity Request (38 tables)
INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive) VALUES
(19, 6, 'Activity Types',                'ACT_ACTIVITY_TYPE',            'ACTIVITY_TYPE',                 1,  1),
(20, 6, 'Budget Activities',             'ACT_BUDGET_ACTIVITIES',        'BUDGET_ACTIVITIES',             2,  1),
(21, 6, 'Cat Additional Specs',          'ACT_CAT_ADD_SPECS',            'CAT_ADD_SPECS',                 3,  1),
(22, 6, 'Cat Apparel Types',             'ACT_CAT_APPAREL_TYPE',         'CAT_APPAREL_TYPE',              4,  1),
(23, 6, 'Cat Bag Specs',                 'ACT_CAT_BAG_SPECS',            'CAT_BAG_SPECS',                 5,  1),
(24, 6, 'Cat Bottles',                   'ACT_CAT_BOTTLES',              'CAT_BOTTLES',                   6,  1),
(25, 6, 'Cat Brand Specific',            'ACT_CAT_BRAND_SPECIFIC',       'CAT_BRAND_SPECIFIC',            7,  1),
(26, 6, 'Cat Clothing Types',            'ACT_CAT_CLOTHING_TYPE',        'CAT_CLOTHING_TYPE',             8,  1),
(27, 6, 'Cat Colors',                    'ACT_CAT_COLORS',               'CAT_COLORS',                    9,  1),
(28, 6, 'Cat Content',                   'ACT_CAT_CONTENT',              'CAT_CONTENT',                   10, 1),
(29, 6, 'Cat Cooler Capacity',           'ACT_CAT_COOLER_CAPACITY',      'CAT_COOLER_CAPACITY',           11, 1),
(30, 6, 'Cat Cooler Model',              'ACT_CAT_COOLER_MODEL',         'CAT_COOLER_MODEL',              12, 1),
(31, 6, 'Cat Cooler Types',              'ACT_CAT_COOLER_TYPE',          'CAT_COOLER_TYPE',               13, 1),
(32, 6, 'Cat File Names',                'ACT_CAT_FILE_NAMES',           'CAT_FILE_NAMES',                14, 1),
(33, 6, 'Cat Gender',                    'ACT_CAT_GENDER',               'CAT_GENDER',                    15, 1),
(34, 6, 'Cat Glass Serving',             'ACT_CAT_GLASS_SERVING',        'CAT_GLASS_SERVING',             16, 1),
(35, 6, 'Cat Insurance',                 'ACT_CAT_INSURRANCE',           'CAT_INSURRANCE',                17, 1),
(36, 6, 'Cat LED',                       'ACT_CAT_LED',                  'CAT_LED',                       18, 1),
(37, 6, 'Cat Maintenance Months',        'ACT_CAT_MAINT_MONTHS',         'CAT_MAINT_MONTHS',              19, 1),
(38, 6, 'Cat Materials',                 'ACT_CAT_MATERIALS',            'CAT_MATERIALS',                 20, 1),
(39, 6, 'Cat Shapes',                    'ACT_CAT_SHAPES',               'CAT_SHAPES',                    21, 1),
(40, 6, 'Cat Sizes',                     'ACT_CAT_SIZES',                'CAT_SIZES',                     22, 1),
(41, 6, 'Cat VAP Types',                 'ACT_CAT_VAP_TYPE',             'CAT_VAP_TYPE',                  23, 1),
(42, 6, 'Customer Non-Client',           'ACT_CUSTOMER_NON_CLIENT',      'CUSTOMER_NON_CLIENT',           24, 1),
(43, 6, 'Customer Sales Groups',         'ACT_CUSTOMER_SALES_GROUP',     'CUSTOMER_SALES_GROUP',          25, 1),
(44, 6, 'Customer Segment Info',         'ACT_CUSTOMER_SEGMENT_INFO',    'CUSTOMER_SEGMENT_INFO',         26, 1),
(45, 6, 'Customer Target Groups',        'ACT_CUSTOMER_TARGET_GROUP',    'CUSTOMER_TARGET_GROUP',         27, 1),
(46, 6, 'Denial Reasons',                'ACT_DENIAL_REASONS',           'DENIAL_REASONS',                28, 1),
(47, 6, 'Entertainment Types',           'ACT_ENTERTAINMENT_TYPE',       'ENTERTAINMENT_TYPE',            29, 1),
(48, 6, 'Facilitators Info',             'ACT_FACILITATORS_INFO',        'FACILITATORS_INFO',             30, 1),
(49, 6, 'Fiscal Years',                  'ACT_FISCAL_YEARS',             'FISCAL_YEARS',                  31, 1),
(50, 6, 'Licores Group',                 'ACT_LICORES_GROUP',            'LICORES_GROUP',                 32, 1),
(51, 6, 'Location Info',                 'ACT_LOCATION_INFO',            'LOCATION_INFO',                 33, 1),
(52, 6, 'POS Category',                  'ACT_POS_CATEGORY',             'POS_CATEGORY',                  34, 1),
(53, 6, 'POS Lend/Give',                 'ACT_POS_LEND_GIVE',            'POS_LEND_GIVE',                 35, 1),
(54, 6, 'POS Materials Status',          'ACT_POS_MATERIALS_STATUS',     'POS_MATERIALS_STATUS',          36, 1),
(55, 6, 'Sponsoring Types',              'ACT_SPONSORING_TYPE',          'SPONSORING_TYPE',               37, 1),
(56, 6, 'Status Codes',                  'ACT_STATUS_CODES',             'STATUS_CODES',                  38, 1);

-- MODULE 7: Aankoopbon (10 tables)
INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive) VALUES
(57, 7, 'AB Products',                   'AB_PRODUCTS_MGT',              'AB_PRODUCTS',                   1,  1),
(58, 7, 'Departments',                   'AB_DEPARTMENTS',               'DEPARTMENTS',                   2,  1),
(59, 7, 'Eenheden (Units)',               'AB_EENHEDEN',                  'EENHEDEN',                      3,  1),
(60, 7, 'Receivers',                     'AB_RECEIVERS',                 'RECEIVERS',                     4,  1),
(61, 7, 'Requestors',                    'AB_REQUESTORS',                'REQUESTORS',                    5,  1),
(62, 7, 'Requestors Vendor',             'AB_REQUESTORS_VENDOR',         'REQUESTORS_VENDOR',             6,  1),
(63, 7, 'Cost Types',                    'AB_COST_TYPE',                 'COST_TYPE',                     7,  1),
(64, 7, 'Vehicle Types',                 'AB_VEHICLE_TYPE',              'VEHICLE_TYPE',                  8,  1),
(65, 7, 'Vehicles',                      'AB_VEHICLES',                  'VEHICLES',                      9,  1),
(66, 7, 'Vendors',                       'AB_VENDORS',                   'VENDORS',                       10, 1);

SET IDENTITY_INSERT dbo.LM_Submodules OFF;
GO

-- ============================================================
-- SEED DATA: SuperAdmin gets full permissions on all submodules
-- ============================================================
INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT
    1 AS RoleId,
    SubmoduleId,
    1 AS CanAccess,
    1 AS CanRead,
    1 AS CanWrite,
    1 AS CanEdit,
    1 AS CanDelete
FROM dbo.LM_Submodules
WHERE IsActive = 1;
GO

-- Admin: full access except delete
INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT
    2 AS RoleId,
    SubmoduleId,
    1 AS CanAccess,
    1 AS CanRead,
    1 AS CanWrite,
    1 AS CanEdit,
    0 AS CanDelete
FROM dbo.LM_Submodules
WHERE IsActive = 1;
GO

-- ReadOnly: read access on all submodules
INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT
    8 AS RoleId,
    SubmoduleId,
    1 AS CanAccess,
    1 AS CanRead,
    0 AS CanWrite,
    0 AS CanEdit,
    0 AS CanDelete
FROM dbo.LM_Submodules
WHERE IsActive = 1;
GO

PRINT 'Auth tables and seed data created successfully.';
GO
