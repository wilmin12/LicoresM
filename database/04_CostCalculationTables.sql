-- ============================================================
-- 04_CostCalculationTables.sql
-- Cost Calculation module tables
-- ============================================================

USE LicoresMaduoDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'COST_CALC_FIN')
BEGIN
    CREATE TABLE COST_CALC_FIN (
        CC_Calc_Number   INT IDENTITY(1,1) NOT NULL,
        CC_Calc_Date     DATE          NULL,
        CC_Forwarder_Code NVARCHAR(10) NULL,
        CC_Forwarder_Name NVARCHAR(50) NULL,
        CC_CurrCode      NVARCHAR(3)   NULL,
        CC_CurrRate      DECIMAL(18,6) NULL,
        CC_Freight       DECIMAL(18,2) NULL,
        CC_Transport     DECIMAL(18,2) NULL,
        CC_Unloading     DECIMAL(18,2) NULL,
        CC_Local_Handling DECIMAL(18,2) NULL,
        CC_TotWeight     DECIMAL(18,4) NULL,
        CC_Status        NVARCHAR(2)   NULL,
        CC_TotOrd        INT           NULL,
        CC_TotQty        DECIMAL(18,4) NULL,
        CC_Warehouse     NVARCHAR(3)   NULL,
        CC_Created_By    NVARCHAR(50)  NULL,
        CC_Created_At    DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_COST_CALC_FIN PRIMARY KEY (CC_Calc_Number)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'COST_CALC_PO_HEAD_FIN')
BEGIN
    CREATE TABLE COST_CALC_PO_HEAD_FIN (
        CCPH_Calc_Number      INT           NOT NULL,
        CCPH_LMPoNo           NVARCHAR(10)  NOT NULL,
        CCPH_VendNo           NVARCHAR(6)   NULL,
        CCPH_VendName         NVARCHAR(50)  NULL,
        CCPH_WareHouse        NVARCHAR(3)   NULL,
        CCPH_CurrCode         NVARCHAR(3)   NULL,
        CCPH_CurrRate         DECIMAL(18,6) NULL,
        CCPH_CurrRate_Cust    DECIMAL(18,6) NULL,
        CCPH_Inv_Number       NVARCHAR(20)  NULL,
        CCPH_Inv_Date         DATE          NULL,
        CCPH_Local_Handling   DECIMAL(18,2) NULL,
        CCPH_Duties           DECIMAL(18,2) NULL,
        CCPH_Econ_Surch       DECIMAL(18,2) NULL,
        CCPH_OB               DECIMAL(18,2) NULL,
        CCPH_Weight           DECIMAL(18,4) NULL,
        CCPH_Freight          DECIMAL(18,2) NULL,
        CCPH_Transport        DECIMAL(18,2) NULL,
        CCPH_Unloading        DECIMAL(18,2) NULL,
        CCPH_Insurance        DECIMAL(18,2) NULL,
        CCPH_TotQty           DECIMAL(18,4) NULL,
        CCPH_TotAmount_FC     DECIMAL(18,2) NULL,
        CCPH_TotAmount        DECIMAL(18,2) NULL,
        CCPH_Inland_Freight_FF DECIMAL(18,2) NULL,
        CCPH_Status           NVARCHAR(2)   NULL,
        CCPH_Created_By       NVARCHAR(50)  NULL,
        CCPH_Confirmed_By     NVARCHAR(50)  NULL,
        CCPH_Approved_By      NVARCHAR(50)  NULL,
        CONSTRAINT PK_COST_CALC_PO_HEAD_FIN PRIMARY KEY (CCPH_Calc_Number, CCPH_LMPoNo),
        CONSTRAINT FK_CCPH_Calc FOREIGN KEY (CCPH_Calc_Number) REFERENCES COST_CALC_FIN(CC_Calc_Number)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'COST_CALC_PO_DET_FIN')
BEGIN
    CREATE TABLE COST_CALC_PO_DET_FIN (
        CCPD_Calc_Number   INT           NOT NULL,
        CCPD_LMPoNo        NVARCHAR(10)  NOT NULL,
        CCPD_ItemNo        NVARCHAR(20)  NOT NULL,
        CCPD_Item_Descr    NVARCHAR(50)  NULL,
        CCPD_UnitCase      INT           NULL,
        CCPD_OrdQty        DECIMAL(18,4) NULL,
        CCPD_FOB_Price     DECIMAL(18,6) NULL,
        CCPD_FOB_Price_Tot DECIMAL(18,2) NULL,
        CCPD_Inland_Freight DECIMAL(18,6) NULL,
        CCPD_Freight       DECIMAL(18,6) NULL,
        CCPD_Local_Handl   DECIMAL(18,6) NULL,
        CCPD_Duties        DECIMAL(18,6) NULL,
        CCPD_Econ_Surch    DECIMAL(18,6) NULL,
        CCPD_OB            DECIMAL(18,6) NULL,
        CCPD_Insurance     DECIMAL(18,6) NULL,
        CCPD_Transport     DECIMAL(18,6) NULL,
        CCPD_Unloading     DECIMAL(18,6) NULL,
        CCPD_Final_Cost    DECIMAL(18,6) NULL,
        CCPD_Warehouse     NVARCHAR(3)   NULL,
        CCPD_Margin_Perc   DECIMAL(10,4) NULL,
        CCPD_Selling_Price DECIMAL(18,6) NULL,
        CONSTRAINT PK_COST_CALC_PO_DET_FIN PRIMARY KEY (CCPD_Calc_Number, CCPD_LMPoNo, CCPD_ItemNo),
        CONSTRAINT FK_CCPD_Head FOREIGN KEY (CCPD_Calc_Number, CCPD_LMPoNo)
            REFERENCES COST_CALC_PO_HEAD_FIN(CCPH_Calc_Number, CCPH_LMPoNo)
    );
END
GO
