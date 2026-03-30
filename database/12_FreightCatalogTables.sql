-- ============================================================
-- Licores Maduro - Freight Forwarder Catalog Tables
-- Script: 12_FreightCatalogTables.sql
-- Run on: LicoresMaduoDB
-- Creates: Currencies, LoadTypes, PortsOfLoading, ShippingLines,
--          ShippingAgent, Routes, ContainerSpecs, ContainerTypes,
--          RoutesByShippingAgents, OceanFreightChargeTypes,
--          InlandFreightChargeTypes, LclChargeTypes, PriceType,
--          AmountType, ChargeAction, ChargeOver, ChargePer
-- ============================================================

USE LicoresMaduoDB;
GO

-- ============================================================
-- Drop existing tables (reverse dependency order)
-- ============================================================
IF OBJECT_ID('dbo.ROUTES_BY_SHIPPING_AGENTS', 'U') IS NOT NULL DROP TABLE dbo.ROUTES_BY_SHIPPING_AGENTS;
IF OBJECT_ID('dbo.CONTAINER_TYPES',           'U') IS NOT NULL DROP TABLE dbo.CONTAINER_TYPES;
IF OBJECT_ID('dbo.CONTAINER_SPECS',           'U') IS NOT NULL DROP TABLE dbo.CONTAINER_SPECS;
IF OBJECT_ID('dbo.ROUTES',                    'U') IS NOT NULL DROP TABLE dbo.ROUTES;
IF OBJECT_ID('dbo.SHIPPING_AGENT',            'U') IS NOT NULL DROP TABLE dbo.SHIPPING_AGENT;
IF OBJECT_ID('dbo.SHIPPING_LINES',            'U') IS NOT NULL DROP TABLE dbo.SHIPPING_LINES;
IF OBJECT_ID('dbo.PORT_OF_LOADING',           'U') IS NOT NULL DROP TABLE dbo.PORT_OF_LOADING;
IF OBJECT_ID('dbo.LOADTYPES',                 'U') IS NOT NULL DROP TABLE dbo.LOADTYPES;
IF OBJECT_ID('dbo.CURRENCIES',                'U') IS NOT NULL DROP TABLE dbo.CURRENCIES;
IF OBJECT_ID('dbo.OCEAN_FREIGHT_CHARGE_TYPES','U') IS NOT NULL DROP TABLE dbo.OCEAN_FREIGHT_CHARGE_TYPES;
IF OBJECT_ID('dbo.INLAND_FREIGHT_CHARGE_TYPES','U') IS NOT NULL DROP TABLE dbo.INLAND_FREIGHT_CHARGE_TYPES;
IF OBJECT_ID('dbo.LCL_CHARGE_TYPES',          'U') IS NOT NULL DROP TABLE dbo.LCL_CHARGE_TYPES;
IF OBJECT_ID('dbo.PRICE_TYPE',                'U') IS NOT NULL DROP TABLE dbo.PRICE_TYPE;
IF OBJECT_ID('dbo.AMOUNT_TYPE',               'U') IS NOT NULL DROP TABLE dbo.AMOUNT_TYPE;
IF OBJECT_ID('dbo.CHARGE_ACTION',             'U') IS NOT NULL DROP TABLE dbo.CHARGE_ACTION;
IF OBJECT_ID('dbo.CHARGE_OVER',               'U') IS NOT NULL DROP TABLE dbo.CHARGE_OVER;
IF OBJECT_ID('dbo.CHARGE_PER',                'U') IS NOT NULL DROP TABLE dbo.CHARGE_PER;
GO

-- ============================================================
-- 1. CURRENCIES
-- ============================================================
CREATE TABLE dbo.CURRENCIES (
    CUR_Id                  INT             NOT NULL IDENTITY(1,1),
    CUR_CODE                NVARCHAR(3)     NOT NULL,
    CUR_DESCRIPTION         NVARCHAR(30)    NOT NULL,
    CUR_BNK_PURCHASE_RATE   FLOAT           NULL,
    CUR_CUSTOMS_RATE        FLOAT           NULL,
    IsActive                BIT             NOT NULL DEFAULT 1,
    CreatedAt               DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_CURRENCIES        PRIMARY KEY CLUSTERED (CUR_Id),
    CONSTRAINT UQ_CURRENCIES_CODE   UNIQUE (CUR_CODE)
);
GO

