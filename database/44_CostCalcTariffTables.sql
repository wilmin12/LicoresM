-- ============================================================
-- 44_CostCalcTariffTables.sql
-- Cost Calculation - Tariff tables
-- CC_TARIFF_ITEMS      : HS codes with duty/econ/OB rates
-- CC_GOODS_CLASSIFICATION : item → HS code mapping
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── 1. HS Code Tariff Rates ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CC_TARIFF_ITEMS')
BEGIN
    CREATE TABLE CC_TARIFF_ITEMS (
        TI_Id          INT IDENTITY(1,1) NOT NULL,
        TI_HS_Code     NVARCHAR(20)  NOT NULL,
        TI_Description NVARCHAR(200) NULL,
        TI_Duty_Rate   DECIMAL(10,6) NOT NULL DEFAULT 0,   -- e.g. 0.15 = 15%
        TI_Econ_Rate   DECIMAL(10,6) NOT NULL DEFAULT 0,   -- e.g. 0.03 = 3%
        TI_OB_Rate     DECIMAL(10,6) NOT NULL DEFAULT 0,   -- e.g. 0.06 = 6%
        IS_Active      BIT           NOT NULL DEFAULT 1,
        Created_At     DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_CC_TARIFF_ITEMS PRIMARY KEY (TI_Id),
        CONSTRAINT UQ_CC_TARIFF_ITEMS_HS UNIQUE (TI_HS_Code)
    );
END
GO

-- ── 2. Goods Classification (item → HS code) ─────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CC_GOODS_CLASSIFICATION')
BEGIN
    CREATE TABLE CC_GOODS_CLASSIFICATION (
        GC_Id          INT IDENTITY(1,1) NOT NULL,
        GC_Item_Code   NVARCHAR(20)  NOT NULL,
        GC_Item_Descr  NVARCHAR(200) NULL,
        GC_HS_Code     NVARCHAR(20)  NOT NULL,
        IS_Active      BIT           NOT NULL DEFAULT 1,
        Created_At     DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_CC_GOODS_CLASSIFICATION PRIMARY KEY (GC_Id),
        CONSTRAINT UQ_CC_GOODS_CLASS_ITEM UNIQUE (GC_Item_Code),
        CONSTRAINT FK_CC_GC_HS FOREIGN KEY (GC_HS_Code)
            REFERENCES CC_TARIFF_ITEMS (TI_HS_Code)
    );
END
GO
