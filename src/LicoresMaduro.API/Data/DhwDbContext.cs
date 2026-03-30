using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Data;

/// <summary>Read-only context for DHW_DATABASE source tables used by Cost Calculation.</summary>
public sealed class DhwDbContext : DbContext
{
    public DhwDbContext(DbContextOptions<DhwDbContext> options) : base(options) { }

    public DbSet<DhwPoHeader>      PoHeaders      => Set<DhwPoHeader>();
    public DbSet<DhwPoDetail>      PoDetails      => Set<DhwPoDetail>();
    public DbSet<DhwShipperMaster> ShipperMasters => Set<DhwShipperMaster>();
    public DbSet<DhwCountry>       Countries      => Set<DhwCountry>();
    public DbSet<DhwSupplier>      Suppliers      => Set<DhwSupplier>();

    // ── Scalar function: Description_Items_BEER ───────────────────────────────
    public static string? DescriptionItemsBeer(string itemCode)
        => throw new NotSupportedException("EF Core only — use in LINQ queries.");
    public DbSet<DhwItemFob>     ItemFobPrices => Set<DhwItemFob>();
    public DbSet<DhwSystemTable> SystemTable  => Set<DhwSystemTable>();

    // ── Route Assignment VIP source tables ────────────────────────────────────
    // NOTE: VIP column names (AS/400 format) - verify against actual DHW_DATABASE schema
    public DbSet<DhwDailyT>  DailyT  => Set<DhwDailyT>();
    public DbSet<DhwBrattT>  BrattT  => Set<DhwBrattT>();
    public DbSet<DhwItemT>   ItemT   => Set<DhwItemT>();

    // ── Stock Analysis VIP source tables ──────────────────────────────────────
    public DbSet<DhwInvent> Invents => Set<DhwInvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Register scalar DB function
        mb.HasDbFunction(typeof(DhwDbContext).GetMethod(nameof(DescriptionItemsBeer), new[] { typeof(string) })!)
            .HasName("Description_Items_BEER")
            .HasSchema("dbo");

        mb.Entity<DhwPoHeader>(e =>
        {
            e.ToTable("POHDRT");
            e.HasKey(x => new { x.PhWhse, x.PhPoNo });
            e.Property(x => x.PhWhse).HasColumnName("PHWHSE").HasMaxLength(3);
            e.Property(x => x.PhPoNo).HasColumnName("PHPO#").HasMaxLength(10);
            e.Property(x => x.PhBorw).HasColumnName("PHBORW").HasMaxLength(1);
            e.Property(x => x.PhBrvr).HasColumnName("PHBRVR").HasMaxLength(6);
            e.Property(x => x.PhOrdt).HasColumnName("PHORDT");
            e.Property(x => x.PhShdt).HasColumnName("PHSHDT");
            e.Property(x => x.PhArdt).HasColumnName("PHARDT");
            e.Property(x => x.PhConNo).HasColumnName("PHCON#").HasMaxLength(20);
            e.Property(x => x.PhOqt).HasColumnName("PHOQTY");
            e.Property(x => x.PhWeig).HasColumnName("PHWEIG");
            e.Property(x => x.PhLtrs).HasColumnName("PHLTRS");
            e.Property(x => x.PhTotAmt).HasColumnName("PHTOT$");
            e.Property(x => x.PhOvrNo).HasColumnName("PHOVR#").HasMaxLength(6);
            e.Property(x => x.PhLine).HasColumnName("PHLINE");
            e.Property(x => x.PhStat).HasColumnName("PHSTAT").HasMaxLength(2);
            e.Property(x => x.PhShip).HasColumnName("PHSHIP").HasMaxLength(10);
        });