-- ============================================================
-- 2. LOADTYPES
-- ============================================================
CREATE TABLE dbo.LOADTYPES (
    LT_Id           INT             NOT NULL IDENTITY(1,1),
    LT_CODE         NVARCHAR(6)     NOT NULL,
    LT_DESCRIPTION  NVARCHAR(25)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_LOADTYPES         PRIMARY KEY CLUSTERED (LT_Id),
    CONSTRAINT UQ_LOADTYPES_CODE    UNIQUE (LT_CODE)
);
GO

-- ============================================================
-- 3. PORT_OF_LOADING
-- ============================================================
CREATE TABLE dbo.PORT_OF_LOADING (
    PL_Id       INT             NOT NULL IDENTITY(1,1),
    PL_CODE     NVARCHAR(10)    NOT NULL,
    PL_NAME     NVARCHAR(30)    NOT NULL,
    PL_COUNTRY  NVARCHAR(3)     NULL,
    IsActive    BIT             NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_PORT_OF_LOADING       PRIMARY KEY CLUSTERED (PL_Id),
    CONSTRAINT UQ_PORT_OF_LOADING_CODE  UNIQUE (PL_CODE)
);
GO

-- ============================================================
-- 4. SHIPPING_LINES
-- ============================================================
CREATE TABLE dbo.SHIPPING_LINES (
    SL_Id       INT             NOT NULL IDENTITY(1,1),
    SL_CODE     NVARCHAR(10)    NOT NULL,
    SL_NAME     NVARCHAR(30)    NOT NULL,
    IsActive    BIT             NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_SHIPPING_LINES        PRIMARY KEY CLUSTERED (SL_Id),
    CONSTRAINT UQ_SHIPPING_LINES_CODE   UNIQUE (SL_CODE)
);
GO

-- ============================================================
-- 5. SHIPPING_AGENT
-- ============================================================
CREATE TABLE dbo.SHIPPING_AGENT (
    SA_Id       INT             NOT NULL IDENTITY(1,1),
    SA_CODE     NVARCHAR(10)    NOT NULL,
    SA_NAME     NVARCHAR(30)    NOT NULL,
    IsActive    BIT             NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_SHIPPING_AGENT        PRIMARY KEY CLUSTERED (SA_Id),
    CONSTRAINT UQ_SHIPPING_AGENT_CODE   UNIQUE (SA_CODE)
);
GO

-- ============================================================
-- 6. ROUTES
-- ============================================================
CREATE TABLE dbo.ROUTES (
    ROU_Id          INT             NOT NULL IDENTITY(1,1),
    ROU_CODE        NVARCHAR(15)    NOT NULL,
    ROU_DESCRIPTION NVARCHAR(30)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_ROUTES        PRIMARY KEY CLUSTERED (ROU_Id),
    CONSTRAINT UQ_ROUTES_CODE   UNIQUE (ROU_CODE)
);
GO

-- ============================================================
-- 7. CONTAINER_SPECS
-- ============================================================
CREATE TABLE dbo.CONTAINER_SPECS (
    CS_Id           INT             NOT NULL IDENTITY(1,1),
    CS_CODE         NVARCHAR(6)     NOT NULL,
    CS_DESCRIPTION  NVARCHAR(25)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_CONTAINER_SPECS       PRIMARY KEY CLUSTERED (CS_Id),
    CONSTRAINT UQ_CONTAINER_SPECS_CODE  UNIQUE (CS_CODE)
);
GO

-- ============================================================
-- 8. CONTAINER_TYPES
-- ============================================================
CREATE TABLE dbo.CONTAINER_TYPES (
    CT_Id               INT             NOT NULL IDENTITY(1,1),
    CT_CODE             NVARCHAR(6)     NOT NULL,
    CT_DESCRIPTION      NVARCHAR(50)    NOT NULL,
    CT_CONTAINER_SPECS  NVARCHAR(6)     NULL,
    CT_Cases            INT             NULL,
    CT_WGHT_Kilogram    INT             NULL,
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_CONTAINER_TYPES       PRIMARY KEY CLUSTERED (CT_Id),
    CONSTRAINT UQ_CONTAINER_TYPES_CODE  UNIQUE (CT_CODE)
);
GO

