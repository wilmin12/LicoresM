USE LicoresMaduoDB;
GO

-- ── STOCK_IDEAL_MONTHS ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'STOCK_IDEAL_MONTHS')
BEGIN
    CREATE TABLE STOCK_IDEAL_MONTHS (
        SimId               INT IDENTITY(1,1) PRIMARY KEY,
        SimItemCode         NVARCHAR(20)  NOT NULL,
        SimIdealMonths      DECIMAL(10,2) NOT NULL DEFAULT 1.5,
        SimOrderFreq        NVARCHAR(20)  NULL,
        SimStockStartDate   DATE          NULL,
        UpdatedAt           DATETIME2     NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_StockIdealMonths_ItemCode UNIQUE (SimItemCode)
    );
END
GO

-- ── STOCK_VENDOR_CONSTRAINTS ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'STOCK_VENDOR_CONSTRAINTS')
BEGIN
    CREATE TABLE STOCK_VENDOR_CONSTRAINTS (
        SvcId                   INT IDENTITY(1,1) PRIMARY KEY,
        SvcFromLocationCode     NVARCHAR(20)   NULL,
        SvcFromLocationName     NVARCHAR(100)  NULL,
        SvcToLocationCode       NVARCHAR(20)   NULL,
        SvcToLocationName       NVARCHAR(100)  NULL,
        SvcShipperCode          NVARCHAR(20)   NULL,
        SvcOrderReviewDay       NVARCHAR(20)   NULL,
        SvcSupplierLeadDays     INT            NULL,
        SvcTransitDays          INT            NULL,
        SvcWarehouseProcessDays INT            NULL,
        SvcSafetyDays           INT            NULL,
        SvcOrderCycleDays       INT            NULL,
        SvcMinOrderQty          DECIMAL(18,4)  NULL,
        SvcOrderIncrement       DECIMAL(18,4)  NULL,
        SvcMinTotalCaseOrder    DECIMAL(18,4)  NULL,
        SvcPurchaserName        NVARCHAR(100)  NULL,
        UpdatedAt               DATETIME2      NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ── STOCK_SALES_BUDGET ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'STOCK_SALES_BUDGET')
BEGIN
    CREATE TABLE STOCK_SALES_BUDGET (
        SsbId               INT IDENTITY(1,1) PRIMARY KEY,
        SsbYear             INT            NOT NULL,
        SsbMonth            INT            NOT NULL,
        SsbItemCode         NVARCHAR(20)   NOT NULL,
        SsbItemDesc         NVARCHAR(100)  NULL,
        SsbBudgetedUnits    DECIMAL(18,4)  NULL DEFAULT 0,
        SsbBudgetedSales    DECIMAL(18,4)  NULL DEFAULT 0,
        SsbBudgetedDiscount DECIMAL(18,4)  NULL DEFAULT 0,
        SsbBudgetedMargin   DECIMAL(18,4)  NULL DEFAULT 0,
        SsbBudgetedGross    DECIMAL(18,4)  NULL DEFAULT 0,
        SsbBudgetedCost     DECIMAL(18,4)  NULL DEFAULT 0,
        CONSTRAINT UQ_StockSalesBudget_YearMonthItem UNIQUE (SsbYear, SsbMonth, SsbItemCode)
    );
END
GO

