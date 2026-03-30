-- =============================================================================
-- 08_RouteAssignmentTables.sql
-- Route Assignment module tables for LicoresMaduoDB
-- =============================================================================

USE LicoresMaduoDB;
GO

-- ---------------------------------------------------------------------------
-- ROUTE_CUSTOMER_EXT
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.ROUTE_CUSTOMER_EXT', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ROUTE_CUSTOMER_EXT (
        RceId                       INT           IDENTITY(1,1) NOT NULL,
        RceAccountNumber            NVARCHAR(20)  NOT NULL,
        RceRouteNpActive            NVARCHAR(20)  NULL,
        RceRouteOvd5                NVARCHAR(20)  NULL,
        RceRouteOvd6                NVARCHAR(20)  NULL,
        RcePareto1Overall           NVARCHAR(10)  NULL,
        RcePareto2Overall           NVARCHAR(10)  NULL,
        RceParetoOthersOverall      NVARCHAR(10)  NULL,
        RcePareto1Beer              NVARCHAR(10)  NULL,
        RcePareto2Beer              NVARCHAR(10)  NULL,
        RceParetoOthersBeer         NVARCHAR(10)  NULL,
        RcePareto1Water             NVARCHAR(10)  NULL,
        RcePareto2Water             NVARCHAR(10)  NULL,
        RceParetoOthersWater        NVARCHAR(10)  NULL,
        RcePareto1Others            NVARCHAR(10)  NULL,
        RcePareto2Others            NVARCHAR(10)  NULL,
        RceParetoOthersOthers       NVARCHAR(10)  NULL,
        RceProyection               NVARCHAR(50)  NULL,
        RceSalesRepActive4          NVARCHAR(20)  NULL,
        RceSalesRepActive5          NVARCHAR(20)  NULL,
        RceSalesRepActive6          NVARCHAR(20)  NULL,
        RceAlternativeSalesRep      NVARCHAR(20)  NULL,
        RceCoolerPolar              BIT           NOT NULL DEFAULT 0,
        RceCoolerCorona             BIT           NOT NULL DEFAULT 0,
        RceCoolerBrasa              BIT           NOT NULL DEFAULT 0,
        RceCoolerWine               BIT           NOT NULL DEFAULT 0,
        RcePaintedPolar             BIT           NOT NULL DEFAULT 0,
        RceBrandingDwl              BIT           NOT NULL DEFAULT 0,
        RceBrandingGreyGoose        BIT           NOT NULL DEFAULT 0,
        RceBrandingBacardi          BIT           NOT NULL DEFAULT 0,
        RceBrandingBrasa            BIT           NOT NULL DEFAULT 0,
        RceHighTraffic              BIT           NOT NULL DEFAULT 0,
        RceIndoorBrandingClaro      BIT           NOT NULL DEFAULT 0,
        RceIndoorBrandingBrasa      BIT           NOT NULL DEFAULT 0,
        RceIndoorBrandingPolar      BIT           NOT NULL DEFAULT 0,
        RceIndoorBrandingMalta      BIT           NOT NULL DEFAULT 0,
        RceIndoorBrandingCorona     BIT           NOT NULL DEFAULT 0,
        RceIndoorBrandingCarloRossi BIT           NOT NULL DEFAULT 0,
        RceWithRackDisplay          BIT           NOT NULL DEFAULT 0,
        RceWithLightHeader          BIT           NOT NULL DEFAULT 0,
        RceWithWallMountedNameboard BIT           NOT NULL DEFAULT 0,
        RceWithBackbar              BIT           NOT NULL DEFAULT 0,
        RceWithLicoresWineAsHousewine BIT         NOT NULL DEFAULT 0,
        UpdatedAt                   DATETIME2     NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_ROUTE_CUSTOMER_EXT PRIMARY KEY (RceId),
        CONSTRAINT UQ_ROUTE_CUSTOMER_EXT_AcctNo UNIQUE (RceAccountNumber)
    );
    PRINT 'Table ROUTE_CUSTOMER_EXT created.';
END
ELSE
    PRINT 'Table ROUTE_CUSTOMER_EXT already exists - skipped.';
GO

