-- ============================================================
-- 05_TrackingTables.sql
-- Tracking module: TRACKING_ORDERS, TRACKING_STATUS_HISTORY
-- ============================================================

USE LicoresMaduoDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TRACKING_ORDERS')
BEGIN
    CREATE TABLE TRACKING_ORDERS (
        TR_Id                           INT IDENTITY(1,1) NOT NULL,
        TR_PoNo                         NVARCHAR(20)   NOT NULL,
        TR_Warehouse                    NVARCHAR(5)    NULL,
        TR_Supplier                     NVARCHAR(10)   NULL,
        TR_Supplier_Name                NVARCHAR(50)   NULL,
        TR_Supplier_Code                NVARCHAR(2)    NULL,
        TR_Country                      NVARCHAR(3)    NULL,
        TR_Freight_Forwarder            NVARCHAR(50)   NULL,
        TR_Borw                         NVARCHAR(1)    NULL,
        TR_Order_Date                   INT            NULL,   -- YYYYMMDD from VIP PHORDT
        TR_Vip_Ship_Date                INT            NULL,   -- YYYYMMDD from VIP PHSHDT
        TR_Vip_Arrival_Date             INT            NULL,   -- YYYYMMDD from VIP PHARDT
        TR_Total_Cases                  DECIMAL(18,4)  NULL,
        TR_Vip_Weight                   DECIMAL(18,4)  NULL,
        TR_Vip_Liters                   DECIMAL(18,4)  NULL,
        TR_Vip_Total_Amount             DECIMAL(18,2)  NULL,
        TR_Vip_Total_Lines              INT            NULL,
        TR_Vip_Status                   NVARCHAR(2)    NULL,
        TR_Status_Code                  NVARCHAR(10)   NULL,
        TR_Comments                     NVARCHAR(500)  NULL,
        TR_Last_Update_Date             DATE           NULL,
        TR_Requested_ETA                DATE           NULL,
        TR_Acknowledge_Order            BIT            NULL,
        -- Tab 1: General
        TR_Date_Loading_Shipper         DATE           NULL,
        -- Tab 2: Shipping
        TR_Shipping_Line                NVARCHAR(50)   NULL,
        TR_Shipping_Agent               NVARCHAR(50)   NULL,
        TR_Vessel                       NVARCHAR(50)   NULL,
        TR_Container_Number             NVARCHAR(20)   NULL,
        TR_Consolidation_Ref            NVARCHAR(50)   NULL,
        TR_Container_Size               NVARCHAR(10)   NULL,
        -- Tab 3: Documentation
        TR_Date_ProForma_Received       DATE           NULL,
        TR_Qty_ProForma                 INT            NULL,
        TR_Factory_Ready_Date           DATE           NULL,
        TR_Est_Departure_Date           DATE           NULL,
        TR_Est_Arrival_Date             DATE           NULL,
        TR_Transit_Time                 NVARCHAR(20)   NULL,
        TR_Bijlage_Done                 BIT            NULL,
        TR_Date_Arrival_Invoice         DATE           NULL,
        TR_Invoice_Number               NVARCHAR(20)   NULL,
        TR_Date_Arrival_Bol             DATE           NULL,
        TR_Remarks                      NVARCHAR(500)  NULL,
        -- Tab 4: Customs
        TR_Date_Arrival_Note_Received   DATE           NULL,
        TR_Date_Manifest_Received       DATE           NULL,
        TR_Date_Copies_Declarant        DATE           NULL,
        TR_Date_Customs_Papers_Ready    DATE           NULL,
        TR_Date_Customs_Papers_Asycuda  DATE           NULL,
        -- Tab 5: Container / CPS
        TR_Date_Container_At_CPS        DATE           NULL,
        TR_Expiration_Date_CPS          DATE           NULL,
        TR_Date_Customs_Papers_CPS      DATE           NULL,
        TR_Date_Container_Arrived       DATE           NULL,
        TR_Date_Container_Opened        DATE           NULL,
        TR_Date_Unload_Ready            DATE           NULL,
        TR_Return_Date_Container        DATE           NULL,
        -- Tab 6: Administration
        TR_Date_Unload_Papers_Admin     DATE           NULL,
        TR_SAD_Number                   NVARCHAR(20)   NULL,
        TR_BC_Number_Orders             NVARCHAR(20)   NULL,
        TR_Exit_Note_Number             NVARCHAR(20)   NULL,
        TR_Issues_Comments              NVARCHAR(1000) NULL,
        -- Audit
        TR_Created_By                   NVARCHAR(50)   NULL,
        TR_Created_At                   DATETIME       NOT NULL DEFAULT GETUTCDATE(),
        TR_Updated_By                   NVARCHAR(50)   NULL,
        TR_Updated_At                   DATETIME       NULL,
        CONSTRAINT PK_TRACKING_ORDERS PRIMARY KEY (TR_Id),
        CONSTRAINT UQ_TRACKING_ORDERS_PoNo UNIQUE (TR_PoNo)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TRACKING_STATUS_HISTORY')
BEGIN
    CREATE TABLE TRACKING_STATUS_HISTORY (
        TSH_Id           INT IDENTITY(1,1) NOT NULL,
        TSH_Tracking_Id  INT           NOT NULL,
        TSH_PoNo         NVARCHAR(20)  NULL,
        TSH_Status_Code  NVARCHAR(10)  NULL,
        TSH_Status_Date  DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        TSH_Comments     NVARCHAR(500) NULL,
        TSH_Changed_By   NVARCHAR(50)  NULL,
        CONSTRAINT PK_TRACKING_STATUS_HISTORY PRIMARY KEY (TSH_Id),
        CONSTRAINT FK_TSH_TrackingOrder FOREIGN KEY (TSH_Tracking_Id)
            REFERENCES TRACKING_ORDERS(TR_Id)
    );
END
GO
