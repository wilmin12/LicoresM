-- ============================================================
-- 02_DHW_RouteAssignment_DataMart.sql
-- Data Mart - Route Assignment Analysis
-- Base de datos : DHW_DATABASE
-- Schema        : DM
-- Consumo       : Power BI
-- ETL           : Pentaho PDI
--
-- SCD Tipo 2 implementado en:
--   DIM_CUSTOMER  -> cambia ruta, vendedor, dia visita, pareto
--   DIM_SALESMAN  -> cambia ruta asignada
--
-- Como funciona el viaje en el tiempo:
--   FACT_INVOICES guarda el CustomerKey/SalesmanKey del momento
--   de la transaccion. Al hacer JOIN con la dimension, el
--   surrogate key apunta automaticamente a la version correcta.
--   No se necesita logica adicional en Power BI.
--
-- Configuracion Pentaho PDI (paso "Dimension Lookup/Update"):
--   - Technical key field  : CustomerKey / SalesmanKey
--   - Natural key field    : AccountNumber / SalesmanCode
--   - Start of date field  : EffectiveDate
--   - End of date field    : ExpirationDate
--   - Use alternative start date: No
--   - Stream date field    : fecha de proceso ETL o fecha del registro
--   - Dimensions fields    : todos los atributos (compara para detectar cambios)
-- ============================================================

USE DHW_DATABASE;
GO

-- ============================================================
-- SCHEMA DM
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'DM')
    EXEC('CREATE SCHEMA DM');
GO


-- ============================================================
-- DIM_TIME
-- Granularidad : DIA
-- TimeKey      : INT formato YYYYMMDD (estandar Power BI)
-- ============================================================
IF OBJECT_ID('DM.DIM_TIME', 'U') IS NOT NULL DROP TABLE DM.DIM_TIME;
GO

CREATE TABLE DM.DIM_TIME (
    TimeKey         INT         NOT NULL,   -- YYYYMMDD  (PK natural, no identity)
    FullDate        DATE        NOT NULL,
    Year            SMALLINT    NOT NULL,
    Quarter         TINYINT     NOT NULL,   -- 1-4
    MonthNumber     TINYINT     NOT NULL,   -- 1-12
    MonthName       VARCHAR(20) NOT NULL,
    MonthShort      CHAR(3)     NOT NULL,   -- Jan, Feb...
    WeekNumber      TINYINT     NOT NULL,   -- ISO week 1-53
    DayOfMonth      TINYINT     NOT NULL,
    DayOfWeek       TINYINT     NOT NULL,   -- 1=Domingo ... 7=Sabado
    DayName         VARCHAR(20) NOT NULL,
    DayShort        CHAR(3)     NOT NULL,
    IsWeekend       BIT         NOT NULL DEFAULT 0,
    YearMonth       INT         NOT NULL,   -- YYYYMM  (join a FACT_BUDGET)
    YearWeek        INT         NOT NULL,   -- YYYYWW
    -- Flags analiticos (se actualizan con el SP)
    IsCurrentYear   BIT         NOT NULL DEFAULT 0,
    IsCurrentMonth  BIT         NOT NULL DEFAULT 0,
    IsCurrentWeek   BIT         NOT NULL DEFAULT 0,
    IsYTD           BIT         NOT NULL DEFAULT 0,
    IsDecember      BIT         NOT NULL DEFAULT 0,

    CONSTRAINT PK_DIM_TIME PRIMARY KEY (TimeKey)
);
GO

-- ============================================================
-- SP: Poblar DIM_TIME
-- Ejecutar una vez; re-ejecutar anualmente para extender rango
-- ============================================================
IF OBJECT_ID('DM.usp_PopulateDimTime', 'P') IS NOT NULL
    DROP PROCEDURE DM.usp_PopulateDimTime;
GO

CREATE PROCEDURE DM.usp_PopulateDimTime
    @StartDate DATE = '2018-01-01',
    @EndDate   DATE = '2030-12-31'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Date DATE = @StartDate;
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    WHILE @Date <= @EndDate
    BEGIN
        DECLARE @Key  INT      = CAST(FORMAT(@Date, 'yyyyMMdd') AS INT);
        DECLARE @Y    SMALLINT = YEAR(@Date);
        DECLARE @M    TINYINT  = MONTH(@Date);
        DECLARE @D    TINYINT  = DAY(@Date);
        DECLARE @DOW  TINYINT  = DATEPART(WEEKDAY, @Date);
        DECLARE @WEEK TINYINT  = DATEPART(ISO_WEEK, @Date);

        IF NOT EXISTS (SELECT 1 FROM DM.DIM_TIME WHERE TimeKey = @Key)
        BEGIN
            INSERT INTO DM.DIM_TIME (
                TimeKey, FullDate, Year, Quarter, MonthNumber, MonthName, MonthShort,
                WeekNumber, DayOfMonth, DayOfWeek, DayName, DayShort,
                IsWeekend, YearMonth, YearWeek,
                IsCurrentYear, IsCurrentMonth, IsCurrentWeek, IsYTD, IsDecember
            )
            VALUES (
                @Key, @Date, @Y,
                DATEPART(QUARTER, @Date),
                @M,
                DATENAME(MONTH, @Date),
                LEFT(DATENAME(MONTH, @Date), 3),
                @WEEK, @D, @DOW,
                DATENAME(WEEKDAY, @Date),
                LEFT(DATENAME(WEEKDAY, @Date), 3),
                CASE WHEN @DOW IN (1,7) THEN 1 ELSE 0 END,
                @Y * 100 + @M,
                @Y * 100 + @WEEK,
                CASE WHEN @Y = YEAR(@Today) THEN 1 ELSE 0 END,
                CASE WHEN @Y = YEAR(@Today) AND @M = MONTH(@Today) THEN 1 ELSE 0 END,
                CASE WHEN @WEEK = DATEPART(ISO_WEEK,@Today) AND @Y = YEAR(@Today) THEN 1 ELSE 0 END,
                CASE WHEN @Date <= @Today AND @Y = YEAR(@Today) THEN 1 ELSE 0 END,
                CASE WHEN @M = 12 THEN 1 ELSE 0 END
            );
        END

        SET @Date = DATEADD(DAY, 1, @Date);
    END

    -- Refrescar flags "current" en toda la tabla
    UPDATE DM.DIM_TIME SET
        IsCurrentYear  = CASE WHEN Year = YEAR(@Today) THEN 1 ELSE 0 END,
        IsCurrentMonth = CASE WHEN Year = YEAR(@Today) AND MonthNumber = MONTH(@Today) THEN 1 ELSE 0 END,
        IsCurrentWeek  = CASE WHEN WeekNumber = DATEPART(ISO_WEEK,@Today) AND Year = YEAR(@Today) THEN 1 ELSE 0 END,
        IsYTD          = CASE WHEN FullDate <= @Today AND Year = YEAR(@Today) THEN 1 ELSE 0 END;

    PRINT 'DIM_TIME OK: ' + CAST(@StartDate AS VARCHAR) + ' -> ' + CAST(@EndDate AS VARCHAR);
