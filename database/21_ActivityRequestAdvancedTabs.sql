-- ============================================================
-- 21_ActivityRequestAdvancedTabs.sql
-- Sub-tables: Cash, Print, Radio, POS Material, Promotions, Others
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── ACTIVITY_RQ_CASH ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ACTIVITY_RQ_CASH')
BEGIN
    CREATE TABLE dbo.ACTIVITY_RQ_CASH (
        ARC_Id        INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ARC_AR_Id     INT            NOT NULL,
        ARC_Type      NVARCHAR(50)   NULL,     -- Cash, Check, Transfer
        ARC_Amount    DECIMAL(18,2)  NULL,
        ARC_Reference NVARCHAR(100)  NULL,
        ARC_Notes     NVARCHAR(300)  NULL,
        IS_Active     BIT            NOT NULL DEFAULT 1,
        Created_At    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ARC_AR FOREIGN KEY (ARC_AR_Id) REFERENCES dbo.ACTIVITY_REQUESTS(AR_Id)
    );
    PRINT 'Table ACTIVITY_RQ_CASH created.';
END ELSE PRINT 'Table ACTIVITY_RQ_CASH already exists.';
GO

-- ── ACTIVITY_RQ_PRINT ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ACTIVITY_RQ_PRINT')
BEGIN
    CREATE TABLE dbo.ACTIVITY_RQ_PRINT (
        ARPR_Id          INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ARPR_AR_Id       INT            NOT NULL,
        ARPR_Publication NVARCHAR(150)  NULL,
        ARPR_Format      NVARCHAR(100)  NULL,
        ARPR_Size        NVARCHAR(50)   NULL,
        ARPR_Cost        DECIMAL(18,2)  NULL,
        ARPR_Notes       NVARCHAR(300)  NULL,
        IS_Active        BIT            NOT NULL DEFAULT 1,
        Created_At       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ARPR_AR FOREIGN KEY (ARPR_AR_Id) REFERENCES dbo.ACTIVITY_REQUESTS(AR_Id)
    );
    PRINT 'Table ACTIVITY_RQ_PRINT created.';
END ELSE PRINT 'Table ACTIVITY_RQ_PRINT already exists.';
GO

-- ── ACTIVITY_RQ_RADIO ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ACTIVITY_RQ_RADIO')
BEGIN
    CREATE TABLE dbo.ACTIVITY_RQ_RADIO (
        ARR_Id        INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ARR_AR_Id     INT            NOT NULL,
        ARR_Station   NVARCHAR(150)  NULL,
        ARR_Duration  NVARCHAR(50)   NULL,    -- e.g. "30s", "60s"
        ARR_Frequency INT            NULL,    -- number of spots
        ARR_Cost      DECIMAL(18,2)  NULL,
        ARR_Notes     NVARCHAR(300)  NULL,
        IS_Active     BIT            NOT NULL DEFAULT 1,
        Created_At    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ARR_AR FOREIGN KEY (ARR_AR_Id) REFERENCES dbo.ACTIVITY_REQUESTS(AR_Id)
    );
    PRINT 'Table ACTIVITY_RQ_RADIO created.';
END ELSE PRINT 'Table ACTIVITY_RQ_RADIO already exists.';
GO

-- ── ACTIVITY_RQ_POS_MAT ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ACTIVITY_RQ_POS_MAT')
BEGIN
    CREATE TABLE dbo.ACTIVITY_RQ_POS_MAT (
        ARPM_Id       INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ARPM_AR_Id    INT            NOT NULL,
        ARPM_Code     NVARCHAR(20)   NULL,
        ARPM_Name     NVARCHAR(150)  NULL,
        ARPM_Quantity INT            NULL,
        ARPM_Unit     NVARCHAR(20)   NULL,
        ARPM_Notes    NVARCHAR(300)  NULL,
        IS_Active     BIT            NOT NULL DEFAULT 1,
        Created_At    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ARPM_AR FOREIGN KEY (ARPM_AR_Id) REFERENCES dbo.ACTIVITY_REQUESTS(AR_Id)
    );
    PRINT 'Table ACTIVITY_RQ_POS_MAT created.';
END ELSE PRINT 'Table ACTIVITY_RQ_POS_MAT already exists.';
GO

-- ── ACTIVITY_RQ_PROMOTIONS ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ACTIVITY_RQ_PROMOTIONS')
BEGIN
    CREATE TABLE dbo.ACTIVITY_RQ_PROMOTIONS (
        ARPO_Id          INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ARPO_AR_Id       INT            NOT NULL,
        ARPO_Type        NVARCHAR(100)  NULL,
        ARPO_Description NVARCHAR(300)  NULL,
        ARPO_Cost        DECIMAL(18,2)  NULL,
        ARPO_Notes       NVARCHAR(300)  NULL,
        IS_Active        BIT            NOT NULL DEFAULT 1,
        Created_At       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ARPO_AR FOREIGN KEY (ARPO_AR_Id) REFERENCES dbo.ACTIVITY_REQUESTS(AR_Id)
    );
    PRINT 'Table ACTIVITY_RQ_PROMOTIONS created.';
END ELSE PRINT 'Table ACTIVITY_RQ_PROMOTIONS already exists.';
GO

-- ── ACTIVITY_RQ_OTHERS ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ACTIVITY_RQ_OTHERS')
BEGIN
    CREATE TABLE dbo.ACTIVITY_RQ_OTHERS (
        ARO_Id          INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ARO_AR_Id       INT            NOT NULL,
        ARO_Description NVARCHAR(300)  NULL,
        ARO_Cost        DECIMAL(18,2)  NULL,
        ARO_Notes       NVARCHAR(300)  NULL,
        IS_Active       BIT            NOT NULL DEFAULT 1,
        Created_At      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ARO_AR FOREIGN KEY (ARO_AR_Id) REFERENCES dbo.ACTIVITY_REQUESTS(AR_Id)
    );
    PRINT 'Table ACTIVITY_RQ_OTHERS created.';
END ELSE PRINT 'Table ACTIVITY_RQ_OTHERS already exists.';
GO

PRINT 'Script 21_ActivityRequestAdvancedTabs.sql completed.';
GO
