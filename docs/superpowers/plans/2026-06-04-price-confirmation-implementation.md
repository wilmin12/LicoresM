# Price Confirmation / VIP Margins — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a 3-level price confirmation workflow: after a Calculation is Approved, the system computes new VIP selling prices using old margins from DHW, notifies the manager, and provides a UI to review and approve each price change.

**Architecture:** DhwDbContext gets two new RANKER entities (old prices + old costs). A new PriceCalculationService runs as fire-and-forget after Approve. A PriceConfirmationController exposes 5 read/write endpoints. The existing price-confirmation.html is redesigned as a 3-level manager UI. Rollback point: commit `03dff32` — run `git reset --hard 03dff32`.

**Tech Stack:** ASP.NET Core 8, Entity Framework Core, SQL Server, Bootstrap 5.3.2, FontAwesome 6.5.0, JS vanilla.

---

## Files

| Action | File |
|--------|------|
| MODIFY | `src/LicoresMaduro.API/Data/DhwDbContext.cs` |
| MODIFY | `src/LicoresMaduro.API/Data/ApplicationDbContext.cs` |
| CREATE | `database/86_PriceConfirmation.sql` |
| CREATE | `src/LicoresMaduro.API/Services/PriceCalculationService.cs` |
| CREATE | `src/LicoresMaduro.API/Controllers/CostCalc/PriceConfirmationController.cs` |
| MODIFY | `src/LicoresMaduro.API/Controllers/CostCalc/CostCalculationsController.cs` (Approve only) |
| MODIFY | `frontend/js/api.js` |
| MODIFY | `frontend/pages/cost-calc/price-confirmation.html` |

---

## Task 1: Map RANKER_553 and RANKER_99T in DhwDbContext

**Files:**
- Modify: `src/LicoresMaduro.API/Data/DhwDbContext.cs`

**Context:** RANKER_953 already exists as pattern (lines 120-135 for OnModelCreating, lines 317-330 for class). RANKER_553 holds old VIP prices. RANKER_99T holds old VIP costs. Column names follow DHW convention: PR01/PR03-PR11 for prices, COST01-COST10 for costs.

- [ ] **Step 1: Verify actual column names in DHW database**

Run this SQL against DHW_DATABASE to confirm column names before mapping:
```sql
SELECT TOP 1 * FROM RANKER_553;
SELECT TOP 1 * FROM RANKER_99T;
```
Expected: columns named PR01, PR03-PR11 for RANKER_553 and COST01-COST10 for RANKER_99T. If different, adjust the property names in Step 2 accordingly.

- [ ] **Step 2: Add DbSet declarations after existing Ranker952 (line ~17)**

In `DhwDbContext.cs`, find the line:
```csharp
    public DbSet<DhwRanker952>     Ranker952      => Set<DhwRanker952>();
```
Add after it:
```csharp
    public DbSet<DhwRanker553>     Ranker553      => Set<DhwRanker553>();
    public DbSet<DhwRanker99T>     Ranker99T      => Set<DhwRanker99T>();
```

- [ ] **Step 3: Add OnModelCreating mappings after the Ranker952 block (line ~135)**

Find the closing `});` of the Ranker952 block and add after it:
```csharp
        mb.Entity<DhwRanker553>(e =>
        {
            e.ToTable("RANKER_553");
            e.HasKey(x => x.Item);
            e.Property(x => x.Item).HasColumnName("ITEM").HasMaxLength(20);
            e.Property(x => x.Pr01).HasColumnName("PR01");
            e.Property(x => x.Pr03).HasColumnName("PR03");
            e.Property(x => x.Pr04).HasColumnName("PR04");
            e.Property(x => x.Pr05).HasColumnName("PR05");
            e.Property(x => x.Pr06).HasColumnName("PR06");
            e.Property(x => x.Pr07).HasColumnName("PR07");
            e.Property(x => x.Pr08).HasColumnName("PR08");
            e.Property(x => x.Pr09).HasColumnName("PR09");
            e.Property(x => x.Pr10).HasColumnName("PR10");
            e.Property(x => x.Pr11).HasColumnName("PR11");
        });

        mb.Entity<DhwRanker99T>(e =>
        {
            e.ToTable("RANKER_99T");
            e.HasKey(x => x.Item);
            e.Property(x => x.Item).HasColumnName("ITEM").HasMaxLength(20);
            e.Property(x => x.Cost01).HasColumnName("COST01");
            e.Property(x => x.Cost02).HasColumnName("COST02");
            e.Property(x => x.Cost03).HasColumnName("COST03");
            e.Property(x => x.Cost04).HasColumnName("COST04");
            e.Property(x => x.Cost05).HasColumnName("COST05");
            e.Property(x => x.Cost06).HasColumnName("COST06");
            e.Property(x => x.Cost07).HasColumnName("COST07");
            e.Property(x => x.Cost08).HasColumnName("COST08");
            e.Property(x => x.Cost09).HasColumnName("COST09");
            e.Property(x => x.Cost10).HasColumnName("COST10");
        });
```

- [ ] **Step 4: Add class definitions after DhwRanker952 class (line ~330)**

After the closing `}` of `DhwRanker952`, add:
```csharp
public class DhwRanker553
{
    public string   Item  { get; set; } = string.Empty;
    public decimal? Pr01  { get; set; }  // WHOLESALE
    public decimal? Pr03  { get; set; }  // STORE NORSA
    public decimal? Pr04  { get; set; }  // STORE RETAIL
    public decimal? Pr05  { get; set; }  // STORE ALLIANCE
    public decimal? Pr06  { get; set; }  // BONDED
    public decimal? Pr07  { get; set; }  // SPECIAL BONDED
    public decimal? Pr08  { get; set; }  // GWC_MANG_ESP
    public decimal? Pr09  { get; set; }  // BONDED YU HUA
    public decimal? Pr10  { get; set; }  // BBB DUTY PAID
    public decimal? Pr11  { get; set; }  // BBB BONDED
}

public class DhwRanker99T
{
    public string   Item   { get; set; } = string.Empty;
    public decimal? Cost01 { get; set; }  // FOB Price
    public decimal? Cost02 { get; set; }  // Inland Freight
    public decimal? Cost03 { get; set; }  // Ocean Freight
    public decimal? Cost04 { get; set; }  // Local Handling
    public decimal? Cost05 { get; set; }  // Duties
    public decimal? Cost06 { get; set; }  // Eco Surcharge
    public decimal? Cost07 { get; set; }  // OB Tax
    public decimal? Cost08 { get; set; }  // Insurance
    public decimal? Cost09 { get; set; }  // Transport
    public decimal? Cost10 { get; set; }  // Unloading
}
```

- [ ] **Step 5: Build and verify**
```bash
cd "C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\src\LicoresMaduro.API"
dotnet build
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 6: Commit**
```bash
git add src/LicoresMaduro.API/Data/DhwDbContext.cs
git commit -m "feat(price-confirm): map RANKER_553 (old prices) and RANKER_99T (old costs) in DhwDbContext"
```

---

## Task 2: SQL Migration 86

**Files:**
- Create: `database/86_PriceConfirmation.sql`

- [ ] **Step 1: Create the migration file**

Create `database/86_PriceConfirmation.sql`:

```sql
USE LicoresMaduoDB;
GO

-- Add reason code and price change date to PO heads
-- First verify table name:
-- SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%CALC%PO%'
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'COST_CALC_PO_HEADS' AND COLUMN_NAME = 'CCPH_ReasonCode')
BEGIN
    ALTER TABLE dbo.COST_CALC_PO_HEADS ADD CCPH_ReasonCode NVARCHAR(2) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'COST_CALC_PO_HEADS' AND COLUMN_NAME = 'CCPH_PriceChangeDate')
BEGIN
    ALTER TABLE dbo.COST_CALC_PO_HEADS ADD CCPH_PriceChangeDate DATE NULL;
END
GO

-- Price change reasons catalog
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CC_PRICE_CHANGE_REASONS')
BEGIN
    CREATE TABLE dbo.CC_PRICE_CHANGE_REASONS (
        PCR_Code        NVARCHAR(2)   NOT NULL CONSTRAINT PK_CC_PRICE_CHANGE_REASONS PRIMARY KEY,
        PCR_Description NVARCHAR(100) NOT NULL,
        PCR_Active      BIT           NOT NULL DEFAULT 1
    );
    INSERT INTO dbo.CC_PRICE_CHANGE_REASONS (PCR_Code, PCR_Description) VALUES ('01', 'Product costs did not change');
END
GO

