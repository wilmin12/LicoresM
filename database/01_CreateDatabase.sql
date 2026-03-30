-- ============================================================
-- 01_CreateDatabase.sql
-- Creates the LicoresMaduoDB database
-- ============================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'LicoresMaduoDB')
BEGIN
    CREATE DATABASE LicoresMaduoDB;
END
GO

USE LicoresMaduoDB;
GO