-- ============================================================
-- 9. ROUTES_BY_SHIPPING_AGENTS
-- ============================================================
CREATE TABLE dbo.ROUTES_BY_SHIPPING_AGENTS (
    RSA_Id              INT             NOT NULL IDENTITY(1,1),
    RSA_PORT            NVARCHAR(10)    NOT NULL,
    RSA_SHIPPING_AGENT  NVARCHAR(25)    NOT NULL,
    RSA_ROUTE           NVARCHAR(10)    NOT NULL,
    RSA_DAYS            SMALLINT        NULL,
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_ROUTES_BY_SHIPPING_AGENTS PRIMARY KEY CLUSTERED (RSA_Id)
);
GO

CREATE INDEX IX_RSA_Port  ON dbo.ROUTES_BY_SHIPPING_AGENTS (RSA_PORT);
CREATE INDEX IX_RSA_Agent ON dbo.ROUTES_BY_SHIPPING_AGENTS (RSA_SHIPPING_AGENT);
GO

-- ============================================================
-- 10. OCEAN_FREIGHT_CHARGE_TYPES
-- ============================================================
CREATE TABLE dbo.OCEAN_FREIGHT_CHARGE_TYPES (
    OFCT_Id          INT             NOT NULL IDENTITY(1,1),
    OFCT_CODE        NVARCHAR(6)     NOT NULL,
    OFCT_DESCRIPTION NVARCHAR(25)    NOT NULL,
    IsActive         BIT             NOT NULL DEFAULT 1,
    CreatedAt        DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_OCEAN_FREIGHT_CHARGE_TYPES        PRIMARY KEY CLUSTERED (OFCT_Id),
    CONSTRAINT UQ_OCEAN_FREIGHT_CHARGE_TYPES_CODE   UNIQUE (OFCT_CODE)
);
GO

-- ============================================================
-- 11. INLAND_FREIGHT_CHARGE_TYPES
-- ============================================================
CREATE TABLE dbo.INLAND_FREIGHT_CHARGE_TYPES (
    IFCT_Id          INT             NOT NULL IDENTITY(1,1),
    IFCT_CODE        NVARCHAR(6)     NOT NULL,
    IFCT_DESCRIPTION NVARCHAR(25)    NOT NULL,
    IsActive         BIT             NOT NULL DEFAULT 1,
    CreatedAt        DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_INLAND_FREIGHT_CHARGE_TYPES       PRIMARY KEY CLUSTERED (IFCT_Id),
    CONSTRAINT UQ_INLAND_FREIGHT_CHARGE_TYPES_CODE  UNIQUE (IFCT_CODE)
);
GO

-- ============================================================
-- 12. LCL_CHARGE_TYPES
-- ============================================================
CREATE TABLE dbo.LCL_CHARGE_TYPES (
    LCT_Id          INT             NOT NULL IDENTITY(1,1),
    LCT_CODE        NVARCHAR(6)     NOT NULL,
    LCT_DESCRIPTION NVARCHAR(25)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_LCL_CHARGE_TYPES      PRIMARY KEY CLUSTERED (LCT_Id),
    CONSTRAINT UQ_LCL_CHARGE_TYPES_CODE UNIQUE (LCT_CODE)
);
GO

-- ============================================================
-- 13. PRICE_TYPE
-- ============================================================
CREATE TABLE dbo.PRICE_TYPE (
    PT_Id           INT             NOT NULL IDENTITY(1,1),
    PT_CODE         NVARCHAR(6)     NOT NULL,
    PT_DESCRIPTION  NVARCHAR(25)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_PRICE_TYPE        PRIMARY KEY CLUSTERED (PT_Id),
    CONSTRAINT UQ_PRICE_TYPE_CODE   UNIQUE (PT_CODE)
);
GO

-- ============================================================
-- 14. AMOUNT_TYPE
-- ============================================================
CREATE TABLE dbo.AMOUNT_TYPE (
    AT_Id           INT             NOT NULL IDENTITY(1,1),
    AT_CODE         NVARCHAR(1)     NOT NULL,
    AT_DESCRIPTION  NVARCHAR(25)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_AMOUNT_TYPE       PRIMARY KEY CLUSTERED (AT_Id),
    CONSTRAINT UQ_AMOUNT_TYPE_CODE  UNIQUE (AT_CODE)
);
GO

-- ============================================================
-- 15. CHARGE_ACTION
-- ============================================================
CREATE TABLE dbo.CHARGE_ACTION (
    CA_Id           INT             NOT NULL IDENTITY(1,1),
    CA_CODE         NVARCHAR(6)     NOT NULL,
    CA_DESCRIPTION  NVARCHAR(25)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_CHARGE_ACTION      PRIMARY KEY CLUSTERED (CA_Id),
    CONSTRAINT UQ_CHARGE_ACTION_CODE UNIQUE (CA_CODE)
);
GO