-- Price confirmation matrix (one row per CalcNumber + PoNo + ItemNo)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CC_PRICE_CONFIRMATION')
BEGIN
    CREATE TABLE dbo.CC_PRICE_CONFIRMATION (
        CCPC_Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CC_PRICE_CONFIRMATION PRIMARY KEY,
        CCPC_CalcNumber      INT           NOT NULL,
        CCPC_PoNo            NVARCHAR(20)  NOT NULL,
        CCPC_ItemNo          NVARCHAR(20)  NOT NULL,
        CCPC_Warehouse       NVARCHAR(10)  NULL,
        CCPC_NewCost11010    DECIMAL(18,4) NULL,
        CCPC_NewCost11060    DECIMAL(18,4) NULL,
        CCPC_OldCost11010    DECIMAL(18,4) NULL,
        CCPC_OldCost11060    DECIMAL(18,4) NULL,
        -- PR01 WHOLESALE
        CCPC_NewPricePr01    DECIMAL(18,4) NULL,
        CCPC_NewMarginPr01   DECIMAL(18,4) NULL,
        CCPC_OldPricePr01    DECIMAL(18,4) NULL,
        CCPC_OldMarginPr01   DECIMAL(18,4) NULL,
        -- PR03 STORE NORSA
        CCPC_NewPricePr03    DECIMAL(18,4) NULL,
        CCPC_NewMarginPr03   DECIMAL(18,4) NULL,
        CCPC_OldPricePr03    DECIMAL(18,4) NULL,
        CCPC_OldMarginPr03   DECIMAL(18,4) NULL,
        -- PR04 STORE RETAIL
        CCPC_NewPricePr04    DECIMAL(18,4) NULL,
        CCPC_NewMarginPr04   DECIMAL(18,4) NULL,
        CCPC_OldPricePr04    DECIMAL(18,4) NULL,
        CCPC_OldMarginPr04   DECIMAL(18,4) NULL,
        -- PR05 STORE ALLIANCE
        CCPC_NewPricePr05    DECIMAL(18,4) NULL,
        CCPC_NewMarginPr05   DECIMAL(18,4) NULL,
        CCPC_OldPricePr05    DECIMAL(18,4) NULL,
        CCPC_OldMarginPr05   DECIMAL(18,4) NULL,
        -- PR06 BONDED
        CCPC_NewPricePr06    DECIMAL(18,4) NULL,
        CCPC_NewMarginPr06   DECIMAL(18,4) NULL,
        CCPC_OldPricePr06    DECIMAL(18,4) NULL,
        CCPC_OldMarginPr06   DECIMAL(18,4) NULL,
        -- PR07 SPECIAL BONDED
        CCPC_NewPricePr07    DECIMAL(18,4) NULL,
        CCPC_NewMarginPr07   DECIMAL(18,4) NULL,
        CCPC_OldPricePr07    DECIMAL(18,4) NULL,
        CCPC_OldMarginPr07   DECIMAL(18,4) NULL,
        -- PR08 GWC_MANG_ESP
        CCPC_NewPricePr08    DECIMAL(18,4) NULL,
        CCPC_NewMarginPr08   DECIMAL(18,4) NULL,
        CCPC_OldPricePr08    DECIMAL(18,4) NULL,
        CCPC_OldMarginPr08   DECIMAL(18,4) NULL,
        -- PR09 BONDED YU HUA
        CCPC_NewPricePr09    DECIMAL(18,4) NULL,
        CCPC_NewMarginPr09   DECIMAL(18,4) NULL,
        CCPC_OldPricePr09    DECIMAL(18,4) NULL,
        CCPC_OldMarginPr09   DECIMAL(18,4) NULL,
        -- PR10 BBB DUTY PAID (PR01 * 0.90)
        CCPC_NewPricePr10    DECIMAL(18,4) NULL,
        CCPC_NewMarginPr10   DECIMAL(18,4) NULL,
        CCPC_OldPricePr10    DECIMAL(18,4) NULL,
        CCPC_OldMarginPr10   DECIMAL(18,4) NULL,
        -- PR11 BBB BONDED
        CCPC_NewPricePr11    DECIMAL(18,4) NULL,
        CCPC_NewMarginPr11   DECIMAL(18,4) NULL,
        CCPC_OldPricePr11    DECIMAL(18,4) NULL,
        CCPC_OldMarginPr11   DECIMAL(18,4) NULL,
        -- Decision per item
        CCPC_PriceChangeFlag BIT           NULL,
        CCPC_ApprovedBy      NVARCHAR(100) NULL,
        CCPC_ApprovedAt      DATETIME      NULL,
        CCPC_CreatedAt       DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_CC_PRICE_CONFIRMATION UNIQUE (CCPC_CalcNumber, CCPC_PoNo, CCPC_ItemNo)
    );
END
GO

-- Verify
SELECT 'CC_PRICE_CONFIRMATION' AS TableName, COUNT(*) AS RowCount FROM dbo.CC_PRICE_CONFIRMATION
UNION ALL
SELECT 'CC_PRICE_CHANGE_REASONS', COUNT(*) FROM dbo.CC_PRICE_CHANGE_REASONS;
GO
```

- [ ] **Step 2: Verify the table name for COST_CALC_PO_HEADS**

Before running the full script, run this against LicoresMaduoDB to confirm:
```sql
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%CALC%PO%';
```
If the table is named differently (e.g. `CC_CALC_PO_HEADS`), update the ALTER TABLE statements in the script.

- [ ] **Step 3: Execute the script in SQL Server**

Run `database/86_PriceConfirmation.sql` against LicoresMaduoDB. The final SELECT should return:
```
CC_PRICE_CONFIRMATION    0
CC_PRICE_CHANGE_REASONS  1
```

- [ ] **Step 4: Commit**
```bash
git add database/86_PriceConfirmation.sql
git commit -m "feat(price-confirm): add CC_PRICE_CONFIRMATION and CC_PRICE_CHANGE_REASONS tables, extend PO heads"
```

---

## Task 3: EF Entities in ApplicationDbContext

**Files:**
- Modify: `src/LicoresMaduro.API/Data/ApplicationDbContext.cs`

- [ ] **Step 1: Add DbSet declarations near CcCalcPoHead (around line 167)**

Find the DbSet section near `CcCalcPoDetail` and add:
```csharp
public DbSet<CcPriceConfirmation>  CcPriceConfirmations  => Set<CcPriceConfirmation>();
public DbSet<CcPriceChangeReason>  CcPriceChangeReasons  => Set<CcPriceChangeReason>();
```

- [ ] **Step 2: Add OnModelCreating mappings**

In `OnModelCreating`, after the `CcCalcPoDetail` mapping block, add:
```csharp
modelBuilder.Entity<CcPriceChangeReason>(e =>
{
    e.ToTable("CC_PRICE_CHANGE_REASONS");
    e.HasKey(x => x.PcrCode);
    e.Property(x => x.PcrCode).HasColumnName("PCR_Code").HasMaxLength(2);
    e.Property(x => x.PcrDescription).HasColumnName("PCR_Description").HasMaxLength(100);
    e.Property(x => x.PcrActive).HasColumnName("PCR_Active");
});

