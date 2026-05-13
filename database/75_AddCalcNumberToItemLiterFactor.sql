-- Add CalcNumber to cc_Item_Liter_Factor_Calc_Fin so the factor is
-- tracked per calculation instead of globally per item.

-- 1. Add column (DEFAULT 0 covers any existing rows)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'cc_Item_Liter_Factor_Calc_Fin'
      AND COLUMN_NAME = 'CalcNumber'
)
BEGIN
    ALTER TABLE dbo.cc_Item_Liter_Factor_Calc_Fin
        ADD CalcNumber INT NOT NULL DEFAULT 0;
END
GO

-- 2. Drop existing single-column PK
DECLARE @pk NVARCHAR(200);
SELECT @pk = CONSTRAINT_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE TABLE_NAME   = 'cc_Item_Liter_Factor_Calc_Fin'
  AND CONSTRAINT_TYPE = 'PRIMARY KEY';

IF @pk IS NOT NULL
    EXEC('ALTER TABLE dbo.cc_Item_Liter_Factor_Calc_Fin DROP CONSTRAINT [' + @pk + ']');
GO

-- 3. New composite PK: (CalcNumber, ItemNo)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_NAME    = 'cc_Item_Liter_Factor_Calc_Fin'
      AND CONSTRAINT_TYPE = 'PRIMARY KEY'
)
BEGIN
    ALTER TABLE dbo.cc_Item_Liter_Factor_Calc_Fin
        ADD CONSTRAINT PK_cc_Item_Liter_Factor_Calc_Fin
            PRIMARY KEY (CalcNumber, ItemNo);
END
GO
