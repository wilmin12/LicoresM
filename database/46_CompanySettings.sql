-- ============================================================
-- Script 46: Company Settings
-- Single-row table that stores the platform's company profile.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'COMPANY_SETTINGS'
)
BEGIN
    CREATE TABLE COMPANY_SETTINGS (
        CS_Id           INT            NOT NULL DEFAULT 1,
        CS_CompanyName  NVARCHAR(200)  NOT NULL DEFAULT 'Licores Maduro',
        CS_LegalName    NVARCHAR(200)  NULL,
        CS_Tagline      NVARCHAR(300)  NULL,
        CS_RNC          NVARCHAR(50)   NULL,
        CS_Address      NVARCHAR(400)  NULL,
        CS_City         NVARCHAR(100)  NULL,
        CS_Country      NVARCHAR(100)  NULL,
        CS_Phone        NVARCHAR(50)   NULL,
        CS_Phone2       NVARCHAR(50)   NULL,
        CS_Email        NVARCHAR(200)  NULL,
        CS_Website      NVARCHAR(200)  NULL,
        CS_LogoUrl      NVARCHAR(500)  NULL,
        CS_UpdatedAt    DATETIME2      NULL,
        CS_UpdatedBy    NVARCHAR(100)  NULL,
        CONSTRAINT PK_COMPANY_SETTINGS PRIMARY KEY (CS_Id),
        CONSTRAINT CK_COMPANY_SETTINGS_SINGLE_ROW CHECK (CS_Id = 1)
    );

    -- Seed the default row
    INSERT INTO COMPANY_SETTINGS (CS_Id, CS_CompanyName, CS_Country)
    VALUES (1, 'Licores Maduro', 'Dominican Republic');

    PRINT 'COMPANY_SETTINGS created and seeded.';
END
ELSE
BEGIN
    PRINT 'COMPANY_SETTINGS already exists – skipped.';
END