END
GO

-- Carga inicial
EXEC DM.usp_PopulateDimTime @StartDate = '2018-01-01', @EndDate = '2030-12-31';
GO


-- ============================================================
-- DIM_ROUTE
-- Fuente : BRATTT campo ROUTE
-- SCD    : Tipo 1 (catalogo estable, sin historial necesario)
-- ============================================================
IF OBJECT_ID('DM.DIM_ROUTE', 'U') IS NOT NULL DROP TABLE DM.DIM_ROUTE;
GO

CREATE TABLE DM.DIM_ROUTE (
    RouteKey         INT          IDENTITY(1,1) NOT NULL,
    RouteCode        VARCHAR(20)  NOT NULL,
    RouteDescription VARCHAR(100) NULL,
    IsActive         BIT          NOT NULL DEFAULT 1,
    CreatedAt        DATETIME     NOT NULL DEFAULT GETDATE(),
    UpdatedAt        DATETIME     NULL,

    CONSTRAINT PK_DIM_ROUTE      PRIMARY KEY (RouteKey),
    CONSTRAINT UQ_DIM_ROUTE_Code UNIQUE (RouteCode)
);
GO

INSERT INTO DM.DIM_ROUTE (RouteCode, RouteDescription, IsActive)
VALUES ('UNKNOWN', 'Sin Ruta Asignada', 0);
GO


-- ============================================================
-- DIM_SALESMAN
-- Fuente : BRATTT (SALESMAN CODE/NAME) + DAILYT (SALES REP)
-- SCD    : TIPO 2 — registra cambios de ruta del vendedor
--
-- Logica SCD2:
--   - EffectiveDate : fecha desde la que aplica esta version
--   - ExpirationDate: fecha hasta (NULL = version activa hoy)
--   - IsCurrent     : 1 = version activa, 0 = version historica
--   - SalesmanCode  : clave natural (puede repetirse, una fila por version)
--   - SalesmanKey   : surrogate key (unico por version, es la FK en FACT)
-- ============================================================
IF OBJECT_ID('DM.DIM_SALESMAN', 'U') IS NOT NULL DROP TABLE DM.DIM_SALESMAN;
GO

CREATE TABLE DM.DIM_SALESMAN (
    SalesmanKey      INT          IDENTITY(1,1) NOT NULL,
    SalesmanCode     VARCHAR(20)  NOT NULL,   -- clave natural (NO UNIQUE, puede repetirse)
    SalesmanName     VARCHAR(100) NULL,
    RouteKey         INT          NOT NULL DEFAULT 1,

    -- SCD Tipo 2
    EffectiveDate    DATE         NOT NULL DEFAULT GETDATE(),
    ExpirationDate   DATE         NULL,       -- NULL = version activa actualmente
    IsCurrent        BIT          NOT NULL DEFAULT 1,

    IsActive         BIT          NOT NULL DEFAULT 1,
    CreatedAt        DATETIME     NOT NULL DEFAULT GETDATE(),
    UpdatedAt        DATETIME     NULL,

    CONSTRAINT PK_DIM_SALESMAN   PRIMARY KEY (SalesmanKey),
    CONSTRAINT FK_SALESMAN_ROUTE FOREIGN KEY (RouteKey) REFERENCES DM.DIM_ROUTE(RouteKey)
    -- Sin UNIQUE en SalesmanCode: puede haber varias versiones del mismo vendedor
);
GO

-- Indice para que Pentaho busque la version activa por codigo
CREATE NONCLUSTERED INDEX IX_DSALES_Code_Current
    ON DM.DIM_SALESMAN (SalesmanCode, IsCurrent)
    INCLUDE (SalesmanKey, SalesmanName, RouteKey, EffectiveDate);
GO

INSERT INTO DM.DIM_SALESMAN (SalesmanCode, SalesmanName, RouteKey, EffectiveDate, IsCurrent, IsActive)
VALUES ('UNKNOWN', 'Sin Vendedor', 1, '2000-01-01', 1, 0);
GO


-- ============================================================
-- DIM_DRIVER
-- Fuente : BRATTT (DRIVER CODE/NAME) + DAILYT (DRIVER)
-- SCD    : Tipo 1 (catalogo estable)
-- ============================================================
IF OBJECT_ID('DM.DIM_DRIVER', 'U') IS NOT NULL DROP TABLE DM.DIM_DRIVER;
GO