modelBuilder.Entity<CcPriceConfirmation>(e =>
{
    e.ToTable("CC_PRICE_CONFIRMATION");
    e.HasKey(x => x.CcpcId);
    e.Property(x => x.CcpcId).HasColumnName("CCPC_Id");
    e.Property(x => x.CcpcCalcNumber).HasColumnName("CCPC_CalcNumber");
    e.Property(x => x.CcpcPoNo).HasColumnName("CCPC_PoNo").HasMaxLength(20);
    e.Property(x => x.CcpcItemNo).HasColumnName("CCPC_ItemNo").HasMaxLength(20);
    e.Property(x => x.CcpcWarehouse).HasColumnName("CCPC_Warehouse").HasMaxLength(10);
    e.Property(x => x.CcpcNewCost11010).HasColumnName("CCPC_NewCost11010");
    e.Property(x => x.CcpcNewCost11060).HasColumnName("CCPC_NewCost11060");
    e.Property(x => x.CcpcOldCost11010).HasColumnName("CCPC_OldCost11010");
    e.Property(x => x.CcpcOldCost11060).HasColumnName("CCPC_OldCost11060");
    e.Property(x => x.CcpcNewPricePr01).HasColumnName("CCPC_NewPricePr01");
    e.Property(x => x.CcpcNewMarginPr01).HasColumnName("CCPC_NewMarginPr01");
    e.Property(x => x.CcpcOldPricePr01).HasColumnName("CCPC_OldPricePr01");
    e.Property(x => x.CcpcOldMarginPr01).HasColumnName("CCPC_OldMarginPr01");
    e.Property(x => x.CcpcNewPricePr03).HasColumnName("CCPC_NewPricePr03");
    e.Property(x => x.CcpcNewMarginPr03).HasColumnName("CCPC_NewMarginPr03");
    e.Property(x => x.CcpcOldPricePr03).HasColumnName("CCPC_OldPricePr03");
    e.Property(x => x.CcpcOldMarginPr03).HasColumnName("CCPC_OldMarginPr03");
    e.Property(x => x.CcpcNewPricePr04).HasColumnName("CCPC_NewPricePr04");
    e.Property(x => x.CcpcNewMarginPr04).HasColumnName("CCPC_NewMarginPr04");
    e.Property(x => x.CcpcOldPricePr04).HasColumnName("CCPC_OldPricePr04");
    e.Property(x => x.CcpcOldMarginPr04).HasColumnName("CCPC_OldMarginPr04");
    e.Property(x => x.CcpcNewPricePr05).HasColumnName("CCPC_NewPricePr05");
    e.Property(x => x.CcpcNewMarginPr05).HasColumnName("CCPC_NewMarginPr05");
    e.Property(x => x.CcpcOldPricePr05).HasColumnName("CCPC_OldPricePr05");
    e.Property(x => x.CcpcOldMarginPr05).HasColumnName("CCPC_OldMarginPr05");
    e.Property(x => x.CcpcNewPricePr06).HasColumnName("CCPC_NewPricePr06");
    e.Property(x => x.CcpcNewMarginPr06).HasColumnName("CCPC_NewMarginPr06");
    e.Property(x => x.CcpcOldPricePr06).HasColumnName("CCPC_OldPricePr06");
    e.Property(x => x.CcpcOldMarginPr06).HasColumnName("CCPC_OldMarginPr06");
    e.Property(x => x.CcpcNewPricePr07).HasColumnName("CCPC_NewPricePr07");
    e.Property(x => x.CcpcNewMarginPr07).HasColumnName("CCPC_NewMarginPr07");
    e.Property(x => x.CcpcOldPricePr07).HasColumnName("CCPC_OldPricePr07");
    e.Property(x => x.CcpcOldMarginPr07).HasColumnName("CCPC_OldMarginPr07");
    e.Property(x => x.CcpcNewPricePr08).HasColumnName("CCPC_NewPricePr08");
    e.Property(x => x.CcpcNewMarginPr08).HasColumnName("CCPC_NewMarginPr08");
    e.Property(x => x.CcpcOldPricePr08).HasColumnName("CCPC_OldPricePr08");
    e.Property(x => x.CcpcOldMarginPr08).HasColumnName("CCPC_OldMarginPr08");
    e.Property(x => x.CcpcNewPricePr09).HasColumnName("CCPC_NewPricePr09");
    e.Property(x => x.CcpcNewMarginPr09).HasColumnName("CCPC_NewMarginPr09");
    e.Property(x => x.CcpcOldPricePr09).HasColumnName("CCPC_OldPricePr09");
    e.Property(x => x.CcpcOldMarginPr09).HasColumnName("CCPC_OldMarginPr09");
    e.Property(x => x.CcpcNewPricePr10).HasColumnName("CCPC_NewPricePr10");
    e.Property(x => x.CcpcNewMarginPr10).HasColumnName("CCPC_NewMarginPr10");
    e.Property(x => x.CcpcOldPricePr10).HasColumnName("CCPC_OldPricePr10");
    e.Property(x => x.CcpcOldMarginPr10).HasColumnName("CCPC_OldMarginPr10");
    e.Property(x => x.CcpcNewPricePr11).HasColumnName("CCPC_NewPricePr11");
    e.Property(x => x.CcpcNewMarginPr11).HasColumnName("CCPC_NewMarginPr11");
    e.Property(x => x.CcpcOldPricePr11).HasColumnName("CCPC_OldPricePr11");
    e.Property(x => x.CcpcOldMarginPr11).HasColumnName("CCPC_OldMarginPr11");
    e.Property(x => x.CcpcPriceChangeFlag).HasColumnName("CCPC_PriceChangeFlag");
    e.Property(x => x.CcpcApprovedBy).HasColumnName("CCPC_ApprovedBy").HasMaxLength(100);
    e.Property(x => x.CcpcApprovedAt).HasColumnName("CCPC_ApprovedAt");
    e.Property(x => x.CcpcCreatedAt).HasColumnName("CCPC_CreatedAt");
});
```

- [ ] **Step 3: Add to existing CcCalcPoHead entity mapping — ReasonCode and PriceChangeDate**

Find the `CcCalcPoHead` mapping in OnModelCreating and add the two new properties:
```csharp
e.Property(x => x.CcphReasonCode).HasColumnName("CCPH_ReasonCode").HasMaxLength(2);
e.Property(x => x.CcphPriceChangeDate).HasColumnName("CCPH_PriceChangeDate");
```

- [ ] **Step 4: Add properties to CcCalcPoHead class**

Find the `CcCalcPoHead` class (around line 2140) and add after `CcphApprovedBy`:
```csharp
    public string?    CcphReasonCode        { get; set; }
    public DateTime?  CcphPriceChangeDate   { get; set; }
```

- [ ] **Step 5: Add new entity classes at end of ApplicationDbContext.cs**

After all existing entity classes, add:
```csharp
public class CcPriceChangeReason
{
    public string PcrCode        { get; set; } = string.Empty;
    public string PcrDescription { get; set; } = string.Empty;
    public bool   PcrActive      { get; set; } = true;
}

