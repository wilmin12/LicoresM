-- ============================================================
-- Script: 64_CreateDhwSystemTable.sql
-- Target: LicoresMaduoDB
-- Description: Creates SYSTEM_TABLE for Cost Calculation config
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SYSTEM_TABLE')
BEGIN
    CREATE TABLE SYSTEM_TABLE (
        COMP_CODE                       VARCHAR(10)     NOT NULL,
        COMP_NAME                       VARCHAR(100)    NULL,
        COMP_ADDRESS                    VARCHAR(300)    NULL,
        COMP_CONTACT                    VARCHAR(100)    NULL,
        COMP_TEL                        VARCHAR(50)     NULL,
        COMP_FAX                        VARCHAR(50)     NULL,
        COMP_EMAIL                      VARCHAR(200)    NULL,
        COMP_CURR_LOCAL                 VARCHAR(3)      NULL,
        COMP_CURR_USD                   VARCHAR(3)      NULL,
        COMP_RATE_USD                   DECIMAL(18,6)   NULL,
        COMP_DOC_NUMBER                 VARCHAR(50)     NULL,
        COMP_DOC_EMAIL                  VARCHAR(200)    NULL,
        COMP_INSURANCE                  DECIMAL(18,6)   NULL,
        COMP_TRANSPORT                  DECIMAL(18,4)   NULL,
        COMP_UNLOADING                  DECIMAL(18,4)   NULL,
        COMP_LOCAL_HANDLING             DECIMAL(18,4)   NULL,
        COMP_FWCODE                     VARCHAR(10)     NULL,
        COMP_FWNAME                     VARCHAR(50)     NULL,
        COMP_FWCURR                     VARCHAR(3)      NULL,
        COMP_SM01_CODE                  VARCHAR(10)     NULL,
        COMP_SM01_EMAIL                 VARCHAR(200)    NULL,
        COMP_SM02_CODE                  VARCHAR(10)     NULL,
        COMP_SM02_EMAIL                 VARCHAR(200)    NULL,
        COMP_SM03_CODE                  VARCHAR(10)     NULL,
        COMP_SM03_EMAIL                 VARCHAR(200)    NULL,
        COMP_PC_EMAIL                   VARCHAR(200)    NULL,
        COMP_OZ_FACTOR                  DECIMAL(18,6)   NULL,
        COMP_LITER_MULTIPLIER           DECIMAL(18,6)   NULL,
        COMP_PRICE_CHANGE_PERC          DECIMAL(10,4)   NULL,
        COMP_PRICE_CHANGE_AMNT          DECIMAL(18,4)   NULL,
        COMP_STORE_RETAIL_PERC          DECIMAL(10,4)   NULL,
        COMP_STORE_ALLIANCE_PERC        DECIMAL(10,4)   NULL,
        COMP_STORE_NORSA_PERC           DECIMAL(10,4)   NULL,
        COMP_PRICE_CHANGE_YEAR          INT             NULL,
        COMP_PRICE_CHANGE_SEQ           INT             NULL,
        COMP_EMAIL_PURCH_DEP            VARCHAR(200)    NULL,
        COMP_EMAIL_APPROVAL             VARCHAR(200)    NULL,
        COMP_PATH_COST_CHANGES          VARCHAR(500)    NULL,
        COMP_PATH_PRICE_CHANGES         VARCHAR(500)    NULL,
        COMP_PATH_COST_CALC             VARCHAR(500)    NULL,
        COMP_EMAIL_COST_CHANGE          VARCHAR(200)    NULL,
        COMP_EMAIL_CONFIRM_MNGR         VARCHAR(200)    NULL,
        COMP_EMAIL_PRICE_CHANGES_FINANCE VARCHAR(200)   NULL,
        CONSTRAINT PK_SYSTEM_TABLE PRIMARY KEY (COMP_CODE)
    );

    -- Seed initial row
    INSERT INTO SYSTEM_TABLE (COMP_CODE, COMP_NAME)
    VALUES ('LM', 'Licores Maduro');

    PRINT 'SYSTEM_TABLE created and seeded.';
END
ELSE
    PRINT 'SYSTEM_TABLE already exists — skipped.';
GO