CREATE TABLE DM.DIM_DRIVER (
    DriverKey    INT          IDENTITY(1,1) NOT NULL,
    DriverCode   VARCHAR(20)  NOT NULL,
    DriverName   VARCHAR(100) NULL,
    IsActive     BIT          NOT NULL DEFAULT 1,
    CreatedAt    DATETIME     NOT NULL DEFAULT GETDATE(),
    UpdatedAt    DATETIME     NULL,

    CONSTRAINT PK_DIM_DRIVER      PRIMARY KEY (DriverKey),
    CONSTRAINT UQ_DIM_DRIVER_Code UNIQUE (DriverCode)
);
GO

INSERT INTO DM.DIM_DRIVER (DriverCode, DriverName, IsActive)
VALUES ('UNKNOWN', 'Sin Chofer', 0);
GO


-- ============================================================
-- DIM_CUSTOMER
-- Fuente principal : BRATTT
-- Fuente adicional : Archivos Excel
-- SCD    : TIPO 2 — registra cambios de ruta, vendedor,
--          dia de visita y clasificacion Pareto
--
-- Logica SCD2:
--   - AccountNumber : clave natural (puede repetirse, una fila por version)
--   - CustomerKey   : surrogate key (unico, FK en FACT_INVOICES)
--   - EffectiveDate : inicio de vigencia de esta version
--   - ExpirationDate: fin de vigencia (NULL = version activa hoy)
--   - IsCurrent     : 1 = version activa, 0 = version historica
--
-- Ejemplo de historial:
--   AccountNumber | CustomerKey | RouteCode | EffectiveDate | ExpirationDate | IsCurrent
--   ACC-001       |      5      | ROUTE-A   |  2022-01-01   |  2024-06-14    |     0
--   ACC-001       |     42      | ROUTE-B   |  2024-06-15   |    NULL        |     1
-- ============================================================
IF OBJECT_ID('DM.DIM_CUSTOMER', 'U') IS NOT NULL DROP TABLE DM.DIM_CUSTOMER;
GO

CREATE TABLE DM.DIM_CUSTOMER (
    CustomerKey              INT           IDENTITY(1,1) NOT NULL,
    AccountNumber            VARCHAR(20)   NOT NULL,   -- clave natural (NO UNIQUE)

    -- Datos basicos (BRATTT)
    AccountName              VARCHAR(200)  NULL,
    AccountAddress           VARCHAR(500)  NULL,
    AccountStatus            VARCHAR(20)   NULL,

    -- Ruta y campos de usuario (BRATTT) — atributos que cambian -> generan nueva version
    UserField4               VARCHAR(20)   NULL,
    UserField4Description    VARCHAR(100)  NULL,
    RouteCode                VARCHAR(20)   NULL,
    RouteDescription         VARCHAR(100)  NULL,

    -- Clasificacion (BRATTT)
    SubClass                 VARCHAR(20)   NULL,
    SubClassDescription      VARCHAR(100)  NULL,
    RetailersClass           VARCHAR(20)   NULL,
    RetailersClassDesc       VARCHAR(100)  NULL,
    OnOffPremise             VARCHAR(20)   NULL,
    IndustryVolume2          VARCHAR(20)   NULL,
    IndustryVolume2Desc      VARCHAR(100)  NULL,

    -- Vendedor / Chofer / Merchandiser (BRATTT) — cambian -> nueva version
    SalesmanCode             VARCHAR(20)   NULL,
    SalesmanName             VARCHAR(100)  NULL,
    DriverCode               VARCHAR(20)   NULL,
    DriverName               VARCHAR(100)  NULL,
    Merchandiser             VARCHAR(100)  NULL,
    RetailersSalesman        VARCHAR(100)  NULL,

    -- Programacion de visitas (BRATTT) — cambia -> nueva version (clave para Reporte 1)
    VisitDaySalesman         VARCHAR(20)   NULL,
    DeliveryDayDriver        VARCHAR(20)   NULL,
    VisitTimeSalesman        VARCHAR(20)   NULL,

    -- ----------------------------------------------------------
    -- DIMENSIONES ADICIONALES - Excel
    -- Cambian periodicamente (trimestral/anual) -> generan nueva version
    -- ----------------------------------------------------------

    RouteNPActive            VARCHAR(20)   NULL,
    RouteOVD5                VARCHAR(20)   NULL,
    RouteOVD6                VARCHAR(20)   NULL,

    -- Pareto General
    Pareto1                  BIT           NULL DEFAULT 0,
    Pareto2                  BIT           NULL DEFAULT 0,
    ParetoOthers             BIT           NULL DEFAULT 0,

    -- Pareto Beer
    Pareto1Beer              BIT           NULL DEFAULT 0,
    Pareto2Beer              BIT           NULL DEFAULT 0,
    ParetoOthersBeer         BIT           NULL DEFAULT 0,

    -- Pareto Water
    Pareto1Water             BIT           NULL DEFAULT 0,
    Pareto2Water             BIT           NULL DEFAULT 0,
    ParetoOthersWater        BIT           NULL DEFAULT 0,

    -- Pareto Others
    Pareto1Others            BIT           NULL DEFAULT 0,
    Pareto2Others            BIT           NULL DEFAULT 0,

    -- Proyeccion de venta
    Proyection               DECIMAL(18,2) NULL,

    -- Sales Rep activos por numero de rutas
    SalesRepActive4Route     VARCHAR(20)   NULL,
    SalesRepActive5Route     VARCHAR(20)   NULL,
    SalesRepActive6Route     VARCHAR(20)   NULL,
    AlternativeSalesRep      VARCHAR(100)  NULL,

    -- Coolers
    CoolerPolar              BIT           NULL DEFAULT 0,
    CoolerCorona             BIT           NULL DEFAULT 0,
    CoolerBrasa              BIT           NULL DEFAULT 0,
    CoolerWine               BIT           NULL DEFAULT 0,

    -- Branding exterior
    PaintedPolar             BIT           NULL DEFAULT 0,
    BrandingDWL              BIT           NULL DEFAULT 0,
    BrandingGreyGoose        BIT           NULL DEFAULT 0,
    BrandingBacardi          BIT           NULL DEFAULT 0,
    BrandingBrasa            BIT           NULL DEFAULT 0,
    HighTraffic              BIT           NULL DEFAULT 0,

    -- Indoor Branding
    IndoorBrandingClaro      BIT           NULL DEFAULT 0,
    IndoorBrandingBrasa      BIT           NULL DEFAULT 0,
    IndoorBrandingPolar      BIT           NULL DEFAULT 0,
    IndoorBrandingMalta      BIT           NULL DEFAULT 0,
    IndoorBrandingCorona     BIT           NULL DEFAULT 0,
    IndoorBrandingCarloRossi BIT           NULL DEFAULT 0,

    -- Display / Equipos en punto de venta
    HasRackDisplay           BIT           NULL DEFAULT 0,
    HasLightHeader           BIT           NULL DEFAULT 0,
    HasWallMountedNameboard  BIT           NULL DEFAULT 0,
    HasBackbar               BIT           NULL DEFAULT 0,
    HasLicoresWineHouseWine  BIT           NULL DEFAULT 0,

    -- ----------------------------------------------------------
    -- SCD TIPO 2 — columnas de control de version
    -- ----------------------------------------------------------
    EffectiveDate            DATE          NOT NULL DEFAULT GETDATE(),
    ExpirationDate           DATE          NULL,       -- NULL = version activa actualmente
    IsCurrent                BIT           NOT NULL DEFAULT 1,

    -- Audit
    IsActive                 BIT           NOT NULL DEFAULT 1,
    CreatedAt                DATETIME      NOT NULL DEFAULT GETDATE(),
    UpdatedAt                DATETIME      NULL,

    CONSTRAINT PK_DIM_CUSTOMER PRIMARY KEY (CustomerKey)
    -- Sin UNIQUE en AccountNumber: puede haber multiples versiones del mismo cliente
);
GO