        mb.Entity<DhwShipperMaster>(e =>
        {
            e.ToTable("SHIPPER_MASTER");
            e.HasKey(x => x.ShipperId);
            e.Property(x => x.ShipperId).HasColumnName("SHIPPER_ID").HasMaxLength(4);
            e.Property(x => x.ShipperName).HasColumnName("SHIPPER_NAME").HasMaxLength(25);
            e.Property(x => x.Address1).HasColumnName("ADDRESS_1").HasMaxLength(25);
            e.Property(x => x.Address2).HasColumnName("ADDRESS_2").HasMaxLength(25);
            e.Property(x => x.IdCity).HasColumnName("ID_CITY");
            e.Property(x => x.TelephoneNumber).HasColumnName("TELEPHONE_NUMBER");
            e.Property(x => x.FaxNumber).HasColumnName("FAX_NUMBER");
            e.Property(x => x.DeleteFlag).HasColumnName("DELETEFLAG").HasMaxLength(1);
            e.Property(x => x.CreateTimestamp).HasColumnName("CREATE_TIMESTAMP");
        });

        mb.Entity<DhwCountry>(e =>
        {
            e.ToTable("COUNTRYT");
            e.HasKey(x => x.Identity);
            e.Property(x => x.Identity).HasColumnName("IDENTITY");
            e.Property(x => x.IsoAlpha2).HasColumnName("ISO_ALPHA_2").HasMaxLength(2);
            e.Property(x => x.IsoAlpha3).HasColumnName("ISO_ALPHA_3").HasMaxLength(3);
            e.Property(x => x.IsoNumeric).HasColumnName("ISO_NUMERIC");
            e.Property(x => x.Description).HasColumnName("DESCRIPTION").HasMaxLength(60);
        });

        mb.Entity<DhwSupplier>(e =>
        {
            e.ToTable("SUPPLIERT");
            e.HasKey(x => x.Identity);
            e.Property(x => x.Identity).HasColumnName("IDENTITY");
            e.Property(x => x.Supplier).HasColumnName("SUPPLIER").HasMaxLength(2);
            e.Property(x => x.SupplierName).HasColumnName("SUPPLIER_NAME").HasMaxLength(25);
            e.Property(x => x.ApVendor).HasColumnName("AP_VENDOR");
            e.Property(x => x.DeleteFlag).HasColumnName("DELETE_FLAG").HasMaxLength(1);
        });

        mb.Entity<DhwPoDetail>(e =>
        {
            e.ToTable("PODTLT");
            e.HasKey(x => new { x.PdWhse, x.PdPoNo, x.PdLine });
            e.Property(x => x.PdWhse).HasColumnName("PDWHSE").HasMaxLength(5);
            e.Property(x => x.PdPoNo).HasColumnName("PDPO#").HasMaxLength(15);
            e.Property(x => x.PdLine).HasColumnName("PDLINE");
            e.Property(x => x.PdItem).HasColumnName("PDITEM").HasMaxLength(6);
            e.Property(x => x.PdOqty).HasColumnName("PDOQTY");
            e.Property(x => x.PdRqty).HasColumnName("PDRQTY");
            e.Property(x => x.PdWeig).HasColumnName("PDWEIG");
            e.Property(x => x.PdLtrs).HasColumnName("PDLTRS");
            e.Property(x => x.PdCstAmt).HasColumnName("PDCST$");
            e.Property(x => x.PdUnit).HasColumnName("PDUNIT");
            e.Property(x => x.PdSitem).HasColumnName("PDSITM").HasMaxLength(14);
            e.Property(x => x.PdBsw).HasColumnName("PDBSW").HasMaxLength(1);
            e.Property(x => x.PdClas).HasColumnName("PDCLAS").HasMaxLength(2);
            e.Property(x => x.PdBrvr).HasColumnName("PDBRVR").HasMaxLength(2);
            e.Property(x => x.PdBran).HasColumnName("PDBRAN").HasMaxLength(3);
            e.Property(x => x.PdStat).HasColumnName("PDSTAT").HasMaxLength(1);
            e.Property(x => x.PdCdAt).HasColumnName("PDCDAT");
            e.Property(x => x.PdRdAt).HasColumnName("PDRDAT");
        });