-- ---------------------------------------------------------------------------
-- ROUTE_PRODUCT_EXT
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.ROUTE_PRODUCT_EXT', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ROUTE_PRODUCT_EXT (
        RpeId                           INT          IDENTITY(1,1) NOT NULL,
        RpeItemCode                     NVARCHAR(20) NOT NULL,
        RpeGroupCodeBeerWaterOthers     NVARCHAR(20) NULL,
        RpeGroupCodeBrandSpecific       NVARCHAR(20) NULL,
        UpdatedAt                       DATETIME2    NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_ROUTE_PRODUCT_EXT PRIMARY KEY (RpeId),
        CONSTRAINT UQ_ROUTE_PRODUCT_EXT_ItemCode UNIQUE (RpeItemCode)
    );
    PRINT 'Table ROUTE_PRODUCT_EXT created.';
END
ELSE
    PRINT 'Table ROUTE_PRODUCT_EXT already exists - skipped.';
GO

-- ---------------------------------------------------------------------------
-- ROUTE_BUDGET
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.ROUTE_BUDGET', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ROUTE_BUDGET (
        RbId            INT           IDENTITY(1,1) NOT NULL,
        RbYear          INT           NOT NULL,
        RbAccountNumber NVARCHAR(20)  NOT NULL,
        RbItemCode      NVARCHAR(20)  NOT NULL,
        RbQty01         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty02         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty03         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty04         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty05         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty06         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty07         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty08         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty09         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty10         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty11         DECIMAL(18,4) NULL DEFAULT 0,
        RbQty12         DECIMAL(18,4) NULL DEFAULT 0,
        CONSTRAINT PK_ROUTE_BUDGET PRIMARY KEY (RbId),
        CONSTRAINT UQ_ROUTE_BUDGET_YearAcctItem UNIQUE (RbYear, RbAccountNumber, RbItemCode)
    );
    PRINT 'Table ROUTE_BUDGET created.';
END
ELSE
    PRINT 'Table ROUTE_BUDGET already exists - skipped.';
GO

-- ---------------------------------------------------------------------------
-- Submodules for MODULE 4 (ROUTE)
-- ---------------------------------------------------------------------------
-- Insert submodules 72-76 if they don't already exist
IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleId = 72)
    INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (72, 4, 'Customer Ext Dimensions', 'ROUTE_CUSTOMER_EXT', 'ROUTE_CUSTOMER_EXT', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleId = 73)
    INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (73, 4, 'Product Ext Dimensions', 'ROUTE_PRODUCT_EXT', 'ROUTE_PRODUCT_EXT', 2);

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleId = 74)
    INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (74, 4, 'Route Budget', 'ROUTE_BUDGET', 'ROUTE_BUDGET', 3);

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleId = 75)
    INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (75, 4, 'Route Reports', 'ROUTE_REPORTS', NULL, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleId = 76)
    INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (76, 4, 'Route Dimensions', 'ROUTE_DIMENSIONS', NULL, 5);
GO

-- ---------------------------------------------------------------------------
-- Grant permissions to Roles 1 (SuperAdmin), 2 (Admin), 8 (ReadOnly)
-- ---------------------------------------------------------------------------
DECLARE @submodules TABLE (SubmoduleId INT);
INSERT INTO @submodules VALUES (72),(73),(74),(75),(76);

DECLARE @roles TABLE (RoleId INT);
INSERT INTO @roles VALUES (1),(2),(8);

-- SuperAdmin/Admin get full access; ReadOnly gets CanAccess only
INSERT INTO dbo.LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT r.RoleId,
       s.SubmoduleId,
       1 AS CanAccess,
       1 AS CanRead,
       CASE WHEN r.RoleId IN (1,2) THEN 1 ELSE 0 END AS CanWrite,
       CASE WHEN r.RoleId IN (1,2) THEN 1 ELSE 0 END AS CanEdit,
       CASE WHEN r.RoleId IN (1,2) THEN 1 ELSE 0 END AS CanDelete
FROM @roles r
CROSS JOIN @submodules s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.LM_RolePermissions rp
    WHERE rp.RoleId = r.RoleId AND rp.SubmoduleId = s.SubmoduleId
);
GO

PRINT 'Route Assignment submodules and permissions seeded.';
GO
