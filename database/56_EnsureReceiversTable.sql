-- ============================================================
-- Script 56: Ensure RECEIVERS table exists
-- ============================================================
USE LicoresMaduoDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RECEIVERS')
BEGIN
    CREATE TABLE dbo.RECEIVERS (
        REC_Id     INT           IDENTITY(1,1) NOT NULL,
        REC_NAME   NVARCHAR(30)  NOT NULL,
        REC_ID_DOC NVARCHAR(15)  NULL,
        IS_Active  BIT           NOT NULL DEFAULT 1,
        Created_At DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_RECEIVERS PRIMARY KEY (REC_Id)
    );
    PRINT 'RECEIVERS table created.';
END
ELSE
    PRINT 'RECEIVERS already exists.';
GO
