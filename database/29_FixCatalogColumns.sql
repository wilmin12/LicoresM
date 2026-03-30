-- ============================================================
-- Script 29: Add missing IS_Active / Created_At columns
--            to catalog tables that pre-existed the schema
-- ============================================================
USE LicoresMaduoDB;
GO

-- Helper macro: add column only if it doesn't exist
-- We'll do it table by table for clarity.

-- ── DEPARTMENTS ───────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DEPARTMENTS') AND name = 'IS_Active')
    ALTER TABLE dbo.DEPARTMENTS ADD IS_Active BIT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DEPARTMENTS') AND name = 'Created_At')
    ALTER TABLE dbo.DEPARTMENTS ADD Created_At DATETIME NOT NULL DEFAULT GETUTCDATE();
GO

-- ── EENHEDEN ──────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EENHEDEN') AND name = 'IS_Active')
    ALTER TABLE dbo.EENHEDEN ADD IS_Active BIT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EENHEDEN') AND name = 'Created_At')
    ALTER TABLE dbo.EENHEDEN ADD Created_At DATETIME NOT NULL DEFAULT GETUTCDATE();
GO

-- ── RECEIVERS ─────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RECEIVERS') AND name = 'IS_Active')
    ALTER TABLE dbo.RECEIVERS ADD IS_Active BIT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RECEIVERS') AND name = 'Created_At')
    ALTER TABLE dbo.RECEIVERS ADD Created_At DATETIME NOT NULL DEFAULT GETUTCDATE();
GO

-- ── REQUESTORS ────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.REQUESTORS') AND name = 'IS_Active')
    ALTER TABLE dbo.REQUESTORS ADD IS_Active BIT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.REQUESTORS') AND name = 'Created_At')
    ALTER TABLE dbo.REQUESTORS ADD Created_At DATETIME NOT NULL DEFAULT GETUTCDATE();
GO

-- ── REQUESTORS_VENDOR ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.REQUESTORS_VENDOR') AND name = 'IS_Active')
    ALTER TABLE dbo.REQUESTORS_VENDOR ADD IS_Active BIT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.REQUESTORS_VENDOR') AND name = 'Created_At')
    ALTER TABLE dbo.REQUESTORS_VENDOR ADD Created_At DATETIME NOT NULL DEFAULT GETUTCDATE();
GO

-- ── COST_TYPE ─────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.COST_TYPE') AND name = 'IS_Active')
    ALTER TABLE dbo.COST_TYPE ADD IS_Active BIT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.COST_TYPE') AND name = 'Created_At')
    ALTER TABLE dbo.COST_TYPE ADD Created_At DATETIME NOT NULL DEFAULT GETUTCDATE();
GO

-- ── VEHICLE_TYPE ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VEHICLE_TYPE') AND name = 'IS_Active')
    ALTER TABLE dbo.VEHICLE_TYPE ADD IS_Active BIT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VEHICLE_TYPE') AND name = 'Created_At')
    ALTER TABLE dbo.VEHICLE_TYPE ADD Created_At DATETIME NOT NULL DEFAULT GETUTCDATE();
GO

-- ── VEHICLES ──────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VEHICLES') AND name = 'IS_Active')
    ALTER TABLE dbo.VEHICLES ADD IS_Active BIT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VEHICLES') AND name = 'Created_At')
    ALTER TABLE dbo.VEHICLES ADD Created_At DATETIME NOT NULL DEFAULT GETUTCDATE();
GO

-- ── VENDORS ───────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VENDORS') AND name = 'IS_Active')
    ALTER TABLE dbo.VENDORS ADD IS_Active BIT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VENDORS') AND name = 'Created_At')
    ALTER TABLE dbo.VENDORS ADD Created_At DATETIME NOT NULL DEFAULT GETUTCDATE();
GO

-- ── AB_PRODUCTS (if exists) ───────────────────────────────────────────────────
IF OBJECT_ID('dbo.AB_PRODUCTS') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AB_PRODUCTS') AND name = 'IS_Active')
        ALTER TABLE dbo.AB_PRODUCTS ADD IS_Active BIT NOT NULL DEFAULT 1;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AB_PRODUCTS') AND name = 'Created_At')
        ALTER TABLE dbo.AB_PRODUCTS ADD Created_At DATETIME NOT NULL DEFAULT GETUTCDATE();
END
GO

PRINT 'Script 29 complete — catalog columns patched.';
GO