-- ── STOCK_ANALYSIS_RESULT ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'STOCK_ANALYSIS_RESULT')
BEGIN
    CREATE TABLE STOCK_ANALYSIS_RESULT (
        SarId                               INT IDENTITY(1,1) PRIMARY KEY,
        SarYear                             INT            NOT NULL,
        SarMonth                            INT            NOT NULL,
        SarItemCode                         NVARCHAR(20)   NOT NULL,
        SarItemDesc                         NVARCHAR(100)  NULL,
        SarProductClassId                   NVARCHAR(10)   NULL,
        SarProductClassDesc                 NVARCHAR(100)  NULL,
        SarSupplierCode                     NVARCHAR(20)   NULL,
        SarSupplierName                     NVARCHAR(100)  NULL,
        SarBrandCode                        NVARCHAR(20)   NULL,
        SarBrandDesc                        NVARCHAR(100)  NULL,
        SarStockStartDate                   DATE           NULL,
        SarOrderFrequency                   NVARCHAR(20)   NULL,
        SarIdealMonthsOfStock               DECIMAL(10,2)  NULL,
        SarOh11010                          DECIMAL(18,4)  NULL,
        SarOh11020                          DECIMAL(18,4)  NULL,
        SarOh11060                          DECIMAL(18,4)  NULL,
        SarCurrentOhUnits                   DECIMAL(18,4)  NULL,
        SarOnOrder11010                     DECIMAL(18,4)  NULL,
        SarOnOrder11020                     DECIMAL(18,4)  NULL,
        SarOnOrder11060                     DECIMAL(18,4)  NULL,
        SarOnOrderUnits                     DECIMAL(18,4)  NULL,
        SarOnOrderEta                       DATE           NULL,
        SarYtdSalesUnits                    DECIMAL(18,4)  NULL,
        SarMonthlySalesUnits                DECIMAL(18,4)  NULL,
        SarIdealStockUnits                  DECIMAL(18,4)  NULL,
        SarOverstockUnits                   DECIMAL(18,4)  NULL,
        SarOverstockUnitsInclOrders         DECIMAL(18,4)  NULL,
        SarMonthsOfStock                    DECIMAL(10,4)  NULL,
        SarYearsOfStock                     DECIMAL(10,4)  NULL,
        SarMonthsOfStockInclOnOrder         DECIMAL(10,4)  NULL,
        SarMonthsOfOverstock                DECIMAL(10,4)  NULL,
        SarMonthsOfOverstockInclOnOrder     DECIMAL(10,4)  NULL,
        SarTotalBudgetUnits                 DECIMAL(18,4)  NULL,
        SarYtdBudgetUnits                   DECIMAL(18,4)  NULL,
        SarTotalBudgetSales                 DECIMAL(18,4)  NULL,
        SarYtdBudgetSales                   DECIMAL(18,4)  NULL,
        SarTotalBudgetCost                  DECIMAL(18,4)  NULL,
        SarYtdBudgetCost                    DECIMAL(18,4)  NULL,
        SarOverUnderPerformanceUnits        DECIMAL(18,4)  NULL,
        SarInventoryValue                   DECIMAL(18,4)  NULL,
        SarInventoryValueOnOrder            DECIMAL(18,4)  NULL,
        SarTotalInventoryValue              DECIMAL(18,4)  NULL,
        SarAvgCostPerCase                   DECIMAL(18,4)  NULL,
        SarIdealStockAng                    DECIMAL(18,4)  NULL,
        SarBudgetedIdealStockAng            DECIMAL(18,4)  NULL,
        SarOverstockAng                     DECIMAL(18,4)  NULL,
        SarOverstockAngInclOrder            DECIMAL(18,4)  NULL,
        SarExpectedMonthlySalesAng          DECIMAL(18,4)  NULL,
        SarMonthsOfStockInclOrderOnValue    DECIMAL(10,4)  NULL,
        SarMonthsOfOverstockInclOrderOnValue DECIMAL(10,4) NULL,
        SarDailyRateOfSale                  DECIMAL(18,4)  NULL,
        SarLastReceiptDate                  DATE           NULL,
        SarQtyLastReceipt                   DECIMAL(18,4)  NULL,
        SarDaysBeforeArrivalOrder           INT            NULL,
        SarMonthsBeforeArrivalOrder         DECIMAL(10,4)  NULL,
        SarUnitSalesBeforeArrivalOrder      DECIMAL(18,4)  NULL,
        SarTotalOhAtArrivalOrder            DECIMAL(18,4)  NULL,
        SarOverstockAtArrivalOrder          DECIMAL(18,4)  NULL,
        SarTotalMonthsBeforeIdealStock      DECIMAL(10,4)  NULL,
        SarGeneratedAt                      DATETIME2      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_StockAnalysisResult_YearMonthItem UNIQUE (SarYear, SarMonth, SarItemCode)
    );
END
GO

-- ── Submodules (ModuleId = 5 = STOCK) ────────────────────────────────────────
SET IDENTITY_INSERT dbo.LM_Submodules ON;

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleId = 77)
    INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive)
    VALUES (77, 5, 'Stock Ideal Months', 'STOCK_IDEAL_MONTHS', 'STOCK_IDEAL_MONTHS', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleId = 78)
    INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive)
    VALUES (78, 5, 'Vendor Constraints', 'STOCK_VENDOR_CONSTRAINTS', 'STOCK_VENDOR_CONSTRAINTS', 2, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleId = 79)
    INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive)
    VALUES (79, 5, 'Sales Budget', 'STOCK_SALES_BUDGET', 'STOCK_SALES_BUDGET', 3, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleId = 80)
    INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive)
    VALUES (80, 5, 'Stock Analysis', 'STOCK_ANALYSIS', NULL, 4, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleId = 81)
    INSERT INTO dbo.LM_Submodules (SubmoduleId, ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder, IsActive)
    VALUES (81, 5, 'Analysis Results', 'STOCK_ANALYSIS_RESULTS', 'STOCK_ANALYSIS_RESULT', 5, 1);

SET IDENTITY_INSERT dbo.LM_Submodules OFF;
GO

-- ── Role Permissions for submodules 77-81 ────────────────────────────────────
-- RoleId 1 = SuperAdmin, 2 = Admin, 8 = Stock/Analyst (adjust RoleId 8 if different in your DB)
DECLARE @roles TABLE (RoleId INT);
INSERT INTO @roles VALUES (1), (2), (8);

DECLARE @subs TABLE (SubmoduleId INT);
INSERT INTO @subs VALUES (77), (78), (79), (80), (81);

INSERT INTO LM_RolePermissions (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
SELECT r.RoleId, s.SubmoduleId, 1, 1, 1, 1, 1
FROM @roles r
CROSS JOIN @subs s
WHERE NOT EXISTS (
    SELECT 1 FROM LM_RolePermissions rp
    WHERE rp.RoleId = r.RoleId AND rp.SubmoduleId = s.SubmoduleId
);
GO
