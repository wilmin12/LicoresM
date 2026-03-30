-- ============================================================
-- Licores Maduro - Cost Calculation Output Tables
-- Script: 04_CostCalculationTables.sql
-- Run on: LicoresMaduoDB
-- ============================================================

USE LicoresMaduoDB;
GO

-- ============================================================
-- MODULE 3: COST CALCULATION (3 tables)
-- ============================================================

-- ------------------------------------------------------------
-- Table 1: COST_CALC_FIN
-- Container/Forwarder level header for a cost calculation.
-- Groups one or more Purchase Orders into a single calculation.
-- ------------------------------------------------------------

IF OBJECT_ID('dbo.COST_CALC_PO_DET_FIN',  'U') IS NOT NULL DROP TABLE dbo.COST_CALC_PO_DET_FIN;
IF OBJECT_ID('dbo.COST_CALC_PO_HEAD_FIN', 'U') IS NOT NULL DROP TABLE dbo.COST_CALC_PO_HEAD_FIN;
IF OBJECT_ID('dbo.COST_CALC_FIN',          'U') IS NOT NULL DROP TABLE dbo.COST_CALC_FIN;
GO

CREATE TABLE dbo.COST_CALC_FIN (
    CC_Calc_Number      INT             NOT NULL IDENTITY(1,1),
    CC_Calc_Date        DATETIME2       NOT NULL DEFAULT GETDATE(),
    CC_Forwarder_Code   NVARCHAR(10)    NULL,
    CC_Forwarder_Name   NVARCHAR(50)    NULL,
    CC_CurrCode         NVARCHAR(3)     NULL,           -- Foreign currency code (e.g. USD)
    CC_CurrRate         DECIMAL(18,6)   NULL,           -- Exchange rate to local currency
    CC_Freight          DECIMAL(18,4)   NULL,           -- Ocean Freight (foreign currency)
    CC_Transport        DECIMAL(18,4)   NULL,           -- Domestic transport (local currency)
    CC_Unloading        DECIMAL(18,4)   NULL,           -- Unloading charges (local currency)
    CC_Local_Handling   DECIMAL(18,4)   NULL,           -- Port/local handling (local currency)
    CC_TotWeight        DECIMAL(18,4)   NULL,           -- Total container weight (kg)
    CC_Status           NVARCHAR(2)     NOT NULL DEFAULT 'DR',  -- DR=Draft, CF=Confirmed, AP=Approved
    CC_TotOrd           INT             NULL,           -- Number of POs in this calculation
    CC_TotQty           DECIMAL(18,2)   NULL,           -- Total cases across all POs
    CC_Warehouse        NVARCHAR(3)     NULL,           -- DP=Duty Paid, DF=Duty Free, ST=Store
    CC_Created_By       NVARCHAR(50)    NULL,
    CC_Created_At       DATETIME2       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_COST_CALC_FIN         PRIMARY KEY CLUSTERED (CC_Calc_Number),
    CONSTRAINT CK_COST_CALC_FIN_Status  CHECK (CC_Status IN ('DR','CF','AP'))
);
GO

CREATE INDEX IX_COST_CALC_FIN_Status    ON dbo.COST_CALC_FIN (CC_Status);
CREATE INDEX IX_COST_CALC_FIN_Date      ON dbo.COST_CALC_FIN (CC_Calc_Date DESC);
GO

-- ------------------------------------------------------------
-- Table 2: COST_CALC_PO_HEAD_FIN
-- One row per Purchase Order within a calculation.
-- ------------------------------------------------------------

CREATE TABLE dbo.COST_CALC_PO_HEAD_FIN (
    CCPH_Calc_Number        INT             NOT NULL,
    CCPH_LMPoNo             NVARCHAR(10)    NOT NULL,   -- LM Purchase Order Number
    CCPH_VendNo             NVARCHAR(6)     NULL,       -- Vendor/Supplier code
    CCPH_VendName           NVARCHAR(50)    NULL,
    CCPH_WareHouse          NVARCHAR(3)     NULL,       -- Receiving warehouse
    CCPH_CurrCode           NVARCHAR(3)     NULL,       -- Invoice currency
    CCPH_CurrRate           DECIMAL(18,6)   NULL,       -- Exchange rate (bank)
    CCPH_CurrRate_Cust      DECIMAL(18,6)   NULL,       -- Exchange rate (customs)
    CCPH_Inv_Number         NVARCHAR(20)    NULL,       -- Invoice number
    CCPH_Inv_Date           DATETIME2       NULL,       -- Invoice date
    CCPH_Local_Handling     DECIMAL(18,4)   NULL,       -- Local handling allocated to this PO
    CCPH_Duties             DECIMAL(18,4)   NULL,       -- Total customs duties
    CCPH_Econ_Surch         DECIMAL(18,4)   NULL,       -- Economic surcharge total
    CCPH_OB                 DECIMAL(18,4)   NULL,       -- Import tax (OB) total
    CCPH_Weight             DECIMAL(18,4)   NULL,       -- PO total weight (kg)
    CCPH_Freight            DECIMAL(18,4)   NULL,       -- Ocean freight allocated to this PO (local currency)
    CCPH_Transport          DECIMAL(18,4)   NULL,       -- Transport allocated to this PO
    CCPH_Unloading          DECIMAL(18,4)   NULL,       -- Unloading allocated to this PO
    CCPH_Insurance          DECIMAL(18,4)   NULL,       -- Insurance allocated to this PO
    CCPH_TotQty             DECIMAL(18,2)   NULL,       -- Total cases in PO
    CCPH_TotAmount_FC       DECIMAL(18,4)   NULL,       -- Total invoice amount (foreign currency)
    CCPH_TotAmount          DECIMAL(18,4)   NULL,       -- Total amount (local currency)
    CCPH_Inland_Freight_FF  DECIMAL(18,4)   NULL,       -- Inland freight (freight forwarder)
    CCPH_Status             NVARCHAR(2)     NOT NULL DEFAULT 'DR',
    CCPH_Created_By         NVARCHAR(50)    NULL,
    CCPH_Confirmed_By       NVARCHAR(50)    NULL,
    CCPH_Approved_By        NVARCHAR(50)    NULL,

    CONSTRAINT PK_COST_CALC_PO_HEAD_FIN
        PRIMARY KEY CLUSTERED (CCPH_Calc_Number, CCPH_LMPoNo),
    CONSTRAINT FK_CCPH_CalcHeader
        FOREIGN KEY (CCPH_Calc_Number)
        REFERENCES dbo.COST_CALC_FIN (CC_Calc_Number)
        ON DELETE CASCADE,
    CONSTRAINT CK_CCPH_Status
        CHECK (CCPH_Status IN ('DR','CF','AP'))
);
GO