public class CcPriceConfirmation
{
    public int      CcpcId             { get; set; }
    public int      CcpcCalcNumber     { get; set; }
    public string   CcpcPoNo           { get; set; } = string.Empty;
    public string   CcpcItemNo         { get; set; } = string.Empty;
    public string?  CcpcWarehouse      { get; set; }
    public decimal? CcpcNewCost11010   { get; set; }
    public decimal? CcpcNewCost11060   { get; set; }
    public decimal? CcpcOldCost11010   { get; set; }
    public decimal? CcpcOldCost11060   { get; set; }
    public decimal? CcpcNewPricePr01   { get; set; }
    public decimal? CcpcNewMarginPr01  { get; set; }
    public decimal? CcpcOldPricePr01   { get; set; }
    public decimal? CcpcOldMarginPr01  { get; set; }
    public decimal? CcpcNewPricePr03   { get; set; }
    public decimal? CcpcNewMarginPr03  { get; set; }
    public decimal? CcpcOldPricePr03   { get; set; }
    public decimal? CcpcOldMarginPr03  { get; set; }
    public decimal? CcpcNewPricePr04   { get; set; }
    public decimal? CcpcNewMarginPr04  { get; set; }
    public decimal? CcpcOldPricePr04   { get; set; }
    public decimal? CcpcOldMarginPr04  { get; set; }
    public decimal? CcpcNewPricePr05   { get; set; }
    public decimal? CcpcNewMarginPr05  { get; set; }
    public decimal? CcpcOldPricePr05   { get; set; }
    public decimal? CcpcOldMarginPr05  { get; set; }
    public decimal? CcpcNewPricePr06   { get; set; }
    public decimal? CcpcNewMarginPr06  { get; set; }
    public decimal? CcpcOldPricePr06   { get; set; }
    public decimal? CcpcOldMarginPr06  { get; set; }
    public decimal? CcpcNewPricePr07   { get; set; }
    public decimal? CcpcNewMarginPr07  { get; set; }
    public decimal? CcpcOldPricePr07   { get; set; }
    public decimal? CcpcOldMarginPr07  { get; set; }
    public decimal? CcpcNewPricePr08   { get; set; }
    public decimal? CcpcNewMarginPr08  { get; set; }
    public decimal? CcpcOldPricePr08   { get; set; }
    public decimal? CcpcOldMarginPr08  { get; set; }
    public decimal? CcpcNewPricePr09   { get; set; }
    public decimal? CcpcNewMarginPr09  { get; set; }
    public decimal? CcpcOldPricePr09   { get; set; }
    public decimal? CcpcOldMarginPr09  { get; set; }
    public decimal? CcpcNewPricePr10   { get; set; }
    public decimal? CcpcNewMarginPr10  { get; set; }
    public decimal? CcpcOldPricePr10   { get; set; }
    public decimal? CcpcOldMarginPr10  { get; set; }
    public decimal? CcpcNewPricePr11   { get; set; }
    public decimal? CcpcNewMarginPr11  { get; set; }
    public decimal? CcpcOldPricePr11   { get; set; }
    public decimal? CcpcOldMarginPr11  { get; set; }
    public bool?    CcpcPriceChangeFlag { get; set; }
    public string?  CcpcApprovedBy     { get; set; }
    public DateTime? CcpcApprovedAt    { get; set; }
    public DateTime CcpcCreatedAt      { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 6: Build and verify**
```bash
dotnet build "C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\src\LicoresMaduro.API"
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 7: Commit**
```bash
git add src/LicoresMaduro.API/Data/ApplicationDbContext.cs
git commit -m "feat(price-confirm): add CcPriceConfirmation and CcPriceChangeReason entities, extend CcCalcPoHead"
```

---

## Task 4: PriceCalculationService

**Files:**
- Create: `src/LicoresMaduro.API/Services/PriceCalculationService.cs`

**Context:** This service loads old prices (RANKER_553) and old costs (RANKER_99T) from DhwDbContext, computes new costs from CcCalcPoDetail, applies the PDF formulas, and persists to CC_PRICE_CONFIRMATION. It runs as fire-and-forget so failures never block Approve.

- [ ] **Step 1: Create the service file**

Create `src/LicoresMaduro.API/Services/PriceCalculationService.cs`:

```csharp
using LicoresMaduro.API.Data;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Services;

public interface IPriceCalculationService
{
    Task ComputeAndPersistAsync(int calcNumber, string? approvedBy, CancellationToken ct = default);
}

public sealed class PriceCalculationService : IPriceCalculationService
{
    private readonly ApplicationDbContext _db;
    private readonly DhwDbContext         _dhw;
    private readonly ILogger<PriceCalculationService> _logger;

    public PriceCalculationService(ApplicationDbContext db, DhwDbContext dhw,
        ILogger<PriceCalculationService> logger)
    { _db = db; _dhw = dhw; _logger = logger; }

    public async Task ComputeAndPersistAsync(int calcNumber, string? approvedBy, CancellationToken ct = default)
    {
        try
        {
            var sysCfg = await _db.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct);
            var poHeads = await _db.CcCalcPoHeads
                .Include(p => p.Details)
                .Where(p => p.CcphCalcNumber == calcNumber)
                .AsNoTracking()
                .ToListAsync(ct);

            var itemCodes = poHeads.SelectMany(p => p.Details)
                .Select(d => d.CcpdItemNo).Distinct().ToList();

            var pricesMap = await _dhw.Ranker553
                .Where(r => itemCodes.Contains(r.Item))
                .AsNoTracking()
                .ToDictionaryAsync(r => r.Item, ct);

            var costsMap = await _dhw.Ranker99T
                .Where(r => itemCodes.Contains(r.Item))
                .AsNoTracking()
                .ToDictionaryAsync(r => r.Item, ct);

            decimal norsaPerc    = (sysCfg?.CompStoreNorsaPerc    ?? 0m) / 100m;
            decimal retailPerc   = (sysCfg?.CompStoreRetailPerc   ?? 0m) / 100m;
            decimal alliancePerc = (sysCfg?.CompStoreAlliancePerc ?? 0m) / 100m;

            var toUpsert = new List<CcPriceConfirmation>();

            foreach (var po in poHeads)
            foreach (var d in po.Details)
            {
                pricesMap.TryGetValue(d.CcpdItemNo, out var op);
                costsMap.TryGetValue(d.CcpdItemNo, out var oc);

                // New costs from CcCalcPoDetail
                decimal newCost11010 = Sum(d.CcpdFobPrice, d.CcpdInlandFreight, d.CcpdFreight,
                    d.CcpdLocalHandl, d.CcpdDuties, d.CcpdEconSurch, d.CcpdOb,
                    d.CcpdInsurance, d.CcpdTransport, d.CcpdUnloading);
                decimal newCost11060 = Sum(d.CcpdFobPrice, d.CcpdInlandFreight, d.CcpdFreight,
                    d.CcpdLocalHandl, d.CcpdInsurance, d.CcpdTransport, d.CcpdUnloading);

                // Old costs from RANKER_99T (same grouping rules)
                decimal oldCost11010 = oc == null ? 0m : Sum(oc.Cost01, oc.Cost02, oc.Cost03,
                    oc.Cost04, oc.Cost05, oc.Cost06, oc.Cost07, oc.Cost08, oc.Cost09, oc.Cost10);
                decimal oldCost11060 = oc == null ? 0m : Sum(oc.Cost01, oc.Cost02, oc.Cost03,
                    oc.Cost04, oc.Cost08, oc.Cost09, oc.Cost10);

                // Old margins from RANKER_553 prices + RANKER_99T costs
                decimal mPR01 = Margin(op?.Pr01, oldCost11010);
                decimal mPR06 = Margin(op?.Pr06, oldCost11060);
                decimal mPR07 = Margin(op?.Pr07, oldCost11060);
                decimal mPR08 = Margin(op?.Pr08, oldCost11060);
                decimal mPR09 = Margin(op?.Pr09, oldCost11060);
                decimal mPR11 = Margin(op?.Pr11, oldCost11060);

                // New prices using old margins
                decimal nPR01 = Price(newCost11010, mPR01);
                decimal nPR06 = Price(newCost11060, mPR06);
                decimal nPR07 = Price(newCost11060, mPR07);
                decimal nPR08 = Price(newCost11060, mPR08);
                decimal nPR09 = Price(newCost11060, mPR09);
                decimal nPR11 = Price(newCost11060, mPR11);
                decimal nPR03 = nPR01 * (1m + norsaPerc);
                decimal nPR04 = nPR01 * (1m + retailPerc);
                decimal nPR05 = nPR01 * (1m + alliancePerc);
                decimal nPR10 = nPR01 * 0.90m;

                toUpsert.Add(new CcPriceConfirmation
                {
                    CcpcCalcNumber   = calcNumber,
                    CcpcPoNo         = po.CcphLmPoNo,
                    CcpcItemNo       = d.CcpdItemNo,
                    CcpcWarehouse    = po.CcphWhse,
                    CcpcNewCost11010 = newCost11010,
                    CcpcNewCost11060 = newCost11060,
                    CcpcOldCost11010 = oldCost11010,
                    CcpcOldCost11060 = oldCost11060,
                    CcpcNewPricePr01 = nPR01, CcpcNewMarginPr01 = Margin(nPR01, newCost11010),
                    CcpcOldPricePr01 = op?.Pr01, CcpcOldMarginPr01 = mPR01,
                    CcpcNewPricePr03 = nPR03, CcpcNewMarginPr03 = Margin(nPR03, newCost11010),
                    CcpcOldPricePr03 = op?.Pr03, CcpcOldMarginPr03 = Margin(op?.Pr03, oldCost11010),
                    CcpcNewPricePr04 = nPR04, CcpcNewMarginPr04 = Margin(nPR04, newCost11010),
                    CcpcOldPricePr04 = op?.Pr04, CcpcOldMarginPr04 = Margin(op?.Pr04, oldCost11010),
                    CcpcNewPricePr05 = nPR05, CcpcNewMarginPr05 = Margin(nPR05, newCost11010),
                    CcpcOldPricePr05 = op?.Pr05, CcpcOldMarginPr05 = Margin(op?.Pr05, oldCost11010),
                    CcpcNewPricePr06 = nPR06, CcpcNewMarginPr06 = Margin(nPR06, newCost11060),
                    CcpcOldPricePr06 = op?.Pr06, CcpcOldMarginPr06 = mPR06,
                    CcpcNewPricePr07 = nPR07, CcpcNewMarginPr07 = Margin(nPR07, newCost11060),
                    CcpcOldPricePr07 = op?.Pr07, CcpcOldMarginPr07 = mPR07,
                    CcpcNewPricePr08 = nPR08, CcpcNewMarginPr08 = Margin(nPR08, newCost11060),
                    CcpcOldPricePr08 = op?.Pr08, CcpcOldMarginPr08 = mPR08,
                    CcpcNewPricePr09 = nPR09, CcpcNewMarginPr09 = Margin(nPR09, newCost11060),
                    CcpcOldPricePr09 = op?.Pr09, CcpcOldMarginPr09 = mPR09,
                    CcpcNewPricePr10 = nPR10, CcpcNewMarginPr10 = Margin(nPR10, newCost11010),
                    CcpcOldPricePr10 = op?.Pr10, CcpcOldMarginPr10 = Margin(op?.Pr10, oldCost11010),
                    CcpcNewPricePr11 = nPR11, CcpcNewMarginPr11 = Margin(nPR11, newCost11060),
                    CcpcOldPricePr11 = op?.Pr11, CcpcOldMarginPr11 = mPR11,
                    CcpcApprovedBy   = approvedBy,
                    CcpcCreatedAt    = DateTime.UtcNow
                });
            }

            // Upsert: delete old rows for this calc then insert fresh
            var existing = _db.CcPriceConfirmations.Where(x => x.CcpcCalcNumber == calcNumber);
            _db.CcPriceConfirmations.RemoveRange(existing);
            _db.CcPriceConfirmations.AddRange(toUpsert);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("PriceCalc #{CalcNumber}: persisted {Count} rows", calcNumber, toUpsert.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PriceCalc #{CalcNumber} failed", calcNumber);
        }
    }

    // Cost sum helper — treats non-nullable decimal (like CcpdDuties) as-is
    private static decimal Sum(params object?[] values)
    {
        decimal total = 0m;
        foreach (var v in values)
            total += v is decimal d ? d : (v is decimal? dn ? dn ?? 0m : 0m);
        return total;
    }

    // Margin = Round(((Price - Cost) / Price) * 100, 2)
    private static decimal Margin(decimal? price, decimal cost)
    {
        if (price == null || price == 0m) return 0m;
        return Math.Round(((price.Value - cost) / price.Value) * 100m, 2);
    }

    // Price = Cost / (1 - Margin/100)
    private static decimal Price(decimal cost, decimal margin)
    {
        if (margin >= 100m) return 0m;
        return cost / (1m - margin / 100m);
    }
}
```

- [ ] **Step 2: Register service in DI (Program.cs or wherever services are registered)**

Find where other services are registered (search for `builder.Services.AddScoped` or `services.AddScoped`). Add:
```csharp
builder.Services.AddScoped<IPriceCalculationService, PriceCalculationService>();
```

- [ ] **Step 3: Build and verify**
```bash
dotnet build "C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\src\LicoresMaduro.API"
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 4: Commit**
```bash
git add src/LicoresMaduro.API/Services/PriceCalculationService.cs
git commit -m "feat(price-confirm): add PriceCalculationService with VIP margin formulas"
```

---

## Task 5: Modify Approve Endpoint

**Files:**
- Modify: `src/LicoresMaduro.API/Controllers/CostCalc/CostCalculationsController.cs`

**Context:** Lines 884-930. Two changes: (1) PO status changes from "AP" to "PC"; (2) fire-and-forget call to PriceCalculationService using IServiceScopeFactory. IServiceScopeFactory needed because service is scoped and fire-and-forget runs outside the request scope.

- [ ] **Step 1: Add IServiceScopeFactory to constructor**

Find the constructor of `CostCalculationsController`:
```csharp
public CostCalculationsController(ApplicationDbContext db, DhwDbContext dhw, ILogger<CostCalculationsController> logger, IPermissionService permissions)
{ _db = db; _dhw = dhw; _logger = logger; _permissions = permissions; }
```

Replace with:
```csharp
private readonly IServiceScopeFactory _scopeFactory;
public CostCalculationsController(ApplicationDbContext db, DhwDbContext dhw,
    ILogger<CostCalculationsController> logger, IPermissionService permissions,
    IServiceScopeFactory scopeFactory)
{ _db = db; _dhw = dhw; _logger = logger; _permissions = permissions; _scopeFactory = scopeFactory; }
```

- [ ] **Step 2: Change PO status from "AP" to "PC" (line 894)**

Find:
```csharp
        foreach (var p in calc.PoHeads) { p.CcphStatus = "AP"; p.CcphApprovedBy = User.Identity?.Name; }
```

Replace with:
```csharp
        foreach (var p in calc.PoHeads) { p.CcphStatus = "PC"; p.CcphApprovedBy = User.Identity?.Name; }
```

- [ ] **Step 3: Add fire-and-forget for price calculation after SaveChangesAsync (line 895)**

Find:
```csharp
        await _db.SaveChangesAsync(ct);

        // Pre-load data needed for background tasks while DbContext is still alive
```

Replace with:
```csharp
        await _db.SaveChangesAsync(ct);

        // Fire-and-forget: compute VIP price matrix in background
        var calcIdForBg   = id;
        var approvedByBg  = User.Identity?.Name;
        var scopeFactory  = _scopeFactory;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IPriceCalculationService>();
                await svc.ComputeAndPersistAsync(calcIdForBg, approvedByBg);
            }
            catch (Exception ex)
            {
                // Failure is logged inside the service; do not propagate
                _ = ex;
            }
        });

        // Pre-load data needed for background tasks while DbContext is still alive
```

- [ ] **Step 4: Add using for IPriceCalculationService at top of file if not present**

Check if `using LicoresMaduro.API.Services;` is already at the top of the file. If not, add it.

- [ ] **Step 5: Build and verify**
```bash
dotnet build "C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\src\LicoresMaduro.API"
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 6: Commit**
```bash
git add src/LicoresMaduro.API/Controllers/CostCalc/CostCalculationsController.cs
git commit -m "feat(price-confirm): modify Approve to set POs to PC status and trigger price matrix calculation"
```

---

## Task 6: PriceConfirmationController

**Files:**
- Create: `src/LicoresMaduro.API/Controllers/CostCalc/PriceConfirmationController.cs`

- [ ] **Step 1: Create the controller**

Create `src/LicoresMaduro.API/Controllers/CostCalc/PriceConfirmationController.cs`:

```csharp
using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/price-confirmations")]
[Authorize]
[Produces("application/json")]
public sealed class PriceConfirmationController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PriceConfirmationController> _logger;

    public PriceConfirmationController(ApplicationDbContext db, ILogger<PriceConfirmationController> logger)
    { _db = db; _logger = logger; }

    // Level 1: POs pending manager price confirmation
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var pos = await _db.CcCalcPoHeads
            .AsNoTracking()
            .Where(p => p.CcphStatus == "PC")
            .OrderBy(p => p.CcphCalcNumber).ThenBy(p => p.CcphLmPoNo)
            .Select(p => new {
                p.CcphCalcNumber,
                p.CcphLmPoNo,
                p.CcphVendNo,
                p.CcphVendName,
                p.CcphCurrCode,
                p.CcphCurrRate,
                p.CcphInvNumber,
                p.CcphInvDate,
                p.CcphTotQty,
                p.CcphTotAmount,
                p.CcphWeight,
                p.CcphInlandFreight
            })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(pos));
    }

    // Level 2: Product analysis for a specific PO
    [HttpGet("{calcId:int}/{poNo}")]
    public async Task<IActionResult> GetPoItems(int calcId, string poNo, CancellationToken ct)
    {
        var items = await _db.CcPriceConfirmations
            .AsNoTracking()
            .Where(x => x.CcpcCalcNumber == calcId && x.CcpcPoNo == poNo)
            .OrderBy(x => x.CcpcItemNo)
            .ToListAsync(ct);
        if (!items.Any()) return NotFound(ApiResponse.Fail($"No price data found for Calc {calcId} / PO {poNo}."));
        return Ok(ApiResponse<object>.Ok(items));
    }

    // Level 3: Full case prices for a specific item
    [HttpGet("{calcId:int}/{poNo}/{itemNo}")]
    public async Task<IActionResult> GetItemPrices(int calcId, string poNo, string itemNo, CancellationToken ct)
    {
        var item = await _db.CcPriceConfirmations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CcpcCalcNumber == calcId
                && x.CcpcPoNo == poNo && x.CcpcItemNo == itemNo, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Item {itemNo} not found in price confirmation."));
        return Ok(ApiResponse<CcPriceConfirmation>.Ok(item));
    }

    // Bidirectional recalc: given price → compute margin, or given margin → compute price
    [HttpPost("recalc")]
    public IActionResult Recalc([FromBody] RecalcDto dto)
    {
        if (dto.Price.HasValue && dto.Price > 0 && dto.Cost > 0)
        {
            var margin = Math.Round(((dto.Price.Value - dto.Cost) / dto.Price.Value) * 100m, 2);
            return Ok(ApiResponse<object>.Ok(new { margin, price = dto.Price.Value }));
        }
        if (dto.Margin.HasValue && dto.Margin < 100 && dto.Cost > 0)
        {
            var price = dto.Cost / (1m - dto.Margin.Value / 100m);
            var margin = Math.Round(((price - dto.Cost) / price) * 100m, 2);
            return Ok(ApiResponse<object>.Ok(new { margin, price }));
        }
        return BadRequest(ApiResponse.Fail("Provide either Price or Margin (not both), and a valid Cost > 0."));
    }

    // Approve price changes for a PO
    [HttpPost("{calcId:int}/{poNo}/approve")]
    public async Task<IActionResult> ApprovePo(int calcId, string poNo,
        [FromBody] ApprovePoDto dto, CancellationToken ct)
    {
        var po = await _db.CcCalcPoHeads
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.CcphCalcNumber == calcId && p.CcphLmPoNo == poNo, ct);
        if (po is null) return NotFound(ApiResponse.Fail($"PO {poNo} not found."));
        if (po.CcphStatus != "PC") return BadRequest(ApiResponse.Fail("Only PC-status POs can be approved here."));

        // Update price change flags on individual items
        if (dto.ItemFlags?.Count > 0)
        {
            var itemNos = dto.ItemFlags.Keys.ToList();
            var rows = await _db.CcPriceConfirmations
                .Where(x => x.CcpcCalcNumber == calcId && x.CcpcPoNo == poNo
                    && itemNos.Contains(x.CcpcItemNo))
                .ToListAsync(ct);
            foreach (var row in rows)
            {
                if (dto.ItemFlags.TryGetValue(row.CcpcItemNo, out var flag))
                {
                    row.CcpcPriceChangeFlag = flag;
                    row.CcpcApprovedBy = User.Identity?.Name;
                    row.CcpcApprovedAt = DateTime.UtcNow;
                }
            }
        }

        // Update PR01 selling price on detail records for compatibility
        foreach (var detail in po.Details)
        {
            var confirmed = await _db.CcPriceConfirmations
                .FirstOrDefaultAsync(x => x.CcpcCalcNumber == calcId
                    && x.CcpcPoNo == poNo && x.CcpcItemNo == detail.CcpdItemNo, ct);
            if (confirmed?.CcpcNewPricePr01 != null)
                detail.CcpdSellingPrice = confirmed.CcpcNewPricePr01;
        }

        po.CcphStatus          = "PD"; // Price Done
        po.CcphReasonCode      = dto.ReasonCode;
        po.CcphPriceChangeDate = dto.PriceChangeDate;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("PriceConfirm: Calc {CalcId} PO {PoNo} approved by {User}",
            calcId, poNo, User.Identity?.Name);
        return Ok(ApiResponse.Ok("Price changes approved."));
    }

    // Reason codes catalog
    [HttpGet("reasons")]
    public async Task<IActionResult> GetReasons(CancellationToken ct)
    {
        var reasons = await _db.CcPriceChangeReasons
            .AsNoTracking()
            .Where(r => r.PcrActive)
            .OrderBy(r => r.PcrCode)
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(reasons));
    }
}