        mb.Entity<DhwItemFob>(e =>
        {
            e.ToTable("ITEM_FOB_PRICES");
            e.HasKey(x => x.ItCode);
            e.Property(x => x.ItCode).HasColumnName("IT_Code").HasMaxLength(20);
            e.Property(x => x.ItPurchasePrice).HasColumnName("IT_Purchase_Price");
            e.Property(x => x.ItCommodity).HasColumnName("IT_Commodity").HasMaxLength(20);
        });

        mb.Entity<DhwSystemTable>(e =>
        {
            e.ToTable("SYSTEM_TABLE");
            e.HasKey(x => x.CompCode);
            e.Property(x => x.CompCode).HasColumnName("COMP_CODE").HasMaxLength(10);
            e.Property(x => x.CompName).HasColumnName("COMP_NAME").HasMaxLength(50);
            e.Property(x => x.CompCurrLocal).HasColumnName("COMP_CURR_LOCAL").HasMaxLength(3);
            e.Property(x => x.CompCurrUsd).HasColumnName("COMP_CURR_USD").HasMaxLength(3);
            e.Property(x => x.CompRateUsd).HasColumnName("COMP_RATE_USD");
            e.Property(x => x.CompInsurance).HasColumnName("COMP_INSURANCE");
            e.Property(x => x.CompTransport).HasColumnName("COMP_TRANSPORT");
            e.Property(x => x.CompUnloading).HasColumnName("COMP_UNLOADING");
            e.Property(x => x.CompLocalHandling).HasColumnName("COMP_LOCAL_HANDLING");
            e.Property(x => x.CompFwCode).HasColumnName("COMP_FWCODE").HasMaxLength(10);
            e.Property(x => x.CompFwName).HasColumnName("COMP_FWNAME").HasMaxLength(50);
            e.Property(x => x.CompFwCurr).HasColumnName("COMP_FWCURR").HasMaxLength(3);
            e.Property(x => x.CompOzFactor).HasColumnName("COMP_OZ_FACTOR");
            e.Property(x => x.CompLiterMultiplier).HasColumnName("COMP_LITER_MULTIPLIER");
        });

        // ── Route Assignment: VIP source tables ───────────────────────────────
        // NOTE: VIP column names (AS/400 format) - verify against actual DHW_DATABASE schema

        mb.Entity<DhwDailyT>(e =>
        {
            e.ToTable("DAILYT");
            e.HasKey(x => new { x.DlyWhse, x.DlyInNo, x.DlyLine });
            e.Property(x => x.DlyWhse).HasColumnName("DLYWHSE").HasMaxLength(3);
            e.Property(x => x.DlyInDt).HasColumnName("DLYINDT");
            e.Property(x => x.DlyInNo).HasColumnName("DLYIN#").HasMaxLength(20);
            e.Property(x => x.DlyLine).HasColumnName("DLYLINE");
            e.Property(x => x.DlyAcct).HasColumnName("DLYACCT").HasMaxLength(20);
            e.Property(x => x.DlyName).HasColumnName("DLYNAME").HasMaxLength(50);
            e.Property(x => x.DlyItem).HasColumnName("DLYITEM").HasMaxLength(20);
            e.Property(x => x.DlyIdsc).HasColumnName("DLYIDSC").HasMaxLength(50);
            e.Property(x => x.DlyCqty).HasColumnName("DLYCQTY");
            e.Property(x => x.DlyBqty).HasColumnName("DLYBQTY");
            e.Property(x => x.DlyUom).HasColumnName("DLYUOM").HasMaxLength(6);
            e.Property(x => x.DlyUpcs).HasColumnName("DLYUPCS");
            e.Property(x => x.DlyUpri).HasColumnName("DLYUPRI");
            e.Property(x => x.DlyDisc).HasColumnName("DLYDISC");
            e.Property(x => x.DlyExtp).HasColumnName("DLYEXTP");
            e.Property(x => x.DlyUcst).HasColumnName("DLYUCST");
            e.Property(x => x.DlyCcst).HasColumnName("DLYCOST");
            e.Property(x => x.DlyRoute).HasColumnName("DLYROUT").HasMaxLength(20);
            e.Property(x => x.DlyLoad).HasColumnName("DLYLOAD").HasMaxLength(20);
            e.Property(x => x.DlyDriver).HasColumnName("DLYDRVR").HasMaxLength(20);
            e.Property(x => x.DlySalesRepName).HasColumnName("DLYRPNM").HasMaxLength(50);
            e.Property(x => x.DlySalesRepNo).HasColumnName("DLYRPNO").HasMaxLength(20);
        });