-- Indice principal para Pentaho (busca version activa por AccountNumber)
CREATE NONCLUSTERED INDEX IX_DCUST_AccNo_Current
    ON DM.DIM_CUSTOMER (AccountNumber, IsCurrent)
    INCLUDE (CustomerKey, RouteCode, SalesmanCode, VisitDaySalesman, EffectiveDate);
GO

-- Indice historico (para queries que viajan en el tiempo por fecha)
CREATE NONCLUSTERED INDEX IX_DCUST_AccNo_Dates
    ON DM.DIM_CUSTOMER (AccountNumber, EffectiveDate, ExpirationDate)
    INCLUDE (CustomerKey, RouteCode, SalesmanCode, IsCurrent);
GO

-- Indice por ruta (para reportes de ruta)
CREATE NONCLUSTERED INDEX IX_DCUST_RouteCode
    ON DM.DIM_CUSTOMER (RouteCode, IsCurrent)
    INCLUDE (AccountNumber, AccountName, SalesmanCode, VisitDaySalesman);
GO

INSERT INTO DM.DIM_CUSTOMER (AccountNumber, AccountName, AccountStatus,
                              EffectiveDate, IsCurrent, IsActive)
VALUES ('UNKNOWN', 'Cliente Desconocido', 'INACTIVE', '2000-01-01', 1, 0);
GO


-- ============================================================
-- DIM_PRODUCT
-- Fuente principal : ITEMT
-- Fuente adicional : Excel (GroupCodes)
-- SCD    : Tipo 1 (producto rara vez cambia de marca/clase)
-- ============================================================
IF OBJECT_ID('DM.DIM_PRODUCT', 'U') IS NOT NULL DROP TABLE DM.DIM_PRODUCT;
GO

CREATE TABLE DM.DIM_PRODUCT (
    ProductKey            INT          IDENTITY(1,1) NOT NULL,
    ItemCode              VARCHAR(30)  NOT NULL,

    ItemDescription       VARCHAR(200) NULL,
    ItemStatus            VARCHAR(20)  NULL,
    SupplierCode          VARCHAR(20)  NULL,
    SupplierName          VARCHAR(100) NULL,
    BrandCode             VARCHAR(20)  NULL,
    BrandDescription      VARCHAR(100) NULL,
    SubCode               VARCHAR(20)  NULL,
    SubDescription        VARCHAR(100) NULL,
    ProductClass          VARCHAR(20)  NULL,
    ProductClassDesc      VARCHAR(100) NULL,
    UnitsPerCase          SMALLINT     NULL,
    MlPerBottle           INT          NULL,

    -- Dimensiones adicionales Excel
    GroupCodeBWO          VARCHAR(20)  NULL,   -- Beer / Water / Others
    GroupCodeBrandSpecific VARCHAR(50) NULL,

    -- Flag para excluir de calculo de comision (ej: Polar)
    ExcludeFromCommission BIT          NOT NULL DEFAULT 0,

    IsActive              BIT          NOT NULL DEFAULT 1,
    CreatedAt             DATETIME     NOT NULL DEFAULT GETDATE(),
    UpdatedAt             DATETIME     NULL,

    CONSTRAINT PK_DIM_PRODUCT      PRIMARY KEY (ProductKey),
    CONSTRAINT UQ_DIM_PRODUCT_Code UNIQUE (ItemCode)
);
GO