-- ============================================================
-- 16. CHARGE_OVER
-- ============================================================
CREATE TABLE dbo.CHARGE_OVER (
    CO_Id           INT             NOT NULL IDENTITY(1,1),
    CO_CODE         NVARCHAR(6)     NOT NULL,
    CO_DESCRIPTION  NVARCHAR(25)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_CHARGE_OVER       PRIMARY KEY CLUSTERED (CO_Id),
    CONSTRAINT UQ_CHARGE_OVER_CODE  UNIQUE (CO_CODE)
);
GO

-- ============================================================
-- 17. CHARGE_PER
-- ============================================================
CREATE TABLE dbo.CHARGE_PER (
    CP_Id           INT             NOT NULL IDENTITY(1,1),
    CP_CODE         NVARCHAR(6)     NOT NULL,
    CP_DESCRIPTION  NVARCHAR(25)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_CHARGE_PER        PRIMARY KEY CLUSTERED (CP_Id),
    CONSTRAINT UQ_CHARGE_PER_CODE   UNIQUE (CP_CODE)
);
GO

-- ============================================================
-- SUBMODULE REGISTRATION (Module 2 = FREIGHT)
-- ============================================================
-- Remove old entries if they exist (for idempotency)
DELETE FROM dbo.LM_RolePermissions
WHERE SubmoduleId IN (
    SELECT SubmoduleId FROM dbo.LM_Submodules
    WHERE SubmoduleCode IN (
        'FF_CURRENCIES','FF_LOADTYPES','FF_PORT_OF_LOADING',
        'FF_SHIPPING_LINES','FF_SHIPPING_AGENTS','FF_ROUTES',
        'FF_CONTAINER_SPECS','FF_CONTAINER_TYPES','FF_ROUTES_BY_SA',
        'FF_OCEAN_FREIGHT_CHARGE','FF_INLAND_FREIGHT_CHARGE','FF_LCL_CHARGE',
        'FF_PRICE_TYPE','FF_AMOUNT_TYPE','FF_CHARGE_ACTION',
        'FF_CHARGE_OVER','FF_CHARGE_PER'
    )
);
DELETE FROM dbo.LM_Submodules
WHERE SubmoduleCode IN (
    'FF_CURRENCIES','FF_LOADTYPES','FF_PORT_OF_LOADING',
    'FF_SHIPPING_LINES','FF_SHIPPING_AGENTS','FF_ROUTES',
    'FF_CONTAINER_SPECS','FF_CONTAINER_TYPES','FF_ROUTES_BY_SA',
    'FF_OCEAN_FREIGHT_CHARGE','FF_INLAND_FREIGHT_CHARGE','FF_LCL_CHARGE',
    'FF_PRICE_TYPE','FF_AMOUNT_TYPE','FF_CHARGE_ACTION',
    'FF_CHARGE_OVER','FF_CHARGE_PER'
);
GO

INSERT INTO dbo.LM_Submodules (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive) VALUES
(2, 'Currencies',                 'FF_CURRENCIES',            'CURRENCIES',                   23, 1),
(2, 'Load Types',                 'FF_LOADTYPES',             'LOADTYPES',                    24, 1),
(2, 'Ports of Loading',           'FF_PORT_OF_LOADING',       'PORT_OF_LOADING',              25, 1),
(2, 'Shipping Lines',             'FF_SHIPPING_LINES',        'SHIPPING_LINES',               26, 1),
(2, 'Shipping Agents',            'FF_SHIPPING_AGENTS',       'SHIPPING_AGENT',               27, 1),
(2, 'Routes',                     'FF_ROUTES',                'ROUTES',                       28, 1),
(2, 'Container Specs',            'FF_CONTAINER_SPECS',       'CONTAINER_SPECS',              29, 1),
(2, 'Container Types',            'FF_CONTAINER_TYPES',       'CONTAINER_TYPES',              30, 1),
(2, 'Routes by Shipping Agent',   'FF_ROUTES_BY_SA',          'ROUTES_BY_SHIPPING_AGENTS',    31, 1),
(2, 'Ocean Freight Charge Types', 'FF_OCEAN_FREIGHT_CHARGE',  'OCEAN_FREIGHT_CHARGE_TYPES',   32, 1),
(2, 'Inland Freight Charge Types','FF_INLAND_FREIGHT_CHARGE', 'INLAND_FREIGHT_CHARGE_TYPES',  33, 1),
(2, 'LCL Charge Types',           'FF_LCL_CHARGE',            'LCL_CHARGE_TYPES',             34, 1),
(2, 'Price Types',                'FF_PRICE_TYPE',            'PRICE_TYPE',                   35, 1),
(2, 'Amount Types',               'FF_AMOUNT_TYPE',           'AMOUNT_TYPE',                  36, 1),
(2, 'Charge Actions',             'FF_CHARGE_ACTION',         'CHARGE_ACTION',                37, 1),
(2, 'Charge Over',                'FF_CHARGE_OVER',           'CHARGE_OVER',                  38, 1),
(2, 'Charge Per',                 'FF_CHARGE_PER',            'CHARGE_PER',                   39, 1);
GO

