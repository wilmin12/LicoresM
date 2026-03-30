-- ============================================================
-- Licores Maduro - Migration: Add OS_Code to ORDER_STATUS
-- Script: 10_AddOrderStatusCode.sql
-- Run on: LicoresMaduoDB
-- Run this ONLY if the ORDER_STATUS table already exists
-- and you want to add the OS_Code column without dropping it.
-- ============================================================

USE LicoresMaduoDB;
GO

-- Step 1: Add the column as nullable first
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ORDER_STATUS' AND COLUMN_NAME = 'OS_Code'
)
BEGIN
    ALTER TABLE dbo.ORDER_STATUS ADD OS_Code NVARCHAR(10) NULL;
    PRINT 'Column OS_Code added to ORDER_STATUS.';
END
ELSE
BEGIN
    PRINT 'Column OS_Code already exists. Skipping.';
END
GO

-- Step 2: Populate OS_Code from existing descriptions
-- (Adjust these manually if needed for your existing rows)
UPDATE dbo.ORDER_STATUS SET OS_Code = LEFT(REPLACE(UPPER(OS_DESCRIPTION), ' ', '_'), 10)
WHERE OS_Code IS NULL;
GO

-- Step 3: Make the column NOT NULL
ALTER TABLE dbo.ORDER_STATUS ALTER COLUMN OS_Code NVARCHAR(10) NOT NULL;
GO

-- Step 4: Add unique constraint if not already present
IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = 'UQ_ORDER_STATUS_Code'
)
BEGIN
    ALTER TABLE dbo.ORDER_STATUS ADD CONSTRAINT UQ_ORDER_STATUS_Code UNIQUE (OS_Code);
    PRINT 'Unique constraint UQ_ORDER_STATUS_Code added.';
END
GO

PRINT N'Migration 10_AddOrderStatusCode completed successfully.';
GO
