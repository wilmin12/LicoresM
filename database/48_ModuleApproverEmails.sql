-- ============================================================
-- 48 – Module Approver Emails
-- One row per module; stores comma-separated approver emails.
-- ============================================================
USE LicoresMaduoDB;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'MODULE_APPROVER_EMAILS'
)
BEGIN
    CREATE TABLE MODULE_APPROVER_EMAILS (
        Mae_Id          INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Mae_ModuleKey   NVARCHAR(50)  NOT NULL,
        Mae_ModuleName  NVARCHAR(100) NOT NULL,
        Mae_Emails      NVARCHAR(MAX) NOT NULL DEFAULT '',
        Mae_UpdatedAt   DATETIME2     NULL,
        Mae_UpdatedBy   NVARCHAR(100) NULL,
        CONSTRAINT UQ_ModuleApproverEmails_Key UNIQUE (Mae_ModuleKey)
    );

    -- Seed the three modules
    INSERT INTO MODULE_APPROVER_EMAILS (Mae_ModuleKey, Mae_ModuleName, Mae_Emails)
    VALUES
        ('AANKOOPBON',      'Aankoopbonnen',    ''),
        ('COSTCALCULATION', 'Cost Calculation', ''),
        ('ACTIVITYREQUEST', 'Activity Request', '');

    PRINT 'Table MODULE_APPROVER_EMAILS created and seeded.';
END
ELSE
    PRINT 'Table MODULE_APPROVER_EMAILS already exists — skipped.';
GO