-- SuperAdmin (RoleId=1): full access
INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT 1, SubmoduleId, 1, 1, 1, 1, 1
FROM dbo.LM_Submodules
WHERE SubmoduleCode IN (
    'FF_CURRENCIES','FF_LOADTYPES','FF_PORT_OF_LOADING',
    'FF_SHIPPING_LINES','FF_SHIPPING_AGENTS','FF_ROUTES',
    'FF_CONTAINER_SPECS','FF_CONTAINER_TYPES','FF_ROUTES_BY_SA',
    'FF_OCEAN_FREIGHT_CHARGE','FF_INLAND_FREIGHT_CHARGE','FF_LCL_CHARGE',
    'FF_PRICE_TYPE','FF_AMOUNT_TYPE','FF_CHARGE_ACTION',
    'FF_CHARGE_OVER','FF_CHARGE_PER'
);
GO

-- Admin (RoleId=2): full access except delete
INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT 2, SubmoduleId, 1, 1, 1, 1, 0
FROM dbo.LM_Submodules
WHERE SubmoduleCode IN (
    'FF_CURRENCIES','FF_LOADTYPES','FF_PORT_OF_LOADING',
    'FF_SHIPPING_LINES','FF_SHIPPING_AGENTS','FF_ROUTES',
    'FF_CONTAINER_SPECS','FF_CONTAINER_TYPES','FF_ROUTES_BY_SA',
    'FF_OCEAN_FREIGHT_CHARGE','FF_INLAND_FREIGHT_CHARGE','FF_LCL_CHARGE',
    'FF_PRICE_TYPE','FF_AMOUNT_TYPE','FF_CHARGE_ACTION',
    'FF_CHARGE_OVER','FF_CHARGE_PER'
);
GO

-- ReadOnly (RoleId=8): read only
INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT 8, SubmoduleId, 1, 1, 0, 0, 0
FROM dbo.LM_Submodules
WHERE SubmoduleCode IN (
    'FF_CURRENCIES','FF_LOADTYPES','FF_PORT_OF_LOADING',
    'FF_SHIPPING_LINES','FF_SHIPPING_AGENTS','FF_ROUTES',
    'FF_CONTAINER_SPECS','FF_CONTAINER_TYPES','FF_ROUTES_BY_SA',
    'FF_OCEAN_FREIGHT_CHARGE','FF_INLAND_FREIGHT_CHARGE','FF_LCL_CHARGE',
    'FF_PRICE_TYPE','FF_AMOUNT_TYPE','FF_CHARGE_ACTION',
    'FF_CHARGE_OVER','FF_CHARGE_PER'
);
GO

-- ============================================================
-- Verification
-- ============================================================
SELECT t.name AS TableName, COUNT(c.column_id) AS Columns
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
WHERE t.name IN (
    'CURRENCIES','LOADTYPES','PORT_OF_LOADING','SHIPPING_LINES',
    'SHIPPING_AGENT','ROUTES','CONTAINER_SPECS','CONTAINER_TYPES',
    'ROUTES_BY_SHIPPING_AGENTS','OCEAN_FREIGHT_CHARGE_TYPES',
    'INLAND_FREIGHT_CHARGE_TYPES','LCL_CHARGE_TYPES','PRICE_TYPE',
    'AMOUNT_TYPE','CHARGE_ACTION','CHARGE_OVER','CHARGE_PER'
)
GROUP BY t.name
ORDER BY t.name;
GO

PRINT '✓ Freight Forwarder catalog tables created in LicoresMaduoDB.';
GO