public record RecalcDto(decimal Cost, decimal? Price, decimal? Margin);
public record ApprovePoDto(string? ReasonCode, DateTime? PriceChangeDate, Dictionary<string, bool>? ItemFlags);
```

- [ ] **Step 2: Build and verify**
```bash
dotnet build "C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\src\LicoresMaduro.API"
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 3: Commit**
```bash
git add src/LicoresMaduro.API/Controllers/CostCalc/PriceConfirmationController.cs
git commit -m "feat(price-confirm): add PriceConfirmationController with 5 endpoints for manager workflow"
```

---

## Task 7: api.js — Price Confirmation Methods

**Files:**
- Modify: `frontend/js/api.js`

- [ ] **Step 1: Add priceConfirmation methods to costCalc object**

Find the last method in the `costCalc` object (currently `deleteVendorCif`):
```javascript
    deleteVendorCif: (code) => del(`/api/cost-calc/vendor-cif/${encodeURIComponent(code)}`)
  };
```

Replace with:
```javascript
    deleteVendorCif: (code) => del(`/api/cost-calc/vendor-cif/${encodeURIComponent(code)}`),
    // Price Confirmation
    getPendingPricePos:     ()                      => get('/api/cost-calc/price-confirmations/pending'),
    getPoItemPrices:        (calcId, poNo)          => get(`/api/cost-calc/price-confirmations/${calcId}/${encodeURIComponent(poNo)}`),
    getItemCasePrices:      (calcId, poNo, itemNo)  => get(`/api/cost-calc/price-confirmations/${calcId}/${encodeURIComponent(poNo)}/${encodeURIComponent(itemNo)}`),
    recalcPrice:            (dto)                   => post('/api/cost-calc/price-confirmations/recalc', dto),
    approvePoPrices:        (calcId, poNo, dto)     => post(`/api/cost-calc/price-confirmations/${calcId}/${encodeURIComponent(poNo)}/approve`, dto),
    getPriceChangeReasons:  ()                      => get('/api/cost-calc/price-confirmations/reasons')
  };
```

