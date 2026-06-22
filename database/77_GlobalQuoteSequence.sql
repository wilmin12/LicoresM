-- 77_GlobalQuoteSequence.sql
-- Change Quote Number unique constraint from (Number, Type) to just (Number)
-- and ensure the sequence is calculated globally.

-- 1. Remove the old composite unique constraint
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'UQ_FF_QUOTE_HEADER_Num_Type' AND type = 'UQ')
BEGIN
    ALTER TABLE FF_QUOTE_HEADER DROP CONSTRAINT UQ_FF_QUOTE_HEADER_Num_Type;
END
GO

-- 2. Create the new global unique constraint
-- Note: This will FAIL if there are currently duplicate numbers across types.
-- The user has been warned and agreed to proceed.
ALTER TABLE FF_QUOTE_HEADER ADD CONSTRAINT UQ_FF_QUOTE_HEADER_Num_Global UNIQUE (FQH_QUOTE_NUMBER);
GO

-- 3. (Optional/Safety) In case duplicates existed, this script might need manual cleanup of data first.
-- For now, we follow the instruction to implement the strict global sequence.


select * from  [dbo].[cc_Forex_History]