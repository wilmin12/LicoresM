-- ============================================================
-- Script  : 59_CreateItemFobPrices.sql
-- Database: LicoresMaduoDB
-- Purpose : Create ITEM_FOB_PRICES table used by Cost Calculation
--           module to store the FOB purchase price per item code.
-- Run on  : LicoresMaduoDB
-- ============================================================

USE LicoresMaduoDB;
GO

IF NOT EXISTS (
    SELECT 1
    FROM   sys.objects
    WHERE  object_id = OBJECT_ID(N'[dbo].[ITEM_FOB_PRICES]')
      AND  type      = N'U'
)
BEGIN
    CREATE TABLE [dbo].[ITEM_FOB_PRICES]
    (
        [IT_Code]           NVARCHAR(20)   NOT NULL,
        [IT_Purchase_Price] DECIMAL(18, 4) NULL,
        [IT_Commodity]      NVARCHAR(20)   NULL,

        CONSTRAINT [PK_ITEM_FOB_PRICES] PRIMARY KEY CLUSTERED ([IT_Code])
    );

    PRINT 'ITEM_FOB_PRICES created successfully.';
END
ELSE
BEGIN
    PRINT 'ITEM_FOB_PRICES already exists — no changes made.';
END
GO