        mb.Entity<DhwBrattT>(e =>
        {
            e.ToTable("BRATTT");
            e.HasKey(x => x.BrAcct);
            e.Property(x => x.BrAcct).HasColumnName("BRACCT").HasMaxLength(20);
            e.Property(x => x.BrName).HasColumnName("BRNAME").HasMaxLength(50);
            e.Property(x => x.BrAddress).HasColumnName("BRADR").HasMaxLength(100);
            e.Property(x => x.BrStatus).HasColumnName("BRSTN").HasMaxLength(2);
            e.Property(x => x.BrUserField4).HasColumnName("BRUSF4").HasMaxLength(20);
            e.Property(x => x.BrUserField4Desc).HasColumnName("BRUS4D").HasMaxLength(50);
            e.Property(x => x.BrRoute).HasColumnName("BRROUT").HasMaxLength(20);
            e.Property(x => x.BrRouteDesc).HasColumnName("BRROUD").HasMaxLength(50);
            e.Property(x => x.BrSubClass).HasColumnName("BRSCLS").HasMaxLength(20);
            e.Property(x => x.BrSubClassDesc).HasColumnName("BRSCLD").HasMaxLength(50);
            e.Property(x => x.BrSalesmanCode).HasColumnName("BRSLSC").HasMaxLength(20);
            e.Property(x => x.BrSalesmanName).HasColumnName("BRSLSN").HasMaxLength(50);
            e.Property(x => x.BrDriverCode).HasColumnName("BRDRVR").HasMaxLength(20);
            e.Property(x => x.BrDriverName).HasColumnName("BRDRVN").HasMaxLength(50);
            e.Property(x => x.BrMerchandiser).HasColumnName("BRMERC").HasMaxLength(20);
            e.Property(x => x.BrOnOffPremise).HasColumnName("BRONPR").HasMaxLength(10);
            e.Property(x => x.BrIndustryVol2).HasColumnName("BRIV2#").HasMaxLength(20);
            e.Property(x => x.BrIndustryVol2Desc).HasColumnName("BRIV2D").HasMaxLength(50);
            e.Property(x => x.BrRetailClass).HasColumnName("BRRCLS").HasMaxLength(20);
            e.Property(x => x.BrRetailClassDesc).HasColumnName("BRRCLD").HasMaxLength(50);
            e.Property(x => x.BrRetailerSalesman).HasColumnName("BRRSLS").HasMaxLength(20);
            e.Property(x => x.BrVisitDaySalesman).HasColumnName("BRVDSL").HasMaxLength(20);
            e.Property(x => x.BrDeliveryDayDriver).HasColumnName("BRDDDR").HasMaxLength(20);
            e.Property(x => x.BrVisitTimeSalesman).HasColumnName("BRVTSL").HasMaxLength(20);
        });