CREATE INDEX IX_CCPH_PoNo     ON dbo.COST_CALC_PO_HEAD_FIN (CCPH_LMPoNo);
CREATE INDEX IX_CCPH_Status   ON dbo.COST_CALC_PO_HEAD_FIN (CCPH_Status);
GO

-- ------------------------------------------------------------
-- Table 3: COST_CALC_PO_DET_FIN
-- One row per item/product line within a PO calculation.
-- Contains the full cost breakdown per item.
-- ------------------------------------------------------------

CREATE TABLE dbo.COST_CALC_PO_DET_FIN (
    CCPD_Calc_Number        INT             NOT NULL,
    CCPD_LMPoNo             NVARCHAR(10)    NOT NULL,
    CCPD_ItemNo             NVARCHAR(20)    NOT NULL,
    CCPD_Item_Descr         NVARCHAR(50)    NULL,
    CCPD_UnitCase           INT             NULL,           -- Units per case
    CCPD_OrdQty             DECIMAL(18,2)   NULL,           -- Ordered quantity (cases)

    -- Cost Components (all in local currency, per case unless noted)
    CCPD_FOB_Price          DECIMAL(18,4)   NULL,           -- FOB price per case
    CCPD_FOB_Price_Tot      DECIMAL(18,4)   NULL,           -- FOB total for this line
    CCPD_Inland_Freight     DECIMAL(18,4)   NULL,           -- Inland freight allocated
    CCPD_Freight            DECIMAL(18,4)   NULL,           -- Ocean freight allocated
    CCPD_Local_Handl        DECIMAL(18,4)   NULL,           -- Local handling allocated
    CCPD_Duties             DECIMAL(18,4)   NOT NULL DEFAULT 0,  -- Customs duties (formula pending)
    CCPD_Econ_Surch         DECIMAL(18,4)   NOT NULL DEFAULT 0,  -- Economic surcharge (formula pending)
    CCPD_OB                 DECIMAL(18,4)   NOT NULL DEFAULT 0,  -- Import tax OB (formula pending)
    CCPD_Insurance          DECIMAL(18,4)   NULL,           -- Insurance allocated
    CCPD_Transport          DECIMAL(18,4)   NULL,           -- Transport allocated
    CCPD_Unloading          DECIMAL(18,4)   NULL,           -- Unloading allocated

    -- Results
    CCPD_Final_Cost         DECIMAL(18,4)   NULL,           -- Final cost price
    CCPD_Warehouse          NVARCHAR(3)     NULL,           -- DP / DF / ST
    CCPD_Margin_Perc        DECIMAL(8,4)    NULL,           -- Margin percentage applied
    CCPD_Selling_Price      DECIMAL(18,4)   NULL,           -- Calculated selling price

    CONSTRAINT PK_COST_CALC_PO_DET_FIN
        PRIMARY KEY CLUSTERED (CCPD_Calc_Number, CCPD_LMPoNo, CCPD_ItemNo),
    CONSTRAINT FK_CCPD_PoHead
        FOREIGN KEY (CCPD_Calc_Number, CCPD_LMPoNo)
        REFERENCES dbo.COST_CALC_PO_HEAD_FIN (CCPH_Calc_Number, CCPH_LMPoNo)
        ON DELETE CASCADE
);
GO

CREATE INDEX IX_CCPD_ItemNo   ON dbo.COST_CALC_PO_DET_FIN (CCPD_ItemNo);
GO

-- ============================================================
-- Verification
-- ============================================================
SELECT
    t.name                          AS TableName,
    COUNT(c.column_id)              AS Columns,
    SUM(CASE WHEN i.is_primary_key = 1 THEN 1 ELSE 0 END) AS PKIndexes
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
LEFT JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.column_id = c.column_id
LEFT JOIN sys.indexes i ON i.object_id = t.object_id AND i.index_id = ic.index_id
WHERE t.name IN ('COST_CALC_FIN','COST_CALC_PO_HEAD_FIN','COST_CALC_PO_DET_FIN')
GROUP BY t.name
ORDER BY t.name;
GO

PRINT '✓ Cost Calculation tables created successfully in LicoresMaduoDB.';
GO
