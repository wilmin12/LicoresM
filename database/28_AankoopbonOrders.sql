-- ============================================================
-- Script 28: Aankoopbon Order Headers & Details
-- ============================================================
USE LicoresMaduoDB;
GO

-- ── AB_ORDER_HEADERS ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AB_ORDER_HEADERS')
BEGIN
    CREATE TABLE dbo.AB_ORDER_HEADERS (
        AOH_Id               INT           IDENTITY(1,1) PRIMARY KEY,
        AOH_Bon_Nr           NVARCHAR(15)  NOT NULL,           -- e.g. 2025/004
        AOH_Status           NVARCHAR(20)  NOT NULL DEFAULT 'DRAFT', -- DRAFT/PENDING/APPROVED/REJECTED
        AOH_Order_Date       DATE          NOT NULL,
        AOH_Requestor        NVARCHAR(15)  NULL,
        AOH_Vendor_Id        INT           NULL,               -- FK to VENDORS
        AOH_Vendor_Name      NVARCHAR(100) NULL,               -- denormalized
        AOH_Vendor_Address   NVARCHAR(100) NULL,               -- denormalized
        AOH_Department       NVARCHAR(15)  NULL,
        AOH_Cost_Type        NVARCHAR(15)  NULL,
        AOH_Remarks          NVARCHAR(255) NULL,
        AOH_Vehicle_Id       INT           NULL,               -- FK to VEHICLES (optional)
        AOH_Vehicle_License  NVARCHAR(10)  NULL,               -- denormalized
        AOH_Vehicle_Type     NVARCHAR(15)  NULL,               -- denormalized
        AOH_Vehicle_Model    NVARCHAR(15)  NULL,               -- denormalized
        AOH_Quotation_Nr     NVARCHAR(15)  NULL,
        AOH_Amount           DECIMAL(18,2) NULL,
        -- Delivery method
        AOH_Meegeven         BIT           NOT NULL DEFAULT 0, -- Deliver in hand
        AOH_Ontvangen        BIT           NOT NULL DEFAULT 0, -- Hereby receives
        AOH_Zenden           BIT           NOT NULL DEFAULT 0, -- Send
        AOH_Andere           BIT           NOT NULL DEFAULT 0, -- Other
        AOH_Receiver_Id      INT           NULL,               -- FK to RECEIVERS
        AOH_Receiver_Name    NVARCHAR(30)  NULL,               -- denormalized
        AOH_Receiver_Id_Doc  NVARCHAR(15)  NULL,               -- denormalized
        -- Approval
        AOH_Approved_By      INT           NULL,
        AOH_Approved_By_Name NVARCHAR(100) NULL,
        AOH_Approved_At      DATETIME      NULL,
        -- Rejection
        AOH_Rejected_By      INT           NULL,
        AOH_Rejected_By_Name NVARCHAR(100) NULL,
        AOH_Rejected_At      DATETIME      NULL,
        AOH_Rejection_Reason NVARCHAR(500) NULL,
        -- Invoice (optional, post-approval)
        AOH_Invoice_Nr       NVARCHAR(15)  NULL,
        AOH_Invoice_Date     DATE          NULL,
        AOH_Invoice_Amount   DECIMAL(18,2) NULL,
        -- Audit
        AOH_Created_By       INT           NOT NULL,
        AOH_Created_By_Name  NVARCHAR(100) NULL,
        IS_Active            BIT           NOT NULL DEFAULT 1,
        Created_At           DATETIME      NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE INDEX UX_AB_ORDER_HEADERS_BonNr   ON dbo.AB_ORDER_HEADERS (AOH_Bon_Nr);
    CREATE        INDEX IX_AB_ORDER_HEADERS_Status  ON dbo.AB_ORDER_HEADERS (AOH_Status);
    CREATE        INDEX IX_AB_ORDER_HEADERS_Vehicle ON dbo.AB_ORDER_HEADERS (AOH_Vehicle_License);
    PRINT 'AB_ORDER_HEADERS created.';
END
ELSE
    PRINT 'AB_ORDER_HEADERS already exists.';
GO

-- ── AB_ORDER_DETAILS ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AB_ORDER_DETAILS')
BEGIN
    CREATE TABLE dbo.AB_ORDER_DETAILS (
        AOD_Id           INT           IDENTITY(1,1) PRIMARY KEY,
        AOD_Header_Id    INT           NOT NULL,
        AOD_Line_Nr      INT           NOT NULL,
        AOD_Product_Code NVARCHAR(20)  NULL,
        AOD_Product_Desc NVARCHAR(100) NOT NULL,
        AOD_Quantity     DECIMAL(18,2) NOT NULL DEFAULT 1,
        AOD_Unit         NVARCHAR(10)  NULL,
        IS_Active        BIT           NOT NULL DEFAULT 1,
        Created_At       DATETIME      NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_AOD_Header FOREIGN KEY (AOD_Header_Id)
            REFERENCES dbo.AB_ORDER_HEADERS (AOH_Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AB_ORDER_DETAILS_Header ON dbo.AB_ORDER_DETAILS (AOD_Header_Id);
    PRINT 'AB_ORDER_DETAILS created.';
END
ELSE
    PRINT 'AB_ORDER_DETAILS already exists.';
GO

-- ── Submodule: Aankoopbon Orders ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.LM_Submodules WHERE SubmoduleCode = 'AB_AANKOOPBON')
BEGIN
    INSERT INTO dbo.LM_Submodules (ModuleId, SubmoduleCode, SubmoduleName, TableName, IsActive)
    SELECT m.ModuleId, 'AB_AANKOOPBON', 'Aankoopbon Orders', 'AB_ORDER_HEADERS', 1
    FROM   dbo.LM_Modules m WHERE m.ModuleCode = 'PURCHASE';
    PRINT 'Submodule AB_AANKOOPBON inserted.';
END
ELSE
    PRINT 'Submodule AB_AANKOOPBON already exists.';
GO

-- Permissions for SuperAdmin(1) and Admin(2)
MERGE dbo.LM_RolePermissions AS target
USING (
    SELECT r.RoleId, s.SubmoduleId
    FROM   dbo.LM_Roles r
    CROSS JOIN dbo.LM_Submodules s
    WHERE  r.RoleId IN (1,2) AND s.SubmoduleCode = 'AB_AANKOOPBON'
) AS source (RoleId, SubmoduleId)
ON target.RoleId = source.RoleId AND target.SubmoduleId = source.SubmoduleId
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess) VALUES (source.RoleId, source.SubmoduleId, 1);
GO

PRINT 'Script 28 complete.';