        mb.Entity<DhwItemT>(e =>
        {
            e.ToTable("ITEMT");
            e.HasKey(x => x.ItItem);
            e.Property(x => x.ItItem).HasColumnName("ITEM_CODE").HasMaxLength(6);
            e.Property(x => x.ItDesc).HasColumnName("PACKAGE_DESCRIPTION").HasMaxLength(55);
            e.Property(x => x.ItStatus).HasColumnName("ITEM_STATUS").HasMaxLength(1);
            e.Property(x => x.ItSupCode).HasColumnName("SUPPLIER_CODE").HasMaxLength(2);
            e.Property(x => x.ItBrandCode).HasColumnName("BRAND_CODE").HasMaxLength(3);
            e.Property(x => x.ItProductClass).HasColumnName("PRODUCT_CLASS").HasMaxLength(2);
            e.Property(x => x.ItUnitsPerCase).HasColumnName("UNITS_CASE");
            e.Property(x => x.ItMlPerBottle).HasColumnName("MILLILITERS_PER_BOTTLE");
        });

        // ── Stock Analysis: INVENT table ──────────────────────────────────────
        // NOTE: IyQty10/11/12 — AS/400 may use hex names IYQTYA, IYQTYB, IYQTYC.
        // Using IYQTY10/IYQTY11/IYQTY12 as default; verify against actual DHW_DATABASE schema.
        mb.Entity<DhwInvent>(e =>
        {
            e.ToTable("INVENT");
            e.HasKey(x => new { x.IyWhse, x.IyItem });
            e.Property(x => x.IyWhse).HasColumnName("IYWHSE").HasMaxLength(6);
            e.Property(x => x.IyItem).HasColumnName("IYITEM").HasMaxLength(20);
            e.Property(x => x.IyOnHa).HasColumnName("IYONHA");
            e.Property(x => x.IyInBo).HasColumnName("IYINBO");
            e.Property(x => x.IyAvCs).HasColumnName("IYAVCS");
            e.Property(x => x.IyQty1).HasColumnName("IYQTY1");
            e.Property(x => x.IyQty2).HasColumnName("IYQTY2");
            e.Property(x => x.IyQty3).HasColumnName("IYQTY3");
            e.Property(x => x.IyQty4).HasColumnName("IYQTY4");
            e.Property(x => x.IyQty5).HasColumnName("IYQTY5");
            e.Property(x => x.IyQty6).HasColumnName("IYQTY6");
            e.Property(x => x.IyQty7).HasColumnName("IYQTY7");
            e.Property(x => x.IyQty8).HasColumnName("IYQTY8");
            e.Property(x => x.IyQty9).HasColumnName("IYQTY9");
            e.Property(x => x.IyQty10).HasColumnName("IYQTY10");
            e.Property(x => x.IyQty11).HasColumnName("IYQTY11");
            e.Property(x => x.IyQty12).HasColumnName("IYQTY12");
            e.Property(x => x.IyStat).HasColumnName("IYSTAT").HasMaxLength(2);
            e.Property(x => x.IyBrvr).HasColumnName("IYBRVR").HasMaxLength(6);
            e.Property(x => x.IyClas).HasColumnName("IYCLAS").HasMaxLength(6);
            e.Property(x => x.IySub).HasColumnName("IYSUB#").HasMaxLength(6);
        });
    }
}

// ── DHW Entity classes ─────────────────────────────────────────────────────────

public class DhwSupplier
{
    public long     Identity     { get; set; }
    public string?  Supplier     { get; set; }
    public string?  SupplierName { get; set; }
    public decimal? ApVendor     { get; set; }
    public string?  DeleteFlag   { get; set; }
}

public class DhwCountry
{
    public long    Identity    { get; set; }
    public string? IsoAlpha2  { get; set; }
    public string? IsoAlpha3  { get; set; }
    public decimal? IsoNumeric { get; set; }
    public string? Description { get; set; }
}

