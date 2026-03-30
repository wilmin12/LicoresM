-- ============================================================
-- 34_FreightQuotes.sql
-- Applied Freight Quotation tables (FF_QUOTE_*)
-- These combine Ocean + Inland + Port-Additional charges into
-- a single quote per Forwarder + Port + Route.
-- ============================================================

USE LicoresMaduoDB;
GO

-- ── Header ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FF_QUOTE_HEADER')
    CREATE TABLE FF_QUOTE_HEADER (
        FQH_Id            INT IDENTITY(1,1) NOT NULL,
        FQH_QUOTE_NUMBER  INT               NOT NULL,   -- auto-generated: MAX+1 per year / globally
        FQH_FORWARDER     NVARCHAR(10)      NOT NULL,
        FQH_PORT          NVARCHAR(10)      NULL,
        FQH_ROUTE         NVARCHAR(15)      NULL,
        FQH_TRANSIT_DAYS  INT               NULL,
        FQH_START_DATE    DATE              NULL,
        FQH_END_DATE      DATE              NULL,
        Created_At        DATETIME          NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_FF_QUOTE_HEADER PRIMARY KEY (FQH_Id),
        CONSTRAINT UQ_FF_QUOTE_HEADER_Num UNIQUE (FQH_QUOTE_NUMBER)
    );
GO

-- ── Ocean Freight charges for the quote ───────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FF_QUOTE_OCEAN_CHARGE')
    CREATE TABLE FF_QUOTE_OCEAN_CHARGE (
        FQOC_Id             INT IDENTITY(1,1) NOT NULL,
        FQOC_Header_Id      INT               NOT NULL,
        FQOC_CHARGE_TYPE    NVARCHAR(6)       NOT NULL,
        FQOC_CONTAINER_TYPE NVARCHAR(6)       NULL,
        FQOC_AMOUNT         DECIMAL(18,2)     NULL,
        FQOC_CURRENCY       NVARCHAR(3)       NULL,
        CONSTRAINT PK_FF_QUOTE_OCEAN_CHARGE PRIMARY KEY (FQOC_Id),
        CONSTRAINT FK_FQOC_Header FOREIGN KEY (FQOC_Header_Id) REFERENCES FF_QUOTE_HEADER(FQH_Id) ON DELETE CASCADE
    );
GO

-- ── Inland Freight — Region (level 1 of 3) ────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FF_QUOTE_INL_REGION')
    CREATE TABLE FF_QUOTE_INL_REGION (
        FQER_Id        INT IDENTITY(1,1) NOT NULL,
        FQER_Header_Id INT               NOT NULL,
        FQER_REGION    NVARCHAR(20)      NOT NULL,
        CONSTRAINT PK_FF_QUOTE_INL_REGION PRIMARY KEY (FQER_Id),
        CONSTRAINT FK_FQER_Header FOREIGN KEY (FQER_Header_Id) REFERENCES FF_QUOTE_HEADER(FQH_Id) ON DELETE CASCADE
    );
GO

-- ── Inland Freight — Region + Charge Type (level 2 of 3) ─────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FF_QUOTE_INL_REGION_TYPE')
    CREATE TABLE FF_QUOTE_INL_REGION_TYPE (
        FQERT_Id          INT IDENTITY(1,1) NOT NULL,
        FQERT_Region_Id   INT               NOT NULL,
        FQERT_CHARGE_TYPE NVARCHAR(6)       NOT NULL,
        FQERT_AMOUNT_MIN  DECIMAL(18,2)     NULL,
        FQERT_AMOUNT_MAX  DECIMAL(18,2)     NULL,
        FQERT_CURRENCY    NVARCHAR(3)       NULL,
        CONSTRAINT PK_FF_QUOTE_INL_REGION_TYPE PRIMARY KEY (FQERT_Id),
        CONSTRAINT FK_FQERT_Region FOREIGN KEY (FQERT_Region_Id) REFERENCES FF_QUOTE_INL_REGION(FQER_Id) ON DELETE CASCADE
    );
GO