INSERT INTO DM.DIM_PRODUCT (ItemCode, ItemDescription, ItemStatus, IsActive)
VALUES ('UNKNOWN', 'Producto Desconocido', 'INACTIVE', 0);
GO


-- ============================================================
-- FACT_INVOICES
-- Fuente      : DAILYT
-- Granularidad: una fila por linea de factura
--
-- CLAVE DEL VIAJE EN EL TIEMPO:
--   CustomerKey y SalesmanKey son surrogate keys.
--   Pentaho los resuelve buscando la version activa (IsCurrent=1)
--   al momento de cargar cada factura. Asi, una factura de 2022
--   queda ligada al CustomerKey que estaba vigente en 2022,
--   no al CustomerKey actual.
-- ============================================================
IF OBJECT_ID('DM.FACT_INVOICES', 'U') IS NOT NULL DROP TABLE DM.FACT_INVOICES;
GO

CREATE TABLE DM.FACT_INVOICES (
    InvoiceKey      BIGINT        IDENTITY(1,1) NOT NULL,

    -- Foreign Keys
    TimeKey         INT           NOT NULL,
    CustomerKey     INT           NOT NULL,   -- surrogate key de la VERSION del cliente en ese momento
    ProductKey      INT           NOT NULL,
    SalesmanKey     INT           NOT NULL,   -- surrogate key de la VERSION del vendedor en ese momento
    DriverKey       INT           NOT NULL,
    RouteKey        INT           NOT NULL,

    -- Dimensiones degeneradas
    InvoiceNumber   VARCHAR(30)   NOT NULL,
    InvoiceDate     DATE          NOT NULL,
    LoadNumber      VARCHAR(20)   NULL,

    -- Medidas de cantidad
    CaseQty         DECIMAL(18,4) NOT NULL DEFAULT 0,
    BottleQty       DECIMAL(18,4) NOT NULL DEFAULT 0,
    UnitsPerCase    SMALLINT      NULL,
    UnitOfMeasure   VARCHAR(10)   NULL,

    -- Medidas de precio / costo
    UnitPrice       DECIMAL(18,4) NOT NULL DEFAULT 0,
    DiscountAmount  DECIMAL(18,4) NOT NULL DEFAULT 0,
    NetSales        DECIMAL(18,4) NOT NULL DEFAULT 0,
    UnitCost        DECIMAL(18,4) NOT NULL DEFAULT 0,
    CaseCost        DECIMAL(18,4) NOT NULL DEFAULT 0,

    -- Columnas calculadas persistidas
    GrossMargin     AS (NetSales - (CaseQty * CaseCost))            PERSISTED,
    CommissionAmt   AS (NetSales * CAST(0.01608 AS DECIMAL(10,5)))  PERSISTED,

    CreatedAt       DATETIME      NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_FACT_INVOICES  PRIMARY KEY (InvoiceKey),
    CONSTRAINT FK_FINV_TIME      FOREIGN KEY (TimeKey)     REFERENCES DM.DIM_TIME(TimeKey),
    CONSTRAINT FK_FINV_CUSTOMER  FOREIGN KEY (CustomerKey) REFERENCES DM.DIM_CUSTOMER(CustomerKey),
    CONSTRAINT FK_FINV_PRODUCT   FOREIGN KEY (ProductKey)  REFERENCES DM.DIM_PRODUCT(ProductKey),
    CONSTRAINT FK_FINV_SALESMAN  FOREIGN KEY (SalesmanKey) REFERENCES DM.DIM_SALESMAN(SalesmanKey),
    CONSTRAINT FK_FINV_DRIVER    FOREIGN KEY (DriverKey)   REFERENCES DM.DIM_DRIVER(DriverKey),
    CONSTRAINT FK_FINV_ROUTE     FOREIGN KEY (RouteKey)    REFERENCES DM.DIM_ROUTE(RouteKey)
);
GO


-- ============================================================
-- FACT_BUDGET
-- Fuente      : Exportacion DI
-- Granularidad: Anio + Mes + Cliente + Producto
-- Nota: usa el CustomerKey ACTUAL (IsCurrent=1) al momento
--       de la carga del presupuesto
-- ============================================================
IF OBJECT_ID('DM.FACT_BUDGET', 'U') IS NOT NULL DROP TABLE DM.FACT_BUDGET;
GO

CREATE TABLE DM.FACT_BUDGET (
    BudgetKey   INT           IDENTITY(1,1) NOT NULL,
    YearMonth   INT           NOT NULL,       -- YYYYMM
    CustomerKey INT           NOT NULL,
    ProductKey  INT           NOT NULL,
    BudgetQty   DECIMAL(18,4) NOT NULL DEFAULT 0,
    CreatedAt   DATETIME      NOT NULL DEFAULT GETDATE(),
    UpdatedAt   DATETIME      NULL,

    CONSTRAINT PK_FACT_BUDGET  PRIMARY KEY (BudgetKey),
    CONSTRAINT FK_FBUDG_CUST   FOREIGN KEY (CustomerKey) REFERENCES DM.DIM_CUSTOMER(CustomerKey),
    CONSTRAINT FK_FBUDG_PROD   FOREIGN KEY (ProductKey)  REFERENCES DM.DIM_PRODUCT(ProductKey),
    CONSTRAINT UQ_FACT_BUDGET  UNIQUE (YearMonth, CustomerKey, ProductKey)
);
GO


-- ============================================================
-- INDEXES
-- ============================================================