public class DhwPoHeader
{
    public string   PhWhse   { get; set; } = string.Empty;
    public string   PhPoNo   { get; set; } = string.Empty;
    public string?  PhBorw   { get; set; }
    public string?  PhBrvr   { get; set; }
    public decimal? PhOrdt   { get; set; }  // numeric(8,0) — YYYYMMDD
    public decimal? PhShdt   { get; set; }  // numeric(8,0) — YYYYMMDD
    public decimal? PhArdt   { get; set; }  // numeric(8,0) — YYYYMMDD
    public string?  PhConNo  { get; set; }
    public decimal? PhOqt    { get; set; }
    public decimal? PhWeig   { get; set; }
    public decimal? PhLtrs   { get; set; }
    public decimal? PhTotAmt { get; set; }
    public string?  PhOvrNo  { get; set; }
    public decimal? PhLine   { get; set; }  // numeric(3,0)
    public string?  PhStat   { get; set; }
    public string?  PhShip   { get; set; }  // Shipper/Freight Forwarder ID → SHIPPER_MASTER
}

public class DhwShipperMaster
{
    public string   ShipperId        { get; set; } = string.Empty;
    public string?  ShipperName      { get; set; }
    public string?  Address1         { get; set; }
    public string?  Address2         { get; set; }
    public long?    IdCity           { get; set; }
    public decimal? TelephoneNumber  { get; set; }
    public decimal? FaxNumber        { get; set; }
    public string?  DeleteFlag       { get; set; }
    public DateTime? CreateTimestamp { get; set; }
}

public class DhwPoDetail
{
    public string   PdWhse   { get; set; } = string.Empty;
    public string   PdPoNo   { get; set; } = string.Empty;
    public decimal  PdLine   { get; set; }  // numeric
    public string?  PdItem   { get; set; }
    public decimal? PdOqty   { get; set; }
    public decimal? PdRqty   { get; set; }
    public decimal? PdWeig   { get; set; }
    public decimal? PdLtrs   { get; set; }
    public decimal? PdCstAmt { get; set; }
    public decimal? PdUnit   { get; set; }  // numeric
    public string?  PdSitem  { get; set; }
    public string?  PdBsw    { get; set; }
    public string?  PdClas   { get; set; }
    public string?  PdBrvr   { get; set; }
    public string?  PdBran   { get; set; }
    public string?  PdStat   { get; set; }
    public decimal? PdCdAt   { get; set; }  // numeric(8,0) – commitment date YYYYMMDD
    public decimal? PdRdAt   { get; set; }  // numeric(8,0) – receipt date YYYYMMDD
}

public class DhwItemFob
{
    public string   ItCode          { get; set; } = string.Empty;
    public decimal? ItPurchasePrice { get; set; }
    public string?  ItCommodity     { get; set; }
}

public class DhwSystemTable
{
    public string   CompCode          { get; set; } = string.Empty;
    public string?  CompName          { get; set; }
    public string?  CompCurrLocal     { get; set; }
    public string?  CompCurrUsd       { get; set; }
    public decimal? CompRateUsd       { get; set; }
    public decimal? CompInsurance     { get; set; }
    public decimal? CompTransport     { get; set; }
    public decimal? CompUnloading     { get; set; }
    public decimal? CompLocalHandling { get; set; }
    public string?  CompFwCode        { get; set; }
    public string?  CompFwName        { get; set; }
    public string?  CompFwCurr        { get; set; }
    public decimal? CompOzFactor      { get; set; }
    public decimal? CompLiterMultiplier { get; set; }
}

// ── Route Assignment VIP entity classes ────────────────────────────────────────
// NOTE: VIP column names (AS/400 format) - verify against actual DHW_DATABASE schema

/// <summary>DAILYT – invoice/sales fact table from VIP (DHW_DATABASE).</summary>
public class DhwDailyT
{
    public string   DlyWhse         { get; set; } = string.Empty;
    public int      DlyInDt         { get; set; }   // YYYYMMDD integer
    public string   DlyInNo         { get; set; } = string.Empty;
    public int      DlyLine         { get; set; }
    public string?  DlyAcct         { get; set; }
    public string?  DlyName         { get; set; }
    public string?  DlyItem         { get; set; }
    public string?  DlyIdsc         { get; set; }
    public decimal? DlyCqty         { get; set; }
    public decimal? DlyBqty         { get; set; }
    public string?  DlyUom          { get; set; }
    public int?     DlyUpcs         { get; set; }
    public decimal? DlyUpri         { get; set; }
    public decimal? DlyDisc         { get; set; }
    public decimal? DlyExtp         { get; set; }
    public decimal? DlyUcst         { get; set; }
    public decimal? DlyCcst         { get; set; }
    public string?  DlyRoute        { get; set; }
    public string?  DlyLoad         { get; set; }
    public string?  DlyDriver       { get; set; }
    public string?  DlySalesRepName { get; set; }
    public string?  DlySalesRepNo   { get; set; }
}