-- ── Inland Freight — Escalonamiento (level 3 of 3) ───────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FF_QUOTE_INL_REGION_TYPE_DET')
    CREATE TABLE FF_QUOTE_INL_REGION_TYPE_DET (
        FQERTD_Id             INT IDENTITY(1,1) NOT NULL,
        FQERTD_RegionType_Id  INT               NOT NULL,
        FQERTD_FROM           DECIMAL(18,4)     NULL,
        FQERTD_TO             DECIMAL(18,4)     NULL,
        FQERTD_PRICE          DECIMAL(18,6)     NULL,
        FQERTD_PRICE_TYPE     NVARCHAR(6)       NULL,
        FQERTD_AMOUNT_MIN     DECIMAL(18,2)     NULL,
        FQERTD_AMOUNT_MAX     DECIMAL(18,2)     NULL,
        CONSTRAINT PK_FF_QUOTE_INL_REGION_TYPE_DET PRIMARY KEY (FQERTD_Id),
        CONSTRAINT FK_FQERTD_RegionType FOREIGN KEY (FQERTD_RegionType_Id) REFERENCES FF_QUOTE_INL_REGION_TYPE(FQERT_Id) ON DELETE CASCADE
    );
GO

-- ── Additional Port Charges for the quote ─────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FF_QUOTE_INL_PORT_ADD')
    CREATE TABLE FF_QUOTE_INL_PORT_ADD (
        FQIPA_Id          INT IDENTITY(1,1) NOT NULL,
        FQIPA_Header_Id   INT               NOT NULL,
        FQIPA_CHARGE_TYPE NVARCHAR(6)       NOT NULL,
        FQIPA_LOAD_TYPE   NVARCHAR(6)       NULL,
        FQIPA_AMOUNT      DECIMAL(18,2)     NULL,
        FQIPA_ACTION      NVARCHAR(6)       NULL,
        FQIPA_CHARGE_OVER NVARCHAR(6)       NULL,
        FQIPA_CHARGE_PER  NVARCHAR(6)       NULL,
        FQIPA_FROM        DECIMAL(18,4)     NULL,
        FQIPA_TO          DECIMAL(18,4)     NULL,
        FQIPA_AMOUNT_MIN  DECIMAL(18,2)     NULL,
        FQIPA_AMOUNT_MAX  DECIMAL(18,2)     NULL,
        FQIPA_CURRENCY    NVARCHAR(3)       NULL,
        CONSTRAINT PK_FF_QUOTE_INL_PORT_ADD PRIMARY KEY (FQIPA_Id),
        CONSTRAINT FK_FQIPA_Header FOREIGN KEY (FQIPA_Header_Id) REFERENCES FF_QUOTE_HEADER(FQH_Id) ON DELETE CASCADE
    );
GO

-- ── Submodule: register FF_QUOTES in LM_Submodules ───────────
MERGE INTO dbo.LM_Submodules AS target
USING (
    SELECT
        (SELECT ModuleId FROM LM_Modules WHERE ModuleCode = 'FREIGHT') AS ModuleId,
        'Applied Quotes'   AS SubmoduleName,
        'FF_QUOTES'        AS SubmoduleCode,
        'FF_QUOTE_HEADER'  AS TableName,
        23                 AS DisplayOrder
) AS source
ON target.SubmoduleCode = source.SubmoduleCode
WHEN NOT MATCHED THEN
    INSERT (ModuleId, SubmoduleName, SubmoduleCode, TableName, DisplayOrder)
    VALUES (source.ModuleId, source.SubmoduleName, source.SubmoduleCode, source.TableName, source.DisplayOrder);
GO

-- ── Permissions: SuperAdmin (role 1) = full, Admin (role 2) = full, User (role 3) = read-only ──
MERGE INTO dbo.LM_RolePermissions AS tgt
USING (
    SELECT sm.SubmoduleId, r.RoleId,
           1                          AS CanAccess,
           1                          AS CanRead,
           CASE WHEN r.RoleId <= 2 THEN 1 ELSE 0 END AS CanWrite,
           CASE WHEN r.RoleId <= 2 THEN 1 ELSE 0 END AS CanEdit,
           CASE WHEN r.RoleId <= 2 THEN 1 ELSE 0 END AS CanDelete
    FROM dbo.LM_Submodules sm
    CROSS JOIN dbo.LM_Roles r
    WHERE sm.SubmoduleCode = 'FF_QUOTES'
      AND r.RoleId IN (1,2,3)
) AS src
ON (tgt.RoleId = src.RoleId AND tgt.SubmoduleId = src.SubmoduleId)
WHEN NOT MATCHED THEN
    INSERT (RoleId, SubmoduleId, CanAccess, CanRead, CanWrite, CanEdit, CanDelete)
    VALUES (src.RoleId, src.SubmoduleId, src.CanAccess, src.CanRead, src.CanWrite, src.CanEdit, src.CanDelete);
GO
