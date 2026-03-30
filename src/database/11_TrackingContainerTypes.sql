-- ============================================================
-- 11_TrackingContainerTypes.sql
-- Creates the TRACKING_CONTAINER_TYPES catalog table
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TRACKING_CONTAINER_TYPES')
BEGIN
    CREATE TABLE TRACKING_CONTAINER_TYPES (
        TCT_Id          INT IDENTITY(1,1) NOT NULL,
        TCT_Code        NVARCHAR(10)  NOT NULL,
        TCT_Description NVARCHAR(100) NOT NULL,
        IS_Active       BIT           NOT NULL DEFAULT 1,
        Created_At      DATETIME      NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT PK_TRACKING_CONTAINER_TYPES PRIMARY KEY (TCT_Id),
        CONSTRAINT UQ_TRACKING_CONTAINER_TYPES_Code UNIQUE (TCT_Code)
    );

    -- Seed common container types
    INSERT INTO TRACKING_CONTAINER_TYPES (TCT_Code, TCT_Description) VALUES
        ('20FT',   '20 Foot Standard'),
        ('40FT',   '40 Foot Standard'),
        ('40HC',   '40 Foot High Cube'),
        ('20RF',   '20 Foot Reefer'),
        ('40RF',   '40 Foot Reefer'),
        ('LCL',    'Less than Container Load');
END
GO