-- FACT_INVOICES
CREATE NONCLUSTERED INDEX IX_FINV_TimeKey
    ON DM.FACT_INVOICES (TimeKey)
    INCLUDE (NetSales, CaseQty, GrossMargin, CommissionAmt);

CREATE NONCLUSTERED INDEX IX_FINV_CustomerKey
    ON DM.FACT_INVOICES (CustomerKey)
    INCLUDE (NetSales, CaseQty, InvoiceNumber, TimeKey);

CREATE NONCLUSTERED INDEX IX_FINV_ProductKey
    ON DM.FACT_INVOICES (ProductKey)
    INCLUDE (NetSales, CaseQty, DiscountAmount);

CREATE NONCLUSTERED INDEX IX_FINV_SalesmanKey
    ON DM.FACT_INVOICES (SalesmanKey)
    INCLUDE (NetSales, CaseQty, TimeKey, CustomerKey);

CREATE NONCLUSTERED INDEX IX_FINV_RouteKey
    ON DM.FACT_INVOICES (RouteKey)
    INCLUDE (NetSales, CaseQty, TimeKey);

-- Indice compuesto para Reporte 1 (Visit Schedule vs Actual)
CREATE NONCLUSTERED INDEX IX_FINV_Visit_Analysis
    ON DM.FACT_INVOICES (SalesmanKey, CustomerKey, InvoiceDate)
    INCLUDE (InvoiceNumber, TimeKey, NetSales, CaseQty);

-- FACT_BUDGET
CREATE NONCLUSTERED INDEX IX_FBUDG_YearMonth
    ON DM.FACT_BUDGET (YearMonth)
    INCLUDE (CustomerKey, ProductKey, BudgetQty);

-- DIM_TIME
CREATE NONCLUSTERED INDEX IX_DTIME_YearMonth
    ON DM.DIM_TIME (YearMonth)
    INCLUDE (TimeKey, Year, MonthNumber, MonthName, WeekNumber);

CREATE NONCLUSTERED INDEX IX_DTIME_YTD
    ON DM.DIM_TIME (IsYTD, Year)
    INCLUDE (TimeKey, MonthNumber);
GO


-- ============================================================
-- VISTAS AUXILIARES (estado actual — para slicers en Power BI)
-- ============================================================

-- Vista: version activa de clientes (para filtros/slicers en Power BI)
IF OBJECT_ID('DM.VW_DIM_CUSTOMER_CURRENT', 'V') IS NOT NULL
    DROP VIEW DM.VW_DIM_CUSTOMER_CURRENT;
GO

CREATE VIEW DM.VW_DIM_CUSTOMER_CURRENT AS
SELECT *
FROM   DM.DIM_CUSTOMER
WHERE  IsCurrent = 1
  AND  AccountNumber <> 'UNKNOWN';
GO

-- Vista: version activa de vendedores
IF OBJECT_ID('DM.VW_DIM_SALESMAN_CURRENT', 'V') IS NOT NULL
    DROP VIEW DM.VW_DIM_SALESMAN_CURRENT;
GO

CREATE VIEW DM.VW_DIM_SALESMAN_CURRENT AS
SELECT *
FROM   DM.DIM_SALESMAN
WHERE  IsCurrent = 1
  AND  SalesmanCode <> 'UNKNOWN';
GO

-- Vista: historial de cambios de ruta/vendedor por cliente
-- Util para auditar cuando se movia un cliente entre rutas
IF OBJECT_ID('DM.VW_CUSTOMER_CHANGE_HISTORY', 'V') IS NOT NULL
    DROP VIEW DM.VW_CUSTOMER_CHANGE_HISTORY;
GO

CREATE VIEW DM.VW_CUSTOMER_CHANGE_HISTORY AS
SELECT
    AccountNumber,
    AccountName,
    RouteCode,
    RouteDescription,
    SalesmanCode,
    SalesmanName,
    VisitDaySalesman,
    Pareto1, Pareto2, ParetoOthers,
    EffectiveDate,
    ExpirationDate,
    IsCurrent,
    DATEDIFF(DAY, EffectiveDate, ISNULL(ExpirationDate, CAST(GETDATE() AS DATE))) AS DaysInVersion
FROM DM.DIM_CUSTOMER
WHERE AccountNumber <> 'UNKNOWN'
-- Sin filtro IsCurrent: muestra TODA la historia
-- Ordenar en Power BI por AccountNumber + EffectiveDate
;
GO


-- ============================================================
-- VISTAS PARA LOS 3 REPORTES REQUERIDOS
-- ============================================================

-- ------------------------------------------------------------
-- VW_VisitScheduleVsActual
-- Reporte 1: Dia programado vs dia real de visita + varianza
--
-- NOTA SCD2: el JOIN a DIM_CUSTOMER usa CustomerKey (surrogate).
-- La factura ya tiene el CustomerKey de la version que estaba
-- vigente cuando se emitio, incluyendo el VisitDaySalesman
-- de ese momento. El analisis es historicamente correcto.
-- ------------------------------------------------------------
IF OBJECT_ID('DM.VW_VisitScheduleVsActual', 'V') IS NOT NULL
    DROP VIEW DM.VW_VisitScheduleVsActual;
GO

