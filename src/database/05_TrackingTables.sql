-- ============================================================
-- Licores Maduro - Tracking Module Tables
-- Script: 05_TrackingTables.sql
-- Run on: LicoresMaduoDB
-- ============================================================

USE LicoresMaduoDB;
GO

IF OBJECT_ID('dbo.TRACKING_STATUS_HISTORY', 'U') IS NOT NULL DROP TABLE dbo.TRACKING_STATUS_HISTORY;
IF OBJECT_ID('dbo.TRACKING_ORDERS',         'U') IS NOT NULL DROP TABLE dbo.TRACKING_ORDERS;
GO

CREATE TABLE dbo.TRACKING_ORDERS (
    TR_Id                       INT             NOT NULL IDENTITY(1,1),
    -- Auto-filled from VIP
    TR_PoNo                     NVARCHAR(20)    NOT NULL,
    TR_Warehouse                NVARCHAR(5)     NULL,
    TR_Supplier                 NVARCHAR(10)    NULL,
    TR_Supplier_Name            NVARCHAR(50)    NULL,
    TR_Country                  NVARCHAR(3)     NULL,
    TR_Freight_Forwarder        NVARCHAR(50)    NULL,
    TR_Order_Date               INT             NULL,       -- YYYYMMDD from VIP
    TR_Total_Cases              DECIMAL(18,2)   NULL,
    TR_Borw                     NVARCHAR(1)     NULL,       -- B=Beer W=Wine
    -- Status
    TR_Status_Code              NVARCHAR(10)    NULL,
    -- Section 1: General
    TR_Comments                 NVARCHAR(500)   NULL,
    TR_Last_Update_Date         DATETIME2       NULL,
    TR_Requested_ETA            DATETIME2       NULL,
    TR_Acknowledge_Order        BIT             NULL,
    TR_Date_Loading_Shipper     DATETIME2       NULL,
    -- Section 2: Shipping
    TR_Shipping_Line            NVARCHAR(50)    NULL,
    TR_Shipping_Agent           NVARCHAR(50)    NULL,
    TR_Vessel                   NVARCHAR(50)    NULL,
    TR_Container_Number         NVARCHAR(20)    NULL,
    TR_Consolidation_Ref        NVARCHAR(50)    NULL,
    TR_Container_Size           NVARCHAR(10)    NULL,
    -- Section 3: Documentation
    TR_Date_ProForma_Received   DATETIME2       NULL,
    TR_Qty_ProForma             DECIMAL(18,2)   NULL,
    TR_Factory_Ready_Date       DATETIME2       NULL,
    TR_Est_Departure_Date       DATETIME2       NULL,
    TR_Est_Arrival_Date         DATETIME2       NULL,
    TR_Transit_Time             NVARCHAR(20)    NULL,
    TR_Bijlage_Done             BIT             NULL,
    TR_Date_Arrival_Invoice     DATETIME2       NULL,
    TR_Invoice_Number           NVARCHAR(20)    NULL,
    TR_Date_Arrival_Bol         DATETIME2       NULL,
    TR_Remarks                  NVARCHAR(500)   NULL,
    -- Section 4: Customs
    TR_Date_Arrival_Note_Received   DATETIME2   NULL,
    TR_Date_Manifest_Received       DATETIME2   NULL,
    TR_Date_Copies_Declarant        DATETIME2   NULL,
    TR_Date_Customs_Papers_Ready    DATETIME2   NULL,
    TR_Date_Customs_Papers_Asycuda  DATETIME2   NULL,
    -- Section 5: Container / CPS
    TR_Date_Container_At_CPS        DATETIME2   NULL,
    TR_Expiration_Date_CPS          DATETIME2   NULL,
    TR_Date_Customs_Papers_CPS      DATETIME2   NULL,
    TR_Date_Container_Arrived       DATETIME2   NULL,
    TR_Date_Container_Opened        DATETIME2   NULL,
    TR_Date_Unload_Ready            DATETIME2   NULL,
    TR_Return_Date_Container        DATETIME2   NULL,
    -- Days_Over_Container is COMPUTED as DATEDIFF(day, TR_Date_Container_Arrived, TR_Return_Date_Container)
    TR_Days_Over_Container AS (
        CASE WHEN TR_Date_Container_Arrived IS NOT NULL AND TR_Return_Date_Container IS NOT NULL
             THEN DATEDIFF(day, TR_Date_Container_Arrived, TR_Return_Date_Container)
             ELSE NULL END
    ) PERSISTED,
    -- Section 6: Administration
    TR_Date_Unload_Papers_Admin     DATETIME2   NULL,
    TR_SAD_Number                   NVARCHAR(20) NULL,
    TR_BC_Number_Orders             NVARCHAR(20) NULL,
    TR_Exit_Note_Number             NVARCHAR(20) NULL,
    TR_Issues_Comments              NVARCHAR(1000) NULL,
    -- Audit
    TR_Created_By               NVARCHAR(50)    NULL,
    TR_Created_At               DATETIME2       NOT NULL DEFAULT GETDATE(),
    TR_Updated_By               NVARCHAR(50)    NULL,
    TR_Updated_At               DATETIME2       NULL,

    CONSTRAINT PK_TRACKING_ORDERS PRIMARY KEY CLUSTERED (TR_Id),
    CONSTRAINT UQ_TRACKING_ORDERS_PoNo UNIQUE (TR_PoNo)
);
GO

CREATE INDEX IX_TRACKING_ORDERS_Status    ON dbo.TRACKING_ORDERS (TR_Status_Code);
CREATE INDEX IX_TRACKING_ORDERS_Warehouse ON dbo.TRACKING_ORDERS (TR_Warehouse);
CREATE INDEX IX_TRACKING_ORDERS_Supplier  ON dbo.TRACKING_ORDERS (TR_Supplier);
GO

CREATE TABLE dbo.TRACKING_STATUS_HISTORY (
    TSH_Id          INT             NOT NULL IDENTITY(1,1),
    TSH_Tracking_Id INT             NOT NULL,
    TSH_PoNo        NVARCHAR(20)    NULL,
    TSH_Status_Code NVARCHAR(10)    NULL,
    TSH_Status_Date DATETIME2       NOT NULL DEFAULT GETDATE(),
    TSH_Comments    NVARCHAR(500)   NULL,
    TSH_Changed_By  NVARCHAR(50)    NULL,

    CONSTRAINT PK_TRACKING_STATUS_HISTORY PRIMARY KEY CLUSTERED (TSH_Id),
    CONSTRAINT FK_TSH_TrackingOrder FOREIGN KEY (TSH_Tracking_Id)
        REFERENCES dbo.TRACKING_ORDERS (TR_Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_TSH_TrackingId ON dbo.TRACKING_STATUS_HISTORY (TSH_Tracking_Id);
GO

PRINT N'Tracking tables created successfully.';
GO
