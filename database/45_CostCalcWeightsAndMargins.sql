-- ============================================================
-- 45_CostCalcWeightsAndMargins.sql
-- Cost Calculation - Item weights, allowed margins,
-- inland tariffs and ship charges
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── 1. Item Weights ───────────────────────────────────────────────────────────
-- Weight per case per item, used for proportional freight allocation
-- within a PO (replaces qty-based proportion with weight-based proportion)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CC_ITEM_WEIGHTS')
BEGIN
    CREATE TABLE CC_ITEM_WEIGHTS (
        IW_Id          INT IDENTITY(1,1) NOT NULL,
        IW_Item_Code   NVARCHAR(20)  NOT NULL,
        IW_Item_Descr  NVARCHAR(200) NULL,
        IW_Weight_Case DECIMAL(18,6) NOT NULL DEFAULT 0,  -- kg per case
        IW_Weight_Unit DECIMAL(18,6) NULL,                -- kg per bottle/unit (optional)
        IS_Active      BIT           NOT NULL DEFAULT 1,
        Created_At     DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_CC_ITEM_WEIGHTS      PRIMARY KEY (IW_Id),
        CONSTRAINT UQ_CC_ITEM_WEIGHTS_CODE UNIQUE (IW_Item_Code)
    );
END
GO

-- ── 2. Allowed Margins ────────────────────────────────────────────────────────
-- Reference min/max/default margins per item or commodity code.
-- Item code takes priority over commodity when both match.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CC_ALLOWED_MARGINS')
BEGIN
    CREATE TABLE CC_ALLOWED_MARGINS (
        AM_Id          INT IDENTITY(1,1) NOT NULL,
        AM_Item_Code   NVARCHAR(20)  NULL,    -- item-specific (takes priority)
        AM_Commodity   NVARCHAR(20)  NULL,    -- fallback: by commodity group
        AM_Description NVARCHAR(200) NULL,
        AM_Min_Margin  DECIMAL(10,4) NOT NULL DEFAULT 0,   -- e.g. 0.15 = 15%
        AM_Max_Margin  DECIMAL(10,4) NOT NULL DEFAULT 1,   -- e.g. 0.50 = 50%
        AM_Def_Margin  DECIMAL(10,4) NOT NULL DEFAULT 0,   -- suggested default
        IS_Active      BIT           NOT NULL DEFAULT 1,
        Created_At     DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_CC_ALLOWED_MARGINS PRIMARY KEY (AM_Id)
    );
END
GO

-- ── 3. Inland Tariffs ─────────────────────────────────────────────────────────
-- Additional inland charge rate per HS code applied on top of CIF value.
-- Separate from duty/econ/OB (which are in CC_TARIFF_ITEMS).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CC_INLAND_TARIFFS')
BEGIN
    CREATE TABLE CC_INLAND_TARIFFS (
        IT_Id          INT IDENTITY(1,1) NOT NULL,
        IT_HS_Code     NVARCHAR(20)  NOT NULL,
        IT_Description NVARCHAR(200) NULL,
        IT_Rate        DECIMAL(10,6) NOT NULL DEFAULT 0,  -- decimal e.g. 0.03 = 3%
        IS_Active      BIT           NOT NULL DEFAULT 1,
        Created_At     DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_CC_INLAND_TARIFFS      PRIMARY KEY (IT_Id),
        CONSTRAINT UQ_CC_INLAND_TARIFFS_HS   UNIQUE (IT_HS_Code)
    );
END
GO

-- ── 4. Ship Charges ──────────────────────────────────────────────────────────
-- Per-calculation additional charges (THC, doc fees, B/L, etc.)
-- Distributed across all items proportionally by qty or weight.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CC_SHIP_CHARGES')
BEGIN
    CREATE TABLE CC_SHIP_CHARGES (
        SC_Id          INT IDENTITY(1,1) NOT NULL,
        SC_Calc_Number INT           NOT NULL,
        SC_Charge_Code NVARCHAR(20)  NOT NULL,
        SC_Description NVARCHAR(200) NULL,
        SC_Amount      DECIMAL(18,2) NOT NULL DEFAULT 0,
        SC_Currency    NVARCHAR(3)   NULL,
        SC_Rate        DECIMAL(18,6) NULL,    -- exchange rate (NULL = use calc rate)
        Created_At     DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_CC_SHIP_CHARGES PRIMARY KEY (SC_Id),
        CONSTRAINT FK_SC_Calc FOREIGN KEY (SC_Calc_Number)
            REFERENCES COST_CALC_FIN(CC_Calc_Number)
    );
END
GO

-- ── 5. Add columns to COST_CALC_PO_DET_FIN ───────────────────────────────────
-- Store inland tariff and ship charge allocations per detail line
-- Store allowed margin reference for reporting/validation
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('COST_CALC_PO_DET_FIN') AND name = 'CCPD_Inland_Tariff')
    ALTER TABLE COST_CALC_PO_DET_FIN ADD CCPD_Inland_Tariff  DECIMAL(18,6) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('COST_CALC_PO_DET_FIN') AND name = 'CCPD_Ship_Charges')
    ALTER TABLE COST_CALC_PO_DET_FIN ADD CCPD_Ship_Charges   DECIMAL(18,6) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('COST_CALC_PO_DET_FIN') AND name = 'CCPD_Allowed_Min')
    ALTER TABLE COST_CALC_PO_DET_FIN ADD CCPD_Allowed_Min    DECIMAL(10,4) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('COST_CALC_PO_DET_FIN') AND name = 'CCPD_Allowed_Max')
    ALTER TABLE COST_CALC_PO_DET_FIN ADD CCPD_Allowed_Max    DECIMAL(10,4) NULL;
GO

-- Add to CC_SHIP_CHARGES total to PO HEAD for reporting
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('COST_CALC_PO_HEAD_FIN') AND name = 'CCPH_Ship_Charges')
    ALTER TABLE COST_CALC_PO_HEAD_FIN ADD CCPH_Ship_Charges  DECIMAL(18,2) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('COST_CALC_PO_HEAD_FIN') AND name = 'CCPH_Inland_Tariff')
    ALTER TABLE COST_CALC_PO_HEAD_FIN ADD CCPH_Inland_Tariff DECIMAL(18,2) NULL;
GO
