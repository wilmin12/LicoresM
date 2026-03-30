-- ============================================================
-- 10_AddOrderStatusCode.sql
-- Creates ORDER_STATUS table and seeds the tracking statuses
-- ============================================================

USE LicoresMaduoDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ORDER_STATUS')
BEGIN
    CREATE TABLE ORDER_STATUS (
        OS_Id          INT IDENTITY(1,1) NOT NULL,
        OS_Code        NVARCHAR(10)  NOT NULL,
        OS_DESCRIPTION NVARCHAR(100) NOT NULL,
        IS_Active      BIT           NOT NULL DEFAULT 1,
        Created_At     DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_ORDER_STATUS  PRIMARY KEY (OS_Id),
        CONSTRAINT UQ_ORDER_STATUS_Code UNIQUE (OS_Code)
    );

    INSERT INTO ORDER_STATUS (OS_Code, OS_DESCRIPTION) VALUES
        ('INVIP',  'IN VIP'),
        ('PEND',   'PENDING'),
        ('SHIP',   'SHIPPED'),
        ('ARR',    'ARRIVED'),
        ('PCPS',   'PROCESSED AT CPS'),
        ('PCCS',   'PROCESSED AT CCS'),
        ('DOCK',   'DOCKED'),
        ('CLOS',   'CLOSED'),
        ('PDLV',   'PENDING DELIVERIES'),
        ('READY',  'READY'),
        ('CANC',   'CANCELLED');
END
GO
