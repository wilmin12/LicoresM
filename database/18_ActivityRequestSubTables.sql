-- ============================================================
-- 18_ActivityRequestSubTables.sql
-- Sub-tables: ACTIVITY_RQ_BRANDS, ACTIVITY_RQ_PRODUCTS
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── ACTIVITY_RQ_BRANDS ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ACTIVITY_RQ_BRANDS')
BEGIN
    CREATE TABLE dbo.ACTIVITY_RQ_BRANDS (
        ARB_Id            INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ARB_AR_Id         INT           NOT NULL,
        ARB_Supplier_Code NVARCHAR(5)   NULL,
        ARB_Supplier_Name NVARCHAR(100) NULL,
        ARB_Brand         NVARCHAR(100) NULL,
        ARB_Budget        DECIMAL(18,2) NULL,
        ARB_Notes         NVARCHAR(300) NULL,
        IS_Active         BIT           NOT NULL DEFAULT 1,
        Created_At        DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ARBrands_AR FOREIGN KEY (ARB_AR_Id)
            REFERENCES dbo.ACTIVITY_REQUESTS(AR_Id)
    );
    PRINT 'Table ACTIVITY_RQ_BRANDS created.';
END
ELSE
    PRINT 'Table ACTIVITY_RQ_BRANDS already exists.';
GO

-- ── ACTIVITY_RQ_PRODUCTS ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ACTIVITY_RQ_PRODUCTS')
BEGIN
    CREATE TABLE dbo.ACTIVITY_RQ_PRODUCTS (
        ARP_Id           INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ARP_AR_Id        INT            NOT NULL,
        ARP_Product_Code NVARCHAR(20)   NULL,
        ARP_Product_Name NVARCHAR(150)  NULL,
        ARP_Quantity     DECIMAL(18,4)  NULL,
        ARP_Unit         NVARCHAR(20)   NULL,
        ARP_Notes        NVARCHAR(300)  NULL,
        IS_Active        BIT            NOT NULL DEFAULT 1,
        Created_At       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ARProducts_AR FOREIGN KEY (ARP_AR_Id)
            REFERENCES dbo.ACTIVITY_REQUESTS(AR_Id)
    );
    PRINT 'Table ACTIVITY_RQ_PRODUCTS created.';
END
ELSE
    PRINT 'Table ACTIVITY_RQ_PRODUCTS already exists.';
GO

PRINT 'Script 18_ActivityRequestSubTables.sql completed.';
GO