CREATE VIEW DM.VW_VisitScheduleVsActual AS
SELECT
    -- Cliente (version historica al momento de la factura)
    c.AccountNumber,
    c.AccountName,
    c.RouteCode,
    c.RouteDescription,
    c.OnOffPremise,
    c.RetailersClass,
    c.EffectiveDate      AS CustomerVersionFrom,
    c.ExpirationDate     AS CustomerVersionTo,

    -- Vendedor
    c.SalesmanCode,
    c.SalesmanName,

    -- Dia programado (el que estaba configurado en ese periodo)
    c.VisitDaySalesman   AS ScheduledDay,

    -- Visita real
    fi.InvoiceNumber,
    fi.InvoiceDate,
    t.DayName            AS ActualDay,
    t.DayOfWeek          AS ActualDayOfWeek,
    t.WeekNumber,
    t.Year,
    t.MonthNumber,
    t.MonthName,
    t.YearMonth,

    -- Varianza
    CASE
        WHEN UPPER(LTRIM(RTRIM(c.VisitDaySalesman))) = UPPER(LTRIM(RTRIM(t.DayName)))
        THEN 1 ELSE 0
    END                  AS IsOnSchedule,

    -- Medidas
    fi.NetSales,
    fi.CaseQty,
    fi.DiscountAmount,
    fi.CommissionAmt

FROM      DM.FACT_INVOICES fi
INNER JOIN DM.DIM_CUSTOMER  c ON fi.CustomerKey = c.CustomerKey
INNER JOIN DM.DIM_TIME      t ON fi.TimeKey     = t.TimeKey
WHERE c.AccountNumber    <> 'UNKNOWN'
  AND c.VisitDaySalesman IS NOT NULL;
GO


-- ------------------------------------------------------------
-- VW_BrandSalesPerSalesman
-- Reporte 2: Ventas por marca / vendedor / cliente
-- Filtrar TotalNetSales = 0 en Power BI para ver marcas sin venta
-- ------------------------------------------------------------
IF OBJECT_ID('DM.VW_BrandSalesPerSalesman', 'V') IS NOT NULL
    DROP VIEW DM.VW_BrandSalesPerSalesman;
GO

CREATE VIEW DM.VW_BrandSalesPerSalesman AS
SELECT
    s.SalesmanCode,
    s.SalesmanName,
    s.EffectiveDate      AS SalesmanVersionFrom,
    r.RouteCode,
    r.RouteDescription,
    c.AccountNumber,
    c.AccountName,
    c.OnOffPremise,
    c.RetailersClass,
    c.RetailersClassDesc,
    c.EffectiveDate      AS CustomerVersionFrom,
    p.BrandCode,
    p.BrandDescription,
    p.GroupCodeBWO,
    p.GroupCodeBrandSpecific,
    p.ItemCode,
    p.ItemDescription,
    t.Year,
    t.MonthNumber,
    t.MonthName,
    t.YearMonth,
    t.WeekNumber,

    COUNT(DISTINCT fi.InvoiceNumber) AS TotalInvoices,
    SUM(fi.CaseQty)                  AS TotalCases,
    SUM(fi.NetSales)                 AS TotalNetSales,
    SUM(fi.DiscountAmount)           AS TotalDiscount,
    SUM(fi.GrossMargin)              AS TotalGrossMargin

FROM      DM.FACT_INVOICES fi
INNER JOIN DM.DIM_SALESMAN  s ON fi.SalesmanKey = s.SalesmanKey
INNER JOIN DM.DIM_CUSTOMER  c ON fi.CustomerKey = c.CustomerKey
INNER JOIN DM.DIM_PRODUCT   p ON fi.ProductKey  = p.ProductKey
INNER JOIN DM.DIM_TIME      t ON fi.TimeKey     = t.TimeKey
INNER JOIN DM.DIM_ROUTE     r ON fi.RouteKey    = r.RouteKey
WHERE s.SalesmanCode <> 'UNKNOWN'
  AND c.AccountNumber <> 'UNKNOWN'
  AND p.ItemCode      <> 'UNKNOWN'
GROUP BY
    s.SalesmanCode, s.SalesmanName, s.EffectiveDate,
    r.RouteCode, r.RouteDescription,
    c.AccountNumber, c.AccountName, c.OnOffPremise,
    c.RetailersClass, c.RetailersClassDesc, c.EffectiveDate,
    p.BrandCode, p.BrandDescription, p.GroupCodeBWO, p.GroupCodeBrandSpecific,
    p.ItemCode, p.ItemDescription,
    t.Year, t.MonthNumber, t.MonthName, t.YearMonth, t.WeekNumber;
GO


-- ------------------------------------------------------------
-- VW_CommissionAnalysis
-- Reporte 3: Comision actual (ACTUAL) y futura (BUDGET)
-- Formula  : NetSales * 24% * 6.7% = NetSales * 0.01608
-- Excluir  : productos con ExcludeFromCommission = 1 (ej: Polar)
-- ------------------------------------------------------------
IF OBJECT_ID('DM.VW_CommissionAnalysis', 'V') IS NOT NULL
    DROP VIEW DM.VW_CommissionAnalysis;
GO

CREATE VIEW DM.VW_CommissionAnalysis AS

-- ---- ACTUAL ----
SELECT
    'ACTUAL'                    AS CommissionType,
    s.SalesmanCode,
    s.SalesmanName,
    s.EffectiveDate             AS SalesmanVersionFrom,
    r.RouteCode,
    c.AccountNumber,
    c.AccountName,
    c.EffectiveDate             AS CustomerVersionFrom,
    p.ItemCode,
    p.ItemDescription,
    p.BrandCode,
    p.BrandDescription,
    p.GroupCodeBWO,
    p.ExcludeFromCommission,
    t.Year,
    t.MonthNumber,
    t.MonthName,
    t.YearMonth,

    SUM(fi.CaseQty)             AS TotalCases,
    SUM(fi.NetSales)            AS TotalNetSales,
    SUM(fi.DiscountAmount)      AS TotalDiscount,
    SUM(fi.GrossMargin)         AS TotalGrossMargin,

    SUM(CASE WHEN p.ExcludeFromCommission = 0
             THEN fi.CommissionAmt ELSE 0 END) AS CommissionAmount,

    NULL                        AS BudgetQty

