-- ============================================================
-- Script 38: Ocean Freight Port-Level Charges
-- Adds FF_OCEAN_FREIGHT_PORT_CHARGES table
-- Charges that apply at port level regardless of shipping line
-- ============================================================
USE LicoresMaduoDB;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FF_OCEAN_FREIGHT_PORT_CHARGES')
BEGIN
    CREATE TABLE FF_OCEAN_FREIGHT_PORT_CHARGES (
        FQOPC_Id             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FQOPC_Port_Id        INT           NOT NULL,
        FQOPC_FORWARDER      NVARCHAR(10)  NOT NULL,
        FQOPC_QUOTENUMBER    NVARCHAR(10)  NOT NULL,
        FQOPC_PORT           NVARCHAR(10)  NOT NULL,
        FQOPC_CHARGE_TYPE    NVARCHAR(6)   NOT NULL,
        FQOPC_CONTAINER_TYPE NVARCHAR(6)   NULL,
        FQOPC_AMOUNT         DECIMAL(18,4) NULL,
        FQOPC_CURRENCY       NVARCHAR(3)   NULL,
        CONSTRAINT FK_FQOPC_Port FOREIGN KEY (FQOPC_Port_Id)
            REFERENCES FF_OCEAN_FREIGHT_PORT(FQOP_Id) ON DELETE CASCADE
    );
    PRINT 'Table FF_OCEAN_FREIGHT_PORT_CHARGES created.';
END
ELSE
    PRINT 'Table FF_OCEAN_FREIGHT_PORT_CHARGES already exists.';
GO
