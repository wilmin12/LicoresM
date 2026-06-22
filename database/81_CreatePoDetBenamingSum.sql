USE LicoresMaduoDB;
GO

-- Script 81: Summary table per HandelsBenaming per PO per calculation
-- Stores one row per unique item (handelsbenaming) per PO, aggregating
-- totals for inland freight, ocean freight, customs value, liters,
-- duties, econ surcharge, OB, and a snapshot of the tariff rates used.

IF OBJECT_ID('dbo.CC_CALC_PO_DET_BENAMING_SUM', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CC_CALC_PO_DET_BENAMING_SUM (
        CCPDS_Calc_Number           INT             NOT NULL,
        CCPDS_LMPoNo                NVARCHAR(20)    NOT NULL,
        CCPDS_HandelsBenam          NVARCHAR(20)    NOT NULL,
        CCPDS_GoedCode              NVARCHAR(10)    NULL,
        CCPDS_Tot_Inland_Freight    DECIMAL(18,4)   NOT NULL CONSTRAINT DF_CCPDS_Inland    DEFAULT 0,
        CCPDS_Tot_Freight           DECIMAL(18,4)   NOT NULL CONSTRAINT DF_CCPDS_Freight   DEFAULT 0,
        CCPDS_Tot_Waarde            DECIMAL(18,4)   NOT NULL CONSTRAINT DF_CCPDS_Waarde    DEFAULT 0,
        CCPDS_Tot_Liters            DECIMAL(18,4)   NOT NULL CONSTRAINT DF_CCPDS_Liters    DEFAULT 0,
        CCPDS_Duties                DECIMAL(18,4)   NOT NULL CONSTRAINT DF_CCPDS_Duties    DEFAULT 0,
        CCPDS_Econ_Surch            DECIMAL(18,4)   NOT NULL CONSTRAINT DF_CCPDS_Econ      DEFAULT 0,
        CCPDS_OB                    DECIMAL(18,4)   NOT NULL CONSTRAINT DF_CCPDS_OB        DEFAULT 0,
        CCPDS_TAR_T01               DECIMAL(18,4)   NULL,
        CCPDS_TAR_T02               DECIMAL(18,4)   NULL,
        CCPDS_TAR_T04               DECIMAL(18,4)   NULL,
        CCPDS_TAR_T05               DECIMAL(18,4)   NULL,
        CCPDS_TAR_T06               DECIMAL(18,4)   NULL,
        CCPDS_TAR_T07               DECIMAL(18,4)   NULL,
        CCPDS_TAR_T08               DECIMAL(18,4)   NULL,
        CCPDS_TAR_T09               DECIMAL(18,4)   NULL,
        CCPDS_TAR_T10               DECIMAL(18,4)   NULL,
        CCPDS_TAR_T12               DECIMAL(18,4)   NULL,
        CONSTRAINT PK_CC_CALC_PO_DET_BENAMING_SUM
            PRIMARY KEY (CCPDS_Calc_Number, CCPDS_LMPoNo, CCPDS_HandelsBenam),
        CONSTRAINT FK_CCPDS_CalcNumber
            FOREIGN KEY (CCPDS_Calc_Number)
            REFERENCES dbo.COST_CALC_FIN (CC_Calc_Number)
            ON DELETE CASCADE
    );

    PRINT 'Table CC_CALC_PO_DET_BENAMING_SUM created.';
END
ELSE
    PRINT 'Table CC_CALC_PO_DET_BENAMING_SUM already exists — skipped.';
GO
