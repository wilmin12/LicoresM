-- Fix: Replace global unique constraint with composite unique constraint (Number + Type)
USE LicoresMaduoDB;
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_FF_QUOTE_HEADER_Num' AND type = 'UQ')
BEGIN
    ALTER TABLE FF_QUOTE_HEADER DROP CONSTRAINT UQ_FF_QUOTE_HEADER_Num;
    PRINT 'Dropped old global unique constraint UQ_FF_QUOTE_HEADER_Num';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_FF_QUOTE_HEADER_Num_Type' AND type = 'UQ')
BEGIN
    ALTER TABLE FF_QUOTE_HEADER ADD CONSTRAINT UQ_FF_QUOTE_HEADER_Num_Type UNIQUE (FQH_QUOTE_NUMBER, FQH_FREIGHT_TYPE);
    PRINT 'Added composite unique constraint UQ_FF_QUOTE_HEADER_Num_Type (Number, Type)';
END
GO
