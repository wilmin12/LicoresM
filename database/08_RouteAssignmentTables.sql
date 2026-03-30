-- ============================================================
-- 08_RouteAssignmentTables.sql
-- Route Assignment module tables
-- ============================================================

USE LicoresMaduoDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ROUTE_CUSTOMER_EXT')
BEGIN
    CREATE TABLE ROUTE_CUSTOMER_EXT (
        RceId                   INT IDENTITY(1,1) NOT NULL,
        RceAccountNumber        NVARCHAR(20)  NOT NULL,
        RceRouteNpActive        NVARCHAR(20)  NULL,
        RceRouteOvd5            NVARCHAR(20)  NULL,
        RceRouteOvd6            NVARCHAR(20)  NULL,
        RcePareto1Overall       NVARCHAR(10)  NULL,
        RcePareto2Overall       NVARCHAR(10)  NULL,
        RceParetoOthersOverall  NVARCHAR(10)  NULL,
        RcePareto1Beer          NVARCHAR(10)  NULL,
        RcePareto2Beer          NVARCHAR(10)  NULL,
        RceParetoOthersBeer     NVARCHAR(10)  NULL,
        RcePareto1Water         NVARCHAR(10)  NULL,
        RcePareto2Water         NVARCHAR(10)  NULL,
        RceParetoOthersWater    NVARCHAR(10)  NULL,
        RcePareto1Others        NVARCHAR(10)  NULL,
        RcePareto2Others        NVARCHAR(10)  NULL,
        RceParetoOthersOthers   NVARCHAR(10)  NULL,
        RceProyection           NVARCHAR(50)  NULL,
        RceSalesRepActive4      NVARCHAR(20)  NULL,
        RceSalesRepActive5      NVARCHAR(20)  NULL,
        RceSalesRepActive6      NVARCHAR(20)  NULL,
        RceAlternativeSalesRep  NVARCHAR(20)  NULL,
        UpdatedAt               DATETIME      NULL,
        CONSTRAINT PK_ROUTE_CUSTOMER_EXT PRIMARY KEY (RceId),
        CONSTRAINT UQ_ROUTE_CUSTOMER_EXT_Account UNIQUE (RceAccountNumber)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ROUTE_PRODUCT_EXT')
BEGIN
    CREATE TABLE ROUTE_PRODUCT_EXT (
        RpeId                       INT IDENTITY(1,1) NOT NULL,
        RpeItemCode                 NVARCHAR(20) NOT NULL,
        RpeGroupCodeBeerWaterOthers NVARCHAR(20) NULL,
        RpeGroupCodeBrandSpecific   NVARCHAR(20) NULL,
        UpdatedAt                   DATETIME     NULL,
        CONSTRAINT PK_ROUTE_PRODUCT_EXT PRIMARY KEY (RpeId),
        CONSTRAINT UQ_ROUTE_PRODUCT_EXT_Item UNIQUE (RpeItemCode)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ROUTE_BUDGET')
BEGIN
    CREATE TABLE ROUTE_BUDGET (
        RbId            INT IDENTITY(1,1) NOT NULL,
        RbYear          INT           NOT NULL,
        RbAccountNumber NVARCHAR(20)  NOT NULL,
        RbItemCode      NVARCHAR(20)  NOT NULL,
        RbQty01         DECIMAL(18,4) NULL,
        RbQty02         DECIMAL(18,4) NULL,
        RbQty03         DECIMAL(18,4) NULL,
        RbQty04         DECIMAL(18,4) NULL,
        RbQty05         DECIMAL(18,4) NULL,
        RbQty06         DECIMAL(18,4) NULL,
        RbQty07         DECIMAL(18,4) NULL,
        RbQty08         DECIMAL(18,4) NULL,
        RbQty09         DECIMAL(18,4) NULL,
        RbQty10         DECIMAL(18,4) NULL,
        RbQty11         DECIMAL(18,4) NULL,
        RbQty12         DECIMAL(18,4) NULL,
        CONSTRAINT PK_ROUTE_BUDGET PRIMARY KEY (RbId),
        CONSTRAINT UQ_ROUTE_BUDGET UNIQUE (RbYear, RbAccountNumber, RbItemCode)
    );
END
GO
