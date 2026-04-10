using LicoresMaduro.API.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Data;

/// <summary>
/// EF Core DbContext for the Licores Maduro application.
/// Covers all auth tables and all 66 web-managed business tables.
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ── Auth / Security ────────────────────────────────────────────────────────
    public DbSet<LmUser>           LmUsers           => Set<LmUser>();
    public DbSet<LmNotification>   LmNotifications   => Set<LmNotification>();
    public DbSet<LmRole>           LmRoles           => Set<LmRole>();
    public DbSet<LmModule>         LmModules         => Set<LmModule>();
    public DbSet<LmSubmodule>      LmSubmodules      => Set<LmSubmodule>();
    public DbSet<LmRolePermission> LmRolePermissions => Set<LmRolePermission>();
    public DbSet<LmUserPermission> LmUserPermissions => Set<LmUserPermission>();
    public DbSet<LmAuditLog>       LmAuditLogs       => Set<LmAuditLog>();
    public DbSet<LmEmailConfig>    LmEmailConfig     => Set<LmEmailConfig>();
    public DbSet<LmSession>        LmSessions        => Set<LmSession>();
    public DbSet<LmChatMessage>    LmChatMessages    => Set<LmChatMessage>();

    // ── MODULE 1: Tracking ─────────────────────────────────────────────────────
    public DbSet<OrderStatus>                OrderStatuses               => Set<OrderStatus>();
    public DbSet<TrackingContainerType>      TrackingContainerTypes      => Set<TrackingContainerType>();

    // ── MODULE 2: Freight Forwarder ────────────────────────────────────────────
    public DbSet<Currency>                   Currencies                  => Set<Currency>();
    public DbSet<LoadType>                   LoadTypes                   => Set<LoadType>();
    public DbSet<PortOfLoading>              PortsOfLoading              => Set<PortOfLoading>();
    public DbSet<ShippingLine>               ShippingLines               => Set<ShippingLine>();
    public DbSet<ShippingAgent>              ShippingAgents              => Set<ShippingAgent>();
    public DbSet<Route>                      Routes                      => Set<Route>();
    public DbSet<ContainerSpec>              ContainerSpecs              => Set<ContainerSpec>();
    public DbSet<ContainerType>              ContainerTypes              => Set<ContainerType>();
    public DbSet<RouteByShippingAgent>       RoutesByShippingAgents      => Set<RouteByShippingAgent>();
    public DbSet<OceanFreightChargeType>     OceanFreightChargeTypes     => Set<OceanFreightChargeType>();
    public DbSet<InlandFreightChargeType>    InlandFreightChargeTypes    => Set<InlandFreightChargeType>();
    public DbSet<Region>                     Regions                     => Set<Region>();
    public DbSet<LclChargeType>              LclChargeTypes              => Set<LclChargeType>();
    public DbSet<PriceType>                  PriceTypes                  => Set<PriceType>();
    public DbSet<AmountType>                 AmountTypes                 => Set<AmountType>();
    public DbSet<ChargeAction>               ChargeActions               => Set<ChargeAction>();
    public DbSet<ChargeOver>                 ChargeOvers                 => Set<ChargeOver>();
    public DbSet<ChargePer>                  ChargePers                  => Set<ChargePer>();

    // ── Freight Forwarder - Forwarders & Quotes ────────────────────────────────
    public DbSet<FreightForwarder>             FreightForwarders          => Set<FreightForwarder>();
    public DbSet<OceanFreightHeader>           OceanFreightHeaders        => Set<OceanFreightHeader>();
    public DbSet<OceanFreightPort>             OceanFreightPorts          => Set<OceanFreightPort>();
    public DbSet<OceanFreightPortSLine>        OceanFreightPortSLines     => Set<OceanFreightPortSLine>();
    public DbSet<OceanFreightPortSLineCharge>  OceanFreightCharges        => Set<OceanFreightPortSLineCharge>();
    public DbSet<OceanFreightPortCharge>       OceanFreightPortCharges    => Set<OceanFreightPortCharge>();
    public DbSet<InlandFreightHeader>          InlandFreightHeaders       => Set<InlandFreightHeader>();
    public DbSet<InlandFreightRegion>          InlandFreightRegions       => Set<InlandFreightRegion>();
    public DbSet<InlandFreightRegionType>      InlandFreightRegionTypes   => Set<InlandFreightRegionType>();
    public DbSet<InlandFreightRegionTypeDet>   InlandFreightRegionTypeDets => Set<InlandFreightRegionTypeDet>();
    public DbSet<LclHeader>                    LclHeaders                 => Set<LclHeader>();
    public DbSet<LclPort>                      LclPorts                   => Set<LclPort>();
    public DbSet<LclPortType>                  LclPortTypes               => Set<LclPortType>();
    public DbSet<LclPortTypeDet>               LclPortTypeDets            => Set<LclPortTypeDet>();
    public DbSet<InlandAdditionalCharge>       InlandAdditionalCharges    => Set<InlandAdditionalCharge>();

    // ── Freight Forwarder - Applied Quotes ─────────────────────────────────────
    public DbSet<FreightQuoteHeader>          FreightQuoteHeaders         => Set<FreightQuoteHeader>();
    public DbSet<FreightQuoteOceanCharge>     FreightQuoteOceanCharges    => Set<FreightQuoteOceanCharge>();
    public DbSet<FreightQuoteInlRegion>       FreightQuoteInlRegions      => Set<FreightQuoteInlRegion>();
    public DbSet<FreightQuoteInlRegionType>   FreightQuoteInlRegionTypes  => Set<FreightQuoteInlRegionType>();
    public DbSet<FreightQuoteInlRegionTypeDet> FreightQuoteInlRegionTypeDets => Set<FreightQuoteInlRegionTypeDet>();
    public DbSet<FreightQuoteInlPortAdd>      FreightQuoteInlPortAdds     => Set<FreightQuoteInlPortAdd>();
    public DbSet<FreightQuoteLclPort>        FreightQuoteLclPorts       => Set<FreightQuoteLclPort>();
    public DbSet<FreightQuoteLclPortType>    FreightQuoteLclPortTypes    => Set<FreightQuoteLclPortType>();
    public DbSet<FreightQuoteLclPortTypeDet> FreightQuoteLclPortTypeDets => Set<FreightQuoteLclPortTypeDet>();
    public DbSet<FreightQuoteOceanPort>     FreightQuoteOceanPorts      => Set<FreightQuoteOceanPort>();
    public DbSet<FreightQuoteOceanPortSLine> FreightQuoteOceanPortSLines => Set<FreightQuoteOceanPortSLine>();

    // ── MODULE 6: Activity Request ─────────────────────────────────────────────
    public DbSet<ActivityType>               ActivityTypes               => Set<ActivityType>();
    public DbSet<BudgetActivity>             BudgetActivities            => Set<BudgetActivity>();
    public DbSet<CatAddSpec>                 CatAddSpecs                 => Set<CatAddSpec>();
    public DbSet<CatApparelType>             CatApparelTypes             => Set<CatApparelType>();
    public DbSet<CatBagSpec>                 CatBagSpecs                 => Set<CatBagSpec>();
    public DbSet<CatBottle>                  CatBottles                  => Set<CatBottle>();
    public DbSet<CatBrandSpecific>           CatBrandSpecifics           => Set<CatBrandSpecific>();
    public DbSet<CatClothingType>            CatClothingTypes            => Set<CatClothingType>();
    public DbSet<CatColor>                   CatColors                   => Set<CatColor>();
    public DbSet<CatContent>                 CatContents                 => Set<CatContent>();
    public DbSet<CatCoolerCapacity>          CatCoolerCapacities         => Set<CatCoolerCapacity>();
    public DbSet<CatCoolerModel>             CatCoolerModels             => Set<CatCoolerModel>();
    public DbSet<CatCoolerType>              CatCoolerTypes              => Set<CatCoolerType>();
    public DbSet<CatFileName>                CatFileNames                => Set<CatFileName>();
    public DbSet<CatGender>                  CatGenders                  => Set<CatGender>();
    public DbSet<CatGlassServing>            CatGlassServings            => Set<CatGlassServing>();
    public DbSet<CatInsurrance>              CatInsurrances              => Set<CatInsurrance>();
    public DbSet<CatLed>                     CatLeds                     => Set<CatLed>();
    public DbSet<CatMaintMonth>              CatMaintMonths              => Set<CatMaintMonth>();
    public DbSet<CatMaterial>                CatMaterials                => Set<CatMaterial>();
    public DbSet<CatShape>                   CatShapes                   => Set<CatShape>();
    public DbSet<CatSize>                    CatSizes                    => Set<CatSize>();
    public DbSet<CatVapType>                 CatVapTypes                 => Set<CatVapType>();
    public DbSet<CustomerNonClient>          CustomerNonClients          => Set<CustomerNonClient>();
    public DbSet<CustomerSalesGroup>         CustomerSalesGroups         => Set<CustomerSalesGroup>();
    public DbSet<CustomerSegmentInfo>        CustomerSegmentInfos        => Set<CustomerSegmentInfo>();
    public DbSet<CustomerTargetGroup>        CustomerTargetGroups        => Set<CustomerTargetGroup>();
    public DbSet<DenialReason>               DenialReasons               => Set<DenialReason>();
    public DbSet<EntertainmentType>          EntertainmentTypes          => Set<EntertainmentType>();
    public DbSet<FacilitatorInfo>            FacilitatorInfos            => Set<FacilitatorInfo>();
    public DbSet<FiscalYear>                 FiscalYears                 => Set<FiscalYear>();
    public DbSet<LicoresGroup>               LicoresGroups               => Set<LicoresGroup>();
    public DbSet<LocationInfo>               LocationInfos               => Set<LocationInfo>();
    public DbSet<PosCategory>                PosCategories               => Set<PosCategory>();
    public DbSet<PosLendGive>                PosLendGives                => Set<PosLendGive>();
    public DbSet<PosMaterialsStatus>         PosMaterialsStatuses        => Set<PosMaterialsStatus>();
    public DbSet<PosMaterial>               PosMaterials                => Set<PosMaterial>();
    public DbSet<SponsoringType>             SponsoringTypes             => Set<SponsoringType>();
    public DbSet<StatusCode>                 StatusCodes                 => Set<StatusCode>();
    public DbSet<MarketingCalendar>          MarketingCalendars          => Set<MarketingCalendar>();
    public DbSet<ActivityRequestHeader>      ActivityRequests            => Set<ActivityRequestHeader>();
    public DbSet<ActivityRqBrand>            ActivityRqBrands            => Set<ActivityRqBrand>();
    public DbSet<ActivityRqProduct>          ActivityRqProducts          => Set<ActivityRqProduct>();
    public DbSet<ActivityRqCash>             ActivityRqCashes            => Set<ActivityRqCash>();
    public DbSet<ActivityRqPrint>            ActivityRqPrints            => Set<ActivityRqPrint>();
    public DbSet<ActivityRqRadio>            ActivityRqRadios            => Set<ActivityRqRadio>();
    public DbSet<ActivityRqPosMat>           ActivityRqPosMats           => Set<ActivityRqPosMat>();
    public DbSet<ActivityRqPromotion>        ActivityRqPromotions        => Set<ActivityRqPromotion>();
    public DbSet<ActivityRqOther>            ActivityRqOthers            => Set<ActivityRqOther>();
    public DbSet<PosLendOut>                 PosLendOuts                 => Set<PosLendOut>();
    public DbSet<PosLendOutItem>             PosLendOutItems             => Set<PosLendOutItem>();

    // ── MODULE 7: Aankoopbon ────────────────────────────────────────────────────
    public DbSet<AbOrderHeader>              AbOrderHeaders              => Set<AbOrderHeader>();
    public DbSet<AbOrderDetail>              AbOrderDetails              => Set<AbOrderDetail>();
    public DbSet<AbProduct>                  AbProducts                  => Set<AbProduct>();
    public DbSet<Department>                 Departments                 => Set<Department>();
    public DbSet<Eenheid>                    Eenheden                    => Set<Eenheid>();
    public DbSet<Receiver>                   Receivers                   => Set<Receiver>();
    public DbSet<Requestor>                  Requestors                  => Set<Requestor>();
    public DbSet<RequestorVendor>            RequestorVendors            => Set<RequestorVendor>();
    public DbSet<CostType>                   CostTypes                   => Set<CostType>();
    public DbSet<VehicleType>                VehicleTypes                => Set<VehicleType>();
    public DbSet<Vehicle>                    Vehicles                    => Set<Vehicle>();
    public DbSet<Vendor>                     Vendors                     => Set<Vendor>();

    // ── MODULE 3: Cost Calculation ─────────────────────────────────────────────
    public DbSet<CcCalcHeader>         CcCalcHeaders         => Set<CcCalcHeader>();
    public DbSet<CcCalcPoHead>         CcCalcPoHeads         => Set<CcCalcPoHead>();
    public DbSet<CcCalcPoDetail>       CcCalcPoDetails       => Set<CcCalcPoDetail>();
    public DbSet<CcTariffItem>          CcTariffItems          => Set<CcTariffItem>();
    public DbSet<CcGoodsClassification> CcGoodsClassifications => Set<CcGoodsClassification>();
    public DbSet<CcItemWeight>          CcItemWeights          => Set<CcItemWeight>();
    public DbSet<CcAllowedMargin>       CcAllowedMargins       => Set<CcAllowedMargin>();
    public DbSet<CcInlandTariff>        CcInlandTariffs        => Set<CcInlandTariff>();
    public DbSet<CcShipCharge>          CcShipCharges          => Set<CcShipCharge>();

    // ── MODULE 1: Tracking (Orders) ────────────────────────────────────────────
    public DbSet<TrackingOrder>         TrackingOrders          => Set<TrackingOrder>();
    public DbSet<TrackingStatusHistory> TrackingStatusHistories => Set<TrackingStatusHistory>();

    // ── MODULE 4: Route Assignment ─────────────────────────────────────────────
    public DbSet<RouteCustomerExt> RouteCustomerExts => Set<RouteCustomerExt>();
    public DbSet<RouteProductExt>  RouteProductExts  => Set<RouteProductExt>();
    public DbSet<RouteBudget>      RouteBudgets      => Set<RouteBudget>();

    // ── SYSTEM: Company Settings & Module Approvers ───────────────────────────
    public DbSet<CompanySettings>      CompanySettings      => Set<CompanySettings>();
    public DbSet<ModuleApproverEmail>  ModuleApproverEmails => Set<ModuleApproverEmail>();

    // ── MODULE 5: Stock Analysis ───────────────────────────────────────────────
    public DbSet<StockIdealMonths>      StockIdealMonths      => Set<StockIdealMonths>();
    public DbSet<StockVendorConstraints> StockVendorConstraints => Set<StockVendorConstraints>();
    public DbSet<StockSalesBudget>      StockSalesBudgets     => Set<StockSalesBudget>();
    public DbSet<StockAnalysisResult>   StockAnalysisResults  => Set<StockAnalysisResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Auth tables ────────────────────────────────────────────────────────
        modelBuilder.Entity<LmRole>(e =>
        {
            e.ToTable("LM_Roles");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<LmUser>(e =>
        {
            e.ToTable("LM_Users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.AvatarUrl).HasMaxLength(300);
            e.HasOne(x => x.Role)
             .WithMany(r => r.Users)
             .HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<LmModule>(e =>
        {
            e.ToTable("LM_Modules");
            e.HasKey(x => x.ModuleId);
        });

        modelBuilder.Entity<LmSubmodule>(e =>
        {
            e.ToTable("LM_Submodules");
            e.HasKey(x => x.SubmoduleId);
            e.HasOne(x => x.Module)
             .WithMany(m => m.Submodules)
             .HasForeignKey(x => x.ModuleId);
        });

        modelBuilder.Entity<LmRolePermission>(e =>
        {
            e.ToTable("LM_RolePermissions");
            e.HasKey(x => x.PermissionId);
            e.HasOne(x => x.Role)
             .WithMany(r => r.Permissions)
             .HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Submodule)
             .WithMany(s => s.Permissions)
             .HasForeignKey(x => x.SubmoduleId);
        });

        modelBuilder.Entity<LmUserPermission>(e =>
        {
            e.ToTable("LM_UserPermissions");
            e.HasKey(x => x.PermissionId);
            e.Property(x => x.PermissionId).HasColumnName("UP_Id");
            e.Property(x => x.UserId).HasColumnName("UP_UserId");
            e.Property(x => x.SubmoduleId).HasColumnName("UP_SubmoduleId");
            e.Property(x => x.CanAccess).HasColumnName("UP_CanAccess");
            e.Property(x => x.CanRead).HasColumnName("UP_CanRead");
            e.Property(x => x.CanWrite).HasColumnName("UP_CanWrite");
            e.Property(x => x.CanEdit).HasColumnName("UP_CanEdit");
            e.Property(x => x.CanDelete).HasColumnName("UP_CanDelete");
            e.Property(x => x.CanApprove).HasColumnName("UP_CanApprove");
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Submodule)
             .WithMany()
             .HasForeignKey(x => x.SubmoduleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LmAuditLog>(e =>
        {
            e.ToTable("LM_AuditLog");
            e.HasKey(x => x.LogId);
        });

        modelBuilder.Entity<LmChatMessage>(e =>
        {
            e.ToTable("LM_ChatMessages");
            e.HasKey(x => x.MessageId);
            e.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            e.HasOne(x => x.FromUser).WithMany().HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToUser).WithMany().HasForeignKey(x => x.ToUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LmSession>(e =>
        {
            e.ToTable("LM_Sessions");
            e.HasKey(x => x.SessionId);
            e.Property(x => x.SessionKey).HasMaxLength(50).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.UserAgent).HasMaxLength(300);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<LmNotification>(e =>
        {
            e.ToTable("LM_NOTIFICATIONS");
            e.HasKey(x => x.NtfId);
            e.Property(x => x.NtfId).HasColumnName("NTF_Id").ValueGeneratedOnAdd();
            e.Property(x => x.NtfUserId).HasColumnName("NTF_UserId");
            e.Property(x => x.NtfTitle).HasColumnName("NTF_Title").HasMaxLength(100).IsRequired();
            e.Property(x => x.NtfMessage).HasColumnName("NTF_Message").HasMaxLength(500).IsRequired();
            e.Property(x => x.NtfType).HasColumnName("NTF_Type").HasMaxLength(20).HasDefaultValue("INFO");
            e.Property(x => x.NtfIsRead).HasColumnName("NTF_IsRead").HasDefaultValue(false);
            e.Property(x => x.NtfUrl).HasColumnName("NTF_Url").HasMaxLength(300);
            e.Property(x => x.NtfRefId).HasColumnName("NTF_RefId");
            e.Property(x => x.NtfRefType).HasColumnName("NTF_RefType").HasMaxLength(30);
            e.Property(x => x.CreatedAt).HasColumnName("Created_At").HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<LmEmailConfig>(e =>
        {
            e.ToTable("LM_EmailConfig");
            e.HasKey(x => x.ConfigId);
            e.Property(x => x.SmtpHost).HasMaxLength(200).IsRequired();
            e.Property(x => x.SenderName).HasMaxLength(100).IsRequired();
            e.Property(x => x.SenderEmail).HasMaxLength(200).IsRequired();
            e.Property(x => x.SenderPassword).HasMaxLength(500).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(100);
        });

        // ── Tracking ───────────────────────────────────────────────────────────
        modelBuilder.Entity<OrderStatus>(e =>
        {
            e.ToTable("ORDER_STATUS");
            e.HasKey(x => x.OsId);
            e.Property(x => x.OsId).HasColumnName("OS_Id");
            e.Property(x => x.OsCode).HasColumnName("OS_Code").HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.OsCode).IsUnique();
            e.Property(x => x.OsDescription).HasColumnName("OS_DESCRIPTION").HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<TrackingContainerType>(e =>
        {
            e.ToTable("TRACKING_CONTAINER_TYPES");
            e.HasKey(x => x.TctId);
            e.Property(x => x.TctId).HasColumnName("TCT_Id");
            e.Property(x => x.TctCode).HasColumnName("TCT_Code").HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.TctCode).IsUnique();
            e.Property(x => x.TctDescription).HasColumnName("TCT_Description").HasMaxLength(100).IsRequired();
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        // ── Freight Forwarder ──────────────────────────────────────────────────
        modelBuilder.Entity<Currency>(e =>
        {
            e.ToTable("CURRENCIES");
            e.HasKey(x => x.CurId);
            e.Property(x => x.CurId).HasColumnName("CUR_Id");
            e.Property(x => x.CurCode).HasColumnName("CUR_CODE").HasMaxLength(3);
            e.Property(x => x.CurDescription).HasColumnName("CUR_DESCRIPTION").HasMaxLength(30);
            e.Property(x => x.CurBnkPurchaseRate).HasColumnName("CUR_BNK_PURCHASE_RATE");
            e.Property(x => x.CurCustomsRate).HasColumnName("CUR_CUSTOMS_RATE");
        });

        modelBuilder.Entity<LoadType>(e => { e.ToTable("LOADTYPES"); e.HasKey(x => x.LtId); e.Property(x => x.LtId).HasColumnName("LT_Id"); e.Property(x => x.LtCode).HasColumnName("LT_CODE").HasMaxLength(6); e.Property(x => x.LtDescription).HasColumnName("LT_DESCRIPTION").HasMaxLength(25); });
        modelBuilder.Entity<PortOfLoading>(e => { e.ToTable("PORT_OF_LOADING"); e.HasKey(x => x.PlId); e.Property(x => x.PlId).HasColumnName("PL_Id"); e.Property(x => x.PlCode).HasColumnName("PL_CODE").HasMaxLength(10); e.Property(x => x.PlName).HasColumnName("PL_NAME").HasMaxLength(30); e.Property(x => x.PlCountry).HasColumnName("PL_COUNTRY").HasMaxLength(3); });
        modelBuilder.Entity<ShippingLine>(e => { e.ToTable("SHIPPING_LINES"); e.HasKey(x => x.SlId); e.Property(x => x.SlId).HasColumnName("SL_Id"); e.Property(x => x.SlCode).HasColumnName("SL_CODE").HasMaxLength(10); e.Property(x => x.SlName).HasColumnName("SL_NAME").HasMaxLength(30); });
        modelBuilder.Entity<ShippingAgent>(e => { e.ToTable("SHIPPING_AGENT"); e.HasKey(x => x.SaId); e.Property(x => x.SaId).HasColumnName("SA_Id"); e.Property(x => x.SaCode).HasColumnName("SA_CODE").HasMaxLength(10); e.Property(x => x.SaName).HasColumnName("SA_NAME").HasMaxLength(30); });
        modelBuilder.Entity<Route>(e => { e.ToTable("ROUTES"); e.HasKey(x => x.RouId); e.Property(x => x.RouId).HasColumnName("ROU_Id"); e.Property(x => x.RouCode).HasColumnName("ROU_CODE").HasMaxLength(15); e.Property(x => x.RouDescription).HasColumnName("ROU_DESCRIPTION").HasMaxLength(30); });
        modelBuilder.Entity<ContainerSpec>(e => { e.ToTable("CONTAINER_SPECS"); e.HasKey(x => x.CsId); e.Property(x => x.CsId).HasColumnName("CS_Id"); e.Property(x => x.CsCode).HasColumnName("CS_CODE").HasMaxLength(6); e.Property(x => x.CsDescription).HasColumnName("CS_DESCRIPTION").HasMaxLength(25); });
        modelBuilder.Entity<ContainerType>(e => { e.ToTable("CONTAINER_TYPES"); e.HasKey(x => x.CtId); e.Property(x => x.CtId).HasColumnName("CT_Id"); e.Property(x => x.CtCode).HasColumnName("CT_CODE").HasMaxLength(6); e.Property(x => x.CtDescription).HasColumnName("CT_DESCRIPTION").HasMaxLength(50); e.Property(x => x.CtContainerSpecs).HasColumnName("CT_CONTAINER_SPECS").HasMaxLength(6); e.Property(x => x.CtCases).HasColumnName("CT_Cases"); e.Property(x => x.CtWghtKilogram).HasColumnName("CT_WGHT_Kilogram"); });
        modelBuilder.Entity<RouteByShippingAgent>(e => { e.ToTable("ROUTES_BY_SHIPPING_AGENTS"); e.HasKey(x => x.RsaId); e.Property(x => x.RsaId).HasColumnName("RSA_Id"); e.Property(x => x.RsaPort).HasColumnName("RSA_PORT").HasMaxLength(10); e.Property(x => x.RsaShippingAgent).HasColumnName("RSA_SHIPPING_AGENT").HasMaxLength(25); e.Property(x => x.RsaRoute).HasColumnName("RSA_ROUTE").HasMaxLength(10); e.Property(x => x.RsaDays).HasColumnName("RSA_DAYS"); });
        modelBuilder.Entity<OceanFreightChargeType>(e => { e.ToTable("OCEAN_FREIGHT_CHARGE_TYPES"); e.HasKey(x => x.OfctId); e.Property(x => x.OfctId).HasColumnName("OFCT_Id"); e.Property(x => x.OfctCode).HasColumnName("OFCT_CODE").HasMaxLength(6); e.Property(x => x.OfctDescription).HasColumnName("OFCT_DESCRIPTION").HasMaxLength(25); });
        modelBuilder.Entity<InlandFreightChargeType>(e => { e.ToTable("INLAND_FREIGHT_CHARGE_TYPES"); e.HasKey(x => x.IfctId); e.Property(x => x.IfctId).HasColumnName("IFCT_Id"); e.Property(x => x.IfctCode).HasColumnName("IFCT_CODE").HasMaxLength(6); e.Property(x => x.IfctDescription).HasColumnName("IFCT_DESCRIPTION").HasMaxLength(25); });
        modelBuilder.Entity<Region>(e => { e.ToTable("REGIONS"); e.HasKey(x => x.RegId); e.Property(x => x.RegId).HasColumnName("REG_Id"); e.Property(x => x.RegCode).HasColumnName("REG_Code").HasMaxLength(50); e.Property(x => x.RegName).HasColumnName("REG_Name").HasMaxLength(50); e.Property(x => x.RegCountry).HasColumnName("REG_Country").HasMaxLength(100); });
        modelBuilder.Entity<LclChargeType>(e => { e.ToTable("LCL_CHARGE_TYPES"); e.HasKey(x => x.LctId); e.Property(x => x.LctId).HasColumnName("LCT_Id"); e.Property(x => x.LctCode).HasColumnName("LCT_CODE").HasMaxLength(6); e.Property(x => x.LctDescription).HasColumnName("LCT_DESCRIPTION").HasMaxLength(25); });
        modelBuilder.Entity<PriceType>(e => { e.ToTable("PRICE_TYPE"); e.HasKey(x => x.PtId); e.Property(x => x.PtId).HasColumnName("PT_Id"); e.Property(x => x.PtCode).HasColumnName("PT_CODE").HasMaxLength(6); e.Property(x => x.PtDescription).HasColumnName("PT_DESCRIPTION").HasMaxLength(25); });
        modelBuilder.Entity<AmountType>(e => { e.ToTable("AMOUNT_TYPE"); e.HasKey(x => x.AtId); e.Property(x => x.AtId).HasColumnName("AT_Id"); e.Property(x => x.AtCode).HasColumnName("AT_CODE").HasMaxLength(1); e.Property(x => x.AtDescription).HasColumnName("AT_DESCRIPTION").HasMaxLength(25); });
        modelBuilder.Entity<ChargeAction>(e => { e.ToTable("CHARGE_ACTION"); e.HasKey(x => x.CaId); e.Property(x => x.CaId).HasColumnName("CA_Id"); e.Property(x => x.CaCode).HasColumnName("CA_CODE").HasMaxLength(6); e.Property(x => x.CaDescription).HasColumnName("CA_DESCRIPTION").HasMaxLength(25); });
        modelBuilder.Entity<ChargeOver>(e => { e.ToTable("CHARGE_OVER"); e.HasKey(x => x.CoId); e.Property(x => x.CoId).HasColumnName("CO_Id"); e.Property(x => x.CoCode).HasColumnName("CO_CODE").HasMaxLength(6); e.Property(x => x.CoDescription).HasColumnName("CO_DESCRIPTION").HasMaxLength(25); });
        modelBuilder.Entity<ChargePer>(e => { e.ToTable("CHARGE_PER"); e.HasKey(x => x.CpId); e.Property(x => x.CpId).HasColumnName("CP_Id"); e.Property(x => x.CpCode).HasColumnName("CP_CODE").HasMaxLength(6); e.Property(x => x.CpDescription).HasColumnName("CP_DESCRIPTION").HasMaxLength(25); });

        // ── Freight Forwarders & Quotes ────────────────────────────────────────
        modelBuilder.Entity<FreightForwarder>(e =>
        {
            e.ToTable("FREIGHT_FORWARDERS");
            e.HasKey(x => x.FfId);
            e.Property(x => x.FfId).HasColumnName("FF_Id");
            e.Property(x => x.FfCode).HasColumnName("FF_CODE").HasMaxLength(10);
            e.Property(x => x.FfName).HasColumnName("FF_NAME").HasMaxLength(50);
            e.Property(x => x.FfAddress1).HasColumnName("FF_ADDRESS_1").HasMaxLength(100);
            e.Property(x => x.FfAddress2).HasColumnName("FF_ADDRESS_2").HasMaxLength(100);
            e.Property(x => x.FfCity).HasColumnName("FF_CITY").HasMaxLength(30);
            e.Property(x => x.FfCountry).HasColumnName("FF_COUNTRY").HasMaxLength(3);
            e.Property(x => x.FfPhone1).HasColumnName("FF_PHONE_1").HasMaxLength(20);
            e.Property(x => x.FfPhone2).HasColumnName("FF_PHONE_2").HasMaxLength(20);
            e.Property(x => x.FfEmail).HasColumnName("FF_EMAIL").HasMaxLength(100);
            e.Property(x => x.FfContact).HasColumnName("FF_CONTACT").HasMaxLength(50);
            e.Property(x => x.FfCurrency).HasColumnName("FF_CURRENCY").HasMaxLength(3);
        });

        modelBuilder.Entity<OceanFreightHeader>(e =>
        {
            e.ToTable("FF_OCEAN_FREIGHT_HEADER");
            e.HasKey(x => x.FqohId);
            e.Property(x => x.FqohId).HasColumnName("FQOH_Id");
            e.Property(x => x.FqohForwarder).HasColumnName("FQOH_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqohQuoteNumber).HasColumnName("FQOH_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqohStartDate).HasColumnName("FQOH_START_DATE");
            e.Property(x => x.FqohEndDate).HasColumnName("FQOH_END_DATE");
            e.Property(x => x.FqohRemarks).HasColumnName("FQOH_REMARKS").HasMaxLength(200);
            e.HasMany(x => x.Ports).WithOne(x => x.Header).HasForeignKey(x => x.FqopHeaderId);
        });

        modelBuilder.Entity<OceanFreightPort>(e =>
        {
            e.ToTable("FF_OCEAN_FREIGHT_PORT");
            e.HasKey(x => x.FqopId);
            e.Property(x => x.FqopId).HasColumnName("FQOP_Id");
            e.Property(x => x.FqopHeaderId).HasColumnName("FQOP_Header_Id");
            e.Property(x => x.FqopForwarder).HasColumnName("FQOP_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqopQuoteNumber).HasColumnName("FQOP_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqopPort).HasColumnName("FQOP_PORT").HasMaxLength(10);
            e.Property(x => x.FqopRemarks).HasColumnName("FQOP_REMARKS").HasMaxLength(200);
            e.HasMany(x => x.ShippingLines).WithOne(x => x.Port).HasForeignKey(x => x.FqoplPortId);
            e.HasMany(x => x.PortCharges).WithOne(x => x.Port).HasForeignKey(x => x.FqopcPortId);
        });

        modelBuilder.Entity<OceanFreightPortSLine>(e =>
        {
            e.ToTable("FF_OCEAN_FREIGHT_PORT_SLINE");
            e.HasKey(x => x.FqoplId);
            e.Property(x => x.FqoplId).HasColumnName("FQOPL_Id");
            e.Property(x => x.FqoplPortId).HasColumnName("FQOPL_Port_Id");
            e.Property(x => x.FqoplForwarder).HasColumnName("FQOPL_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqoplQuoteNumber).HasColumnName("FQOPL_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqoplPort).HasColumnName("FQOPL_PORT").HasMaxLength(10);
            e.Property(x => x.FqoplShipLine).HasColumnName("FQOPL_SHIPLINE").HasMaxLength(10);
            e.Property(x => x.FqoplRoute).HasColumnName("FQOPL_ROUTE").HasMaxLength(15);
            e.Property(x => x.FqoplDays).HasColumnName("FQOPL_DAYS");
            e.Property(x => x.FqoplRemarks).HasColumnName("FQOPL_REMARKS").HasMaxLength(200);
            e.HasMany(x => x.Charges).WithOne(x => x.PortSLine).HasForeignKey(x => x.FqoplcSLineId);
        });

        modelBuilder.Entity<OceanFreightPortSLineCharge>(e =>
        {
            e.ToTable("FF_OCEAN_FREIGHT_PORT_SLINE_CHARGES");
            e.HasKey(x => x.FqoplcId);
            e.Property(x => x.FqoplcId).HasColumnName("FQOPLC_Id");
            e.Property(x => x.FqoplcSLineId).HasColumnName("FQOPLC_SLine_Id");
            e.Property(x => x.FqoplcForwarder).HasColumnName("FQOPLC_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqoplcQuoteNumber).HasColumnName("FQOPLC_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqoplcPort).HasColumnName("FQOPLC_PORT").HasMaxLength(10);
            e.Property(x => x.FqoplcShipLine).HasColumnName("FQOPLC_SHIPLINE").HasMaxLength(10);
            e.Property(x => x.FqoplcChargeType).HasColumnName("FQOPLC_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqoplcContainerType).HasColumnName("FQOPLC_CONTAINER_TYPE").HasMaxLength(6);
            e.Property(x => x.FqoplcAmount).HasColumnName("FQOPLC_AMOUNT");
            e.Property(x => x.FqoplcCurrency).HasColumnName("FQOPLC_CURRENCY").HasMaxLength(3);
        });

        modelBuilder.Entity<OceanFreightPortCharge>(e =>
        {
            e.ToTable("FF_OCEAN_FREIGHT_PORT_CHARGES");
            e.HasKey(x => x.FqopcId);
            e.Property(x => x.FqopcId).HasColumnName("FQOPC_Id");
            e.Property(x => x.FqopcPortId).HasColumnName("FQOPC_Port_Id");
            e.Property(x => x.FqopcForwarder).HasColumnName("FQOPC_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqopcQuoteNumber).HasColumnName("FQOPC_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqopcPort).HasColumnName("FQOPC_PORT").HasMaxLength(10);
            e.Property(x => x.FqopcChargeType).HasColumnName("FQOPC_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqopcContainerType).HasColumnName("FQOPC_CONTAINER_TYPE").HasMaxLength(6);
            e.Property(x => x.FqopcAmount).HasColumnName("FQOPC_AMOUNT");
            e.Property(x => x.FqopcCurrency).HasColumnName("FQOPC_CURRENCY").HasMaxLength(3);
        });

        modelBuilder.Entity<InlandFreightHeader>(e =>
        {
            e.ToTable("FF_INLAND_FREIGHT_HEADER");
            e.HasKey(x => x.FqihId);
            e.Property(x => x.FqihId).HasColumnName("FQIH_Id");
            e.Property(x => x.FqihForwarder).HasColumnName("FQIH_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqihQuoteNumber).HasColumnName("FQIH_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqihStartDate).HasColumnName("FQIH_START_DATE");
            e.Property(x => x.FqihEndDate).HasColumnName("FQIH_END_DATE");
            e.Property(x => x.FqihRemarks).HasColumnName("FQIH_REMARKS").HasMaxLength(200);
            e.HasMany(x => x.Regions).WithOne(x => x.Header).HasForeignKey(x => x.FqirHeaderId);
        });

        modelBuilder.Entity<InlandFreightRegion>(e =>
        {
            e.ToTable("FF_INLAND_FREIGHT_REGION");
            e.HasKey(x => x.FqirId);
            e.Property(x => x.FqirId).HasColumnName("FQIR_Id");
            e.Property(x => x.FqirHeaderId).HasColumnName("FQIR_Header_Id");
            e.Property(x => x.FqirForwarder).HasColumnName("FQIR_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqirQuoteNumber).HasColumnName("FQIR_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqirRegion).HasColumnName("FQIR_REGION").HasMaxLength(20);
            e.HasMany(x => x.RegionTypes).WithOne(x => x.Region).HasForeignKey(x => x.FqirtRegionId);
        });

        modelBuilder.Entity<InlandFreightRegionType>(e =>
        {
            e.ToTable("FF_INLAND_FREIGHT_REGION_TYPE");
            e.HasKey(x => x.FqirtId);
            e.Property(x => x.FqirtId).HasColumnName("FQIRT_Id");
            e.Property(x => x.FqirtRegionId).HasColumnName("FQIRT_Region_Id");
            e.Property(x => x.FqirtForwarder).HasColumnName("FQIRT_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqirtQuoteNumber).HasColumnName("FQIRT_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqirtRegion).HasColumnName("FQIRT_REGION").HasMaxLength(20);
            e.Property(x => x.FqirtChargeType).HasColumnName("FQIRT_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqirtAmountMin).HasColumnName("FQIRT_AMOUNT_MIN");
            e.Property(x => x.FqirtAmountMax).HasColumnName("FQIRT_AMOUNT_MAX");
            e.Property(x => x.FqirtCurrency).HasColumnName("FQIRT_CURRENCY").HasMaxLength(3);
            e.HasMany(x => x.Details).WithOne(x => x.RegionType).HasForeignKey(x => x.FqirtdRegionTypeId);
        });

        modelBuilder.Entity<InlandFreightRegionTypeDet>(e =>
        {
            e.ToTable("FF_INLAND_FREIGHT_REGION_TYPE_DET");
            e.HasKey(x => x.FqirtdId);
            e.Property(x => x.FqirtdId).HasColumnName("FQIRTD_Id");
            e.Property(x => x.FqirtdRegionTypeId).HasColumnName("FQIRTD_RegionType_Id");
            e.Property(x => x.FqirtdForwarder).HasColumnName("FQIRTD_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqirtdQuoteNumber).HasColumnName("FQIRTD_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqirtdRegion).HasColumnName("FQIRTD_REGION").HasMaxLength(20);
            e.Property(x => x.FqirtdChargeType).HasColumnName("FQIRTD_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqirtdFrom).HasColumnName("FQIRTD_FROM");
            e.Property(x => x.FqirtdTo).HasColumnName("FQIRTD_TO");
            e.Property(x => x.FqirtdPrice).HasColumnName("FQIRTD_PRICE");
            e.Property(x => x.FqirtdPriceType).HasColumnName("FQIRTD_PRICE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqirtdAmountMin).HasColumnName("FQIRTD_AMOUNT_MIN");
            e.Property(x => x.FqirtdAmountMax).HasColumnName("FQIRTD_AMOUNT_MAX");
        });

        modelBuilder.Entity<LclHeader>(e =>
        {
            e.ToTable("FF_LCL_HEADER");
            e.HasKey(x => x.FqlhId);
            e.Property(x => x.FqlhId).HasColumnName("FQLH_Id");
            e.Property(x => x.FqlhForwarder).HasColumnName("FQLH_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqlhQuoteNumber).HasColumnName("FQLH_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqlhStartDate).HasColumnName("FQLH_START_DATE");
            e.Property(x => x.FqlhEndDate).HasColumnName("FQLH_END_DATE");
            e.Property(x => x.FqlhRemarks).HasColumnName("FQLH_REMARKS").HasMaxLength(200);
            e.HasMany(x => x.Ports).WithOne(x => x.Header).HasForeignKey(x => x.FqlpHeaderId);
        });

        modelBuilder.Entity<LclPort>(e =>
        {
            e.ToTable("FF_LCL_PORT");
            e.HasKey(x => x.FqlpId);
            e.Property(x => x.FqlpId).HasColumnName("FQLP_Id");
            e.Property(x => x.FqlpHeaderId).HasColumnName("FQLP_Header_Id");
            e.Property(x => x.FqlpForwarder).HasColumnName("FQLP_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqlpQuoteNumber).HasColumnName("FQLP_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqlpPort).HasColumnName("FQLP_PORT").HasMaxLength(10);
            e.Property(x => x.FqlpRemarks).HasColumnName("FQLP_REMARKS").HasMaxLength(200);
            e.HasMany(x => x.PortTypes).WithOne(x => x.Port).HasForeignKey(x => x.FqlptPortId);
        });

        modelBuilder.Entity<LclPortType>(e =>
        {
            e.ToTable("FF_LCL_PORT_TYPE");
            e.HasKey(x => x.FqlptId);
            e.Property(x => x.FqlptId).HasColumnName("FQLPT_Id");
            e.Property(x => x.FqlptPortId).HasColumnName("FQLPT_Port_Id");
            e.Property(x => x.FqlptForwarder).HasColumnName("FQLPT_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqlptQuoteNumber).HasColumnName("FQLPT_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqlptPort).HasColumnName("FQLPT_PORT").HasMaxLength(10);
            e.Property(x => x.FqlptChargeType).HasColumnName("FQLPT_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqlptAmountMin).HasColumnName("FQLPT_AMOUNT_MIN");
            e.Property(x => x.FqlptAmountMax).HasColumnName("FQLPT_AMOUNT_MAX");
            e.Property(x => x.FqlptCurrency).HasColumnName("FQLPT_CURRENCY").HasMaxLength(3);
            e.HasMany(x => x.Details).WithOne(x => x.PortType).HasForeignKey(x => x.FqlptdPortTypeId);
        });

        modelBuilder.Entity<LclPortTypeDet>(e =>
        {
            e.ToTable("FF_LCL_PORT_TYPE_DET");
            e.HasKey(x => x.FqlptdId);
            e.Property(x => x.FqlptdId).HasColumnName("FQLPTD_Id");
            e.Property(x => x.FqlptdPortTypeId).HasColumnName("FQLPTD_PortType_Id");
            e.Property(x => x.FqlptdForwarder).HasColumnName("FQLPTD_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqlptdQuoteNumber).HasColumnName("FQLPTD_QUOTENUMBER").HasMaxLength(10);
            e.Property(x => x.FqlptdPort).HasColumnName("FQLPTD_PORT").HasMaxLength(10);
            e.Property(x => x.FqlptdChargeType).HasColumnName("FQLPTD_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqlptdFrom).HasColumnName("FQLPTD_FROM");
            e.Property(x => x.FqlptdTo).HasColumnName("FQLPTD_TO");
            e.Property(x => x.FqlptdPrice).HasColumnName("FQLPTD_PRICE");
            e.Property(x => x.FqlptdOver).HasColumnName("FQLPTD_OVER");
            e.Property(x => x.FqlptdPriceType).HasColumnName("FQLPTD_PRICE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqlptdAmountMin).HasColumnName("FQLPTD_AMOUNT_MIN");
            e.Property(x => x.FqlptdAmountMax).HasColumnName("FQLPTD_AMOUNT_MAX");
        });

        modelBuilder.Entity<InlandAdditionalCharge>(e =>
        {
            e.ToTable("FF_INLAND_ADDITIONAL_CHARGES");
            e.HasKey(x => x.FqiaId);
            e.Property(x => x.FqiaId).HasColumnName("FQIA_Id");
            e.Property(x => x.FqiaForwarder).HasColumnName("FQIA_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqiaChargeType).HasColumnName("FQIA_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqiaLoadType).HasColumnName("FQIA_LOADTYPE").HasMaxLength(6);
            e.Property(x => x.FqiaAmount).HasColumnName("FQIA_AMOUNT");
            e.Property(x => x.FqiaAction).HasColumnName("FQIA_ACTION").HasMaxLength(6);
            e.Property(x => x.FqiaChargeOver).HasColumnName("FQIA_CHARGE_OVER").HasMaxLength(6);
            e.Property(x => x.FqiaChargePer).HasColumnName("FQIA_CHARGE_PER").HasMaxLength(6);
            e.Property(x => x.FqiaFrom).HasColumnName("FQIA_FROM");
            e.Property(x => x.FqiaTo).HasColumnName("FQIA_TO");
            e.Property(x => x.FqiaAmountMin).HasColumnName("FQIA_AMOUNT_MIN");
            e.Property(x => x.FqiaAmountMax).HasColumnName("FQIA_AMOUNT_MAX");
            e.Property(x => x.FqiaCurrency).HasColumnName("FQIA_CURRENCY").HasMaxLength(3);
        });

        // ── Applied Freight Quotes ────────────────────────────────────────────
        modelBuilder.Entity<FreightQuoteHeader>(e =>
        {
            e.ToTable("FF_QUOTE_HEADER");
            e.HasKey(x => x.FqhId);
            e.Property(x => x.FqhId).HasColumnName("FQH_Id");
            e.Property(x => x.FqhQuoteNumber).HasColumnName("FQH_QUOTE_NUMBER");
            e.Property(x => x.FqhForwarder).HasColumnName("FQH_FORWARDER").HasMaxLength(10);
            e.Property(x => x.FqhPort).HasColumnName("FQH_PORT").HasMaxLength(10);
            e.Property(x => x.FqhRoute).HasColumnName("FQH_ROUTE").HasMaxLength(15);
            e.Property(x => x.FqhTransitDays).HasColumnName("FQH_TRANSIT_DAYS");
            e.Property(x => x.FqhStartDate).HasColumnName("FQH_START_DATE");
            e.Property(x => x.FqhEndDate).HasColumnName("FQH_END_DATE");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At").HasDefaultValueSql("GETUTCDATE()");
            e.HasMany(x => x.OceanPorts).WithOne(x => x.Header).HasForeignKey(x => x.FqopHeaderId);
            e.HasMany(x => x.InlandRegions).WithOne(x => x.Header).HasForeignKey(x => x.FqerHeaderId);
            e.HasMany(x => x.InlandPortAdds).WithOne(x => x.Header).HasForeignKey(x => x.FqipaHeaderId);
            e.HasMany(x => x.LclPorts).WithOne(x => x.Header).HasForeignKey(x => x.FqlcpHeaderId);
        });

        modelBuilder.Entity<FreightQuoteOceanCharge>(e =>
        {
            e.ToTable("FF_QUOTE_OCEAN_CHARGE");
            e.HasKey(x => x.FqocId);
            e.Property(x => x.FqocId).HasColumnName("FQOC_Id");
            e.Property(x => x.FqocSLineId).HasColumnName("FQOC_SLine_Id");
            e.Property(x => x.FqocChargeType).HasColumnName("FQOC_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqocContainerType).HasColumnName("FQOC_CONTAINER_TYPE").HasMaxLength(6);
            e.Property(x => x.FqocAmount).HasColumnName("FQOC_AMOUNT");
            e.Property(x => x.FqocCurrency).HasColumnName("FQOC_CURRENCY").HasMaxLength(3);
        });

        modelBuilder.Entity<FreightQuoteOceanPort>(e =>
        {
            e.ToTable("FF_QUOTE_OCEAN_PORT");
            e.HasKey(x => x.FqopId);
            e.Property(x => x.FqopId).HasColumnName("FQOP_Id");
            e.Property(x => x.FqopHeaderId).HasColumnName("FQOP_Header_Id");
            e.Property(x => x.FqopPort).HasColumnName("FQOP_PORT").HasMaxLength(10);
            e.Property(x => x.FqopRemarks).HasColumnName("FQOP_REMARKS").HasMaxLength(200);
            e.HasMany(x => x.ShippingLines).WithOne(x => x.Port).HasForeignKey(x => x.FqopsPortId);
        });

        modelBuilder.Entity<FreightQuoteOceanPortSLine>(e =>
        {
            e.ToTable("FF_QUOTE_OCEAN_PORT_SLINE");
            e.HasKey(x => x.FqopsId);
            e.Property(x => x.FqopsId).HasColumnName("FQOPS_Id");
            e.Property(x => x.FqopsPortId).HasColumnName("FQOPS_Port_Id");
            e.Property(x => x.FqopsShippingLine).HasColumnName("FQOPS_SHIPPING_LINE").HasMaxLength(10);
            e.Property(x => x.FqopsRoute).HasColumnName("FQOPS_ROUTE").HasMaxLength(15);
            e.Property(x => x.FqopsDays).HasColumnName("FQOPS_DAYS");
            e.HasMany(x => x.Charges).WithOne(x => x.SLine).HasForeignKey(x => x.FqocSLineId);
        });

        modelBuilder.Entity<FreightQuoteInlRegion>(e =>
        {
            e.ToTable("FF_QUOTE_INL_REGION");
            e.HasKey(x => x.FqerId);
            e.Property(x => x.FqerId).HasColumnName("FQER_Id");
            e.Property(x => x.FqerHeaderId).HasColumnName("FQER_Header_Id");
            e.Property(x => x.FqerRegion).HasColumnName("FQER_REGION").HasMaxLength(20);
            e.HasMany(x => x.RegionTypes).WithOne(x => x.Region).HasForeignKey(x => x.FqertRegionId);
        });

        modelBuilder.Entity<FreightQuoteInlRegionType>(e =>
        {
            e.ToTable("FF_QUOTE_INL_REGION_TYPE");
            e.HasKey(x => x.FqertId);
            e.Property(x => x.FqertId).HasColumnName("FQERT_Id");
            e.Property(x => x.FqertRegionId).HasColumnName("FQERT_Region_Id");
            e.Property(x => x.FqertChargeType).HasColumnName("FQERT_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqertAmountMin).HasColumnName("FQERT_AMOUNT_MIN");
            e.Property(x => x.FqertAmountMax).HasColumnName("FQERT_AMOUNT_MAX");
            e.Property(x => x.FqertCurrency).HasColumnName("FQERT_CURRENCY").HasMaxLength(3);
            e.HasMany(x => x.Details).WithOne(x => x.RegionType).HasForeignKey(x => x.FqertdRegionTypeId);
        });

        modelBuilder.Entity<FreightQuoteInlRegionTypeDet>(e =>
        {
            e.ToTable("FF_QUOTE_INL_REGION_TYPE_DET");
            e.HasKey(x => x.FqertdId);
            e.Property(x => x.FqertdId).HasColumnName("FQERTD_Id");
            e.Property(x => x.FqertdRegionTypeId).HasColumnName("FQERTD_RegionType_Id");
            e.Property(x => x.FqertdFrom).HasColumnName("FQERTD_FROM");
            e.Property(x => x.FqertdTo).HasColumnName("FQERTD_TO");
            e.Property(x => x.FqertdPrice).HasColumnName("FQERTD_PRICE");
            e.Property(x => x.FqertdPriceType).HasColumnName("FQERTD_PRICE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqertdAmountMin).HasColumnName("FQERTD_AMOUNT_MIN");
            e.Property(x => x.FqertdAmountMax).HasColumnName("FQERTD_AMOUNT_MAX");
        });

        modelBuilder.Entity<FreightQuoteInlPortAdd>(e =>
        {
            e.ToTable("FF_QUOTE_INL_PORT_ADD");
            e.HasKey(x => x.FqipaId);
            e.Property(x => x.FqipaId).HasColumnName("FQIPA_Id");
            e.Property(x => x.FqipaHeaderId).HasColumnName("FQIPA_Header_Id");
            e.Property(x => x.FqipaChargeType).HasColumnName("FQIPA_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqipaLoadType).HasColumnName("FQIPA_LOAD_TYPE").HasMaxLength(6);
            e.Property(x => x.FqipaAmount).HasColumnName("FQIPA_AMOUNT");
            e.Property(x => x.FqipaAction).HasColumnName("FQIPA_ACTION").HasMaxLength(6);
            e.Property(x => x.FqipaChargeOver).HasColumnName("FQIPA_CHARGE_OVER").HasMaxLength(6);
            e.Property(x => x.FqipaChargePer).HasColumnName("FQIPA_CHARGE_PER").HasMaxLength(6);
            e.Property(x => x.FqipaFrom).HasColumnName("FQIPA_FROM");
            e.Property(x => x.FqipaTo).HasColumnName("FQIPA_TO");
            e.Property(x => x.FqipaAmountMin).HasColumnName("FQIPA_AMOUNT_MIN");
            e.Property(x => x.FqipaAmountMax).HasColumnName("FQIPA_AMOUNT_MAX");
            e.Property(x => x.FqipaCurrency).HasColumnName("FQIPA_CURRENCY").HasMaxLength(3);
        });

        modelBuilder.Entity<FreightQuoteLclPort>(e =>
        {
            e.ToTable("FF_QUOTE_LCL_PORT");
            e.HasKey(x => x.FqlcpId);
            e.Property(x => x.FqlcpId).HasColumnName("FQLCP_Id");
            e.Property(x => x.FqlcpHeaderId).HasColumnName("FQLCP_Header_Id");
            e.Property(x => x.FqlcpPort).HasColumnName("FQLCP_PORT").HasMaxLength(10);
            e.Property(x => x.FqlcpRemarks).HasColumnName("FQLCP_REMARKS").HasMaxLength(200);
            e.HasMany(x => x.PortTypes).WithOne(x => x.Port).HasForeignKey(x => x.FqlcptPortId);
        });

        modelBuilder.Entity<FreightQuoteLclPortType>(e =>
        {
            e.ToTable("FF_QUOTE_LCL_PORT_TYPE");
            e.HasKey(x => x.FqlcptId);
            e.Property(x => x.FqlcptId).HasColumnName("FQLCPT_Id");
            e.Property(x => x.FqlcptPortId).HasColumnName("FQLCPT_Port_Id");
            e.Property(x => x.FqlcptChargeType).HasColumnName("FQLCPT_CHARGE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqlcptAmountMin).HasColumnName("FQLCPT_AMOUNT_MIN");
            e.Property(x => x.FqlcptAmountMax).HasColumnName("FQLCPT_AMOUNT_MAX");
            e.Property(x => x.FqlcptCurrency).HasColumnName("FQLCPT_CURRENCY").HasMaxLength(3);
            e.HasMany(x => x.Details).WithOne(x => x.PortType).HasForeignKey(x => x.FqlcptdPortTypeId);
        });

        modelBuilder.Entity<FreightQuoteLclPortTypeDet>(e =>
        {
            e.ToTable("FF_QUOTE_LCL_PORT_TYPE_DET");
            e.HasKey(x => x.FqlcptdId);
            e.Property(x => x.FqlcptdId).HasColumnName("FQLCPTD_Id");
            e.Property(x => x.FqlcptdPortTypeId).HasColumnName("FQLCPTD_PortType_Id");
            e.Property(x => x.FqlcptdFrom).HasColumnName("FQLCPTD_FROM");
            e.Property(x => x.FqlcptdTo).HasColumnName("FQLCPTD_TO");
            e.Property(x => x.FqlcptdPrice).HasColumnName("FQLCPTD_PRICE");
            e.Property(x => x.FqlcptdOver).HasColumnName("FQLCPTD_OVER");
            e.Property(x => x.FqlcptdPriceType).HasColumnName("FQLCPTD_PRICE_TYPE").HasMaxLength(6);
            e.Property(x => x.FqlcptdAmountMin).HasColumnName("FQLCPTD_AMOUNT_MIN");
            e.Property(x => x.FqlcptdAmountMax).HasColumnName("FQLCPTD_AMOUNT_MAX");
        });

        // ── Activity Request ───────────────────────────────────────────────────
        modelBuilder.Entity<ActivityType>(e => { e.ToTable("ACTIVITY_TYPE"); e.HasKey(x => x.AtId); e.Property(x => x.AtId).HasColumnName("AT_Id"); });
        modelBuilder.Entity<BudgetActivity>(e => { e.ToTable("BUDGET_ACTIVITIES"); e.HasKey(x => x.BaId); e.Property(x => x.BaId).HasColumnName("BA_Id"); });
        modelBuilder.Entity<CatAddSpec>(e => { e.ToTable("CAT_ADD_SPECS"); e.HasKey(x => x.CasId); e.Property(x => x.CasId).HasColumnName("CAS_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatApparelType>(e => { e.ToTable("CAT_APPAREL_TYPE"); e.HasKey(x => x.CatId); e.Property(x => x.CatId).HasColumnName("CAT_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatBagSpec>(e => { e.ToTable("CAT_BAG_SPECS"); e.HasKey(x => x.CbsId); e.Property(x => x.CbsId).HasColumnName("CBS_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatBottle>(e => { e.ToTable("CAT_BOTTLES"); e.HasKey(x => x.CbId); e.Property(x => x.CbId).HasColumnName("CB_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatBrandSpecific>(e => { e.ToTable("CAT_BRAND_SPECIFIC"); e.HasKey(x => x.CbrId); e.Property(x => x.CbrId).HasColumnName("CBR_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatClothingType>(e => { e.ToTable("CAT_CLOTHING_TYPE"); e.HasKey(x => x.CctId); e.Property(x => x.CctId).HasColumnName("CCT_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatColor>(e => { e.ToTable("CAT_COLORS"); e.HasKey(x => x.CcId); e.Property(x => x.CcId).HasColumnName("CC_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatContent>(e => { e.ToTable("CAT_CONTENT"); e.HasKey(x => x.CcoId); e.Property(x => x.CcoId).HasColumnName("CCO_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatCoolerCapacity>(e => { e.ToTable("CAT_COOLER_CAPACITY"); e.HasKey(x => x.CccId); e.Property(x => x.CccId).HasColumnName("CCC_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatCoolerModel>(e => { e.ToTable("CAT_COOLER_MODEL"); e.HasKey(x => x.CcmId); e.Property(x => x.CcmId).HasColumnName("CCM_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatCoolerType>(e => { e.ToTable("CAT_COOLER_TYPE"); e.HasKey(x => x.CctyId); e.Property(x => x.CctyId).HasColumnName("CCTY_Id"); e.Property(x => x.CatPrefix).HasColumnName("CAT_Prefix"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatFileName>(e => { e.ToTable("CAT_FILE_NAMES"); e.HasKey(x => x.CfnId); e.Property(x => x.CfnId).HasColumnName("CFN_Id"); e.Property(x => x.FileNames).HasColumnName("File_Names"); e.Property(x => x.DisplayText).HasColumnName("Display_Text"); });
        modelBuilder.Entity<CatGender>(e => { e.ToTable("CAT_GENDER"); e.HasKey(x => x.CgId); e.Property(x => x.CgId).HasColumnName("CG_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatGlassServing>(e => { e.ToTable("CAT_GLASS_SERVING"); e.HasKey(x => x.CgsId); e.Property(x => x.CgsId).HasColumnName("CGS_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatInsurrance>(e => { e.ToTable("CAT_INSURRANCE"); e.HasKey(x => x.CiId); e.Property(x => x.CiId).HasColumnName("CI_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatLed>(e => { e.ToTable("CAT_LED"); e.HasKey(x => x.ClId); e.Property(x => x.ClId).HasColumnName("CL_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatMaintMonth>(e => { e.ToTable("CAT_MAINT_MONTHS"); e.HasKey(x => x.CmmId); e.Property(x => x.CmmId).HasColumnName("CMM_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatMaterial>(e => { e.ToTable("CAT_MATERIALS"); e.HasKey(x => x.CmId); e.Property(x => x.CmId).HasColumnName("CM_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatShape>(e => { e.ToTable("CAT_SHAPES"); e.HasKey(x => x.Cs2Id); e.Property(x => x.Cs2Id).HasColumnName("CS2_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatSize>(e => { e.ToTable("CAT_SIZES"); e.HasKey(x => x.CszId); e.Property(x => x.CszId).HasColumnName("CSZ_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CatVapType>(e => { e.ToTable("CAT_VAP_TYPE"); e.HasKey(x => x.CvtId); e.Property(x => x.CvtId).HasColumnName("CVT_Id"); e.Property(x => x.CatSel).HasColumnName("CAT_Sel"); });
        modelBuilder.Entity<CustomerNonClient>(e => { e.ToTable("CUSTOMER_NON_CLIENT"); e.HasKey(x => x.CncId); e.Property(x => x.CncId).HasColumnName("CNC_Id"); });
        modelBuilder.Entity<CustomerSalesGroup>(e => { e.ToTable("CUSTOMER_SALES_GROUP"); e.HasKey(x => x.CsgId); e.Property(x => x.CsgId).HasColumnName("CSG_Id"); });
        modelBuilder.Entity<CustomerSegmentInfo>(e => { e.ToTable("CUSTOMER_SEGMENT_INFO"); e.HasKey(x => x.CsiId); e.Property(x => x.CsiId).HasColumnName("CSI_Id"); });
        modelBuilder.Entity<CustomerTargetGroup>(e => { e.ToTable("CUSTOMER_TARGET_GROUP"); e.HasKey(x => x.CtgId); e.Property(x => x.CtgId).HasColumnName("CTG_Id"); });
        modelBuilder.Entity<DenialReason>(e => { e.ToTable("DENIAL_REASONS"); e.HasKey(x => x.DrId); e.Property(x => x.DrId).HasColumnName("DR_Id"); });
        modelBuilder.Entity<EntertainmentType>(e => { e.ToTable("ENTERTAINMENT_TYPE"); e.HasKey(x => x.EtId); e.Property(x => x.EtId).HasColumnName("ET_Id"); });
        modelBuilder.Entity<FacilitatorInfo>(e => { e.ToTable("FACILITATORS_INFO"); e.HasKey(x => x.FiId); e.Property(x => x.FiId).HasColumnName("FI_Id"); });
        modelBuilder.Entity<FiscalYear>(e => { e.ToTable("FISCAL_YEARS"); e.HasKey(x => x.FyId); e.Property(x => x.FyId).HasColumnName("FY_Id"); e.Property(x => x.FyYear).HasColumnName("FY_Year"); e.Property(x => x.FyStartDate).HasColumnName("FY_StartDate"); e.Property(x => x.FyEndDate).HasColumnName("FY_EndDate"); });
        modelBuilder.Entity<LicoresGroup>(e => { e.ToTable("LICORES_GROUP"); e.HasKey(x => x.LgId); e.Property(x => x.LgId).HasColumnName("LG_Id"); });
        modelBuilder.Entity<LocationInfo>(e => { e.ToTable("LOCATION_INFO"); e.HasKey(x => x.LiId); e.Property(x => x.LiId).HasColumnName("LI_Id"); });
        modelBuilder.Entity<PosCategory>(e => { e.ToTable("POS_CATEGORY"); e.HasKey(x => x.PcId); e.Property(x => x.PcId).HasColumnName("PC_Id"); });
        modelBuilder.Entity<PosLendGive>(e => { e.ToTable("POS_LEND_GIVE"); e.HasKey(x => x.PlgId); e.Property(x => x.PlgId).HasColumnName("PLG_Id"); });
        modelBuilder.Entity<PosMaterialsStatus>(e => { e.ToTable("POS_MATERIALS_STATUS"); e.HasKey(x => x.PmsId); e.Property(x => x.PmsId).HasColumnName("PMS_Id"); });
        modelBuilder.Entity<PosMaterial>(e =>
        {
            e.ToTable("POS_MATERIALS");
            e.HasKey(x => x.PmId);
            e.Property(x => x.PmId).HasColumnName("PM_Id");
            e.Property(x => x.PmCode).HasColumnName("PM_Code");
            e.Property(x => x.PmName).HasColumnName("PM_Name");
            e.Property(x => x.PmCategoryCode).HasColumnName("PM_Category_Code");
            e.Property(x => x.PmCategoryDesc).HasColumnName("PM_Category_Desc");
            e.Property(x => x.PmDescription).HasColumnName("PM_Description");
            e.Property(x => x.PmUnit).HasColumnName("PM_Unit");
            e.Property(x => x.PmStockTotal).HasColumnName("PM_Stock_Total");
            e.Property(x => x.PmStockAvailable).HasColumnName("PM_Stock_Available");
            e.Property(x => x.PmNotes).HasColumnName("PM_Notes");
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });
        modelBuilder.Entity<SponsoringType>(e => { e.ToTable("SPONSORING_TYPE"); e.HasKey(x => x.StId); e.Property(x => x.StId).HasColumnName("ST_Id"); });
        modelBuilder.Entity<StatusCode>(e => { e.ToTable("STATUS_CODES"); e.HasKey(x => x.ScId); e.Property(x => x.ScId).HasColumnName("SC_Id"); });
        modelBuilder.Entity<MarketingCalendar>(e =>
        {
            e.ToTable("MARKETING_CALENDAR");
            e.HasKey(x => x.McId);
            e.Property(x => x.McId).HasColumnName("MC_Id");
            e.Property(x => x.McYear).HasColumnName("MC_Year");
            e.Property(x => x.McSupplierCode).HasColumnName("MC_Supplier_Code").HasMaxLength(5);
            e.Property(x => x.McSupplierName).HasColumnName("MC_Supplier_Name").HasMaxLength(100);
            e.Property(x => x.McBrand).HasColumnName("MC_Brand").HasMaxLength(100).IsRequired();
            e.Property(x => x.McBudget).HasColumnName("MC_Budget").HasColumnType("decimal(18,2)");
            e.Property(x => x.McMonth1).HasColumnName("MC_Month1").HasMaxLength(300);
            e.Property(x => x.McMonth2).HasColumnName("MC_Month2").HasMaxLength(300);
            e.Property(x => x.McMonth3).HasColumnName("MC_Month3").HasMaxLength(300);
            e.Property(x => x.McMonth4).HasColumnName("MC_Month4").HasMaxLength(300);
            e.Property(x => x.McMonth5).HasColumnName("MC_Month5").HasMaxLength(300);
            e.Property(x => x.McMonth6).HasColumnName("MC_Month6").HasMaxLength(300);
            e.Property(x => x.McMonth7).HasColumnName("MC_Month7").HasMaxLength(300);
            e.Property(x => x.McMonth8).HasColumnName("MC_Month8").HasMaxLength(300);
            e.Property(x => x.McMonth9).HasColumnName("MC_Month9").HasMaxLength(300);
            e.Property(x => x.McMonth10).HasColumnName("MC_Month10").HasMaxLength(300);
            e.Property(x => x.McMonth11).HasColumnName("MC_Month11").HasMaxLength(300);
            e.Property(x => x.McMonth12).HasColumnName("MC_Month12").HasMaxLength(300);
            e.Property(x => x.McNotes).HasColumnName("MC_Notes").HasMaxLength(500);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<ActivityRequestHeader>(e =>
        {
            e.ToTable("ACTIVITY_REQUESTS");
            e.HasKey(x => x.ArId);
            e.Property(x => x.ArId).HasColumnName("AR_Id");
            e.Property(x => x.ArNumber).HasColumnName("AR_Number").HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.ArNumber).IsUnique();
            e.Property(x => x.ArYear).HasColumnName("AR_Year");
            e.Property(x => x.ArStatus).HasColumnName("AR_Status").HasMaxLength(20).IsRequired();
            e.Property(x => x.ArSupplierCode).HasColumnName("AR_Supplier_Code").HasMaxLength(5);
            e.Property(x => x.ArSupplierName).HasColumnName("AR_Supplier_Name").HasMaxLength(100);
            e.Property(x => x.ArBrand).HasColumnName("AR_Brand").HasMaxLength(100);
            e.Property(x => x.ArActivityTypeCode).HasColumnName("AR_Activity_Type_Code").HasMaxLength(20);
            e.Property(x => x.ArActivityTypeDesc).HasColumnName("AR_Activity_Type_Desc").HasMaxLength(100);
            e.Property(x => x.ArDescription).HasColumnName("AR_Description").HasMaxLength(500);
            e.Property(x => x.ArStartDate).HasColumnName("AR_Start_Date");
            e.Property(x => x.ArEndDate).HasColumnName("AR_End_Date");
            e.Property(x => x.ArLocationCode).HasColumnName("AR_Location_Code").HasMaxLength(20);
            e.Property(x => x.ArLocationName).HasColumnName("AR_Location_Name").HasMaxLength(100);
            e.Property(x => x.ArBudget).HasColumnName("AR_Budget").HasColumnType("decimal(18,2)");
            e.Property(x => x.ArSegmentCode).HasColumnName("AR_Segment_Code").HasMaxLength(20);
            e.Property(x => x.ArTargetGroupCode).HasColumnName("AR_Target_Group_Code").HasMaxLength(20);
            e.Property(x => x.ArSalesGroupCode).HasColumnName("AR_Sales_Group_Code").HasMaxLength(20);
            e.Property(x => x.ArNonClientCode).HasColumnName("AR_Non_Client_Code").HasMaxLength(20);
            e.Property(x => x.ArNonClientName).HasColumnName("AR_Non_Client_Name").HasMaxLength(100);
            e.Property(x => x.ArFacilitatorCode).HasColumnName("AR_Facilitator_Code").HasMaxLength(20);
            e.Property(x => x.ArFacilitatorName).HasColumnName("AR_Facilitator_Name").HasMaxLength(100);
            e.Property(x => x.ArSponsoringTypeCode).HasColumnName("AR_Sponsoring_Type_Code").HasMaxLength(20);
            e.Property(x => x.ArEntertainmentTypeCode).HasColumnName("AR_Entertainment_Type_Code").HasMaxLength(20);
            e.Property(x => x.ArNotes).HasColumnName("AR_Notes").HasMaxLength(1000);
            e.Property(x => x.ArCreatedBy).HasColumnName("AR_Created_By");
            e.Property(x => x.ArCreatedByName).HasColumnName("AR_Created_By_Name").HasMaxLength(100);
            e.Property(x => x.ArApprovedBy).HasColumnName("AR_Approved_By");
            e.Property(x => x.ArApprovedByName).HasColumnName("AR_Approved_By_Name").HasMaxLength(100);
            e.Property(x => x.ArApprovedAt).HasColumnName("AR_Approved_At");
            e.Property(x => x.ArDeniedBy).HasColumnName("AR_Denied_By");
            e.Property(x => x.ArDeniedByName).HasColumnName("AR_Denied_By_Name").HasMaxLength(100);
            e.Property(x => x.ArDeniedAt).HasColumnName("AR_Denied_At");
            e.Property(x => x.ArDenialReason).HasColumnName("AR_Denial_Reason").HasMaxLength(500);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<ActivityRqBrand>(e =>
        {
            e.ToTable("ACTIVITY_RQ_BRANDS");
            e.HasKey(x => x.ArbId);
            e.Property(x => x.ArbId).HasColumnName("ARB_Id");
            e.Property(x => x.ArbArId).HasColumnName("ARB_AR_Id");
            e.Property(x => x.ArbSupplierCode).HasColumnName("ARB_Supplier_Code").HasMaxLength(5);
            e.Property(x => x.ArbSupplierName).HasColumnName("ARB_Supplier_Name").HasMaxLength(100);
            e.Property(x => x.ArbBrand).HasColumnName("ARB_Brand").HasMaxLength(100);
            e.Property(x => x.ArbBudget).HasColumnName("ARB_Budget").HasColumnType("decimal(18,2)");
            e.Property(x => x.ArbNotes).HasColumnName("ARB_Notes").HasMaxLength(300);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<ActivityRqProduct>(e =>
        {
            e.ToTable("ACTIVITY_RQ_PRODUCTS");
            e.HasKey(x => x.ArpId);
            e.Property(x => x.ArpId).HasColumnName("ARP_Id");
            e.Property(x => x.ArpArId).HasColumnName("ARP_AR_Id");
            e.Property(x => x.ArpProductCode).HasColumnName("ARP_Product_Code").HasMaxLength(20);
            e.Property(x => x.ArpProductName).HasColumnName("ARP_Product_Name").HasMaxLength(150);
            e.Property(x => x.ArpQuantity).HasColumnName("ARP_Quantity").HasColumnType("decimal(18,4)");
            e.Property(x => x.ArpUnit).HasColumnName("ARP_Unit").HasMaxLength(20);
            e.Property(x => x.ArpNotes).HasColumnName("ARP_Notes").HasMaxLength(300);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<ActivityRqCash>(e =>
        {
            e.ToTable("ACTIVITY_RQ_CASH"); e.HasKey(x => x.ArcId);
            e.Property(x => x.ArcId).HasColumnName("ARC_Id");
            e.Property(x => x.ArcArId).HasColumnName("ARC_AR_Id");
            e.Property(x => x.ArcType).HasColumnName("ARC_Type").HasMaxLength(50);
            e.Property(x => x.ArcAmount).HasColumnName("ARC_Amount").HasColumnType("decimal(18,2)");
            e.Property(x => x.ArcReference).HasColumnName("ARC_Reference").HasMaxLength(100);
            e.Property(x => x.ArcNotes).HasColumnName("ARC_Notes").HasMaxLength(300);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });
        modelBuilder.Entity<ActivityRqPrint>(e =>
        {
            e.ToTable("ACTIVITY_RQ_PRINT"); e.HasKey(x => x.ArprId);
            e.Property(x => x.ArprId).HasColumnName("ARPR_Id");
            e.Property(x => x.ArprArId).HasColumnName("ARPR_AR_Id");
            e.Property(x => x.ArprPublication).HasColumnName("ARPR_Publication").HasMaxLength(150);
            e.Property(x => x.ArprFormat).HasColumnName("ARPR_Format").HasMaxLength(100);
            e.Property(x => x.ArprSize).HasColumnName("ARPR_Size").HasMaxLength(50);
            e.Property(x => x.ArprCost).HasColumnName("ARPR_Cost").HasColumnType("decimal(18,2)");
            e.Property(x => x.ArprNotes).HasColumnName("ARPR_Notes").HasMaxLength(300);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });
        modelBuilder.Entity<ActivityRqRadio>(e =>
        {
            e.ToTable("ACTIVITY_RQ_RADIO"); e.HasKey(x => x.ArrId);
            e.Property(x => x.ArrId).HasColumnName("ARR_Id");
            e.Property(x => x.ArrArId).HasColumnName("ARR_AR_Id");
            e.Property(x => x.ArrStation).HasColumnName("ARR_Station").HasMaxLength(150);
            e.Property(x => x.ArrDuration).HasColumnName("ARR_Duration").HasMaxLength(50);
            e.Property(x => x.ArrFrequency).HasColumnName("ARR_Frequency");
            e.Property(x => x.ArrCost).HasColumnName("ARR_Cost").HasColumnType("decimal(18,2)");
            e.Property(x => x.ArrNotes).HasColumnName("ARR_Notes").HasMaxLength(300);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });
        modelBuilder.Entity<ActivityRqPosMat>(e =>
        {
            e.ToTable("ACTIVITY_RQ_POS_MAT"); e.HasKey(x => x.ArpmId);
            e.Property(x => x.ArpmId).HasColumnName("ARPM_Id");
            e.Property(x => x.ArpmArId).HasColumnName("ARPM_AR_Id");
            e.Property(x => x.ArpmCode).HasColumnName("ARPM_Code").HasMaxLength(20);
            e.Property(x => x.ArpmName).HasColumnName("ARPM_Name").HasMaxLength(150);
            e.Property(x => x.ArpmQuantity).HasColumnName("ARPM_Quantity");
            e.Property(x => x.ArpmUnit).HasColumnName("ARPM_Unit").HasMaxLength(20);
            e.Property(x => x.ArpmNotes).HasColumnName("ARPM_Notes").HasMaxLength(300);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });
        modelBuilder.Entity<ActivityRqPromotion>(e =>
        {
            e.ToTable("ACTIVITY_RQ_PROMOTIONS"); e.HasKey(x => x.ArpoId);
            e.Property(x => x.ArpoId).HasColumnName("ARPO_Id");
            e.Property(x => x.ArpoArId).HasColumnName("ARPO_AR_Id");
            e.Property(x => x.ArpoType).HasColumnName("ARPO_Type").HasMaxLength(100);
            e.Property(x => x.ArpoDescription).HasColumnName("ARPO_Description").HasMaxLength(300);
            e.Property(x => x.ArpoCost).HasColumnName("ARPO_Cost").HasColumnType("decimal(18,2)");
            e.Property(x => x.ArpoNotes).HasColumnName("ARPO_Notes").HasMaxLength(300);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });
        modelBuilder.Entity<ActivityRqOther>(e =>
        {
            e.ToTable("ACTIVITY_RQ_OTHERS"); e.HasKey(x => x.AroId);
            e.Property(x => x.AroId).HasColumnName("ARO_Id");
            e.Property(x => x.AroArId).HasColumnName("ARO_AR_Id");
            e.Property(x => x.AroDescription).HasColumnName("ARO_Description").HasMaxLength(300);
            e.Property(x => x.AroCost).HasColumnName("ARO_Cost").HasColumnType("decimal(18,2)");
            e.Property(x => x.AroNotes).HasColumnName("ARO_Notes").HasMaxLength(300);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<PosLendOut>(e =>
        {
            e.ToTable("POS_LEND_OUT");
            e.HasKey(x => x.PloId);
            e.Property(x => x.PloId).HasColumnName("PLO_Id");
            e.Property(x => x.PloNumber).HasColumnName("PLO_Number").HasMaxLength(20);
            e.Property(x => x.PloYear).HasColumnName("PLO_Year");
            e.Property(x => x.PloStatus).HasColumnName("PLO_Status").HasMaxLength(20);
            e.Property(x => x.PloDate).HasColumnName("PLO_Date");
            e.Property(x => x.PloExpectedReturn).HasColumnName("PLO_Expected_Return");
            e.Property(x => x.PloActualReturn).HasColumnName("PLO_Actual_Return");
            e.Property(x => x.PloClientCode).HasColumnName("PLO_Client_Code").HasMaxLength(20);
            e.Property(x => x.PloClientName).HasColumnName("PLO_Client_Name").HasMaxLength(150);
            e.Property(x => x.PloContactName).HasColumnName("PLO_Contact_Name").HasMaxLength(150);
            e.Property(x => x.PloContactPhone).HasColumnName("PLO_Contact_Phone").HasMaxLength(50);
            e.Property(x => x.PloNotes).HasColumnName("PLO_Notes").HasMaxLength(300);
            e.Property(x => x.PloCreatedById).HasColumnName("PLO_Created_By_Id");
            e.Property(x => x.PloCreatedByName).HasColumnName("PLO_Created_By_Name").HasMaxLength(150);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<PosLendOutItem>(e =>
        {
            e.ToTable("POS_LEND_OUT_ITEMS");
            e.HasKey(x => x.PloiId);
            e.Property(x => x.PloiId).HasColumnName("PLOI_Id");
            e.Property(x => x.PloiPloId).HasColumnName("PLOI_PLO_Id");
            e.Property(x => x.PloiPmCode).HasColumnName("PLOI_PM_Code").HasMaxLength(20);
            e.Property(x => x.PloiPmName).HasColumnName("PLOI_PM_Name").HasMaxLength(150);
            e.Property(x => x.PloiQuantityLent).HasColumnName("PLOI_Quantity_Lent");
            e.Property(x => x.PloiQuantityReturned).HasColumnName("PLOI_Quantity_Returned");
            e.Property(x => x.PloiNotes).HasColumnName("PLOI_Notes").HasMaxLength(300);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        // ── Aankoopbon ─────────────────────────────────────────────────────────
        modelBuilder.Entity<AbOrderHeader>(e =>
        {
            e.ToTable("AB_ORDER_HEADERS");
            e.HasKey(x => x.AohId);
            e.Property(x => x.AohId).HasColumnName("AOH_Id");
            e.Property(x => x.AohBonNr).HasColumnName("AOH_Bon_Nr").HasMaxLength(15).IsRequired();
            e.HasIndex(x => x.AohBonNr).IsUnique();
            e.Property(x => x.AohStatus).HasColumnName("AOH_Status").HasMaxLength(20).IsRequired();
            e.Property(x => x.AohOrderDate).HasColumnName("AOH_Order_Date");
            e.Property(x => x.AohRequestor).HasColumnName("AOH_Requestor").HasMaxLength(15);
            e.Property(x => x.AohVendorId).HasColumnName("AOH_Vendor_Id");
            e.Property(x => x.AohVendorName).HasColumnName("AOH_Vendor_Name").HasMaxLength(100);
            e.Property(x => x.AohVendorAddress).HasColumnName("AOH_Vendor_Address").HasMaxLength(100);
            e.Property(x => x.AohDepartment).HasColumnName("AOH_Department").HasMaxLength(15);
            e.Property(x => x.AohCostType).HasColumnName("AOH_Cost_Type").HasMaxLength(15);
            e.Property(x => x.AohRemarks).HasColumnName("AOH_Remarks").HasMaxLength(255);
            e.Property(x => x.AohVehicleId).HasColumnName("AOH_Vehicle_Id");
            e.Property(x => x.AohVehicleLicense).HasColumnName("AOH_Vehicle_License").HasMaxLength(10);
            e.Property(x => x.AohVehicleType).HasColumnName("AOH_Vehicle_Type").HasMaxLength(15);
            e.Property(x => x.AohVehicleModel).HasColumnName("AOH_Vehicle_Model").HasMaxLength(15);
            e.Property(x => x.AohQuotationNr).HasColumnName("AOH_Quotation_Nr").HasMaxLength(15);
            e.Property(x => x.AohAmount).HasColumnName("AOH_Amount").HasColumnType("decimal(18,2)");
            e.Property(x => x.AohMeegeven).HasColumnName("AOH_Meegeven");
            e.Property(x => x.AohOntvangen).HasColumnName("AOH_Ontvangen");
            e.Property(x => x.AohZenden).HasColumnName("AOH_Zenden");
            e.Property(x => x.AohAndere).HasColumnName("AOH_Andere");
            e.Property(x => x.AohReceiverId).HasColumnName("AOH_Receiver_Id");
            e.Property(x => x.AohReceiverName).HasColumnName("AOH_Receiver_Name").HasMaxLength(30);
            e.Property(x => x.AohReceiverIdDoc).HasColumnName("AOH_Receiver_Id_Doc").HasMaxLength(15);
            e.Property(x => x.AohApprovedBy).HasColumnName("AOH_Approved_By");
            e.Property(x => x.AohApprovedByName).HasColumnName("AOH_Approved_By_Name").HasMaxLength(100);
            e.Property(x => x.AohApprovedAt).HasColumnName("AOH_Approved_At");
            e.Property(x => x.AohRejectedBy).HasColumnName("AOH_Rejected_By");
            e.Property(x => x.AohRejectedByName).HasColumnName("AOH_Rejected_By_Name").HasMaxLength(100);
            e.Property(x => x.AohRejectedAt).HasColumnName("AOH_Rejected_At");
            e.Property(x => x.AohRejectionReason).HasColumnName("AOH_Rejection_Reason").HasMaxLength(500);
            e.Property(x => x.AohQuotationPdfPath).HasColumnName("AOH_Quotation_PDF_Path").HasMaxLength(500);
            e.Property(x => x.AohInvoiceNr).HasColumnName("AOH_Invoice_Nr").HasMaxLength(15);
            e.Property(x => x.AohInvoiceDate).HasColumnName("AOH_Invoice_Date");
            e.Property(x => x.AohInvoiceAmount).HasColumnName("AOH_Invoice_Amount").HasColumnType("decimal(18,2)");
            e.Property(x => x.AohClosedBy).HasColumnName("AOH_Closed_By");
            e.Property(x => x.AohClosedByName).HasColumnName("AOH_Closed_By_Name").HasMaxLength(100);
            e.Property(x => x.AohClosedAt).HasColumnName("AOH_Closed_At");
            e.Property(x => x.AohCreatedBy).HasColumnName("AOH_Created_By");
            e.Property(x => x.AohCreatedByName).HasColumnName("AOH_Created_By_Name").HasMaxLength(100);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
            e.HasMany(x => x.Details).WithOne(x => x.Header).HasForeignKey(x => x.AodHeaderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AbOrderDetail>(e =>
        {
            e.ToTable("AB_ORDER_DETAILS");
            e.HasKey(x => x.AodId);
            e.Property(x => x.AodId).HasColumnName("AOD_Id");
            e.Property(x => x.AodHeaderId).HasColumnName("AOD_Header_Id");
            e.Property(x => x.AodLineNr).HasColumnName("AOD_Line_Nr");
            e.Property(x => x.AodProductCode).HasColumnName("AOD_Product_Code").HasMaxLength(20);
            e.Property(x => x.AodProductDesc).HasColumnName("AOD_Product_Desc").HasMaxLength(100).IsRequired();
            e.Property(x => x.AodAdditionalDesc).HasColumnName("AOD_Additional_Desc").HasMaxLength(255);
            e.Property(x => x.AodCostType).HasColumnName("AOD_Cost_Type").HasMaxLength(50);
            e.Property(x => x.AodQuantity).HasColumnName("AOD_Quantity").HasColumnType("decimal(18,2)");
            e.Property(x => x.AodUnit).HasColumnName("AOD_Unit").HasMaxLength(10);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<AbProduct>(e => { e.ToTable("AB_PRODUCTS"); e.HasKey(x => x.AbpId); e.Property(x => x.AbpId).HasColumnName("ABP_Id"); e.Property(x => x.ItemKode).HasMaxLength(20); e.Property(x => x.Omschrijving).HasMaxLength(100); e.Property(x => x.VendorCode).HasMaxLength(6); e.Property(x => x.CostType).HasColumnName("Cost_Type").HasMaxLength(15); e.Property(x => x.Eenheid).HasMaxLength(10); e.Property(x => x.IsActive).HasColumnName("IS_Active"); e.Property(x => x.CreatedAt).HasColumnName("Created_At"); });
        modelBuilder.Entity<Department>(e => { e.ToTable("DEPARTMENTS"); e.HasKey(x => x.DpId); e.Property(x => x.DpId).HasColumnName("DP_Id"); e.Property(x => x.DpName).HasColumnName("DP_NAME").HasMaxLength(15); e.Property(x => x.IsActive).HasColumnName("IS_Active"); e.Property(x => x.CreatedAt).HasColumnName("Created_At"); });
        modelBuilder.Entity<Eenheid>(e => { e.ToTable("EENHEDEN"); e.HasKey(x => x.EeId); e.Property(x => x.EeId).HasColumnName("EE_Id"); e.Property(x => x.UnitCode).HasMaxLength(10); e.Property(x => x.Omschrijving).HasMaxLength(50); e.Property(x => x.OmrekenFaktor).HasColumnName("OmrekenFaktor"); e.Property(x => x.IsActive).HasColumnName("IS_Active"); e.Property(x => x.CreatedAt).HasColumnName("Created_At"); });
        modelBuilder.Entity<Receiver>(e => { e.ToTable("RECEIVERS"); e.HasKey(x => x.RecId); e.Property(x => x.RecId).HasColumnName("REC_Id"); e.Property(x => x.RecName).HasColumnName("REC_NAME").HasMaxLength(30); e.Property(x => x.RecIdDoc).HasColumnName("REC_ID_DOC").HasMaxLength(15); e.Property(x => x.IsActive).HasColumnName("IS_Active"); e.Property(x => x.CreatedAt).HasColumnName("Created_At"); });
        modelBuilder.Entity<Requestor>(e => { e.ToTable("REQUESTORS"); e.HasKey(x => x.ReqId); e.Property(x => x.ReqId).HasColumnName("REQ_Id"); e.Property(x => x.ReqName).HasColumnName("REQ_NAME").HasMaxLength(15); e.Property(x => x.ReqEmail).HasColumnName("REQ_EMAIL").HasMaxLength(50); e.Property(x => x.IsActive).HasColumnName("IS_Active"); e.Property(x => x.CreatedAt).HasColumnName("Created_At"); });
        modelBuilder.Entity<RequestorVendor>(e => { e.ToTable("REQUESTORS_VENDOR"); e.HasKey(x => x.RvId); e.Property(x => x.RvId).HasColumnName("RV_Id"); e.Property(x => x.RsRequestor).HasColumnName("RS_REQUESTOR").HasMaxLength(15); e.Property(x => x.RsVendor).HasColumnName("RS_VENDOR").HasMaxLength(6); e.Property(x => x.IsActive).HasColumnName("IS_Active"); e.Property(x => x.CreatedAt).HasColumnName("Created_At"); });
        modelBuilder.Entity<CostType>(e => { e.ToTable("COST_TYPE"); e.HasKey(x => x.CtId); e.Property(x => x.CtId).HasColumnName("CT_Id"); e.Property(x => x.TcName).HasColumnName("TC_NAME").HasMaxLength(15); e.Property(x => x.IsActive).HasColumnName("IS_Active"); e.Property(x => x.CreatedAt).HasColumnName("Created_At"); });
        modelBuilder.Entity<VehicleType>(e => { e.ToTable("VEHICLE_TYPE"); e.HasKey(x => x.VtId); e.Property(x => x.VtId).HasColumnName("VT_Id"); e.Property(x => x.VtName).HasColumnName("VT_NAME").HasMaxLength(15); e.Property(x => x.IsActive).HasColumnName("IS_Active"); e.Property(x => x.CreatedAt).HasColumnName("Created_At"); });
        modelBuilder.Entity<Vehicle>(e => { e.ToTable("VEHICLES"); e.HasKey(x => x.VhId); e.Property(x => x.VhId).HasColumnName("VH_Id"); e.Property(x => x.VhLicense).HasColumnName("VH_LICENSE").HasMaxLength(10); e.Property(x => x.VhType).HasColumnName("VH_TYPE").HasMaxLength(15); e.Property(x => x.VhModel).HasColumnName("VH_MODEL").HasMaxLength(15); e.Property(x => x.IsActive).HasColumnName("IS_Active"); e.Property(x => x.CreatedAt).HasColumnName("Created_At"); });
        modelBuilder.Entity<Vendor>(e => { e.ToTable("VENDORS"); e.HasKey(x => x.VndId); e.Property(x => x.VndId).HasColumnName("VND_Id"); e.Property(x => x.VndCode).HasColumnName("VND_Code").HasMaxLength(6); e.Property(x => x.VndName).HasColumnName("VND_Name").HasMaxLength(100); e.Property(x => x.VndAddress1).HasColumnName("VND_Address_1").HasMaxLength(100); e.Property(x => x.VndPhone1).HasColumnName("VND_Phone_1").HasMaxLength(25); e.Property(x => x.VndEmail).HasColumnName("VND_Email").HasMaxLength(50); e.Property(x => x.VndContact).HasColumnName("VND_Contact").HasMaxLength(100); e.Property(x => x.VndCurr).HasColumnName("VND_CURR").HasMaxLength(3); e.Property(x => x.VndCrib).HasColumnName("VND_CRIB").HasMaxLength(15); e.Property(x => x.VndKvk).HasColumnName("VND_KVK").HasMaxLength(15); e.Property(x => x.VndCash).HasColumnName("VND_CASH"); e.Property(x => x.VndQuoteMandatory).HasColumnName("VND_Quote_Mandatory"); e.Property(x => x.IsActive).HasColumnName("IS_Active"); e.Property(x => x.CreatedAt).HasColumnName("Created_At"); });

        // ── Cost Calculation ───────────────────────────────────────────────────
        modelBuilder.Entity<CcCalcHeader>(e =>
        {
            e.ToTable("COST_CALC_FIN");
            e.HasKey(x => x.CcCalcNumber);
            e.Property(x => x.CcCalcNumber).HasColumnName("CC_Calc_Number");
            e.Property(x => x.CcCalcDate).HasColumnName("CC_Calc_Date");
            e.Property(x => x.CcForwarderCode).HasColumnName("CC_Forwarder_Code").HasMaxLength(10);
            e.Property(x => x.CcForwarderName).HasColumnName("CC_Forwarder_Name").HasMaxLength(50);
            e.Property(x => x.CcCurrCode).HasColumnName("CC_CurrCode").HasMaxLength(3);
            e.Property(x => x.CcCurrRate).HasColumnName("CC_CurrRate");
            e.Property(x => x.CcFreight).HasColumnName("CC_Freight");
            e.Property(x => x.CcTransport).HasColumnName("CC_Transport");
            e.Property(x => x.CcUnloading).HasColumnName("CC_Unloading");
            e.Property(x => x.CcLocalHandling).HasColumnName("CC_Local_Handling");
            e.Property(x => x.CcTotWeight).HasColumnName("CC_TotWeight");
            e.Property(x => x.CcStatus).HasColumnName("CC_Status").HasMaxLength(2);
            e.Property(x => x.CcTotOrd).HasColumnName("CC_TotOrd");
            e.Property(x => x.CcTotQty).HasColumnName("CC_TotQty");
            e.Property(x => x.CcWarehouse).HasColumnName("CC_Warehouse").HasMaxLength(3);
            e.Property(x => x.CcCreatedBy).HasColumnName("CC_Created_By").HasMaxLength(50);
            e.Property(x => x.CcCreatedAt).HasColumnName("CC_Created_At");
            e.HasMany(x => x.PoHeads).WithOne(x => x.CalcHeader).HasForeignKey(x => x.CcphCalcNumber);
        });

        modelBuilder.Entity<CcCalcPoHead>(e =>
        {
            e.ToTable("COST_CALC_PO_HEAD_FIN");
            e.HasKey(x => new { x.CcphCalcNumber, x.CcphLmPoNo });
            e.Property(x => x.CcphCalcNumber).HasColumnName("CCPH_Calc_Number");
            e.Property(x => x.CcphLmPoNo).HasColumnName("CCPH_LMPoNo").HasMaxLength(10);
            e.Property(x => x.CcphVendNo).HasColumnName("CCPH_VendNo").HasMaxLength(6);
            e.Property(x => x.CcphVendName).HasColumnName("CCPH_VendName").HasMaxLength(50);
            e.Property(x => x.CcphWhse).HasColumnName("CCPH_WareHouse").HasMaxLength(3);
            e.Property(x => x.CcphCurrCode).HasColumnName("CCPH_CurrCode").HasMaxLength(3);
            e.Property(x => x.CcphCurrRate).HasColumnName("CCPH_CurrRate");
            e.Property(x => x.CcphCurrRateCust).HasColumnName("CCPH_CurrRate_Cust");
            e.Property(x => x.CcphInvNumber).HasColumnName("CCPH_Inv_Number").HasMaxLength(20);
            e.Property(x => x.CcphInvDate).HasColumnName("CCPH_Inv_Date");
            e.Property(x => x.CcphLocalHandling).HasColumnName("CCPH_Local_Handling");
            e.Property(x => x.CcphDuties).HasColumnName("CCPH_Duties");
            e.Property(x => x.CcphEconSurch).HasColumnName("CCPH_Econ_Surch");
            e.Property(x => x.CcphOb).HasColumnName("CCPH_OB");
            e.Property(x => x.CcphWeight).HasColumnName("CCPH_Weight");
            e.Property(x => x.CcphFreight).HasColumnName("CCPH_Freight");
            e.Property(x => x.CcphTransport).HasColumnName("CCPH_Transport");
            e.Property(x => x.CcphUnloading).HasColumnName("CCPH_Unloading");
            e.Property(x => x.CcphInsurance).HasColumnName("CCPH_Insurance");
            e.Property(x => x.CcphTotQty).HasColumnName("CCPH_TotQty");
            e.Property(x => x.CcphTotAmountFC).HasColumnName("CCPH_TotAmount_FC");
            e.Property(x => x.CcphTotAmount).HasColumnName("CCPH_TotAmount");
            e.Property(x => x.CcphInlandFreight).HasColumnName("CCPH_Inland_Freight_FF");
            e.Property(x => x.CcphShipCharges).HasColumnName("CCPH_Ship_Charges");
            e.Property(x => x.CcphInlandTariff).HasColumnName("CCPH_Inland_Tariff");
            e.Property(x => x.CcphStatus).HasColumnName("CCPH_Status").HasMaxLength(2);
            e.Property(x => x.CcphCreatedBy).HasColumnName("CCPH_Created_By").HasMaxLength(50);
            e.Property(x => x.CcphConfirmedBy).HasColumnName("CCPH_Confirmed_By").HasMaxLength(50);
            e.Property(x => x.CcphApprovedBy).HasColumnName("CCPH_Approved_By").HasMaxLength(50);
            e.HasMany(x => x.Details).WithOne(x => x.PoHead).HasForeignKey(x => new { x.CcpdCalcNumber, x.CcpdLmPoNo });
        });

        modelBuilder.Entity<CcCalcPoDetail>(e =>
        {
            e.ToTable("COST_CALC_PO_DET_FIN");
            e.HasKey(x => new { x.CcpdCalcNumber, x.CcpdLmPoNo, x.CcpdItemNo });
            e.Property(x => x.CcpdCalcNumber).HasColumnName("CCPD_Calc_Number");
            e.Property(x => x.CcpdLmPoNo).HasColumnName("CCPD_LMPoNo").HasMaxLength(10);
            e.Property(x => x.CcpdItemNo).HasColumnName("CCPD_ItemNo").HasMaxLength(20);
            e.Property(x => x.CcpdItemDescr).HasColumnName("CCPD_Item_Descr").HasMaxLength(50);
            e.Property(x => x.CcpdUnitCase).HasColumnName("CCPD_UnitCase");
            e.Property(x => x.CcpdOrdQty).HasColumnName("CCPD_OrdQty");
            e.Property(x => x.CcpdFobPrice).HasColumnName("CCPD_FOB_Price");
            e.Property(x => x.CcpdFobPriceTot).HasColumnName("CCPD_FOB_Price_Tot");
            e.Property(x => x.CcpdInlandFreight).HasColumnName("CCPD_Inland_Freight");
            e.Property(x => x.CcpdFreight).HasColumnName("CCPD_Freight");
            e.Property(x => x.CcpdLocalHandl).HasColumnName("CCPD_Local_Handl");
            e.Property(x => x.CcpdDuties).HasColumnName("CCPD_Duties");
            e.Property(x => x.CcpdEconSurch).HasColumnName("CCPD_Econ_Surch");
            e.Property(x => x.CcpdOb).HasColumnName("CCPD_OB");
            e.Property(x => x.CcpdInlandTariff).HasColumnName("CCPD_Inland_Tariff");
            e.Property(x => x.CcpdShipCharges).HasColumnName("CCPD_Ship_Charges");
            e.Property(x => x.CcpdAllowedMin).HasColumnName("CCPD_Allowed_Min");
            e.Property(x => x.CcpdAllowedMax).HasColumnName("CCPD_Allowed_Max");
            e.Property(x => x.CcpdInsurance).HasColumnName("CCPD_Insurance");
            e.Property(x => x.CcpdTransport).HasColumnName("CCPD_Transport");
            e.Property(x => x.CcpdUnloading).HasColumnName("CCPD_Unloading");
            e.Property(x => x.CcpdFinalCost).HasColumnName("CCPD_Final_Cost");
            e.Property(x => x.CcpdWarehouse).HasColumnName("CCPD_Warehouse").HasMaxLength(3);
            e.Property(x => x.CcpdMarginPerc).HasColumnName("CCPD_Margin_Perc");
            e.Property(x => x.CcpdSellingPrice).HasColumnName("CCPD_Selling_Price");
        });

        modelBuilder.Entity<CcTariffItem>(e =>
        {
            e.ToTable("CC_TARIFF_ITEMS");
            e.HasKey(x => x.TiId);
            e.Property(x => x.TiId).HasColumnName("TI_Id");
            e.Property(x => x.TiHsCode).HasColumnName("TI_HS_Code").HasMaxLength(20);
            e.Property(x => x.TiDescription).HasColumnName("TI_Description").HasMaxLength(200);
            e.Property(x => x.TiDutyRate).HasColumnName("TI_Duty_Rate");
            e.Property(x => x.TiEconRate).HasColumnName("TI_Econ_Rate");
            e.Property(x => x.TiObRate).HasColumnName("TI_OB_Rate");
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<CcGoodsClassification>(e =>
        {
            e.ToTable("CC_GOODS_CLASSIFICATION");
            e.HasKey(x => x.GcId);
            e.Property(x => x.GcId).HasColumnName("GC_Id");
            e.Property(x => x.GcItemCode).HasColumnName("GC_Item_Code").HasMaxLength(20);
            e.Property(x => x.GcItemDescr).HasColumnName("GC_Item_Descr").HasMaxLength(200);
            e.Property(x => x.GcHsCode).HasColumnName("GC_HS_Code").HasMaxLength(20);
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<CcItemWeight>(e =>
        {
            e.ToTable("CC_ITEM_WEIGHTS");
            e.HasKey(x => x.IwId);
            e.Property(x => x.IwId).HasColumnName("IW_Id");
            e.Property(x => x.IwItemCode).HasColumnName("IW_Item_Code").HasMaxLength(20);
            e.Property(x => x.IwItemDescr).HasColumnName("IW_Item_Descr").HasMaxLength(200);
            e.Property(x => x.IwWeightCase).HasColumnName("IW_Weight_Case");
            e.Property(x => x.IwWeightUnit).HasColumnName("IW_Weight_Unit");
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<CcAllowedMargin>(e =>
        {
            e.ToTable("CC_ALLOWED_MARGINS");
            e.HasKey(x => x.AmId);
            e.Property(x => x.AmId).HasColumnName("AM_Id");
            e.Property(x => x.AmItemCode).HasColumnName("AM_Item_Code").HasMaxLength(20);
            e.Property(x => x.AmCommodity).HasColumnName("AM_Commodity").HasMaxLength(20);
            e.Property(x => x.AmDescription).HasColumnName("AM_Description").HasMaxLength(200);
            e.Property(x => x.AmMinMargin).HasColumnName("AM_Min_Margin");
            e.Property(x => x.AmMaxMargin).HasColumnName("AM_Max_Margin");
            e.Property(x => x.AmDefMargin).HasColumnName("AM_Def_Margin");
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<CcInlandTariff>(e =>
        {
            e.ToTable("CC_INLAND_TARIFFS");
            e.HasKey(x => x.ItId);
            e.Property(x => x.ItId).HasColumnName("IT_Id");
            e.Property(x => x.ItHsCode).HasColumnName("IT_HS_Code").HasMaxLength(20);
            e.Property(x => x.ItDescription).HasColumnName("IT_Description").HasMaxLength(200);
            e.Property(x => x.ItRate).HasColumnName("IT_Rate");
            e.Property(x => x.IsActive).HasColumnName("IS_Active");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        modelBuilder.Entity<CcShipCharge>(e =>
        {
            e.ToTable("CC_SHIP_CHARGES");
            e.HasKey(x => x.ScId);
            e.Property(x => x.ScId).HasColumnName("SC_Id");
            e.Property(x => x.ScCalcNumber).HasColumnName("SC_Calc_Number");
            e.Property(x => x.ScChargeCode).HasColumnName("SC_Charge_Code").HasMaxLength(20);
            e.Property(x => x.ScDescription).HasColumnName("SC_Description").HasMaxLength(200);
            e.Property(x => x.ScAmount).HasColumnName("SC_Amount");
            e.Property(x => x.ScCurrency).HasColumnName("SC_Currency").HasMaxLength(3);
            e.Property(x => x.ScRate).HasColumnName("SC_Rate");
            e.Property(x => x.CreatedAt).HasColumnName("Created_At");
        });

        // ── Tracking Orders ────────────────────────────────────────────────────
        modelBuilder.Entity<TrackingOrder>(e =>
        {
            e.ToTable("TRACKING_ORDERS");
            e.HasKey(x => x.TrId);
            e.Property(x => x.TrId).HasColumnName("TR_Id");
            e.Property(x => x.TrPoNo).HasColumnName("TR_PoNo").HasMaxLength(20);
            e.Property(x => x.TrWarehouse).HasColumnName("TR_Warehouse").HasMaxLength(5);
            e.Property(x => x.TrSupplier).HasColumnName("TR_Supplier").HasMaxLength(10);
            e.Property(x => x.TrSupplierName).HasColumnName("TR_Supplier_Name").HasMaxLength(50);
            e.Property(x => x.TrCountry).HasColumnName("TR_Country").HasMaxLength(3);
            e.Property(x => x.TrFreightForwarder).HasColumnName("TR_Freight_Forwarder").HasMaxLength(50);
            e.Property(x => x.TrOrderDate).HasColumnName("TR_Order_Date");
            e.Property(x => x.TrVipShipDate).HasColumnName("TR_Vip_Ship_Date");
            e.Property(x => x.TrVipArrivalDate).HasColumnName("TR_Vip_Arrival_Date");
            e.Property(x => x.TrTotalCases).HasColumnName("TR_Total_Cases");
            e.Property(x => x.TrVipWeight).HasColumnName("TR_Vip_Weight");
            e.Property(x => x.TrVipLiters).HasColumnName("TR_Vip_Liters");
            e.Property(x => x.TrVipTotalAmount).HasColumnName("TR_Vip_Total_Amount");
            e.Property(x => x.TrVipTotalLines).HasColumnName("TR_Vip_Total_Lines");
            e.Property(x => x.TrVipStatus).HasColumnName("TR_Vip_Status").HasMaxLength(2);
            e.Property(x => x.TrSupplierCode).HasColumnName("TR_Supplier_Code").HasMaxLength(2);
            e.Property(x => x.TrVendorBrand).HasColumnName("TR_Vendor_Brand").HasMaxLength(100);
            e.Property(x => x.TrStatusCode).HasColumnName("TR_Status_Code").HasMaxLength(10);
            e.Property(x => x.TrBorw).HasColumnName("TR_Borw").HasMaxLength(1);
            e.Property(x => x.TrComments).HasColumnName("TR_Comments").HasMaxLength(500);
            e.Property(x => x.TrLastUpdateDate).HasColumnName("TR_Last_Update_Date");
            e.Property(x => x.TrRequestedEta).HasColumnName("TR_Requested_ETA");
            e.Property(x => x.TrAcknowledgeOrder).HasColumnName("TR_Acknowledge_Order");
            e.Property(x => x.TrDateLoadingShipper).HasColumnName("TR_Date_Loading_Shipper");
            e.Property(x => x.TrShippingLine).HasColumnName("TR_Shipping_Line").HasMaxLength(50);
            e.Property(x => x.TrShippingAgent).HasColumnName("TR_Shipping_Agent").HasMaxLength(50);
            e.Property(x => x.TrVessel).HasColumnName("TR_Vessel").HasMaxLength(50);
            e.Property(x => x.TrContainerNumber).HasColumnName("TR_Container_Number").HasMaxLength(20);
            e.Property(x => x.TrConsolidationRef).HasColumnName("TR_Consolidation_Ref").HasMaxLength(50);
            e.Property(x => x.TrContainerSize).HasColumnName("TR_Container_Size").HasMaxLength(10);
            e.Property(x => x.TrDateProFormaReceived).HasColumnName("TR_Date_ProForma_Received");
            e.Property(x => x.TrQtyProForma).HasColumnName("TR_Qty_ProForma");
            e.Property(x => x.TrFactoryReadyDate).HasColumnName("TR_Factory_Ready_Date");
            e.Property(x => x.TrEstDepartureDate).HasColumnName("TR_Est_Departure_Date");
            e.Property(x => x.TrEstArrivalDate).HasColumnName("TR_Est_Arrival_Date");
            e.Property(x => x.TrTransitTime).HasColumnName("TR_Transit_Time").HasMaxLength(20);
            e.Property(x => x.TrBijlageDone).HasColumnName("TR_Bijlage_Done");
            e.Property(x => x.TrDateArrivalInvoice).HasColumnName("TR_Date_Arrival_Invoice");
            e.Property(x => x.TrInvoiceNumber).HasColumnName("TR_Invoice_Number").HasMaxLength(20);
            e.Property(x => x.TrDateArrivalBol).HasColumnName("TR_Date_Arrival_Bol");
            e.Property(x => x.TrRemarks).HasColumnName("TR_Remarks").HasMaxLength(500);
            e.Property(x => x.TrDateArrivalNoteReceived).HasColumnName("TR_Date_Arrival_Note_Received");
            e.Property(x => x.TrDateManifestReceived).HasColumnName("TR_Date_Manifest_Received");
            e.Property(x => x.TrDateCopiesToDeclarant).HasColumnName("TR_Date_Copies_Declarant");
            e.Property(x => x.TrDateCustomsPapersReady).HasColumnName("TR_Date_Customs_Papers_Ready");
            e.Property(x => x.TrDateCustomsPapersAsycuda).HasColumnName("TR_Date_Customs_Papers_Asycuda");
            e.Property(x => x.TrDateContainerAtCps).HasColumnName("TR_Date_Container_At_CPS");
            e.Property(x => x.TrExpirationDateCps).HasColumnName("TR_Expiration_Date_CPS");
            e.Property(x => x.TrDateCustomsPapersToCps).HasColumnName("TR_Date_Customs_Papers_CPS");
            e.Property(x => x.TrDateContainerArrivedLicores).HasColumnName("TR_Date_Container_Arrived");
            e.Property(x => x.TrDateContainerOpenedCustoms).HasColumnName("TR_Date_Container_Opened");
            e.Property(x => x.TrDateContainerUnloadReady).HasColumnName("TR_Date_Unload_Ready");
            e.Property(x => x.TrReturnDateContainer).HasColumnName("TR_Return_Date_Container");
            e.Property(x => x.TrDateUnloadPapersAdmin).HasColumnName("TR_Date_Unload_Papers_Admin");
            e.Property(x => x.TrSadNumber).HasColumnName("TR_SAD_Number").HasMaxLength(20);
            e.Property(x => x.TrBcNumberOrders).HasColumnName("TR_BC_Number_Orders").HasMaxLength(20);
            e.Property(x => x.TrExitNoteNumber).HasColumnName("TR_Exit_Note_Number").HasMaxLength(20);
            e.Property(x => x.TrIssuesComments).HasColumnName("TR_Issues_Comments").HasMaxLength(1000);
            e.Property(x => x.TrReceiptStatus).HasColumnName("TR_Receipt_Status").HasMaxLength(10);
            e.Property(x => x.TrQtyShortage).HasColumnName("TR_Qty_Shortage");
            e.Property(x => x.TrQtyDamages).HasColumnName("TR_Qty_Damages");
            e.Property(x => x.TrReceiptComments).HasColumnName("TR_Receipt_Comments").HasMaxLength(500);
            e.Property(x => x.TrActualDeliveryDate).HasColumnName("TR_Actual_Delivery_Date");
            e.Property(x => x.TrIsClosed).HasColumnName("TR_Is_Closed");
            e.Property(x => x.TrClosedAt).HasColumnName("TR_Closed_At");
            e.Property(x => x.TrClosedBy).HasColumnName("TR_Closed_By").HasMaxLength(50);
            e.Property(x => x.TrCreatedBy).HasColumnName("TR_Created_By").HasMaxLength(50);
            e.Property(x => x.TrCreatedAt).HasColumnName("TR_Created_At");
            e.Property(x => x.TrUpdatedBy).HasColumnName("TR_Updated_By").HasMaxLength(50);
            e.Property(x => x.TrUpdatedAt).HasColumnName("TR_Updated_At");
            e.HasMany(x => x.StatusHistory).WithOne(x => x.TrackingOrder).HasForeignKey(x => x.TshTrackingId);
        });

        modelBuilder.Entity<TrackingStatusHistory>(e =>
        {
            e.ToTable("TRACKING_STATUS_HISTORY");
            e.HasKey(x => x.TshId);
            e.Property(x => x.TshId).HasColumnName("TSH_Id");
            e.Property(x => x.TshTrackingId).HasColumnName("TSH_Tracking_Id");
            e.Property(x => x.TshPoNo).HasColumnName("TSH_PoNo").HasMaxLength(20);
            e.Property(x => x.TshStatusCode).HasColumnName("TSH_Status_Code").HasMaxLength(10);
            e.Property(x => x.TshStatusDate).HasColumnName("TSH_Status_Date");
            e.Property(x => x.TshComments).HasColumnName("TSH_Comments").HasMaxLength(500);
            e.Property(x => x.TshChangedBy).HasColumnName("TSH_Changed_By").HasMaxLength(50);
        });

        // ── MODULE 4: Route Assignment ─────────────────────────────────────────
        modelBuilder.Entity<RouteCustomerExt>(e =>
        {
            e.ToTable("ROUTE_CUSTOMER_EXT");
            e.HasKey(x => x.RceId);
            e.Property(x => x.RceId).HasColumnName("RceId");
            e.Property(x => x.RceAccountNumber).HasColumnName("RceAccountNumber").HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.RceAccountNumber).IsUnique();
            e.Property(x => x.RceRouteNpActive).HasColumnName("RceRouteNpActive").HasMaxLength(20);
            e.Property(x => x.RceRouteOvd5).HasColumnName("RceRouteOvd5").HasMaxLength(20);
            e.Property(x => x.RceRouteOvd6).HasColumnName("RceRouteOvd6").HasMaxLength(20);
            e.Property(x => x.RcePareto1Overall).HasColumnName("RcePareto1Overall").HasMaxLength(10);
            e.Property(x => x.RcePareto2Overall).HasColumnName("RcePareto2Overall").HasMaxLength(10);
            e.Property(x => x.RceParetoOthersOverall).HasColumnName("RceParetoOthersOverall").HasMaxLength(10);
            e.Property(x => x.RcePareto1Beer).HasColumnName("RcePareto1Beer").HasMaxLength(10);
            e.Property(x => x.RcePareto2Beer).HasColumnName("RcePareto2Beer").HasMaxLength(10);
            e.Property(x => x.RceParetoOthersBeer).HasColumnName("RceParetoOthersBeer").HasMaxLength(10);
            e.Property(x => x.RcePareto1Water).HasColumnName("RcePareto1Water").HasMaxLength(10);
            e.Property(x => x.RcePareto2Water).HasColumnName("RcePareto2Water").HasMaxLength(10);
            e.Property(x => x.RceParetoOthersWater).HasColumnName("RceParetoOthersWater").HasMaxLength(10);
            e.Property(x => x.RcePareto1Others).HasColumnName("RcePareto1Others").HasMaxLength(10);
            e.Property(x => x.RcePareto2Others).HasColumnName("RcePareto2Others").HasMaxLength(10);
            e.Property(x => x.RceParetoOthersOthers).HasColumnName("RceParetoOthersOthers").HasMaxLength(10);
            e.Property(x => x.RceProyection).HasColumnName("RceProyection").HasMaxLength(50);
            e.Property(x => x.RceSalesRepActive4).HasColumnName("RceSalesRepActive4").HasMaxLength(20);
            e.Property(x => x.RceSalesRepActive5).HasColumnName("RceSalesRepActive5").HasMaxLength(20);
            e.Property(x => x.RceSalesRepActive6).HasColumnName("RceSalesRepActive6").HasMaxLength(20);
            e.Property(x => x.RceAlternativeSalesRep).HasColumnName("RceAlternativeSalesRep").HasMaxLength(20);
            e.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
        });

        modelBuilder.Entity<RouteProductExt>(e =>
        {
            e.ToTable("ROUTE_PRODUCT_EXT");
            e.HasKey(x => x.RpeId);
            e.Property(x => x.RpeId).HasColumnName("RpeId");
            e.Property(x => x.RpeItemCode).HasColumnName("RpeItemCode").HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.RpeItemCode).IsUnique();
            e.Property(x => x.RpeGroupCodeBeerWaterOthers).HasColumnName("RpeGroupCodeBeerWaterOthers").HasMaxLength(20);
            e.Property(x => x.RpeGroupCodeBrandSpecific).HasColumnName("RpeGroupCodeBrandSpecific").HasMaxLength(20);
            e.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
        });

        // ── MODULE 5: Stock Analysis ───────────────────────────────────────────
        modelBuilder.Entity<StockIdealMonths>(e =>
        {
            e.ToTable("STOCK_IDEAL_MONTHS");
            e.HasKey(x => x.SimId);
            e.Property(x => x.SimId).HasColumnName("SimId");
            e.Property(x => x.SimItemCode).HasColumnName("SimItemCode").HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.SimItemCode).IsUnique();
            e.Property(x => x.SimIdealMonths).HasColumnName("SimIdealMonths").HasColumnType("decimal(10,2)");
            e.Property(x => x.SimOrderFreq).HasColumnName("SimOrderFreq").HasMaxLength(20);
            e.Property(x => x.SimStockStartDate).HasColumnName("SimStockStartDate");
            e.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
        });

        modelBuilder.Entity<StockVendorConstraints>(e =>
        {
            e.ToTable("STOCK_VENDOR_CONSTRAINTS");
            e.HasKey(x => x.SvcId);
            e.Property(x => x.SvcId).HasColumnName("SvcId");
            e.Property(x => x.SvcFromLocationCode).HasColumnName("SvcFromLocationCode").HasMaxLength(20);
            e.Property(x => x.SvcFromLocationName).HasColumnName("SvcFromLocationName").HasMaxLength(100);
            e.Property(x => x.SvcToLocationCode).HasColumnName("SvcToLocationCode").HasMaxLength(20);
            e.Property(x => x.SvcToLocationName).HasColumnName("SvcToLocationName").HasMaxLength(100);
            e.Property(x => x.SvcShipperCode).HasColumnName("SvcShipperCode").HasMaxLength(20);
            e.Property(x => x.SvcOrderReviewDay).HasColumnName("SvcOrderReviewDay").HasMaxLength(20);
            e.Property(x => x.SvcSupplierLeadDays).HasColumnName("SvcSupplierLeadDays");
            e.Property(x => x.SvcTransitDays).HasColumnName("SvcTransitDays");
            e.Property(x => x.SvcWarehouseProcessDays).HasColumnName("SvcWarehouseProcessDays");
            e.Property(x => x.SvcSafetyDays).HasColumnName("SvcSafetyDays");
            e.Property(x => x.SvcOrderCycleDays).HasColumnName("SvcOrderCycleDays");
            e.Property(x => x.SvcMinOrderQty).HasColumnName("SvcMinOrderQty").HasColumnType("decimal(18,4)");
            e.Property(x => x.SvcOrderIncrement).HasColumnName("SvcOrderIncrement").HasColumnType("decimal(18,4)");
            e.Property(x => x.SvcMinTotalCaseOrder).HasColumnName("SvcMinTotalCaseOrder").HasColumnType("decimal(18,4)");
            e.Property(x => x.SvcPurchaserName).HasColumnName("SvcPurchaserName").HasMaxLength(100);
            e.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
        });

        modelBuilder.Entity<StockSalesBudget>(e =>
        {
            e.ToTable("STOCK_SALES_BUDGET");
            e.HasKey(x => x.SsbId);
            e.Property(x => x.SsbId).HasColumnName("SsbId");
            e.Property(x => x.SsbYear).HasColumnName("SsbYear").IsRequired();
            e.Property(x => x.SsbMonth).HasColumnName("SsbMonth").IsRequired();
            e.Property(x => x.SsbItemCode).HasColumnName("SsbItemCode").HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.SsbYear, x.SsbMonth, x.SsbItemCode }).IsUnique();
            e.Property(x => x.SsbItemDesc).HasColumnName("SsbItemDesc").HasMaxLength(100);
            e.Property(x => x.SsbBudgetedUnits).HasColumnName("SsbBudgetedUnits").HasColumnType("decimal(18,4)");
            e.Property(x => x.SsbBudgetedSales).HasColumnName("SsbBudgetedSales").HasColumnType("decimal(18,4)");
            e.Property(x => x.SsbBudgetedDiscount).HasColumnName("SsbBudgetedDiscount").HasColumnType("decimal(18,4)");
            e.Property(x => x.SsbBudgetedMargin).HasColumnName("SsbBudgetedMargin").HasColumnType("decimal(18,4)");
            e.Property(x => x.SsbBudgetedGross).HasColumnName("SsbBudgetedGross").HasColumnType("decimal(18,4)");
            e.Property(x => x.SsbBudgetedCost).HasColumnName("SsbBudgetedCost").HasColumnType("decimal(18,4)");
        });

        modelBuilder.Entity<StockAnalysisResult>(e =>
        {
            e.ToTable("STOCK_ANALYSIS_RESULT");
            e.HasKey(x => x.SarId);
            e.Property(x => x.SarId).HasColumnName("SarId");
            e.Property(x => x.SarYear).HasColumnName("SarYear").IsRequired();
            e.Property(x => x.SarMonth).HasColumnName("SarMonth").IsRequired();
            e.Property(x => x.SarItemCode).HasColumnName("SarItemCode").HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.SarYear, x.SarMonth, x.SarItemCode }).IsUnique();
            e.Property(x => x.SarItemDesc).HasColumnName("SarItemDesc").HasMaxLength(100);
            e.Property(x => x.SarProductClassId).HasColumnName("SarProductClassId").HasMaxLength(10);
            e.Property(x => x.SarProductClassDesc).HasColumnName("SarProductClassDesc").HasMaxLength(100);
            e.Property(x => x.SarSupplierCode).HasColumnName("SarSupplierCode").HasMaxLength(20);
            e.Property(x => x.SarSupplierName).HasColumnName("SarSupplierName").HasMaxLength(100);
            e.Property(x => x.SarBrandCode).HasColumnName("SarBrandCode").HasMaxLength(20);
            e.Property(x => x.SarBrandDesc).HasColumnName("SarBrandDesc").HasMaxLength(100);
            e.Property(x => x.SarStockStartDate).HasColumnName("SarStockStartDate");
            e.Property(x => x.SarOrderFrequency).HasColumnName("SarOrderFrequency").HasMaxLength(20);
            e.Property(x => x.SarIdealMonthsOfStock).HasColumnName("SarIdealMonthsOfStock").HasColumnType("decimal(10,2)");
            e.Property(x => x.SarOh11010).HasColumnName("SarOh11010").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOh11020).HasColumnName("SarOh11020").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOh11060).HasColumnName("SarOh11060").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarCurrentOhUnits).HasColumnName("SarCurrentOhUnits").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOnOrder11010).HasColumnName("SarOnOrder11010").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOnOrder11020).HasColumnName("SarOnOrder11020").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOnOrder11060).HasColumnName("SarOnOrder11060").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOnOrderUnits).HasColumnName("SarOnOrderUnits").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOnOrderEta).HasColumnName("SarOnOrderEta");
            e.Property(x => x.SarYtdSalesUnits).HasColumnName("SarYtdSalesUnits").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarMonthlySalesUnits).HasColumnName("SarMonthlySalesUnits").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarIdealStockUnits).HasColumnName("SarIdealStockUnits").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOverstockUnits).HasColumnName("SarOverstockUnits").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOverstockUnitsInclOrders).HasColumnName("SarOverstockUnitsInclOrders").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarMonthsOfStock).HasColumnName("SarMonthsOfStock").HasColumnType("decimal(10,4)");
            e.Property(x => x.SarYearsOfStock).HasColumnName("SarYearsOfStock").HasColumnType("decimal(10,4)");
            e.Property(x => x.SarMonthsOfStockInclOnOrder).HasColumnName("SarMonthsOfStockInclOnOrder").HasColumnType("decimal(10,4)");
            e.Property(x => x.SarMonthsOfOverstock).HasColumnName("SarMonthsOfOverstock").HasColumnType("decimal(10,4)");
            e.Property(x => x.SarMonthsOfOverstockInclOnOrder).HasColumnName("SarMonthsOfOverstockInclOnOrder").HasColumnType("decimal(10,4)");
            e.Property(x => x.SarTotalBudgetUnits).HasColumnName("SarTotalBudgetUnits").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarYtdBudgetUnits).HasColumnName("SarYtdBudgetUnits").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarTotalBudgetSales).HasColumnName("SarTotalBudgetSales").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarYtdBudgetSales).HasColumnName("SarYtdBudgetSales").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarTotalBudgetCost).HasColumnName("SarTotalBudgetCost").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarYtdBudgetCost).HasColumnName("SarYtdBudgetCost").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOverUnderPerformanceUnits).HasColumnName("SarOverUnderPerformanceUnits").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarInventoryValue).HasColumnName("SarInventoryValue").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarInventoryValueOnOrder).HasColumnName("SarInventoryValueOnOrder").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarTotalInventoryValue).HasColumnName("SarTotalInventoryValue").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarAvgCostPerCase).HasColumnName("SarAvgCostPerCase").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarIdealStockAng).HasColumnName("SarIdealStockAng").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarBudgetedIdealStockAng).HasColumnName("SarBudgetedIdealStockAng").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOverstockAng).HasColumnName("SarOverstockAng").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOverstockAngInclOrder).HasColumnName("SarOverstockAngInclOrder").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarExpectedMonthlySalesAng).HasColumnName("SarExpectedMonthlySalesAng").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarMonthsOfStockInclOrderOnValue).HasColumnName("SarMonthsOfStockInclOrderOnValue").HasColumnType("decimal(10,4)");
            e.Property(x => x.SarMonthsOfOverstockInclOrderOnValue).HasColumnName("SarMonthsOfOverstockInclOrderOnValue").HasColumnType("decimal(10,4)");
            e.Property(x => x.SarDailyRateOfSale).HasColumnName("SarDailyRateOfSale").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarLastReceiptDate).HasColumnName("SarLastReceiptDate");
            e.Property(x => x.SarQtyLastReceipt).HasColumnName("SarQtyLastReceipt").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarDaysBeforeArrivalOrder).HasColumnName("SarDaysBeforeArrivalOrder");
            e.Property(x => x.SarMonthsBeforeArrivalOrder).HasColumnName("SarMonthsBeforeArrivalOrder").HasColumnType("decimal(10,4)");
            e.Property(x => x.SarUnitSalesBeforeArrivalOrder).HasColumnName("SarUnitSalesBeforeArrivalOrder").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarTotalOhAtArrivalOrder).HasColumnName("SarTotalOhAtArrivalOrder").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarOverstockAtArrivalOrder).HasColumnName("SarOverstockAtArrivalOrder").HasColumnType("decimal(18,4)");
            e.Property(x => x.SarTotalMonthsBeforeIdealStock).HasColumnName("SarTotalMonthsBeforeIdealStock").HasColumnType("decimal(10,4)");
            e.Property(x => x.SarGeneratedAt).HasColumnName("SarGeneratedAt");
        });

        modelBuilder.Entity<RouteBudget>(e =>
        {
            e.ToTable("ROUTE_BUDGET");
            e.HasKey(x => x.RbId);
            e.Property(x => x.RbId).HasColumnName("RbId");
            e.Property(x => x.RbYear).HasColumnName("RbYear").IsRequired();
            e.Property(x => x.RbAccountNumber).HasColumnName("RbAccountNumber").HasMaxLength(20).IsRequired();
            e.Property(x => x.RbItemCode).HasColumnName("RbItemCode").HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.RbYear, x.RbAccountNumber, x.RbItemCode }).IsUnique();
            e.Property(x => x.RbQty01).HasColumnName("RbQty01").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty02).HasColumnName("RbQty02").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty03).HasColumnName("RbQty03").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty04).HasColumnName("RbQty04").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty05).HasColumnName("RbQty05").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty06).HasColumnName("RbQty06").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty07).HasColumnName("RbQty07").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty08).HasColumnName("RbQty08").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty09).HasColumnName("RbQty09").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty10).HasColumnName("RbQty10").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty11).HasColumnName("RbQty11").HasColumnType("decimal(18,4)");
            e.Property(x => x.RbQty12).HasColumnName("RbQty12").HasColumnType("decimal(18,4)");
        });

        // ── Module Approver Emails ─────────────────────────────────────────────
        modelBuilder.Entity<ModuleApproverEmail>(e =>
        {
            e.ToTable("MODULE_APPROVER_EMAILS");
            e.HasKey(x => x.MaeId);
            e.Property(x => x.MaeId).HasColumnName("Mae_Id");
            e.Property(x => x.MaeModuleKey).HasColumnName("Mae_ModuleKey").HasMaxLength(50).IsRequired();
            e.Property(x => x.MaeModuleName).HasColumnName("Mae_ModuleName").HasMaxLength(100).IsRequired();
            e.Property(x => x.MaeEmails).HasColumnName("Mae_Emails");
            e.Property(x => x.MaeUpdatedAt).HasColumnName("Mae_UpdatedAt");
            e.Property(x => x.MaeUpdatedBy).HasColumnName("Mae_UpdatedBy").HasMaxLength(100);
        });

        // ── Company Settings ───────────────────────────────────────────────────
        modelBuilder.Entity<CompanySettings>(e =>
        {
            e.ToTable("COMPANY_SETTINGS");
            e.HasKey(x => x.CsId);
            e.Property(x => x.CsId).HasColumnName("CS_Id");
            e.Property(x => x.CsCompanyName).HasColumnName("CS_CompanyName").HasMaxLength(200).IsRequired();
            e.Property(x => x.CsLegalName).HasColumnName("CS_LegalName").HasMaxLength(200);
            e.Property(x => x.CsTagline).HasColumnName("CS_Tagline").HasMaxLength(300);
            e.Property(x => x.CsRnc).HasColumnName("CS_RNC").HasMaxLength(50);
            e.Property(x => x.CsAddress).HasColumnName("CS_Address").HasMaxLength(400);
            e.Property(x => x.CsCity).HasColumnName("CS_City").HasMaxLength(100);
            e.Property(x => x.CsCountry).HasColumnName("CS_Country").HasMaxLength(100);
            e.Property(x => x.CsPhone).HasColumnName("CS_Phone").HasMaxLength(50);
            e.Property(x => x.CsPhone2).HasColumnName("CS_Phone2").HasMaxLength(50);
            e.Property(x => x.CsEmail).HasColumnName("CS_Email").HasMaxLength(200);
            e.Property(x => x.CsWebsite).HasColumnName("CS_Website").HasMaxLength(200);
            e.Property(x => x.CsLogoUrl).HasColumnName("CS_LogoUrl").HasMaxLength(500);
            e.Property(x => x.CsUpdatedAt).HasColumnName("CS_UpdatedAt");
            e.Property(x => x.CsUpdatedBy).HasColumnName("CS_UpdatedBy").HasMaxLength(100);
        });
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Entity class definitions (inline for single-file DbContext)
// ═══════════════════════════════════════════════════════════════════════════════

// ── System ─────────────────────────────────────────────────────────────────────
public class ModuleApproverEmail
{
    public int      MaeId         { get; set; }
    public string   MaeModuleKey  { get; set; } = string.Empty;
    public string   MaeModuleName { get; set; } = string.Empty;
    public string   MaeEmails     { get; set; } = string.Empty;
    public DateTime? MaeUpdatedAt { get; set; }
    public string?  MaeUpdatedBy  { get; set; }
}

public class CompanySettings
{
    public int      CsId          { get; set; } = 1;
    public string   CsCompanyName { get; set; } = "Licores Maduro";
    public string?  CsLegalName   { get; set; }
    public string?  CsTagline     { get; set; }
    public string?  CsRnc         { get; set; }
    public string?  CsAddress     { get; set; }
    public string?  CsCity        { get; set; }
    public string?  CsCountry     { get; set; }
    public string?  CsPhone       { get; set; }
    public string?  CsPhone2      { get; set; }
    public string?  CsEmail       { get; set; }
    public string?  CsWebsite     { get; set; }
    public string?  CsLogoUrl     { get; set; }
    public DateTime? CsUpdatedAt  { get; set; }
    public string?  CsUpdatedBy   { get; set; }
}

// ── Tracking ───────────────────────────────────────────────────────────────────
public class OrderStatus { public int OsId { get; set; } public string OsCode { get; set; } = string.Empty; public string OsDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class TrackingContainerType { public int TctId { get; set; } public string TctCode { get; set; } = string.Empty; public string TctDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }

// ── Freight Forwarder ──────────────────────────────────────────────────────────
public class Currency { public int CurId { get; set; } public string CurCode { get; set; } = string.Empty; public string CurDescription { get; set; } = string.Empty; public double? CurBnkPurchaseRate { get; set; } public double? CurCustomsRate { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class LoadType { public int LtId { get; set; } public string LtCode { get; set; } = string.Empty; public string LtDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class PortOfLoading { public int PlId { get; set; } public string PlCode { get; set; } = string.Empty; public string PlName { get; set; } = string.Empty; public string? PlCountry { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ShippingLine { public int SlId { get; set; } public string SlCode { get; set; } = string.Empty; public string SlName { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ShippingAgent { public int SaId { get; set; } public string SaCode { get; set; } = string.Empty; public string SaName { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Route { public int RouId { get; set; } public string RouCode { get; set; } = string.Empty; public string RouDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ContainerSpec { public int CsId { get; set; } public string CsCode { get; set; } = string.Empty; public string CsDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ContainerType { public int CtId { get; set; } public string CtCode { get; set; } = string.Empty; public string CtDescription { get; set; } = string.Empty; public string? CtContainerSpecs { get; set; } public int? CtCases { get; set; } public int? CtWghtKilogram { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class RouteByShippingAgent { public int RsaId { get; set; } public string RsaPort { get; set; } = string.Empty; public string RsaShippingAgent { get; set; } = string.Empty; public string RsaRoute { get; set; } = string.Empty; public short? RsaDays { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class OceanFreightChargeType { public int OfctId { get; set; } public string OfctCode { get; set; } = string.Empty; public string OfctDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class InlandFreightChargeType { public int IfctId { get; set; } public string IfctCode { get; set; } = string.Empty; public string IfctDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Region { public int RegId { get; set; } public string RegCode { get; set; } = string.Empty; public string RegName { get; set; } = string.Empty; public string? RegCountry { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class LclChargeType { public int LctId { get; set; } public string LctCode { get; set; } = string.Empty; public string LctDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class PriceType { public int PtId { get; set; } public string PtCode { get; set; } = string.Empty; public string PtDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class AmountType { public int AtId { get; set; } public string AtCode { get; set; } = string.Empty; public string AtDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ChargeAction { public int CaId { get; set; } public string CaCode { get; set; } = string.Empty; public string CaDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ChargeOver { public int CoId { get; set; } public string CoCode { get; set; } = string.Empty; public string CoDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ChargePer { public int CpId { get; set; } public string CpCode { get; set; } = string.Empty; public string CpDescription { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }

// ── Activity Request ───────────────────────────────────────────────────────────
public class ActivityType { public int AtId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool ActivityRelated { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class BudgetActivity { public int BaId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string? BillingDescr { get; set; } public short? DisplaySeq { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatAddSpec { public int CasId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatApparelType { public int CatId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatBagSpec { public int CbsId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatBottle { public int CbId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatBrandSpecific { public int CbrId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatClothingType { public int CctId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatColor { public int CcId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatContent { public int CcoId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatCoolerCapacity { public int CccId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatCoolerModel { public int CcmId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatCoolerType { public int CctyId { get; set; } public string? CatPrefix { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatFileName { public int CfnId { get; set; } public string FileNames { get; set; } = string.Empty; public string? DisplayText { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatGender { public int CgId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatGlassServing { public int CgsId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatInsurrance { public int CiId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatLed { public int ClId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatMaintMonth { public int CmmId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatMaterial { public int CmId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatShape { public int Cs2Id { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatSize { public int CszId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CatVapType { public int CvtId { get; set; } public string CatSel { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CustomerNonClient { public int CncId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CustomerSalesGroup { public int CsgId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CustomerSegmentInfo { public int CsiId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CustomerTargetGroup { public int CtgId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class DenialReason { public int DrId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class EntertainmentType { public int EtId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class FacilitatorInfo { public int FiId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string? Email { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class FiscalYear { public int FyId { get; set; } public int FyYear { get; set; } public DateOnly FyStartDate { get; set; } public DateOnly FyEndDate { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class LicoresGroup { public int LgId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class LocationInfo { public int LiId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string? Address { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class PosCategory { public int PcId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class PosLendGive { public int PlgId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class PosMaterialsStatus { public int PmsId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class PosMaterial
{
    public int     PmId              { get; set; }
    public string  PmCode            { get; set; } = string.Empty;
    public string  PmName            { get; set; } = string.Empty;
    public string? PmCategoryCode    { get; set; }
    public string? PmCategoryDesc    { get; set; }
    public string? PmDescription     { get; set; }
    public string? PmUnit            { get; set; }
    public int     PmStockTotal      { get; set; } = 0;
    public int     PmStockAvailable  { get; set; } = 0;
    public string? PmNotes           { get; set; }
    public bool    IsActive          { get; set; } = true;
    public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
}
public class ActivityRqCash      { public int ArcId { get; set; } public int ArcArId { get; set; } public string? ArcType { get; set; } public decimal? ArcAmount { get; set; } public string? ArcReference { get; set; } public string? ArcNotes { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ActivityRqPrint     { public int ArprId { get; set; } public int ArprArId { get; set; } public string? ArprPublication { get; set; } public string? ArprFormat { get; set; } public string? ArprSize { get; set; } public decimal? ArprCost { get; set; } public string? ArprNotes { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ActivityRqRadio     { public int ArrId { get; set; } public int ArrArId { get; set; } public string? ArrStation { get; set; } public string? ArrDuration { get; set; } public int? ArrFrequency { get; set; } public decimal? ArrCost { get; set; } public string? ArrNotes { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ActivityRqPosMat    { public int ArpmId { get; set; } public int ArpmArId { get; set; } public string? ArpmCode { get; set; } public string? ArpmName { get; set; } public int? ArpmQuantity { get; set; } public string? ArpmUnit { get; set; } public string? ArpmNotes { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ActivityRqPromotion { public int ArpoId { get; set; } public int ArpoArId { get; set; } public string? ArpoType { get; set; } public string? ArpoDescription { get; set; } public decimal? ArpoCost { get; set; } public string? ArpoNotes { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class ActivityRqOther     { public int AroId { get; set; } public int AroArId { get; set; } public string? AroDescription { get; set; } public decimal? AroCost { get; set; } public string? AroNotes { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }

public class PosLendOut
{
    public int      PloId             { get; set; }
    public string   PloNumber         { get; set; } = string.Empty;
    public int      PloYear           { get; set; }
    public string   PloStatus         { get; set; } = "DRAFT";
    public DateOnly? PloDate          { get; set; }
    public DateOnly? PloExpectedReturn{ get; set; }
    public DateOnly? PloActualReturn  { get; set; }
    public string?  PloClientCode     { get; set; }
    public string?  PloClientName     { get; set; }
    public string?  PloContactName    { get; set; }
    public string?  PloContactPhone   { get; set; }
    public string?  PloNotes          { get; set; }
    public int?     PloCreatedById    { get; set; }
    public string?  PloCreatedByName  { get; set; }
    public bool     IsActive          { get; set; } = true;
    public DateTime CreatedAt         { get; set; } = DateTime.UtcNow;
}
public class PosLendOutItem
{
    public int     PloiId               { get; set; }
    public int     PloiPloId            { get; set; }
    public string? PloiPmCode           { get; set; }
    public string? PloiPmName           { get; set; }
    public int     PloiQuantityLent     { get; set; } = 0;
    public int     PloiQuantityReturned { get; set; } = 0;
    public string? PloiNotes            { get; set; }
    public bool    IsActive             { get; set; } = true;
    public DateTime CreatedAt           { get; set; } = DateTime.UtcNow;
}
public class SponsoringType { public int StId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class StatusCode { public int ScId { get; set; } public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class MarketingCalendar
{
    public int      McId           { get; set; }
    public int      McYear         { get; set; }
    public string?  McSupplierCode { get; set; }
    public string?  McSupplierName { get; set; }
    public string   McBrand        { get; set; } = string.Empty;
    public decimal? McBudget       { get; set; }
    public string?  McMonth1       { get; set; }
    public string?  McMonth2       { get; set; }
    public string?  McMonth3       { get; set; }
    public string?  McMonth4       { get; set; }
    public string?  McMonth5       { get; set; }
    public string?  McMonth6       { get; set; }
    public string?  McMonth7       { get; set; }
    public string?  McMonth8       { get; set; }
    public string?  McMonth9       { get; set; }
    public string?  McMonth10      { get; set; }
    public string?  McMonth11      { get; set; }
    public string?  McMonth12      { get; set; }
    public string?  McNotes        { get; set; }
    public bool     IsActive       { get; set; } = true;
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
}

public class ActivityRequestHeader
{
    public int      ArId                   { get; set; }
    public string   ArNumber               { get; set; } = string.Empty;
    public int      ArYear                 { get; set; }
    public string   ArStatus               { get; set; } = "DRAFT";
    public string?  ArSupplierCode         { get; set; }
    public string?  ArSupplierName         { get; set; }
    public string?  ArBrand                { get; set; }
    public string?  ArActivityTypeCode     { get; set; }
    public string?  ArActivityTypeDesc     { get; set; }
    public string?  ArDescription          { get; set; }
    public DateOnly? ArStartDate           { get; set; }
    public DateOnly? ArEndDate             { get; set; }
    public string?  ArLocationCode         { get; set; }
    public string?  ArLocationName         { get; set; }
    public decimal? ArBudget               { get; set; }
    public string?  ArSegmentCode          { get; set; }
    public string?  ArTargetGroupCode      { get; set; }
    public string?  ArSalesGroupCode       { get; set; }
    public string?  ArNonClientCode        { get; set; }
    public string?  ArNonClientName        { get; set; }
    public string?  ArFacilitatorCode      { get; set; }
    public string?  ArFacilitatorName      { get; set; }
    public string?  ArSponsoringTypeCode   { get; set; }
    public string?  ArEntertainmentTypeCode{ get; set; }
    public string?  ArNotes                { get; set; }
    public int?     ArCreatedBy            { get; set; }
    public string?  ArCreatedByName        { get; set; }
    public int?     ArApprovedBy           { get; set; }
    public string?  ArApprovedByName       { get; set; }
    public DateTime? ArApprovedAt          { get; set; }
    public int?     ArDeniedBy             { get; set; }
    public string?  ArDeniedByName         { get; set; }
    public DateTime? ArDeniedAt            { get; set; }
    public string?  ArDenialReason         { get; set; }
    public bool     IsActive               { get; set; } = true;
    public DateTime CreatedAt              { get; set; } = DateTime.UtcNow;
}

public class ActivityRqBrand
{
    public int      ArbId          { get; set; }
    public int      ArbArId        { get; set; }
    public string?  ArbSupplierCode{ get; set; }
    public string?  ArbSupplierName{ get; set; }
    public string?  ArbBrand       { get; set; }
    public decimal? ArbBudget      { get; set; }
    public string?  ArbNotes       { get; set; }
    public bool     IsActive       { get; set; } = true;
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
}

public class ActivityRqProduct
{
    public int      ArpId          { get; set; }
    public int      ArpArId        { get; set; }
    public string?  ArpProductCode { get; set; }
    public string?  ArpProductName { get; set; }
    public decimal? ArpQuantity    { get; set; }
    public string?  ArpUnit        { get; set; }
    public string?  ArpNotes       { get; set; }
    public bool     IsActive       { get; set; } = true;
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
}

// ── Aankoopbon ─────────────────────────────────────────────────────────────────
public class AbProduct { public int AbpId { get; set; } public string ItemKode { get; set; } = string.Empty; public string Omschrijving { get; set; } = string.Empty; public string? VendorCode { get; set; } public string? CostType { get; set; } public string? Eenheid { get; set; } public double? UnitQuantity { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Department { public int DpId { get; set; } public string DpName { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Eenheid { public int EeId { get; set; } public string UnitCode { get; set; } = string.Empty; public string Omschrijving { get; set; } = string.Empty; public double? OmrekenFaktor { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Receiver { public int RecId { get; set; } public string RecName { get; set; } = string.Empty; public string? RecIdDoc { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Requestor { public int ReqId { get; set; } public string ReqName { get; set; } = string.Empty; public string? ReqEmail { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class RequestorVendor { public int RvId { get; set; } public string RsRequestor { get; set; } = string.Empty; public string RsVendor { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CostType { public int CtId { get; set; } public string TcName { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class VehicleType { public int VtId { get; set; } public string VtName { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Vehicle { public int VhId { get; set; } public string VhLicense { get; set; } = string.Empty; public string? VhType { get; set; } public string? VhModel { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Vendor { public int VndId { get; set; } public string VndCode { get; set; } = string.Empty; public string VndName { get; set; } = string.Empty; public string? VndAddress1 { get; set; } public string? VndPhone1 { get; set; } public string? VndEmail { get; set; } public string? VndContact { get; set; } public string? VndCurr { get; set; } public string? VndCrib { get; set; } public string? VndKvk { get; set; } public bool VndCash { get; set; } = false; public bool VndQuoteMandatory { get; set; } = false; public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }

// ── Aankoopbon Orders ─────────────────────────────────────────────────────────
public class AbOrderHeader
{
    public int       AohId              { get; set; }
    public string    AohBonNr           { get; set; } = string.Empty;
    public string    AohStatus          { get; set; } = "DRAFT"; // DRAFT/PENDING/APPROVED/REJECTED/CLOSED
    public DateTime  AohOrderDate       { get; set; } = DateTime.UtcNow;
    public string?   AohRequestor       { get; set; }
    public int?      AohVendorId        { get; set; }
    public string?   AohVendorName      { get; set; }
    public string?   AohVendorAddress   { get; set; }
    public string?   AohDepartment      { get; set; }
    public string?   AohCostType        { get; set; }
    public string?   AohRemarks         { get; set; }
    public int?      AohVehicleId       { get; set; }
    public string?   AohVehicleLicense  { get; set; }
    public string?   AohVehicleType     { get; set; }
    public string?   AohVehicleModel    { get; set; }
    public string?   AohQuotationNr     { get; set; }
    public decimal?  AohAmount          { get; set; }
    public bool      AohMeegeven        { get; set; } = false;
    public bool      AohOntvangen       { get; set; } = false;
    public bool      AohZenden          { get; set; } = false;
    public bool      AohAndere          { get; set; } = false;
    public int?      AohReceiverId      { get; set; }
    public string?   AohReceiverName    { get; set; }
    public string?   AohReceiverIdDoc   { get; set; }
    public int?      AohApprovedBy      { get; set; }
    public string?   AohApprovedByName  { get; set; }
    public DateTime? AohApprovedAt      { get; set; }
    public int?      AohRejectedBy      { get; set; }
    public string?   AohRejectedByName  { get; set; }
    public DateTime? AohRejectedAt      { get; set; }
    public string?   AohRejectionReason { get; set; }
    public string?   AohQuotationPdfPath{ get; set; }
    public string?   AohInvoiceNr       { get; set; }
    public DateTime? AohInvoiceDate     { get; set; }
    public decimal?  AohInvoiceAmount   { get; set; }
    public int?      AohClosedBy        { get; set; }
    public string?   AohClosedByName    { get; set; }
    public DateTime? AohClosedAt        { get; set; }
    public int       AohCreatedBy       { get; set; }
    public string?   AohCreatedByName   { get; set; }
    public bool      IsActive           { get; set; } = true;
    public DateTime  CreatedAt          { get; set; } = DateTime.UtcNow;

    public ICollection<AbOrderDetail> Details { get; set; } = [];
}

public class AbOrderDetail
{
    public int      AodId          { get; set; }
    public int      AodHeaderId    { get; set; }
    public int      AodLineNr      { get; set; }
    public string?  AodProductCode     { get; set; }
    public string   AodProductDesc     { get; set; } = string.Empty;
    public string?  AodAdditionalDesc  { get; set; }
    public string?  AodCostType        { get; set; }
    public decimal  AodQuantity        { get; set; } = 1;
    public string?  AodUnit            { get; set; }
    public bool     IsActive           { get; set; } = true;
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;

    public AbOrderHeader? Header { get; set; }
}

// ── Cost Calculation ───────────────────────────────────────────────────────────
public class CcCalcHeader
{
    public int      CcCalcNumber    { get; set; }
    public DateTime CcCalcDate      { get; set; } = DateTime.UtcNow;
    public string?  CcForwarderCode { get; set; }
    public string?  CcForwarderName { get; set; }
    public string?  CcCurrCode      { get; set; }
    public decimal? CcCurrRate      { get; set; }
    public decimal? CcFreight       { get; set; }
    public decimal? CcTransport     { get; set; }
    public decimal? CcUnloading     { get; set; }
    public decimal? CcLocalHandling { get; set; }
    public decimal? CcTotWeight     { get; set; }
    public string   CcStatus        { get; set; } = "DR"; // DR=Draft, CF=Confirmed, AP=Approved
    public int?     CcTotOrd        { get; set; }
    public decimal? CcTotQty        { get; set; }
    public string?  CcWarehouse     { get; set; }
    public string?  CcCreatedBy     { get; set; }
    public DateTime CcCreatedAt     { get; set; } = DateTime.UtcNow;
    public ICollection<CcCalcPoHead> PoHeads { get; set; } = [];
}

public class CcCalcPoHead
{
    public int      CcphCalcNumber  { get; set; }
    public string   CcphLmPoNo      { get; set; } = string.Empty;
    public string?  CcphVendNo      { get; set; }
    public string?  CcphVendName    { get; set; }
    public string?  CcphWhse        { get; set; }
    public string?  CcphCurrCode    { get; set; }
    public decimal? CcphCurrRate    { get; set; }
    public decimal? CcphCurrRateCust { get; set; }
    public string?  CcphInvNumber   { get; set; }
    public DateTime? CcphInvDate    { get; set; }
    public decimal? CcphLocalHandling { get; set; }
    public decimal? CcphDuties      { get; set; }
    public decimal? CcphEconSurch   { get; set; }
    public decimal? CcphOb          { get; set; }
    public decimal? CcphWeight      { get; set; }
    public decimal? CcphFreight     { get; set; }
    public decimal? CcphTransport   { get; set; }
    public decimal? CcphUnloading   { get; set; }
    public decimal? CcphInsurance   { get; set; }
    public decimal? CcphTotQty      { get; set; }
    public decimal? CcphTotAmountFC { get; set; }
    public decimal? CcphTotAmount   { get; set; }
    public decimal? CcphInlandFreight  { get; set; }
    public decimal? CcphShipCharges    { get; set; }
    public decimal? CcphInlandTariff   { get; set; }
    public string   CcphStatus         { get; set; } = "DR";
    public string?  CcphCreatedBy   { get; set; }
    public string?  CcphConfirmedBy { get; set; }
    public string?  CcphApprovedBy  { get; set; }
    public CcCalcHeader? CalcHeader { get; set; }
    public ICollection<CcCalcPoDetail> Details { get; set; } = [];
}

public class CcCalcPoDetail
{
    public int      CcpdCalcNumber  { get; set; }
    public string   CcpdLmPoNo      { get; set; } = string.Empty;
    public string   CcpdItemNo      { get; set; } = string.Empty;
    public string?  CcpdItemDescr   { get; set; }
    public int?     CcpdUnitCase    { get; set; }
    public decimal? CcpdOrdQty      { get; set; }
    public decimal? CcpdFobPrice    { get; set; }
    public decimal? CcpdFobPriceTot { get; set; }
    public decimal? CcpdInlandFreight { get; set; }
    public decimal? CcpdFreight     { get; set; }
    public decimal? CcpdLocalHandl  { get; set; }
    public decimal  CcpdDuties       { get; set; } = 0;
    public decimal  CcpdEconSurch    { get; set; } = 0;
    public decimal  CcpdOb           { get; set; } = 0;
    public decimal? CcpdInlandTariff { get; set; }
    public decimal? CcpdShipCharges  { get; set; }
    public decimal? CcpdAllowedMin   { get; set; }
    public decimal? CcpdAllowedMax   { get; set; }
    public decimal? CcpdInsurance    { get; set; }
    public decimal? CcpdTransport   { get; set; }
    public decimal? CcpdUnloading   { get; set; }
    public decimal? CcpdFinalCost   { get; set; }
    public string?  CcpdWarehouse   { get; set; }
    public decimal? CcpdMarginPerc  { get; set; }
    public decimal? CcpdSellingPrice { get; set; }
    public CcCalcPoHead? PoHead     { get; set; }
}

public class CcTariffItem
{
    public int     TiId          { get; set; }
    public string  TiHsCode      { get; set; } = string.Empty;
    public string? TiDescription { get; set; }
    public decimal TiDutyRate    { get; set; } = 0;
    public decimal TiEconRate    { get; set; } = 0;
    public decimal TiObRate      { get; set; } = 0;
    public bool    IsActive      { get; set; } = true;
    public DateTime CreatedAt    { get; set; }
}

public class CcGoodsClassification
{
    public int     GcId        { get; set; }
    public string  GcItemCode  { get; set; } = string.Empty;
    public string? GcItemDescr { get; set; }
    public string  GcHsCode    { get; set; } = string.Empty;
    public bool    IsActive    { get; set; } = true;
    public DateTime CreatedAt  { get; set; }
}

public class CcItemWeight
{
    public int      IwId         { get; set; }
    public string   IwItemCode   { get; set; } = string.Empty;
    public string?  IwItemDescr  { get; set; }
    public decimal  IwWeightCase { get; set; } = 0;
    public decimal? IwWeightUnit { get; set; }
    public bool     IsActive     { get; set; } = true;
    public DateTime CreatedAt    { get; set; }
}

public class CcAllowedMargin
{
    public int      AmId          { get; set; }
    public string?  AmItemCode    { get; set; }
    public string?  AmCommodity   { get; set; }
    public string?  AmDescription { get; set; }
    public decimal  AmMinMargin   { get; set; } = 0;
    public decimal  AmMaxMargin   { get; set; } = 1;
    public decimal  AmDefMargin   { get; set; } = 0;
    public bool     IsActive      { get; set; } = true;
    public DateTime CreatedAt     { get; set; }
}

public class CcInlandTariff
{
    public int      ItId          { get; set; }
    public string   ItHsCode      { get; set; } = string.Empty;
    public string?  ItDescription { get; set; }
    public decimal  ItRate        { get; set; } = 0;
    public bool     IsActive      { get; set; } = true;
    public DateTime CreatedAt     { get; set; }
}

public class CcShipCharge
{
    public int      ScId          { get; set; }
    public int      ScCalcNumber  { get; set; }
    public string   ScChargeCode  { get; set; } = string.Empty;
    public string?  ScDescription { get; set; }
    public decimal  ScAmount      { get; set; } = 0;
    public string?  ScCurrency    { get; set; }
    public decimal? ScRate        { get; set; }
    public DateTime CreatedAt     { get; set; }
}

// ── Tracking ────────────────────────────────────────────────────────────────────
public class TrackingOrder
{
    public int      TrId                        { get; set; }
    // Auto-filled from VIP/DHW
    public string   TrPoNo                      { get; set; } = string.Empty;
    public string?  TrWarehouse                 { get; set; }
    public string?  TrSupplier                  { get; set; }
    public string?  TrSupplierName              { get; set; }
    public string?  TrCountry                   { get; set; }
    public string?  TrFreightForwarder          { get; set; }
    public int?     TrOrderDate                 { get; set; }  // YYYYMMDD from VIP
    public int?     TrVipShipDate              { get; set; }  // PHSHDT YYYYMMDD
    public int?     TrVipArrivalDate           { get; set; }  // PHARDT YYYYMMDD
    public decimal? TrTotalCases                { get; set; }
    public decimal? TrVipWeight                { get; set; }  // PHWEIG
    public decimal? TrVipLiters                { get; set; }  // PHLTRS
    public decimal? TrVipTotalAmount           { get; set; }  // PHTOT$
    public int?     TrVipTotalLines            { get; set; }  // PHLINE
    public string?  TrVipStatus                { get; set; }  // PHSTAT
    public string?  TrSupplierCode             { get; set; }  // PHBRVR
    public string?  TrVendorBrand              { get; set; }  // auto-filled from PODTLT PDBRAN (distinct brands)
    public string?  TrBorw                      { get; set; }  // B=Beer, W=Wine
    // Status
    public string?  TrStatusCode                { get; set; }
    // Section 1: General
    public string?  TrComments                  { get; set; }
    public DateTime? TrLastUpdateDate           { get; set; }
    public DateTime? TrRequestedEta             { get; set; }
    public bool?    TrAcknowledgeOrder           { get; set; }
    public DateTime? TrDateLoadingShipper        { get; set; }
    // Section 2: Shipping
    public string?  TrShippingLine              { get; set; }
    public string?  TrShippingAgent             { get; set; }
    public string?  TrVessel                    { get; set; }
    public string?  TrContainerNumber           { get; set; }
    public string?  TrConsolidationRef          { get; set; }
    public string?  TrContainerSize             { get; set; }
    // Section 3: Documentation
    public DateTime? TrDateProFormaReceived     { get; set; }
    public decimal? TrQtyProForma               { get; set; }
    public DateTime? TrFactoryReadyDate         { get; set; }
    public DateTime? TrEstDepartureDate         { get; set; }
    public DateTime? TrEstArrivalDate           { get; set; }
    public string?  TrTransitTime               { get; set; }
    public bool?    TrBijlageDone               { get; set; }
    public DateTime? TrDateArrivalInvoice       { get; set; }
    public string?  TrInvoiceNumber             { get; set; }
    public DateTime? TrDateArrivalBol           { get; set; }
    public string?  TrRemarks                   { get; set; }
    // Section 4: Customs
    public DateTime? TrDateArrivalNoteReceived  { get; set; }
    public DateTime? TrDateManifestReceived     { get; set; }
    public DateTime? TrDateCopiesToDeclarant    { get; set; }
    public DateTime? TrDateCustomsPapersReady   { get; set; }
    public DateTime? TrDateCustomsPapersAsycuda { get; set; }
    // Section 5: Container / CPS
    public DateTime? TrDateContainerAtCps       { get; set; }
    public DateTime? TrExpirationDateCps        { get; set; }
    public DateTime? TrDateCustomsPapersToCps   { get; set; }
    public DateTime? TrDateContainerArrivedLicores { get; set; }
    public DateTime? TrDateContainerOpenedCustoms  { get; set; }
    public DateTime? TrDateContainerUnloadReady { get; set; }
    public DateTime? TrReturnDateContainer      { get; set; }
    // TrDaysOverContainer is CALCULATED: ReturnDate - ArrivedLicores (not stored)
    // Section 6: Administration
    public DateTime? TrDateUnloadPapersAdmin    { get; set; }
    public string?  TrSadNumber                 { get; set; }
    public string?  TrBcNumberOrders            { get; set; }
    public string?  TrExitNoteNumber            { get; set; }
    public string?  TrIssuesComments            { get; set; }
    // Section 7: Goods Receipt
    public string?  TrReceiptStatus             { get; set; }  // PENDING/COMPLETE/SHORTAGE/OVERAGE/DAMAGED
    public decimal? TrQtyShortage               { get; set; }
    public decimal? TrQtyDamages                { get; set; }
    public string?  TrReceiptComments           { get; set; }
    // Section 8: Actual Delivery (#2)
    public DateTime? TrActualDeliveryDate       { get; set; }  // Real arrival date vs naviera ETA
    // Close / Lock (#6)
    public bool     TrIsClosed                  { get; set; }
    public DateTime? TrClosedAt                 { get; set; }
    public string?  TrClosedBy                  { get; set; }
    // Audit
    public string?  TrCreatedBy                 { get; set; }
    public DateTime TrCreatedAt                 { get; set; } = DateTime.UtcNow;
    public string?  TrUpdatedBy                 { get; set; }
    public DateTime? TrUpdatedAt                { get; set; }
    // Navigation
    public ICollection<TrackingStatusHistory> StatusHistory { get; set; } = [];
}

public class TrackingStatusHistory
{
    public int      TshId          { get; set; }
    public int      TshTrackingId  { get; set; }
    public string?  TshPoNo        { get; set; }
    public string?  TshStatusCode  { get; set; }
    public DateTime TshStatusDate  { get; set; } = DateTime.UtcNow;
    public string?  TshComments    { get; set; }
    public string?  TshChangedBy   { get; set; }
    public TrackingOrder? TrackingOrder { get; set; }
}

// ── Freight Forwarder - Main Entity & Quotes ───────────────────────────────────
public class FreightForwarder
{
    public int      FfId        { get; set; }
    public string   FfCode      { get; set; } = string.Empty;
    public string   FfName      { get; set; } = string.Empty;
    public string?  FfAddress1  { get; set; }
    public string?  FfAddress2  { get; set; }
    public string?  FfCity      { get; set; }
    public string?  FfCountry   { get; set; }
    public string?  FfPhone1    { get; set; }
    public string?  FfPhone2    { get; set; }
    public string?  FfEmail     { get; set; }
    public string?  FfContact   { get; set; }
    public string?  FfCurrency  { get; set; }
    public bool     IsActive    { get; set; } = true;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
}

public class OceanFreightHeader
{
    public int       FqohId          { get; set; }
    public string    FqohForwarder   { get; set; } = string.Empty;
    public string    FqohQuoteNumber { get; set; } = string.Empty;
    public DateOnly? FqohStartDate   { get; set; }
    public DateOnly? FqohEndDate     { get; set; }
    public string?   FqohRemarks     { get; set; }
    public ICollection<OceanFreightPort> Ports { get; set; } = [];
}

public class OceanFreightPort
{
    public int     FqopId          { get; set; }
    public int     FqopHeaderId    { get; set; }
    public string  FqopForwarder   { get; set; } = string.Empty;
    public string  FqopQuoteNumber { get; set; } = string.Empty;
    public string  FqopPort        { get; set; } = string.Empty;
    public string? FqopRemarks     { get; set; }
    public OceanFreightHeader? Header { get; set; }
    public ICollection<OceanFreightPortSLine>  ShippingLines { get; set; } = [];
    public ICollection<OceanFreightPortCharge> PortCharges   { get; set; } = [];
}

public class OceanFreightPortSLine
{
    public int     FqoplId          { get; set; }
    public int     FqoplPortId      { get; set; }
    public string  FqoplForwarder   { get; set; } = string.Empty;
    public string  FqoplQuoteNumber { get; set; } = string.Empty;
    public string  FqoplPort        { get; set; } = string.Empty;
    public string  FqoplShipLine    { get; set; } = string.Empty;
    public string? FqoplRoute       { get; set; }
    public short?  FqoplDays        { get; set; }
    public string? FqoplRemarks     { get; set; }
    public OceanFreightPort? Port { get; set; }
    public ICollection<OceanFreightPortSLineCharge> Charges { get; set; } = [];
}

public class OceanFreightPortSLineCharge
{
    public int      FqoplcId            { get; set; }
    public int      FqoplcSLineId       { get; set; }
    public string   FqoplcForwarder     { get; set; } = string.Empty;
    public string   FqoplcQuoteNumber   { get; set; } = string.Empty;
    public string   FqoplcPort          { get; set; } = string.Empty;
    public string   FqoplcShipLine      { get; set; } = string.Empty;
    public string   FqoplcChargeType    { get; set; } = string.Empty;
    public string?  FqoplcContainerType { get; set; }
    public decimal? FqoplcAmount        { get; set; }
    public string?  FqoplcCurrency      { get; set; }
    public OceanFreightPortSLine? PortSLine { get; set; }
}

public class OceanFreightPortCharge
{
    public int      FqopcId            { get; set; }
    public int      FqopcPortId        { get; set; }
    public string   FqopcForwarder     { get; set; } = string.Empty;
    public string   FqopcQuoteNumber   { get; set; } = string.Empty;
    public string   FqopcPort          { get; set; } = string.Empty;
    public string   FqopcChargeType    { get; set; } = string.Empty;
    public string?  FqopcContainerType { get; set; }
    public decimal? FqopcAmount        { get; set; }
    public string?  FqopcCurrency      { get; set; }
    public OceanFreightPort? Port { get; set; }
}

public class InlandFreightHeader
{
    public int       FqihId          { get; set; }
    public string    FqihForwarder   { get; set; } = string.Empty;
    public string    FqihQuoteNumber { get; set; } = string.Empty;
    public DateOnly? FqihStartDate   { get; set; }
    public DateOnly? FqihEndDate     { get; set; }
    public string?   FqihRemarks     { get; set; }
    public ICollection<InlandFreightRegion> Regions { get; set; } = [];
}

public class InlandFreightRegion
{
    public int     FqirId          { get; set; }
    public int     FqirHeaderId    { get; set; }
    public string  FqirForwarder   { get; set; } = string.Empty;
    public string  FqirQuoteNumber { get; set; } = string.Empty;
    public string  FqirRegion      { get; set; } = string.Empty;
    public InlandFreightHeader? Header { get; set; }
    public ICollection<InlandFreightRegionType> RegionTypes { get; set; } = [];
}

public class InlandFreightRegionType
{
    public int      FqirtId          { get; set; }
    public int      FqirtRegionId    { get; set; }
    public string   FqirtForwarder   { get; set; } = string.Empty;
    public string   FqirtQuoteNumber { get; set; } = string.Empty;
    public string   FqirtRegion      { get; set; } = string.Empty;
    public string   FqirtChargeType  { get; set; } = string.Empty;
    public decimal? FqirtAmountMin   { get; set; }
    public decimal? FqirtAmountMax   { get; set; }
    public string?  FqirtCurrency    { get; set; }
    public InlandFreightRegion? Region { get; set; }
    public ICollection<InlandFreightRegionTypeDet> Details { get; set; } = [];
}

public class InlandFreightRegionTypeDet
{
    public int      FqirtdId            { get; set; }
    public int      FqirtdRegionTypeId  { get; set; }
    public string   FqirtdForwarder     { get; set; } = string.Empty;
    public string   FqirtdQuoteNumber   { get; set; } = string.Empty;
    public string   FqirtdRegion        { get; set; } = string.Empty;
    public string   FqirtdChargeType    { get; set; } = string.Empty;
    public decimal? FqirtdFrom          { get; set; }
    public decimal? FqirtdTo            { get; set; }
    public decimal? FqirtdPrice         { get; set; }
    public string?  FqirtdPriceType     { get; set; }
    public decimal? FqirtdAmountMin     { get; set; }
    public decimal? FqirtdAmountMax     { get; set; }
    public InlandFreightRegionType? RegionType { get; set; }
}

public class LclHeader
{
    public int       FqlhId          { get; set; }
    public string    FqlhForwarder   { get; set; } = string.Empty;
    public string    FqlhQuoteNumber { get; set; } = string.Empty;
    public DateOnly? FqlhStartDate   { get; set; }
    public DateOnly? FqlhEndDate     { get; set; }
    public string?   FqlhRemarks     { get; set; }
    public ICollection<LclPort> Ports { get; set; } = [];
}

public class LclPort
{
    public int     FqlpId          { get; set; }
    public int     FqlpHeaderId    { get; set; }
    public string  FqlpForwarder   { get; set; } = string.Empty;
    public string  FqlpQuoteNumber { get; set; } = string.Empty;
    public string  FqlpPort        { get; set; } = string.Empty;
    public string? FqlpRemarks     { get; set; }
    public LclHeader? Header { get; set; }
    public ICollection<LclPortType> PortTypes { get; set; } = [];
}

public class LclPortType
{
    public int      FqlptId          { get; set; }
    public int      FqlptPortId      { get; set; }
    public string   FqlptForwarder   { get; set; } = string.Empty;
    public string   FqlptQuoteNumber { get; set; } = string.Empty;
    public string   FqlptPort        { get; set; } = string.Empty;
    public string   FqlptChargeType  { get; set; } = string.Empty;
    public decimal? FqlptAmountMin   { get; set; }
    public decimal? FqlptAmountMax   { get; set; }
    public string?  FqlptCurrency    { get; set; }
    public LclPort? Port { get; set; }
    public ICollection<LclPortTypeDet> Details { get; set; } = [];
}

public class LclPortTypeDet
{
    public int      FqlptdId           { get; set; }
    public int      FqlptdPortTypeId   { get; set; }
    public string   FqlptdForwarder    { get; set; } = string.Empty;
    public string   FqlptdQuoteNumber  { get; set; } = string.Empty;
    public string   FqlptdPort         { get; set; } = string.Empty;
    public string   FqlptdChargeType   { get; set; } = string.Empty;
    public decimal? FqlptdFrom         { get; set; }
    public decimal? FqlptdTo           { get; set; }
    public decimal? FqlptdPrice        { get; set; }
    public decimal? FqlptdOver         { get; set; }
    public string?  FqlptdPriceType    { get; set; }
    public decimal? FqlptdAmountMin    { get; set; }
    public decimal? FqlptdAmountMax    { get; set; }
    public LclPortType? PortType { get; set; }
}

public class InlandAdditionalCharge
{
    public int      FqiaId         { get; set; }
    public string   FqiaForwarder  { get; set; } = string.Empty;
    public string   FqiaChargeType { get; set; } = string.Empty;
    public string?  FqiaLoadType   { get; set; }
    public decimal? FqiaAmount     { get; set; }
    public string?  FqiaAction     { get; set; }
    public string?  FqiaChargeOver { get; set; }
    public string?  FqiaChargePer  { get; set; }
    public decimal? FqiaFrom       { get; set; }
    public decimal? FqiaTo         { get; set; }
    public decimal? FqiaAmountMin  { get; set; }
    public decimal? FqiaAmountMax  { get; set; }
    public string?  FqiaCurrency   { get; set; }
}

// ── MODULE 4: Route Assignment ─────────────────────────────────────────────────
public class RouteCustomerExt
{
    public int      RceId                       { get; set; }
    public string   RceAccountNumber            { get; set; } = string.Empty;
    public string?  RceRouteNpActive            { get; set; }
    public string?  RceRouteOvd5                { get; set; }
    public string?  RceRouteOvd6                { get; set; }
    public string?  RcePareto1Overall           { get; set; }
    public string?  RcePareto2Overall           { get; set; }
    public string?  RceParetoOthersOverall      { get; set; }
    public string?  RcePareto1Beer              { get; set; }
    public string?  RcePareto2Beer              { get; set; }
    public string?  RceParetoOthersBeer         { get; set; }
    public string?  RcePareto1Water             { get; set; }
    public string?  RcePareto2Water             { get; set; }
    public string?  RceParetoOthersWater        { get; set; }
    public string?  RcePareto1Others            { get; set; }
    public string?  RcePareto2Others            { get; set; }
    public string?  RceParetoOthersOthers       { get; set; }
    public string?  RceProyection               { get; set; }
    public string?  RceSalesRepActive4          { get; set; }
    public string?  RceSalesRepActive5          { get; set; }
    public string?  RceSalesRepActive6          { get; set; }
    public string?  RceAlternativeSalesRep      { get; set; }
    public bool     RceCoolerPolar              { get; set; }
    public bool     RceCoolerCorona             { get; set; }
    public bool     RceCoolerBrasa              { get; set; }
    public bool     RceCoolerWine               { get; set; }
    public bool     RcePaintedPolar             { get; set; }
    public bool     RceBrandingDwl              { get; set; }
    public bool     RceBrandingGreyGoose        { get; set; }
    public bool     RceBrandingBacardi          { get; set; }
    public bool     RceBrandingBrasa            { get; set; }
    public bool     RceHighTraffic              { get; set; }
    public bool     RceIndoorBrandingClaro      { get; set; }
    public bool     RceIndoorBrandingBrasa      { get; set; }
    public bool     RceIndoorBrandingPolar      { get; set; }
    public bool     RceIndoorBrandingMalta      { get; set; }
    public bool     RceIndoorBrandingCorona     { get; set; }
    public bool     RceIndoorBrandingCarloRossi { get; set; }
    public bool     RceWithRackDisplay          { get; set; }
    public bool     RceWithLightHeader          { get; set; }
    public bool     RceWithWallMountedNameboard { get; set; }
    public bool     RceWithBackbar              { get; set; }
    public bool     RceWithLicoresWineAsHousewine { get; set; }
    public DateTime UpdatedAt                   { get; set; } = DateTime.UtcNow;
}

public class RouteProductExt
{
    public int      RpeId                        { get; set; }
    public string   RpeItemCode                  { get; set; } = string.Empty;
    public string?  RpeGroupCodeBeerWaterOthers  { get; set; }
    public string?  RpeGroupCodeBrandSpecific    { get; set; }
    public DateTime UpdatedAt                    { get; set; } = DateTime.UtcNow;
}

public class RouteBudget
{
    public int      RbId            { get; set; }
    public int      RbYear          { get; set; }
    public string   RbAccountNumber { get; set; } = string.Empty;
    public string   RbItemCode      { get; set; } = string.Empty;
    public decimal? RbQty01         { get; set; } = 0;
    public decimal? RbQty02         { get; set; } = 0;
    public decimal? RbQty03         { get; set; } = 0;
    public decimal? RbQty04         { get; set; } = 0;
    public decimal? RbQty05         { get; set; } = 0;
    public decimal? RbQty06         { get; set; } = 0;
    public decimal? RbQty07         { get; set; } = 0;
    public decimal? RbQty08         { get; set; } = 0;
    public decimal? RbQty09         { get; set; } = 0;
    public decimal? RbQty10         { get; set; } = 0;
    public decimal? RbQty11         { get; set; } = 0;
    public decimal? RbQty12         { get; set; } = 0;
}

// ── MODULE 5: Stock Analysis ───────────────────────────────────────────────────

public class StockIdealMonths
{
    public int      SimId              { get; set; }
    public string   SimItemCode        { get; set; } = string.Empty;
    public decimal  SimIdealMonths     { get; set; } = 1.5m;
    public string?  SimOrderFreq       { get; set; }
    public DateOnly? SimStockStartDate { get; set; }
    public DateTime UpdatedAt          { get; set; } = DateTime.UtcNow;
}

public class StockVendorConstraints
{
    public int      SvcId                   { get; set; }
    public string?  SvcFromLocationCode     { get; set; }
    public string?  SvcFromLocationName     { get; set; }
    public string?  SvcToLocationCode       { get; set; }
    public string?  SvcToLocationName       { get; set; }
    public string?  SvcShipperCode          { get; set; }
    public string?  SvcOrderReviewDay       { get; set; }
    public int?     SvcSupplierLeadDays     { get; set; }
    public int?     SvcTransitDays          { get; set; }
    public int?     SvcWarehouseProcessDays { get; set; }
    public int?     SvcSafetyDays           { get; set; }
    public int?     SvcOrderCycleDays       { get; set; }
    public decimal? SvcMinOrderQty          { get; set; }
    public decimal? SvcOrderIncrement       { get; set; }
    public decimal? SvcMinTotalCaseOrder    { get; set; }
    public string?  SvcPurchaserName        { get; set; }
    public DateTime UpdatedAt               { get; set; } = DateTime.UtcNow;
}

public class StockSalesBudget
{
    public int      SsbId               { get; set; }
    public int      SsbYear             { get; set; }
    public int      SsbMonth            { get; set; }
    public string   SsbItemCode         { get; set; } = string.Empty;
    public string?  SsbItemDesc         { get; set; }
    public decimal? SsbBudgetedUnits    { get; set; } = 0;
    public decimal? SsbBudgetedSales    { get; set; } = 0;
    public decimal? SsbBudgetedDiscount { get; set; } = 0;
    public decimal? SsbBudgetedMargin   { get; set; } = 0;
    public decimal? SsbBudgetedGross    { get; set; } = 0;
    public decimal? SsbBudgetedCost     { get; set; } = 0;
}

public class StockAnalysisResult
{
    public int      SarId                               { get; set; }
    public int      SarYear                             { get; set; }
    public int      SarMonth                            { get; set; }
    public string   SarItemCode                         { get; set; } = string.Empty;
    public string?  SarItemDesc                         { get; set; }
    public string?  SarProductClassId                   { get; set; }
    public string?  SarProductClassDesc                 { get; set; }
    public string?  SarSupplierCode                     { get; set; }
    public string?  SarSupplierName                     { get; set; }
    public string?  SarBrandCode                        { get; set; }
    public string?  SarBrandDesc                        { get; set; }
    public DateOnly? SarStockStartDate                  { get; set; }
    public string?  SarOrderFrequency                   { get; set; }
    public decimal? SarIdealMonthsOfStock               { get; set; }
    public decimal? SarOh11010                          { get; set; }
    public decimal? SarOh11020                          { get; set; }
    public decimal? SarOh11060                          { get; set; }
    public decimal? SarCurrentOhUnits                   { get; set; }
    public decimal? SarOnOrder11010                     { get; set; }
    public decimal? SarOnOrder11020                     { get; set; }
    public decimal? SarOnOrder11060                     { get; set; }
    public decimal? SarOnOrderUnits                     { get; set; }
    public DateOnly? SarOnOrderEta                      { get; set; }
    public decimal? SarYtdSalesUnits                    { get; set; }
    public decimal? SarMonthlySalesUnits                { get; set; }
    public decimal? SarIdealStockUnits                  { get; set; }
    public decimal? SarOverstockUnits                   { get; set; }
    public decimal? SarOverstockUnitsInclOrders         { get; set; }
    public decimal? SarMonthsOfStock                    { get; set; }
    public decimal? SarYearsOfStock                     { get; set; }
    public decimal? SarMonthsOfStockInclOnOrder         { get; set; }
    public decimal? SarMonthsOfOverstock                { get; set; }
    public decimal? SarMonthsOfOverstockInclOnOrder     { get; set; }
    public decimal? SarTotalBudgetUnits                 { get; set; }
    public decimal? SarYtdBudgetUnits                   { get; set; }
    public decimal? SarTotalBudgetSales                 { get; set; }
    public decimal? SarYtdBudgetSales                   { get; set; }
    public decimal? SarTotalBudgetCost                  { get; set; }
    public decimal? SarYtdBudgetCost                    { get; set; }
    public decimal? SarOverUnderPerformanceUnits        { get; set; }
    public decimal? SarInventoryValue                   { get; set; }
    public decimal? SarInventoryValueOnOrder            { get; set; }
    public decimal? SarTotalInventoryValue              { get; set; }
    public decimal? SarAvgCostPerCase                   { get; set; }
    public decimal? SarIdealStockAng                    { get; set; }
    public decimal? SarBudgetedIdealStockAng            { get; set; }
    public decimal? SarOverstockAng                     { get; set; }
    public decimal? SarOverstockAngInclOrder            { get; set; }
    public decimal? SarExpectedMonthlySalesAng          { get; set; }
    public decimal? SarMonthsOfStockInclOrderOnValue    { get; set; }
    public decimal? SarMonthsOfOverstockInclOrderOnValue { get; set; }
    public decimal? SarDailyRateOfSale                  { get; set; }
    public DateOnly? SarLastReceiptDate                 { get; set; }
    public decimal? SarQtyLastReceipt                   { get; set; }
    public int?     SarDaysBeforeArrivalOrder           { get; set; }
    public decimal? SarMonthsBeforeArrivalOrder         { get; set; }
    public decimal? SarUnitSalesBeforeArrivalOrder      { get; set; }
    public decimal? SarTotalOhAtArrivalOrder            { get; set; }
    public decimal? SarOverstockAtArrivalOrder          { get; set; }
    public decimal? SarTotalMonthsBeforeIdealStock      { get; set; }
    public DateTime SarGeneratedAt                      { get; set; } = DateTime.UtcNow;
}

public class LmNotification
{
    public int      NtfId      { get; set; }
    public int      NtfUserId  { get; set; }
    public string   NtfTitle   { get; set; } = string.Empty;
    public string   NtfMessage { get; set; } = string.Empty;
    public string   NtfType    { get; set; } = "INFO";
    public bool     NtfIsRead  { get; set; }
    public string?  NtfUrl     { get; set; }
    public int?     NtfRefId   { get; set; }
    public string?  NtfRefType { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}

// ── Applied Freight Quote entities ────────────────────────────────────────────

public class FreightQuoteHeader
{
    public int       FqhId          { get; set; }
    public int       FqhQuoteNumber { get; set; }
    public string    FqhForwarder   { get; set; } = string.Empty;
    public string?   FqhPort        { get; set; }
    public string?   FqhRoute       { get; set; }
    public int?      FqhTransitDays { get; set; }
    public DateOnly? FqhStartDate   { get; set; }
    public DateOnly? FqhEndDate     { get; set; }
    public DateTime  CreatedAt      { get; set; } = DateTime.UtcNow;
    public ICollection<FreightQuoteOceanPort>    OceanPorts    { get; set; } = [];
    public ICollection<FreightQuoteInlRegion>    InlandRegions { get; set; } = [];
    public ICollection<FreightQuoteInlPortAdd>   InlandPortAdds{ get; set; } = [];
    public ICollection<FreightQuoteLclPort>      LclPorts      { get; set; } = [];
}

public class FreightQuoteOceanCharge
{
    public int      FqocId            { get; set; }
    public int      FqocSLineId       { get; set; }
    public string   FqocChargeType    { get; set; } = string.Empty;
    public string?  FqocContainerType { get; set; }
    public decimal? FqocAmount        { get; set; }
    public string?  FqocCurrency      { get; set; }
    public FreightQuoteOceanPortSLine? SLine { get; set; }
}

public class FreightQuoteOceanPort
{
    public int     FqopId       { get; set; }
    public int     FqopHeaderId { get; set; }
    public string  FqopPort     { get; set; } = string.Empty;
    public string? FqopRemarks  { get; set; }
    public FreightQuoteHeader? Header { get; set; }
    public ICollection<FreightQuoteOceanPortSLine> ShippingLines { get; set; } = [];
}

public class FreightQuoteOceanPortSLine
{
    public int     FqopsId           { get; set; }
    public int     FqopsPortId       { get; set; }
    public string  FqopsShippingLine { get; set; } = string.Empty;
    public string? FqopsRoute        { get; set; }
    public int?    FqopsDays         { get; set; }
    public FreightQuoteOceanPort? Port { get; set; }
    public ICollection<FreightQuoteOceanCharge> Charges { get; set; } = [];
}

public class FreightQuoteInlRegion
{
    public int    FqerId      { get; set; }
    public int    FqerHeaderId{ get; set; }
    public string FqerRegion  { get; set; } = string.Empty;
    public FreightQuoteHeader? Header { get; set; }
    public ICollection<FreightQuoteInlRegionType> RegionTypes { get; set; } = [];
}

public class FreightQuoteInlRegionType
{
    public int      FqertId         { get; set; }
    public int      FqertRegionId   { get; set; }
    public string   FqertChargeType { get; set; } = string.Empty;
    public decimal? FqertAmountMin  { get; set; }
    public decimal? FqertAmountMax  { get; set; }
    public string?  FqertCurrency   { get; set; }
    public FreightQuoteInlRegion? Region { get; set; }
    public ICollection<FreightQuoteInlRegionTypeDet> Details { get; set; } = [];
}

public class FreightQuoteInlRegionTypeDet
{
    public int      FqertdId          { get; set; }
    public int      FqertdRegionTypeId{ get; set; }
    public decimal? FqertdFrom        { get; set; }
    public decimal? FqertdTo          { get; set; }
    public decimal? FqertdPrice       { get; set; }
    public string?  FqertdPriceType   { get; set; }
    public decimal? FqertdAmountMin   { get; set; }
    public decimal? FqertdAmountMax   { get; set; }
    public FreightQuoteInlRegionType? RegionType { get; set; }
}

public class FreightQuoteInlPortAdd
{
    public int      FqipaId         { get; set; }
    public int      FqipaHeaderId   { get; set; }
    public string   FqipaChargeType { get; set; } = string.Empty;
    public string?  FqipaLoadType   { get; set; }
    public decimal? FqipaAmount     { get; set; }
    public string?  FqipaAction     { get; set; }
    public string?  FqipaChargeOver { get; set; }
    public string?  FqipaChargePer  { get; set; }
    public decimal? FqipaFrom       { get; set; }
    public decimal? FqipaTo         { get; set; }
    public decimal? FqipaAmountMin  { get; set; }
    public decimal? FqipaAmountMax  { get; set; }
    public string?  FqipaCurrency   { get; set; }
    public FreightQuoteHeader? Header { get; set; }
}

public class FreightQuoteLclPort
{
    public int      FqlcpId       { get; set; }
    public int      FqlcpHeaderId { get; set; }
    public string   FqlcpPort     { get; set; } = string.Empty;
    public string?  FqlcpRemarks  { get; set; }
    public FreightQuoteHeader? Header { get; set; }
    public ICollection<FreightQuoteLclPortType> PortTypes { get; set; } = [];
}

public class FreightQuoteLclPortType
{
    public int      FqlcptId         { get; set; }
    public int      FqlcptPortId     { get; set; }
    public string   FqlcptChargeType { get; set; } = string.Empty;
    public decimal? FqlcptAmountMin  { get; set; }
    public decimal? FqlcptAmountMax  { get; set; }
    public string?  FqlcptCurrency   { get; set; }
    public FreightQuoteLclPort? Port { get; set; }
    public ICollection<FreightQuoteLclPortTypeDet> Details { get; set; } = [];
}

public class FreightQuoteLclPortTypeDet
{
    public int      FqlcptdId         { get; set; }
    public int      FqlcptdPortTypeId { get; set; }
    public decimal? FqlcptdFrom       { get; set; }
    public decimal? FqlcptdTo         { get; set; }
    public decimal? FqlcptdPrice      { get; set; }
    public decimal? FqlcptdOver       { get; set; }
    public string?  FqlcptdPriceType  { get; set; }
    public decimal? FqlcptdAmountMin  { get; set; }
    public decimal? FqlcptdAmountMax  { get; set; }
    public FreightQuoteLclPortType? PortType { get; set; }
}
