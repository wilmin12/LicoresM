-- ============================================================
-- Script 25: Email Configuration Table
-- Creates LM_EmailConfig (single-row SMTP configuration)
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'LM_EmailConfig'
)
BEGIN
    CREATE TABLE LM_EmailConfig (
        ConfigId        INT           NOT NULL CONSTRAINT PK_LM_EmailConfig PRIMARY KEY DEFAULT 1,
        SmtpHost        NVARCHAR(200) NOT NULL DEFAULT '',
        SmtpPort        INT           NOT NULL DEFAULT 587,
        UseSsl          BIT           NOT NULL DEFAULT 1,
        SenderName      NVARCHAR(100) NOT NULL DEFAULT '',
        SenderEmail     NVARCHAR(200) NOT NULL DEFAULT '',
        SenderPassword  NVARCHAR(500) NOT NULL DEFAULT '',
        Recipients      NVARCHAR(MAX) NOT NULL DEFAULT '',  -- semicolon-separated
        StaleOrderDays  INT           NOT NULL DEFAULT 4,
        IsEnabled       BIT           NOT NULL DEFAULT 0,
        UpdatedAt       DATETIME2     NULL,
        UpdatedBy       NVARCHAR(100) NULL,
        CONSTRAINT CK_LM_EmailConfig_SingleRow CHECK (ConfigId = 1)
    );

    -- Seed default row
    INSERT INTO LM_EmailConfig (ConfigId, SmtpHost, SmtpPort, UseSsl, SenderName, SenderEmail, SenderPassword, Recipients, StaleOrderDays, IsEnabled)
    VALUES (1, 'smtp.gmail.com', 587, 1, 'Licores Maduro Sistema', '', '', '', 4, 0);

    PRINT 'LM_EmailConfig table created and seeded.';
END
ELSE
BEGIN
    PRINT 'LM_EmailConfig table already exists.';
END
GO