FROM      DM.FACT_INVOICES fi
INNER JOIN DM.DIM_SALESMAN  s ON fi.SalesmanKey = s.SalesmanKey
INNER JOIN DM.DIM_CUSTOMER  c ON fi.CustomerKey = c.CustomerKey
INNER JOIN DM.DIM_PRODUCT   p ON fi.ProductKey  = p.ProductKey
INNER JOIN DM.DIM_TIME      t ON fi.TimeKey     = t.TimeKey
INNER JOIN DM.DIM_ROUTE     r ON fi.RouteKey    = r.RouteKey
WHERE s.SalesmanCode <> 'UNKNOWN'
GROUP BY
    s.SalesmanCode, s.SalesmanName, s.EffectiveDate, r.RouteCode,
    c.AccountNumber, c.AccountName, c.EffectiveDate,
    p.ItemCode, p.ItemDescription, p.BrandCode, p.BrandDescription,
    p.GroupCodeBWO, p.ExcludeFromCommission,
    t.Year, t.MonthNumber, t.MonthName, t.YearMonth

UNION ALL

-- ---- BUDGET (comision futura proyectada) ----
SELECT
    'BUDGET'                    AS CommissionType,
    s.SalesmanCode,
    s.SalesmanName,
    s.EffectiveDate             AS SalesmanVersionFrom,
    c.RouteCode,
    c.AccountNumber,
    c.AccountName,
    c.EffectiveDate             AS CustomerVersionFrom,
    p.ItemCode,
    p.ItemDescription,
    p.BrandCode,
    p.BrandDescription,
    p.GroupCodeBWO,
    p.ExcludeFromCommission,
    CAST(LEFT(CAST(fb.YearMonth AS VARCHAR(6)), 4) AS SMALLINT) AS Year,
    CAST(RIGHT(CAST(fb.YearMonth AS VARCHAR(6)), 2) AS TINYINT) AS MonthNumber,
    dt.MonthName,
    fb.YearMonth,

    SUM(fb.BudgetQty)           AS TotalCases,
    NULL                        AS TotalNetSales,
    NULL                        AS TotalDiscount,
    NULL                        AS TotalGrossMargin,
    -- Comision futura: calcular en DAX usando precio promedio historico
    NULL                        AS CommissionAmount,
    SUM(fb.BudgetQty)           AS BudgetQty

FROM      DM.FACT_BUDGET   fb
INNER JOIN DM.DIM_CUSTOMER  c  ON fb.CustomerKey = c.CustomerKey
INNER JOIN DM.DIM_PRODUCT   p  ON fb.ProductKey  = p.ProductKey
LEFT  JOIN DM.DIM_SALESMAN  s  ON s.SalesmanCode = c.SalesmanCode AND s.IsCurrent = 1
LEFT  JOIN DM.DIM_TIME      dt ON dt.YearMonth   = fb.YearMonth AND dt.DayOfMonth = 1
WHERE c.AccountNumber <> 'UNKNOWN'
GROUP BY
    s.SalesmanCode, s.SalesmanName, s.EffectiveDate, c.RouteCode,
    c.AccountNumber, c.AccountName, c.EffectiveDate,
    p.ItemCode, p.ItemDescription, p.BrandCode, p.BrandDescription,
    p.GroupCodeBWO, p.ExcludeFromCommission,
    fb.YearMonth, dt.MonthName;
GO


-- ============================================================
-- VALIDACION FINAL
-- ============================================================
SELECT 'DM.DIM_TIME'      AS Tabla, COUNT(*) AS Registros FROM DM.DIM_TIME     UNION ALL
SELECT 'DM.DIM_ROUTE',             COUNT(*)               FROM DM.DIM_ROUTE    UNION ALL
SELECT 'DM.DIM_SALESMAN',          COUNT(*)               FROM DM.DIM_SALESMAN UNION ALL
SELECT 'DM.DIM_DRIVER',            COUNT(*)               FROM DM.DIM_DRIVER   UNION ALL
SELECT 'DM.DIM_CUSTOMER',          COUNT(*)               FROM DM.DIM_CUSTOMER UNION ALL
SELECT 'DM.DIM_PRODUCT',           COUNT(*)               FROM DM.DIM_PRODUCT  UNION ALL
SELECT 'DM.FACT_INVOICES',         COUNT(*)               FROM DM.FACT_INVOICES UNION ALL
SELECT 'DM.FACT_BUDGET',           COUNT(*)               FROM DM.FACT_BUDGET;
GO

-- Verificar versiones activas vs historicas
SELECT
    'DIM_CUSTOMER' AS Tabla,
    SUM(CASE WHEN IsCurrent = 1 THEN 1 ELSE 0 END) AS VersionesActivas,
    SUM(CASE WHEN IsCurrent = 0 THEN 1 ELSE 0 END) AS VersionesHistoricas,
    COUNT(*)                                        AS TotalRegistros
FROM DM.DIM_CUSTOMER
UNION ALL
SELECT
    'DIM_SALESMAN',
    SUM(CASE WHEN IsCurrent = 1 THEN 1 ELSE 0 END),
    SUM(CASE WHEN IsCurrent = 0 THEN 1 ELSE 0 END),
    COUNT(*)
FROM DM.DIM_SALESMAN;
GO

PRINT '=== DHW_DATABASE - Data Mart Route Assignment con SCD Tipo 2 creado exitosamente ===';
GO


select * 
from DM.DIM_ROUTE;

select CMBRT from BRATTT;

select * from DM.DIM_CUSTOMER