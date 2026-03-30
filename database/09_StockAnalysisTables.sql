-- ============================================================
-- 09_StockAnalysisTables.sql
-- Stock Analysis module tables
-- ============================================================

USE LicoresMaduoDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'STOCK_IDEAL_MONTHS')
BEGIN
    CREATE TABLE STOCK_IDEAL_MONTHS (
        SimId            INT IDENTITY(1,1) NOT NULL,
        SimItemCode      NVARCHAR(20)      NOT NULL,
        SimIdealMonths   DECIMAL(10,2)     NULL,
        SimOrderFreq     NVARCHAR(20)      NULL,
        SimStockStartDate DATE             NULL,
        UpdatedAt        DATETIME          NULL,
        CONSTRAINT PK_STOCK_IDEAL_MONTHS PRIMARY KEY (SimId),
        CONSTRAINT UQ_STOCK_IDEAL_MONTHS_Item UNIQUE (SimItemCode)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'STOCK_VENDOR_CONSTRAINTS')
BEGIN
    CREATE TABLE STOCK_VENDOR_CONSTRAINTS (
        SvcId                  INT IDENTITY(1,1) NOT NULL,
        SvcFromLocationCode    NVARCHAR(20)  NULL,
        SvcFromLocationName    NVARCHAR(100) NULL,
        SvcToLocationCode      NVARCHAR(20)  NULL,
        SvcToLocationName      NVARCHAR(100) NULL,
        SvcShipperCode         NVARCHAR(20)  NULL,
        SvcOrderReviewDay      NVARCHAR(20)  NULL,
        SvcSupplierLeadDays    INT           NULL,
        SvcTransitDays         INT           NULL,
        SvcWarehouseProcessDays INT          NULL,
        SvcSafetyDays          INT           NULL,
        SvcOrderCycleDays      INT           NULL,
        SvcMinOrderQty         DECIMAL(18,4) NULL,
        SvcOrderIncrement      DECIMAL(18,4) NULL,
        SvcMinTotalCaseOrder   DECIMAL(18,4) NULL,
        SvcPurchaserName       NVARCHAR(100) NULL,
        UpdatedAt              DATETIME      NULL,
        CONSTRAINT PK_STOCK_VENDOR_CONSTRAINTS PRIMARY KEY (SvcId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'STOCK_SALES_BUDGET')
BEGIN
    CREATE TABLE STOCK_SALES_BUDGET (
        SsbId               INT IDENTITY(1,1) NOT NULL,
        SsbYear             INT           NOT NULL,
        SsbMonth            INT           NOT NULL,
        SsbItemCode         NVARCHAR(20)  NOT NULL,
        SsbItemDesc         NVARCHAR(100) NULL,
        SsbBudgetedUnits    DECIMAL(18,4) NULL,
        SsbBudgetedSales    DECIMAL(18,4) NULL,
        SsbBudgetedDiscount DECIMAL(18,4) NULL,
        SsbBudgetedMargin   DECIMAL(18,4) NULL,
        SsbBudgetedGross    DECIMAL(18,4) NULL,
        SsbBudgetedCost     DECIMAL(18,4) NULL,
        CONSTRAINT PK_STOCK_SALES_BUDGET PRIMARY KEY (SsbId),
        CONSTRAINT UQ_STOCK_SALES_BUDGET UNIQUE (SsbYear, SsbMonth, SsbItemCode)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'STOCK_ANALYSIS_RESULT')
BEGIN
    CREATE TABLE STOCK_ANALYSIS_RESULT (
        SarId                               INT IDENTITY(1,1) NOT NULL,
        SarYear                             INT           NOT NULL,
        SarMonth                            INT           NOT NULL,
        SarItemCode                         NVARCHAR(20)  NOT NULL,
        SarItemDesc                         NVARCHAR(100) NULL,
        SarProductClassId                   NVARCHAR(10)  NULL,
        SarProductClassDesc                 NVARCHAR(100) NULL,
        SarSupplierCode                     NVARCHAR(20)  NULL,
        SarSupplierName                     NVARCHAR(100) NULL,
        SarBrandCode                        NVARCHAR(20)  NULL,
        SarBrandDesc                        NVARCHAR(100) NULL,
        SarStockStartDate                   DATE          NULL,
        SarOrderFrequency                   NVARCHAR(20)  NULL,
        SarIdealMonthsOfStock               DECIMAL(10,2) NULL,
        SarOh11010                          DECIMAL(18,4) NULL,
        SarOh11020                          DECIMAL(18,4) NULL,
        SarOh11060                          DECIMAL(18,4) NULL,
        SarCurrentOhUnits                   DECIMAL(18,4) NULL,
        SarOnOrder11010                     DECIMAL(18,4) NULL,
        SarOnOrder11020                     DECIMAL(18,4) NULL,
        SarOnOrder11060                     DECIMAL(18,4) NULL,
        SarOnOrderUnits                     DECIMAL(18,4) NULL,
        SarOnOrderEta                       DATE          NULL,
        SarYtdSalesUnits                    DECIMAL(18,4) NULL,
        SarMonthlySalesUnits                DECIMAL(18,4) NULL,
        SarIdealStockUnits                  DECIMAL(18,4) NULL,
        SarOverstockUnits                   DECIMAL(18,4) NULL,
        SarOverstockUnitsInclOrders         DECIMAL(18,4) NULL,
        SarMonthsOfStock                    DECIMAL(10,4) NULL,
        SarYearsOfStock                     DECIMAL(10,4) NULL,
        SarMonthsOfStockInclOnOrder         DECIMAL(10,4) NULL,
        SarMonthsOfOverstock                DECIMAL(10,4) NULL,
        SarMonthsOfOverstockInclOnOrder     DECIMAL(10,4) NULL,
        SarTotalBudgetUnits                 DECIMAL(18,4) NULL,
        SarYtdBudgetUnits                   DECIMAL(18,4) NULL,
        SarTotalBudgetSales                 DECIMAL(18,4) NULL,
        SarYtdBudgetSales                   DECIMAL(18,4) NULL,
        SarTotalBudgetCost                  DECIMAL(18,4) NULL,
        SarYtdBudgetCost                    DECIMAL(18,4) NULL,
        SarOverUnderPerformanceUnits        DECIMAL(18,4) NULL,
        SarInventoryValue                   DECIMAL(18,4) NULL,
        SarInventoryValueOnOrder            DECIMAL(18,4) NULL,
        SarTotalInventoryValue              DECIMAL(18,4) NULL,
        SarAvgCostPerCase                   DECIMAL(18,4) NULL,
        SarIdealStockAng                    DECIMAL(18,4) NULL,
        SarBudgetedIdealStockAng            DECIMAL(18,4) NULL,
        SarOverstockAng                     DECIMAL(18,4) NULL,
        SarOverstockAngInclOrder            DECIMAL(18,4) NULL,
        SarExpectedMonthlySalesAng          DECIMAL(18,4) NULL,
        SarMonthsOfStockInclOrderOnValue    DECIMAL(10,4) NULL,
        SarMonthsOfOverstockInclOrderOnValue DECIMAL(10,4) NULL,
        SarDailyRateOfSale                  DECIMAL(18,4) NULL,
        SarLastReceiptDate                  DATE          NULL,
        SarQtyLastReceipt                   DECIMAL(18,4) NULL,
        SarDaysBeforeArrivalOrder           INT           NULL,
        SarMonthsBeforeArrivalOrder         DECIMAL(10,4) NULL,
        SarUnitSalesBeforeArrivalOrder      DECIMAL(18,4) NULL,
        SarTotalOhAtArrivalOrder            DECIMAL(18,4) NULL,
        SarOverstockAtArrivalOrder          DECIMAL(18,4) NULL,
        SarTotalMonthsBeforeIdealStock      DECIMAL(10,4) NULL,
        SarGeneratedAt                      DATETIME      NULL,
        CONSTRAINT PK_STOCK_ANALYSIS_RESULT PRIMARY KEY (SarId),
        CONSTRAINT UQ_STOCK_ANALYSIS_RESULT UNIQUE (SarYear, SarMonth, SarItemCode)
    );
END
GO
