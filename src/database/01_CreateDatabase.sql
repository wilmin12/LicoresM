-- ============================================================
-- Licores Maduro - Database Creation Script
-- Script: 01_CreateDatabase.sql
-- Description: Creates the LicoresMaduoDB database
-- ============================================================

USE master;
GO

-- Drop database if exists (comment out in production!)
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'LicoresMaduoDB')
BEGIN
    ALTER DATABASE LicoresMaduoDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LicoresMaduoDB;
END
GO

-- Create Database
CREATE DATABASE LicoresMaduoDB
    COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

-- Switch to the new database
USE LicoresMaduoDB;
GO

-- Set database options
ALTER DATABASE LicoresMaduoDB SET RECOVERY FULL;
ALTER DATABASE LicoresMaduoDB SET AUTO_SHRINK OFF;
ALTER DATABASE LicoresMaduoDB SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE LicoresMaduoDB SET AUTO_UPDATE_STATISTICS ON;
GO

PRINT 'Database LicoresMaduoDB created successfully.';
GO
