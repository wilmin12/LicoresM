-- ============================================================
-- 52 – Cost Calculation Email Sub-Categories
-- Adds 7 specific email notification rows for Cost Calc module.
-- ============================================================
USE LicoresMaduoDB;
GO

IF NOT EXISTS (
    SELECT 1 FROM MODULE_APPROVER_EMAILS WHERE Mae_ModuleKey = 'COSTCALC_PURCHASE_DEPT'
)
BEGIN
    INSERT INTO MODULE_APPROVER_EMAILS (Mae_ModuleKey, Mae_ModuleName, Mae_Emails)
    VALUES
        ('COSTCALC_PURCHASE_DEPT',       'Purchase Department',                           ''),
        ('COSTCALC_APPROVED_SALES',      'Approved by Sales Manager',                     ''),
        ('COSTCALC_APPROVED_FINANCIAL',  'Approved by Financial Manager',                 ''),
        ('COSTCALC_PRICE_CALC',          'Cost Price Calculations (To: Financials)',       ''),
        ('COSTCALC_PRICE_CHANGE',        'Cost Price Change (To: Financials)',             ''),
        ('COSTCALC_DOLLAR_RATE',         'Dollar Rate Edit — Calculations (PDF)',          ''),
        ('COSTCALC_DOLLAR_SELLING',      'Dollar Rate Edit — Selling Prices',              '');

    PRINT '7 Cost Calculation email sub-categories inserted.';
END
ELSE
    PRINT 'Cost Calculation email sub-categories already exist — skipped.';
GO