- [ ] **Step 2: Verify**
```bash
grep -n "getPendingPricePos\|approvePoPrices\|getPriceChangeReasons" "C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\frontend\js\api.js"
```
Expected: 3 lines found.

- [ ] **Step 3: Commit**
```bash
git add frontend/js/api.js
git commit -m "feat(price-confirm): add price confirmation API methods to api.js"
```

---

## Task 8: price-confirmation.html — 3-Level Manager UI

**Files:**
- Modify: `frontend/pages/cost-calc/price-confirmation.html`

**Context:** Completely replaces existing simple page. 3 views on one page: Level 1 = pending PO list, Level 2 = product analysis, Level 3 = case prices modal. Navigation: back buttons return to previous level. Uses `API.costCalc.getPendingPricePos()`, `getPoItemPrices()`, `getItemCasePrices()`, `recalcPrice()`, `approvePoPrices()`.

- [ ] **Step 1: Replace price-confirmation.html completely**

Read the current file first (it exists with a simple structure). Then replace the entire contents with:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/><meta name="viewport" content="width=device-width, initial-scale=1.0"/>
  <title>Price Confirmation - Licores Maduro</title>
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css"/>
  <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css"/>
  <link rel="stylesheet" href="../../css/main.css"/>
  <style>
    .level { display:none; }
    .level.active { display:block; }
    .tbl th { font-size:.75rem; white-space:nowrap; background:#f1f5f9; }
    .tbl td { font-size:.8rem; vertical-align:middle; }
    .wh-label { font-size:.7rem; color:#6b7280; font-weight:600; }
    .price-input { width:90px; text-align:right; font-size:.8rem; }
    .margin-input { width:70px; text-align:right; font-size:.8rem; }
    .change-pos { color:#16a34a; font-weight:600; }
    .change-neg { color:#dc2626; font-weight:600; }
    .badge-pc  { background:#f59e0b; color:#fff; }
    .pr-col-new { background:#fefce8; }
    .pr-col-old { background:#f8fafc; color:#6b7280; }
  </style>
</head>
<body>
<div id="sidebar-overlay" style="display:none;position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:999;"></div>
<div class="app-wrapper">
  <nav id="sidebar" class="sidebar">
    <div class="sidebar-header"><img src="../../img/LogoLicores.png" alt="Licores Maduro" class="sidebar-logo-img"/><span class="sidebar-logo-text">LICORES MADURO</span></div>
    <div id="sidebar-nav" class="sidebar-nav"></div>
    <div class="sidebar-footer"><div class="sidebar-user"><div class="sidebar-avatar" id="sidebar-user-initials">LM</div><div class="sidebar-text"><div id="sidebar-user-name" style="font-weight:600;">-</div></div></div></div>
  </nav>
  <div class="main-area">
    <header class="topbar">
      <button id="btn-sidebar-toggle" class="btn-sidebar-toggle"><i class="fas fa-bars"></i></button>
      <span class="topbar-title"><i class="fas fa-tags me-2 text-wine"></i>Price Confirmation</span>
      <div class="ms-auto d-flex align-items-center gap-2">
        <div class="dropdown">
          <button class="btn btn-outline-secondary btn-sm dropdown-toggle d-flex align-items-center gap-2" type="button" data-bs-toggle="dropdown">
            <span class="sidebar-avatar" id="topbar-user-initials" style="width:28px;height:28px;font-size:.75rem;">LM</span>
            <span id="topbar-user-name" class="d-none d-md-inline">-</span>
          </button>
          <ul class="dropdown-menu dropdown-menu-end shadow-sm">
            <li><div class="dropdown-header"><strong id="topbar-user-fullname">User</strong><br/><small class="text-muted" id="topbar-user-role">Role</small></div></li>
            <li><hr class="dropdown-divider"/></li>
            <li><a class="dropdown-item text-danger" href="#" id="btn-logout"><i class="fas fa-sign-out-alt me-2"></i>Logout</a></li>
          </ul>
        </div>
      </div>
    </header>
    <main class="page-content">
      <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
          <li class="breadcrumb-item"><a href="../../dashboard.html"><i class="fas fa-home me-1"></i>Home</a></li>
          <li class="breadcrumb-item"><a href="index.html">Cost Calculations</a></li>
          <li class="breadcrumb-item active" id="bc-label">Price Confirmation</li>
        </ol>
      </nav>

      <!-- LEVEL 1: Pending PO list -->
      <div id="level1" class="level active">
        <div class="page-title">Purchase Order Selling Prices to be Approved</div>
        <div class="page-subtitle">POs pending price confirmation after cost calculation approval.</div>
        <div class="card mt-3">
          <div class="card-header d-flex align-items-center justify-content-between">
            <span><i class="fas fa-list me-2"></i>Pending POs</span>
            <span class="badge" style="background:var(--primary);color:#fff;" id="l1-count">0</span>
          </div>
          <div class="card-body p-0">
            <div class="table-responsive">
              <table class="table table-hover tbl mb-0">
                <thead><tr>
                  <th>LM P.O. Number</th><th>Calc No.</th><th>Vendor</th><th>Vendor Name</th>
                  <th>Curr</th><th>Rate</th><th>Invoice Nr.</th><th>Invoice Date</th>
                  <th class="text-end">Cases</th><th class="text-end">Amount</th>
                  <th class="text-end">Act. Wght</th><th class="text-end">Inl. Frght.</th>
                </tr></thead>
                <tbody id="l1-tbody"><tr><td colspan="12" class="text-center py-4 text-muted"><i class="fas fa-spinner fa-spin me-2"></i>Loading...</td></tr></tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      <!-- LEVEL 2: Product price analysis -->
      <div id="level2" class="level">
        <div class="d-flex align-items-center gap-3 mb-3">
          <button class="btn btn-outline-secondary btn-sm" onclick="showLevel(1)"><i class="fas fa-arrow-left me-1"></i>Back</button>
          <div>
            <div class="page-title mb-0">Approve Price Changes by SALES MANAGER</div>
            <div class="text-muted small" id="l2-subtitle"></div>
          </div>
        </div>
        <div class="card">
          <div class="card-body p-0">
            <div class="table-responsive">
              <table class="table tbl mb-0">
                <thead><tr>
                  <th>Item No.</th><th>Description</th><th>Qty</th><th>FOB F/C</th>
                  <th>WH</th><th class="text-end">Allowed Margin%</th><th class="text-end">Actual Margin%</th>
                  <th class="text-end">New Cost</th><th class="text-end">Old Cost</th>
                  <th class="text-end">Change Cost</th><th class="text-end">New Price Calc</th>
                  <th class="text-end">Old Price</th><th class="text-end">Change Price</th>
                  <th class="text-end">Change MRG</th><th class="text-end">New Price (Case)</th>
                  <th></th>
                </tr></thead>
                <tbody id="l2-tbody"></tbody>
              </table>
            </div>
          </div>
          <div class="card-footer d-flex align-items-center gap-3 flex-wrap">
            <div class="d-flex align-items-center gap-2">
              <label class="form-label mb-0 small fw-semibold">Reason Code:</label>
              <select id="sel-reason" class="form-select form-select-sm" style="width:260px;"></select>
            </div>
            <div class="d-flex align-items-center gap-2">
              <label class="form-label mb-0 small fw-semibold">Price Change Date:</label>
              <input type="date" id="inp-date" class="form-control form-control-sm" style="width:150px;"/>
            </div>
            <button class="btn btn-wine btn-sm ms-auto" id="btn-approve-po">
              <i class="fas fa-check-circle me-1"></i>APPROVE PRICE CHANGES AND SEND EMAIL
            </button>
          </div>
        </div>
      </div>

      <!-- LEVEL 3: Case prices modal-like panel -->
      <div id="level3" class="level">
        <div class="d-flex align-items-center gap-3 mb-3">
          <button class="btn btn-outline-secondary btn-sm" id="btn-back-l2"><i class="fas fa-arrow-left me-1"></i>Back</button>
          <div class="page-title mb-0">Case Prices — <span id="l3-item-label"></span></div>
        </div>
        <div class="card">
          <div class="card-body p-0">
            <div class="table-responsive">
              <table class="table tbl mb-0" id="l3-table">
                <thead>
                  <tr id="l3-pr-header"></tr>
                </thead>
                <tbody id="l3-tbody"></tbody>
              </table>
            </div>
          </div>
          <div class="card-footer d-flex align-items-center gap-3">
            <div class="form-check form-check-inline">
              <input class="form-check-input" type="radio" name="priceChange" id="chk-change" value="1"/>
              <label class="form-check-label fw-semibold text-success" for="chk-change">PRICE CHANGE</label>
            </div>
            <div class="form-check form-check-inline">
              <input class="form-check-input" type="radio" name="priceChange" id="chk-nochange" value="0"/>
              <label class="form-check-label fw-semibold text-secondary" for="chk-nochange">NO PRICE CHANGE</label>
            </div>
            <button class="btn btn-wine btn-sm ms-auto" id="btn-save-l3">Save &amp; Back</button>
          </div>
        </div>
      </div>
    </main>
  </div>
</div>

<div class="toast-container position-fixed bottom-0 end-0 p-3" style="z-index:9999;">
  <div id="toast" class="toast align-items-center text-white border-0" role="alert">
    <div class="d-flex"><div class="toast-body" id="toast-msg"></div>
    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>
  </div>
</div>

<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
<script src="../../js/i18n.js"></script><script src="../../js/api.js"></script>
<script src="../../js/auth.js"></script><script src="../../js/sidebar.js"></script>
<script>
const PR_LABELS = {
  Pr01:'PR01 WHOLESALE', Pr03:'PR03 STORE NORSA', Pr04:'PR04 STORE RETAIL',
  Pr05:'PR05 STORE ALLIANCE', Pr06:'PR06 BONDED', Pr07:'PR07 SPECIAL BONDED',
  Pr08:'PR08 GWC_MAN_ESP', Pr09:'PR09 BONDED YU HUA', Pr10:'PR10 BBB DUTY PAID', Pr11:'PR11 BBB BONDED'
};
const PR_KEYS = Object.keys(PR_LABELS);

let allPos = [], reasons = [];
let currentCalcId = null, currentPoNo = null, currentItemNo = null;
let itemFlags = {}; // { itemNo: true/false }
let l3Data = null;

document.addEventListener('DOMContentLoaded', async () => {
  if (!Auth.requireAuth()) return;
  Auth.startExpiryWatcher(); Auth.populateUserUI(); Sidebar.init();
  document.getElementById('btn-logout').addEventListener('click', e => { e.preventDefault(); Auth.logout(); });
  document.getElementById('btn-approve-po').addEventListener('click', approvePo);
  document.getElementById('btn-back-l2').addEventListener('click', () => showLevel(2));
  document.getElementById('btn-save-l3').addEventListener('click', saveL3);
  await Promise.all([loadPos(), loadReasons()]);
});

async function loadPos() {
  try {
    const r = await API.costCalc.getPendingPricePos();
    allPos = r?.Data || [];
    renderL1();
  } catch(e) { showToast(e.message,'danger'); }
}

async function loadReasons() {
  try {
    const r = await API.costCalc.getPriceChangeReasons();
    reasons = r?.Data || [];
    const sel = document.getElementById('sel-reason');
    sel.innerHTML = '<option value="">-- Select reason --</option>' +
      reasons.map(r => `<option value="${esc(r.PcrCode)}">${esc(r.PcrCode)} - ${esc(r.PcrDescription)}</option>`).join('');
  } catch(e) { /* non-blocking */ }
}

function renderL1() {
  const tbody = document.getElementById('l1-tbody');
  document.getElementById('l1-count').textContent = allPos.length + ' records';
  if (!allPos.length) { tbody.innerHTML = '<tr><td colspan="12" class="text-center py-4 text-muted">No pending POs.</td></tr>'; return; }
  tbody.innerHTML = allPos.map(p => `<tr style="cursor:pointer;" onclick="openPo(${p.CcphCalcNumber},'${esc(p.CcphLmPoNo)}')">
    <td><strong>${esc(p.CcphLmPoNo)}</strong></td>
    <td>${p.CcphCalcNumber}</td>
    <td>${esc(p.CcphVendNo||'')}</td>
    <td>${esc(p.CcphVendName||'')}</td>
    <td>${esc(p.CcphCurrCode||'')}</td>
    <td class="text-end">${fmt(p.CcphCurrRate,4)}</td>
    <td>${esc(p.CcphInvNumber||'')}</td>
    <td>${p.CcphInvDate?new Date(p.CcphInvDate).toLocaleDateString('en-US'):'-'}</td>
    <td class="text-end">${fmt(p.CcphTotQty,0)}</td>
    <td class="text-end">${fmt(p.CcphTotAmount)}</td>
    <td class="text-end">${fmt(p.CcphWeight)}</td>
    <td class="text-end">${fmt(p.CcphInlandFreight)}</td>
  </tr>`).join('');
}

async function openPo(calcId, poNo) {
  currentCalcId = calcId; currentPoNo = poNo; itemFlags = {};
  document.getElementById('l2-subtitle').textContent = `Calc #${calcId} — PO: ${poNo}`;
  const tbody = document.getElementById('l2-tbody');
  tbody.innerHTML = '<tr><td colspan="16" class="text-center py-3 text-muted"><i class="fas fa-spinner fa-spin me-2"></i>Loading...</td></tr>';
  showLevel(2);
  try {
    const r = await API.costCalc.getPoItemPrices(calcId, poNo);
    const items = r?.Data || [];
    renderL2(items);
  } catch(e) { showToast(e.message,'danger'); }
}

function renderL2(items) {
  const tbody = document.getElementById('l2-tbody');
  if (!items.length) { tbody.innerHTML = '<tr><td colspan="16" class="text-center py-3 text-muted">No items.</td></tr>'; return; }
  tbody.innerHTML = items.map(d => {
    const chg11010 = (d.CcpcNewCost11010||0) - (d.CcpcOldCost11010||0);
    const chgP = (d.CcpcNewPricePr01||0) - (d.CcpcOldPricePr01||0);
    const chgM = (d.CcpcNewMarginPr01||0) - (d.CcpcOldMarginPr01||0);
    const chgCls = v => v > 0 ? 'change-pos' : v < 0 ? 'change-neg' : '';
    return `<tr>
      <td><strong>${esc(d.CcpcItemNo)}</strong></td>
      <td>${esc(d.CcpcWarehouse||'')}</td>
      <td></td><td></td>
      <td><span class="wh-label">11010:</span><br/><span class="wh-label">11060:</span></td>
      <td class="text-end">-</td>
      <td class="text-end">${fmt(d.CcpcNewMarginPr01)}<br/>${fmt(d.CcpcNewMarginPr06)}</td>
      <td class="text-end">${fmt(d.CcpcNewCost11010)}<br/>${fmt(d.CcpcNewCost11060)}</td>
      <td class="text-end">${fmt(d.CcpcOldCost11010)}<br/>${fmt(d.CcpcOldCost11060)}</td>
      <td class="text-end ${chgCls(chg11010)}">${fmt(chg11010)}</td>
      <td class="text-end">${fmt(d.CcpcNewPricePr01)}</td>
      <td class="text-end">${fmt(d.CcpcOldPricePr01)}</td>
      <td class="text-end ${chgCls(chgP)}">${fmt(chgP)}</td>
      <td class="text-end ${chgCls(chgM)}">${fmt(chgM)}</td>
      <td class="text-end fw-bold">${fmt(d.CcpcNewPricePr01)}</td>
      <td><button class="btn btn-outline-secondary btn-sm" onclick="openL3(${currentCalcId},'${esc(currentPoNo)}','${esc(d.CcpcItemNo)}')">SHOW ALL PRICES &gt;&gt;&gt;</button></td>
    </tr>`;
  }).join('');
}

async function openL3(calcId, poNo, itemNo) {
  currentItemNo = itemNo;
  document.getElementById('l3-item-label').textContent = itemNo;
  showLevel(3);
  try {
    const r = await API.costCalc.getItemCasePrices(calcId, poNo, itemNo);
    l3Data = r?.Data;
    renderL3(l3Data);
    if (itemFlags[itemNo] !== undefined) {
      document.getElementById(itemFlags[itemNo] ? 'chk-change' : 'chk-nochange').checked = true;
    }
  } catch(e) { showToast(e.message,'danger'); }
}

function renderL3(d) {
  const header = document.getElementById('l3-pr-header');
  const tbody = document.getElementById('l3-tbody');
  header.innerHTML = '<th>Label</th>' + PR_KEYS.map(k => `<th class="text-center">${PR_LABELS[k]}</th>`).join('');

  const makeRow = (label, vals, cls, editable, type) =>
    `<tr><td class="fw-semibold">${label}</td>` +
    PR_KEYS.map((k,i) => {
      const v = vals[i];
      if (editable) return `<td class="${cls}"><input class="${type}-input form-control form-control-sm ${cls}" data-key="${k}" data-type="${type}" value="${v!=null?Number(v).toFixed(2):''}"/></td>`;
      return `<td class="${cls} text-end ${v!=null&&Number(v)>0?'':'text-muted'}">${v!=null?fmt(v):'–'}</td>`;
    }).join('') + '</tr>';

  const newPrices  = PR_KEYS.map(k => d[`CcpcNewPrice${k.charAt(0).toUpperCase()+k.slice(1)}`]);
  const newMargins = PR_KEYS.map(k => d[`CcpcNewMargin${k.charAt(0).toUpperCase()+k.slice(1)}`]);
  const oldPrices  = PR_KEYS.map(k => d[`CcpcOldPrice${k.charAt(0).toUpperCase()+k.slice(1)}`]);
  const oldMargins = PR_KEYS.map(k => d[`CcpcOldMargin${k.charAt(0).toUpperCase()+k.slice(1)}`]);

  tbody.innerHTML =
    makeRow('New Price',  newPrices,  'pr-col-new', true,  'price')  +
    makeRow('New Margin', newMargins, 'pr-col-new', true,  'margin') +
    makeRow('Old Price',  oldPrices,  'pr-col-old', false, '') +
    makeRow('Old Margin', oldMargins, 'pr-col-old', false, '');

  // Bidirectional recalc on input
  tbody.querySelectorAll('input[data-type]').forEach(inp => {
    inp.addEventListener('change', async () => {
      const key  = inp.dataset.key;
      const type = inp.dataset.type;
      const val  = parseFloat(inp.value);
      if (isNaN(val)) return;
      const cost = type === 'price'
        ? (key.endsWith('Pr06')||key.endsWith('Pr07')||key.endsWith('Pr08')||key.endsWith('Pr09')||key.endsWith('Pr11')
           ? (d.CcpcNewCost11060||0) : (d.CcpcNewCost11010||0))
        : (key.endsWith('Pr06')||key.endsWith('Pr07')||key.endsWith('Pr08')||key.endsWith('Pr09')||key.endsWith('Pr11')
           ? (d.CcpcNewCost11060||0) : (d.CcpcNewCost11010||0));
      try {
        const dto = type === 'price' ? {Cost:cost, Price:val} : {Cost:cost, Margin:val};
        const r   = await API.costCalc.recalcPrice(dto);
        const res = r?.Data;
        if (res) {
          const sibling = type === 'price' ? 'margin' : 'price';
          const sibKey  = `Ccpc${sibling.charAt(0).toUpperCase()+sibling.slice(1)}${key.charAt(0).toUpperCase()+key.slice(1)}`;
          const sibInp  = tbody.querySelector(`input[data-key="${key}"][data-type="${sibling}"]`);
          if (sibInp) sibInp.value = Number(res[sibling]).toFixed(2);
        }
      } catch(e) { /* non-blocking */ }
    });
  });
}

function saveL3() {
  const flag = document.querySelector('input[name="priceChange"]:checked')?.value;
  if (flag !== undefined && currentItemNo) itemFlags[currentItemNo] = flag === '1';
  showLevel(2);
}

async function approvePo() {
  const reason = document.getElementById('sel-reason').value;
  const date   = document.getElementById('inp-date').value;
  if (!reason) { showToast('Please select a Reason Code.','danger'); return; }
  if (!date)   { showToast('Please enter a Price Change Date.','danger'); return; }
  const btn = document.getElementById('btn-approve-po');
  btn.disabled = true;
  try {
    await API.costCalc.approvePoPrices(currentCalcId, currentPoNo, {
      ReasonCode: reason,
      PriceChangeDate: date,
      ItemFlags: itemFlags
    });
    showToast('Price changes approved successfully.','success');
    await loadPos();
    showLevel(1);
  } catch(e) { showToast(e.message||'Error approving.','danger'); }
  finally { btn.disabled = false; }
}

function showLevel(n) {
  document.querySelectorAll('.level').forEach(el => el.classList.remove('active'));
  document.getElementById('level'+n).classList.add('active');
  document.getElementById('bc-label').textContent =
    n === 1 ? 'Price Confirmation' : n === 2 ? `PO: ${currentPoNo}` : `Item: ${currentItemNo}`;
}

function fmt(v, d=2) { return v!=null ? Number(v).toLocaleString('en-US',{minimumFractionDigits:d,maximumFractionDigits:d}) : '–'; }
function showToast(msg, type='success') {
  const el = document.getElementById('toast');
  el.className = `toast align-items-center text-white border-0 bg-${type==='success'?'success':'danger'}`;
  document.getElementById('toast-msg').textContent = msg;
  new bootstrap.Toast(el, {delay:3500}).show();
}
function esc(s) { return String(s??'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
</script>
</body>
</html>
```

- [ ] **Step 2: Verify file exists**
```bash
Test-Path "C:\Desarrollo\Prueba Gemini\Proyecto Licores Maduro\frontend\pages\cost-calc\price-confirmation.html"
```
Expected: `True`

- [ ] **Step 3: Commit**
```bash
git add frontend/pages/cost-calc/price-confirmation.html
git commit -m "feat(price-confirm): redesign price-confirmation.html as 3-level manager UI"
```

---

## Self-Review Checklist

- [x] RANKER_553 (old prices) and RANKER_99T (old costs) mapped in DhwDbContext — Task 1
- [x] CC_PRICE_CONFIRMATION and CC_PRICE_CHANGE_REASONS tables with seed data — Task 2
- [x] EF entities + CcCalcPoHead extended with ReasonCode/PriceChangeDate — Task 3
- [x] PriceCalculationService: all PDF formulas (11010/11060, margins, PR01-PR11, PR03/04/05 from %, PR10=PR01*0.90) — Task 4
- [x] Fire-and-forget via IServiceScopeFactory — does not block Approve — Task 5
- [x] Approve now sets PO status to PC (not AP) — Task 5
- [x] 5 endpoints: pending, items, case prices, recalc, approve — Task 6
- [x] api.js 6 new methods — Task 7
- [x] Level 1 PO list, Level 2 product analysis, Level 3 case prices with bidirectional edit — Task 8
- [x] Reason Code + Price Change Date captured at PO level — Task 8
- [x] PRICE CHANGE / NO PRICE CHANGE checkboxes per item — Task 8
- [x] Backward compatibility: CcpdSellingPrice updated with PR01 new price — Task 6