/// <summary>BRATTT – customer dimension table from VIP (DHW_DATABASE).</summary>
public class DhwBrattT
{
    public string  BrAcct               { get; set; } = string.Empty;
    public string? BrName               { get; set; }
    public string? BrAddress            { get; set; }
    public string? BrStatus             { get; set; }
    public string? BrUserField4         { get; set; }
    public string? BrUserField4Desc     { get; set; }
    public string? BrRoute              { get; set; }
    public string? BrRouteDesc          { get; set; }
    public string? BrSubClass           { get; set; }
    public string? BrSubClassDesc       { get; set; }
    public string? BrSalesmanCode       { get; set; }
    public string? BrSalesmanName       { get; set; }
    public string? BrDriverCode         { get; set; }
    public string? BrDriverName         { get; set; }
    public string? BrMerchandiser       { get; set; }
    public string? BrOnOffPremise       { get; set; }
    public string? BrIndustryVol2       { get; set; }
    public string? BrIndustryVol2Desc   { get; set; }
    public string? BrRetailClass        { get; set; }
    public string? BrRetailClassDesc    { get; set; }
    public string? BrRetailerSalesman   { get; set; }
    public string? BrVisitDaySalesman   { get; set; }
    public string? BrDeliveryDayDriver  { get; set; }
    public string? BrVisitTimeSalesman  { get; set; }
}

/// <summary>INVENT – inventory on-hand and rolling-sales table from VIP (DHW_DATABASE). READ ONLY.</summary>
/// <remarks>
/// IyQty1..IyQty12 are the 12 most recent monthly sales quantities (IyQty1 = most recent month).
/// Column naming note: AS/400 may use hex names IYQTYA/IYQTYB/IYQTYC for months 10/11/12.
/// Verify against actual DHW_DATABASE schema if queries return null for those columns.
/// </remarks>
public class DhwInvent
{
    public string   IyWhse  { get; set; } = string.Empty;
    public string   IyItem  { get; set; } = string.Empty;
    public decimal? IyOnHa  { get; set; }
    public decimal? IyInBo  { get; set; }
    public decimal? IyAvCs  { get; set; }
    public decimal? IyQty1  { get; set; }
    public decimal? IyQty2  { get; set; }
    public decimal? IyQty3  { get; set; }
    public decimal? IyQty4  { get; set; }
    public decimal? IyQty5  { get; set; }
    public decimal? IyQty6  { get; set; }
    public decimal? IyQty7  { get; set; }
    public decimal? IyQty8  { get; set; }
    public decimal? IyQty9  { get; set; }
    public decimal? IyQty10 { get; set; }
    public decimal? IyQty11 { get; set; }
    public decimal? IyQty12 { get; set; }
    public string?  IyStat  { get; set; }
    public string?  IyBrvr  { get; set; }
    public string?  IyClas  { get; set; }
    public string?  IySub   { get; set; }
}

/// <summary>ITEMT – product dimension table from VIP (DHW_DATABASE).</summary>
public class DhwItemT
{
    public string  ItItem         { get; set; } = string.Empty;
    public string? ItDesc         { get; set; }
    public string? ItStatus       { get; set; }
    public string? ItSupCode      { get; set; }
    public string? ItBrandCode    { get; set; }
    public string? ItProductClass { get; set; }
    public int?    ItUnitsPerCase { get; set; }
    public int?    ItMlPerBottle  { get; set; }
}
