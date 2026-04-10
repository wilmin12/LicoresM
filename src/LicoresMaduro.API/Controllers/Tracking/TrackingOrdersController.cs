using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Tracking;

[ApiController]
[Route("api/tracking/orders")]
[Authorize]
[Produces("application/json")]
public sealed class TrackingOrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly DhwDbContext         _dhw;
    private readonly ILogger<TrackingOrdersController> _logger;

    public TrackingOrdersController(ApplicationDbContext db, DhwDbContext dhw, ILogger<TrackingOrdersController> logger)
    { _db = db; _dhw = dhw; _logger = logger; }

    private string? CurrentUser =>
        User.FindFirst("fullName")?.Value ??
        User.FindFirst("username")?.Value ??
        null;

    // ── Helpers: VIP lookups ──────────────────────────────────────────────────
    private async Task<string?> GetSupplierNameAsync(string? brvr, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(brvr)) return null;
        var brvrTrimmed = brvr.Trim();

        // Load full table to memory — SUPPLIERT is a small catalog (~20-50 rows).
        // EF Core cannot translate: variable.StartsWith(dbColumn), so we evaluate in C#.
        var suppliers = await _dhw.Suppliers.AsNoTracking()
            .Where(s => s.Supplier != null)
            .ToListAsync(ct);

        var match = suppliers.FirstOrDefault(s =>
            s.Supplier!.Trim() == brvrTrimmed ||
            brvrTrimmed.StartsWith(s.Supplier.Trim()));

        return match?.SupplierName?.Trim();
    }

    private async Task<string?> GetVendorBrandAsync(string poNo, CancellationToken ct)
    {
        var brands = await _dhw.PoDetails.AsNoTracking()
            .Where(x => x.PdPoNo.StartsWith(poNo) && x.PdBran != null)
            .Select(x => x.PdBran)
            .Distinct()
            .ToListAsync(ct);
        var list = brands.Select(b => b?.Trim()).Where(b => !string.IsNullOrWhiteSpace(b)).Distinct().ToList();
        return list.Count > 0 ? string.Join(", ", list) : null;
    }

    // ── List tracking orders ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search          = null,
        [FromQuery] string? status          = null,
        [FromQuery] string? warehouse       = null,
        [FromQuery] string? borw            = null,
        [FromQuery] string? freightForwarder = null,
        [FromQuery] string? itemNo          = null,
        [FromQuery] int     page            = 1,
        [FromQuery] int     pageSize        = 20,
        CancellationToken   ct              = default)
    {
        var q = _db.TrackingOrders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.TrPoNo.Contains(search) || (x.TrSupplier != null && x.TrSupplier.Contains(search)) || (x.TrSupplierName != null && x.TrSupplierName.Contains(search)) || (x.TrContainerNumber != null && x.TrContainerNumber.Contains(search)));
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(x => x.TrStatusCode == status);
        if (!string.IsNullOrWhiteSpace(warehouse))
            q = q.Where(x => x.TrWarehouse == warehouse);
        if (!string.IsNullOrWhiteSpace(borw))
            q = q.Where(x => x.TrBorw == borw);
        if (!string.IsNullOrWhiteSpace(freightForwarder))
            q = q.Where(x => x.TrFreightForwarder != null && x.TrFreightForwarder.Contains(freightForwarder));

        // Item number: cross-context query against PODTLT (DHW_DATABASE)
        if (!string.IsNullOrWhiteSpace(itemNo))
        {
            var matchingPoNos = await _dhw.PoDetails.AsNoTracking()
                .Where(x => x.PdItem != null && x.PdItem.StartsWith(itemNo.Trim()))
                .Select(x => x.PdPoNo.Trim())
                .Distinct()
                .ToListAsync(ct);
            q = q.Where(x => matchingPoNos.Contains(x.TrPoNo));
        }

        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(x => x.TrCreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var result = data.Select(t => new TrackingOrderDto(t)).ToList();
        return Ok(PagedResponse<TrackingOrderDto>.Ok(result, page, pageSize, total));
    }

    // ── Get single tracking order ─────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var t = await _db.TrackingOrders.AsNoTracking()
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.TrId == id, ct);
        if (t is null) return NotFound(ApiResponse.Fail($"Tracking order {id} not found."));

        // Calculate receipt summary from PODTLT
        var lines = await _dhw.PoDetails.AsNoTracking()
            .Where(x => x.PdPoNo.StartsWith(t.TrPoNo.Trim()))
            .Select(x => new { x.PdOqty, x.PdRqty })
            .ToListAsync(ct);

        ReceiptSummaryDto? receiptSummary = null;
        if (lines.Count > 0)
        {
            var totalOrdered  = lines.Sum(l => l.PdOqty ?? 0);
            var totalReceived = lines.Sum(l => l.PdRqty ?? 0);
            receiptSummary = ReceiptSummaryDto.Calculate(totalOrdered, totalReceived);
            // Override with DAMAGED if manually flagged
            if (t.TrReceiptStatus == "DAMAGED")
                receiptSummary = new ReceiptSummaryDto
                {
                    TotalOrdered  = receiptSummary.TotalOrdered,
                    TotalReceived = receiptSummary.TotalReceived,
                    Difference    = receiptSummary.Difference,
                    Status        = "DAMAGED"
                };
        }

        var dto = new TrackingOrderDto(t) { ReceiptSummary = receiptSummary };
        return Ok(ApiResponse<TrackingOrderDto>.Ok(dto));
    }

    // ── Search VIP POs (for lookup/autocomplete) ─────────────────────────────
    [HttpGet("search-vip")]
    public async Task<IActionResult> SearchVip([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return BadRequest(ApiResponse.Fail("Query must be at least 2 characters."));

        // AS/400 stores CHAR fields right-padded with spaces — use StartsWith for partial match
        var results = await _dhw.PoHeaders.AsNoTracking()
            .Where(x => x.PhPoNo.StartsWith(q.Trim()))
            .OrderByDescending(x => x.PhOrdt)
            .Take(50)
            .Select(x => new { x.PhPoNo, x.PhWhse, x.PhOvrNo, x.PhBorw, x.PhOrdt, x.PhOqt, x.PhStat })
            .ToListAsync(ct);

        // Trim trailing spaces from AS/400 fixed-length fields before returning
        var trimmed = results.Select(x => new {
            PhPoNo  = x.PhPoNo?.TrimEnd(),
            PhWhse  = x.PhWhse?.TrimEnd(),
            PhOvrNo = x.PhOvrNo?.TrimEnd(),
            PhBorw  = x.PhBorw?.TrimEnd(),
            x.PhOrdt,
            x.PhOqt,
            PhStat  = x.PhStat?.TrimEnd()
        }).ToList();

        return Ok(ApiResponse<object>.Ok(trimmed));
    }

    // ── Preview VIP data for a PO number (before creating tracking record) ──────
    [HttpGet("preview-vip")]
    public async Task<IActionResult> PreviewVip([FromQuery] string poNo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(poNo))
            return BadRequest(ApiResponse.Fail("poNo is required."));

        var clean = poNo.Trim();

        // AS/400 stores CHAR fields right-padded with spaces — StartsWith handles "1234567   "
        var poHeader = await _dhw.PoHeaders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhPoNo.StartsWith(clean), ct);

        if (poHeader is null)
            return NotFound(ApiResponse.Fail($"PO '{poNo}' not found in VIP."));

        // Look up Freight Forwarder from VIP SHIPPER_MASTER via POHDRT.PHSHIP
        var shipperIdRaw = poHeader.PhShip?.Trim();
        var shipper = !string.IsNullOrWhiteSpace(shipperIdRaw)
            ? await _dhw.ShipperMasters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ShipperId.StartsWith(shipperIdRaw), ct)
            : null;

        var alreadyTracked = await _db.TrackingOrders.AnyAsync(x => x.TrPoNo == clean, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            PoNo             = poHeader.PhPoNo?.Trim(),
            Warehouse        = poHeader.PhWhse?.Trim(),
            Supplier         = poHeader.PhOvrNo?.Trim(),
            BorW             = poHeader.PhBorw?.Trim(),
            OrderDate        = (int?)poHeader.PhOrdt,
            TotalCases       = poHeader.PhOqt,
            ShipDate         = (int?)poHeader.PhShdt,
            ArrivalDate      = (int?)poHeader.PhArdt,
            ContainerNo      = poHeader.PhConNo?.Trim(),
            Status           = poHeader.PhStat?.Trim(),
            FreightForwarder = shipper?.ShipperName?.Trim(),
            AlreadyTracked   = alreadyTracked
        }));
    }

    // ── Create tracking order from a VIP PO ───────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTrackingDto dto, CancellationToken ct)
    {
        if (await _db.TrackingOrders.AnyAsync(x => x.TrPoNo == dto.PoNo, ct))
            return Conflict(ApiResponse.Fail($"PO {dto.PoNo} is already being tracked."));

        // AS/400 stores CHAR fields right-padded with spaces — StartsWith handles "1234567   "
        var cleanPoNo = dto.PoNo.Trim();
        var poHeader = await _dhw.PoHeaders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhPoNo.StartsWith(cleanPoNo), ct);

        if (poHeader is null)
            return NotFound(ApiResponse.Fail($"PO '{dto.PoNo}' not found in VIP. Verify the PO number and try again."));

        // Look up Freight Forwarder from VIP SHIPPER_MASTER via POHDRT.PHSHIP
        var shipperIdRawC = poHeader.PhShip?.Trim();
        var shipperC = !string.IsNullOrWhiteSpace(shipperIdRawC)
            ? await _dhw.ShipperMasters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ShipperId.StartsWith(shipperIdRawC), ct)
            : null;

        var supplierNameC  = await GetSupplierNameAsync(poHeader.PhBrvr, ct);
        var vendorBrandC   = await GetVendorBrandAsync(cleanPoNo, ct);

        var order = new TrackingOrder
        {
            TrPoNo             = cleanPoNo,
            TrWarehouse        = poHeader.PhWhse?.Trim(),
            TrSupplier         = poHeader.PhOvrNo?.Trim(),
            TrSupplierCode     = poHeader.PhBrvr?.Trim(),
            TrSupplierName     = supplierNameC ?? poHeader.PhOvrNo?.Trim(),
            TrVendorBrand      = vendorBrandC,
            TrBorw             = poHeader.PhBorw?.Trim(),
            TrOrderDate        = (int?)poHeader.PhOrdt,
            TrVipShipDate      = (int?)poHeader.PhShdt,
            TrVipArrivalDate   = (int?)poHeader.PhArdt,
            TrTotalCases       = poHeader.PhOqt,
            TrVipWeight        = poHeader.PhWeig,
            TrVipLiters        = poHeader.PhLtrs,
            TrVipTotalAmount   = poHeader.PhTotAmt,
            TrVipTotalLines    = (int?)poHeader.PhLine,
            TrVipStatus        = poHeader.PhStat?.Trim(),
            TrContainerNumber  = poHeader.PhConNo?.Trim(),
            TrFreightForwarder = dto.FreightForwarder ?? shipperC?.ShipperName?.Trim(),
            TrStatusCode       = dto.StatusCode ?? "INVIP",
            TrComments         = dto.Comments,
            TrCreatedBy        = CurrentUser,
            TrCreatedAt        = DateTime.UtcNow
        };

        _db.TrackingOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        // Record initial status history
        _db.TrackingStatusHistories.Add(new TrackingStatusHistory
        {
            TshTrackingId = order.TrId,
            TshPoNo       = order.TrPoNo,
            TshStatusCode = order.TrStatusCode,
            TshStatusDate = DateTime.UtcNow,
            TshComments   = "Order tracking created.",
            TshChangedBy  = CurrentUser
        });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Tracking order created for PO {PoNo}", dto.PoNo);
        return CreatedAtAction(nameof(GetById), new { id = order.TrId },
            ApiResponse<TrackingOrderDto>.Ok(new TrackingOrderDto(order), "Tracking order created."));
    }

    // ── Update tracking order ─────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTrackingDto dto, CancellationToken ct)
    {
        var order = await _db.TrackingOrders.FindAsync([id], ct);
        if (order is null) return NotFound(ApiResponse.Fail($"Tracking order {id} not found."));

        // Block edits on closed orders (only SuperAdmin can override)
        if (order.TrIsClosed && !User.IsInRole("SuperAdmin"))
            return BadRequest(ApiResponse.Fail("This tracking order is closed and cannot be modified. Contact a SuperAdmin to reopen it."));

        bool statusChanged = !string.IsNullOrWhiteSpace(dto.StatusCode) && dto.StatusCode != order.TrStatusCode;

        // Apply all fields
        if (dto.StatusCode          != null) order.TrStatusCode                 = dto.StatusCode;
        if (dto.Comments            != null) order.TrComments                   = dto.Comments;
        if (dto.LastUpdateDate.HasValue)     order.TrLastUpdateDate             = dto.LastUpdateDate;
        if (dto.RequestedEta.HasValue)       order.TrRequestedEta               = dto.RequestedEta;
        if (dto.AcknowledgeOrder.HasValue)   order.TrAcknowledgeOrder           = dto.AcknowledgeOrder;
        if (dto.DateLoadingShipper.HasValue) order.TrDateLoadingShipper         = dto.DateLoadingShipper;
        if (dto.ShippingLine        != null) order.TrShippingLine               = dto.ShippingLine;
        if (dto.ShippingAgent       != null) order.TrShippingAgent              = dto.ShippingAgent;
        if (dto.Vessel              != null) order.TrVessel                     = dto.Vessel;
        if (dto.ContainerNumber     != null) order.TrContainerNumber            = dto.ContainerNumber;
        if (dto.ConsolidationRef    != null) order.TrConsolidationRef           = dto.ConsolidationRef;
        if (dto.ContainerSize       != null) order.TrContainerSize              = dto.ContainerSize;
        if (dto.DateProFormaReceived.HasValue)   order.TrDateProFormaReceived   = dto.DateProFormaReceived;
        if (dto.QtyProForma.HasValue)            order.TrQtyProForma            = dto.QtyProForma;
        if (dto.FactoryReadyDate.HasValue)       order.TrFactoryReadyDate       = dto.FactoryReadyDate;
        if (dto.EstDepartureDate.HasValue)       order.TrEstDepartureDate       = dto.EstDepartureDate;
        if (dto.EstArrivalDate.HasValue)         order.TrEstArrivalDate         = dto.EstArrivalDate;
        if (dto.TransitTime         != null) order.TrTransitTime                = dto.TransitTime;
        if (dto.BijlageDone.HasValue)            order.TrBijlageDone            = dto.BijlageDone;
        if (dto.DateArrivalInvoice.HasValue)     order.TrDateArrivalInvoice     = dto.DateArrivalInvoice;
        if (dto.InvoiceNumber       != null) order.TrInvoiceNumber              = dto.InvoiceNumber;
        if (dto.DateArrivalBol.HasValue)         order.TrDateArrivalBol         = dto.DateArrivalBol;
        if (dto.Remarks             != null) order.TrRemarks                    = dto.Remarks;
        if (dto.DateArrivalNoteReceived.HasValue) order.TrDateArrivalNoteReceived = dto.DateArrivalNoteReceived;
        if (dto.DateManifestReceived.HasValue)    order.TrDateManifestReceived   = dto.DateManifestReceived;
        if (dto.DateCopiesToDeclarant.HasValue)   order.TrDateCopiesToDeclarant  = dto.DateCopiesToDeclarant;
        if (dto.DateCustomsPapersReady.HasValue)  order.TrDateCustomsPapersReady = dto.DateCustomsPapersReady;
        if (dto.DateCustomsPapersAsycuda.HasValue) order.TrDateCustomsPapersAsycuda = dto.DateCustomsPapersAsycuda;
        if (dto.DateContainerAtCps.HasValue)      order.TrDateContainerAtCps    = dto.DateContainerAtCps;
        if (dto.ExpirationDateCps.HasValue)       order.TrExpirationDateCps     = dto.ExpirationDateCps;
        if (dto.DateCustomsPapersToCps.HasValue)  order.TrDateCustomsPapersToCps = dto.DateCustomsPapersToCps;
        if (dto.DateContainerArrivedLicores.HasValue) order.TrDateContainerArrivedLicores = dto.DateContainerArrivedLicores;
        if (dto.DateContainerOpenedCustoms.HasValue)  order.TrDateContainerOpenedCustoms  = dto.DateContainerOpenedCustoms;
        if (dto.DateContainerUnloadReady.HasValue)    order.TrDateContainerUnloadReady    = dto.DateContainerUnloadReady;
        if (dto.ReturnDateContainer.HasValue)         order.TrReturnDateContainer         = dto.ReturnDateContainer;
        if (dto.DateUnloadPapersAdmin.HasValue)       order.TrDateUnloadPapersAdmin       = dto.DateUnloadPapersAdmin;
        if (dto.SadNumber           != null) order.TrSadNumber                   = dto.SadNumber;
        if (dto.BcNumberOrders      != null) order.TrBcNumberOrders              = dto.BcNumberOrders;
        if (dto.ExitNoteNumber      != null) order.TrExitNoteNumber              = dto.ExitNoteNumber;
        if (dto.IssuesComments      != null) order.TrIssuesComments              = dto.IssuesComments;
        if (dto.ReceiptStatus           != null) order.TrReceiptStatus             = dto.ReceiptStatus;
        if (dto.QtyShortage.HasValue)          order.TrQtyShortage               = dto.QtyShortage;
        if (dto.QtyDamages.HasValue)           order.TrQtyDamages                = dto.QtyDamages;
        if (dto.ReceiptComments         != null) order.TrReceiptComments         = dto.ReceiptComments;
        if (dto.SupplierName            != null) order.TrSupplierName            = dto.SupplierName;
        if (dto.VendorBrand             != null) order.TrVendorBrand             = dto.VendorBrand;
        if (dto.Country                 != null) order.TrCountry                 = dto.Country;
        if (dto.FreightForwarder        != null) order.TrFreightForwarder        = dto.FreightForwarder;
        if (dto.ActualDeliveryDate.HasValue)     order.TrActualDeliveryDate      = dto.ActualDeliveryDate;

        order.TrLastUpdateDate = DateTime.UtcNow;
        order.TrUpdatedBy      = CurrentUser;
        order.TrUpdatedAt      = DateTime.UtcNow;

        if (statusChanged)
        {
            _db.TrackingStatusHistories.Add(new TrackingStatusHistory
            {
                TshTrackingId = order.TrId,
                TshPoNo       = order.TrPoNo,
                TshStatusCode = dto.StatusCode!,
                TshStatusDate = DateTime.UtcNow,
                TshComments   = dto.StatusComment ?? $"Status changed to {dto.StatusCode}",
                TshChangedBy  = CurrentUser
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<TrackingOrderDto>.Ok(new TrackingOrderDto(order), "Updated."));
    }

    // ── Close tracking order (locks it for further edits) ────────────────────
    [HttpPost("{id:int}/close")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        var order = await _db.TrackingOrders.FindAsync([id], ct);
        if (order is null) return NotFound(ApiResponse.Fail($"Tracking order {id} not found."));
        if (order.TrIsClosed) return BadRequest(ApiResponse.Fail("Tracking order is already closed."));

        order.TrIsClosed   = true;
        order.TrClosedAt   = DateTime.UtcNow;
        order.TrClosedBy   = CurrentUser;
        order.TrUpdatedAt  = DateTime.UtcNow;
        order.TrUpdatedBy  = CurrentUser;

        _db.TrackingStatusHistories.Add(new TrackingStatusHistory
        {
            TshTrackingId = order.TrId,
            TshPoNo       = order.TrPoNo,
            TshStatusCode = order.TrStatusCode,
            TshStatusDate = DateTime.UtcNow,
            TshComments   = "Tracking order closed/locked.",
            TshChangedBy  = CurrentUser
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Tracking order {PoNo} closed by {User}", order.TrPoNo, CurrentUser);
        return Ok(ApiResponse<TrackingOrderDto>.Ok(new TrackingOrderDto(order), "Tracking order closed."));
    }

    // ── Reopen tracking order (SuperAdmin only) ───────────────────────────────
    [HttpPost("{id:int}/reopen")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Reopen(int id, CancellationToken ct)
    {
        var order = await _db.TrackingOrders.FindAsync([id], ct);
        if (order is null) return NotFound(ApiResponse.Fail($"Tracking order {id} not found."));
        if (!order.TrIsClosed) return BadRequest(ApiResponse.Fail("Tracking order is not closed."));

        order.TrIsClosed  = false;
        order.TrClosedAt  = null;
        order.TrClosedBy  = null;
        order.TrUpdatedAt = DateTime.UtcNow;
        order.TrUpdatedBy = CurrentUser;

        _db.TrackingStatusHistories.Add(new TrackingStatusHistory
        {
            TshTrackingId = order.TrId,
            TshPoNo       = order.TrPoNo,
            TshStatusCode = order.TrStatusCode,
            TshStatusDate = DateTime.UtcNow,
            TshComments   = "Tracking order reopened.",
            TshChangedBy  = CurrentUser
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Tracking order {PoNo} reopened by {User}", order.TrPoNo, CurrentUser);
        return Ok(ApiResponse<TrackingOrderDto>.Ok(new TrackingOrderDto(order), "Tracking order reopened."));
    }

    // ── Get orders sharing the same container number ──────────────────────────
    [HttpGet("by-container/{containerNo}")]
    public async Task<IActionResult> GetByContainer(string containerNo, CancellationToken ct)
    {
        var clean = containerNo.Trim().ToUpper();
        var orders = await _db.TrackingOrders.AsNoTracking()
            .Where(x => x.TrContainerNumber != null && x.TrContainerNumber.ToUpper() == clean)
            .OrderByDescending(x => x.TrCreatedAt)
            .Select(x => new { x.TrId, x.TrPoNo, x.TrStatusCode, x.TrSupplierName, x.TrWarehouse, x.TrBorw })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(orders));
    }

    // ── Delete tracking order ─────────────────────────────────────────────────
    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var order = await _db.TrackingOrders.Include(x => x.StatusHistory).FirstOrDefaultAsync(x => x.TrId == id, ct);
        if (order is null) return NotFound(ApiResponse.Fail($"Tracking order {id} not found."));
        _db.TrackingOrders.Remove(order);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }

    // ── Get status history for a tracking order ───────────────────────────────
    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id, CancellationToken ct)
    {
        var history = await _db.TrackingStatusHistories.AsNoTracking()
            .Where(x => x.TshTrackingId == id)
            .OrderByDescending(x => x.TshStatusDate)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<TrackingStatusHistory>>.Ok(history));
    }

    // ── Get PO product lines from PODTLT (VIP) ───────────────────────────────
    [HttpGet("{id:int}/products")]
    public async Task<IActionResult> GetProducts(int id, CancellationToken ct)
    {
        var order = await _db.TrackingOrders.AsNoTracking()
            .Where(x => x.TrId == id)
            .Select(x => new { x.TrPoNo })
            .FirstOrDefaultAsync(ct);
        if (order is null) return NotFound(ApiResponse.Fail($"Tracking order {id} not found."));

        // Use dbo.Description_Items_BEER() scalar function to get item descriptions
        var lines = await _dhw.PoDetails.AsNoTracking()
            .Where(x => x.PdPoNo.StartsWith(order.TrPoNo.Trim()))
            .OrderBy(x => x.PdLine)
            .Select(x => new
            {
                x.PdLine, x.PdItem, x.PdBran, x.PdBrvr,
                x.PdOqty, x.PdRqty, x.PdCstAmt, x.PdUnit,
                x.PdSitem, x.PdBsw, x.PdClas, x.PdStat, x.PdRdAt,
                ItemDesc = x.PdItem != null ? DhwDbContext.DescriptionItemsBeer(x.PdItem) : null
            })
            .ToListAsync(ct);

        var result = lines.Select(l => new PoDetailLineDto(
            Line:         l.PdLine,
            ItemCode:     l.PdItem?.TrimEnd(),
            ItemDesc:     l.ItemDesc?.TrimEnd(),
            BrandCode:    l.PdBran?.TrimEnd(),
            BrandDesc:    null,
            SupplierCode: l.PdBrvr?.TrimEnd(),
            SupplierName: null,
            QtyOrdered:   l.PdOqty,
            QtyReceived:  l.PdRqty,
            UnitPrice:    l.PdCstAmt,
            UnitsPerCase: l.PdUnit,
            SupplierItem: l.PdSitem?.TrimEnd(),
            BeerOrWine:   l.PdBsw?.TrimEnd(),
            Class:        l.PdClas?.TrimEnd(),
            Status:       l.PdStat?.TrimEnd(),
            ReceiveDate:  l.PdRdAt
        )).ToList();

        return Ok(ApiResponse<List<PoDetailLineDto>>.Ok(result));
    }

    // ── Auto-import all POHDRT records with PHSTAT=0 not yet tracked ─────────
    [HttpPost("auto-import-vip")]
    public async Task<IActionResult> AutoImportVip(CancellationToken ct)
    {
        try
        {
        // ── 1. Load VIP PO headers with PHSTAT starting with "0" ─────────────
        // PhStat is CHAR(2) so values may be "0 " — StartsWith("0") handles that.
        var vipPos = await _dhw.PoHeaders.AsNoTracking()
            .Where(x => x.PhStat != null && x.PhStat.StartsWith("0") && x.PhPoNo != null)
            .ToListAsync(ct);

        if (!vipPos.Any())
            return Ok(ApiResponse<object>.Ok(new { Imported = 0 }, "No VIP orders with PHSTAT=0 found."));

        // ── 2. PO numbers already tracked (case-insensitive, trimmed) ─────────
        var trackedPoNos = (await _db.TrackingOrders.AsNoTracking()
            .Select(x => x.TrPoNo)
            .ToListAsync(ct))
            .Select(p => p?.Trim() ?? "")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // ── 3. Keep only those not yet tracked ────────────────────────────────
        var toImport = vipPos
            .Where(po => !string.IsNullOrWhiteSpace(po.PhPoNo) &&
                         !trackedPoNos.Contains(po.PhPoNo!.Trim()))
            .ToList();

        if (!toImport.Any())
            return Ok(ApiResponse<object>.Ok(new { Imported = 0 }, "All VIP orders are already tracked."));

        // ── 4. Load ALL shippers into memory (small catalog) ─────────────────
        // EF Core cannot translate: shipperIds.Any(id => s.ShipperId.StartsWith(id))
        // so we load the full catalog and match in C#.
        var allShippers = await _dhw.ShipperMasters.AsNoTracking().ToListAsync(ct);

        // ── 5. Load ALL supplier names into memory ────────────────────────────
        var allSuppliers = await _dhw.Suppliers.AsNoTracking()
            .Where(s => s.DeleteFlag != "Y")
            .ToListAsync(ct);
        // Use GroupBy to handle duplicate supplier codes in SUPPLIERT
        var supplierNameMap = allSuppliers
            .Where(s => !string.IsNullOrWhiteSpace(s.Supplier))
            .GroupBy(s => s.Supplier!.Trim())
            .ToDictionary(g => g.Key, g => g.First().SupplierName?.Trim());

        var now = DateTime.UtcNow;
        var newOrders = new List<TrackingOrder>();

        foreach (var po in toImport)
        {
            var cleanPoNo = po.PhPoNo?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(cleanPoNo)) continue;

            // Freight forwarder: find shipper whose ShipperId starts with po.PhShip
            string? freightForwarder = null;
            var shipId = po.PhShip?.Trim();
            if (!string.IsNullOrWhiteSpace(shipId))
            {
                var match = allShippers.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.ShipperId) && s.ShipperId.Trim().StartsWith(shipId));
                freightForwarder = match?.ShipperName?.Trim();
            }

            // Supplier name: find supplier whose code is a prefix of po.PhBrvr
            string? supplierName = null;
            var brvrTrimmed = po.PhBrvr?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(brvrTrimmed))
                supplierName = supplierNameMap.FirstOrDefault(kvp => brvrTrimmed.StartsWith(kvp.Key)).Value;

            newOrders.Add(new TrackingOrder
            {
                TrPoNo             = cleanPoNo,
                TrWarehouse        = po.PhWhse?.Trim(),
                TrSupplier         = po.PhOvrNo?.Trim(),
                TrSupplierCode     = po.PhBrvr?.Trim(),
                TrSupplierName     = supplierName ?? po.PhOvrNo?.Trim(),
                TrBorw             = po.PhBorw?.Trim(),
                TrOrderDate        = (int?)po.PhOrdt,
                TrVipShipDate      = (int?)po.PhShdt,
                TrVipArrivalDate   = (int?)po.PhArdt,
                TrTotalCases       = po.PhOqt,
                TrVipWeight        = po.PhWeig,
                TrVipLiters        = po.PhLtrs,
                TrVipTotalAmount   = po.PhTotAmt,
                TrVipTotalLines    = (int?)po.PhLine,
                TrVipStatus        = po.PhStat?.Trim(),
                TrContainerNumber  = po.PhConNo?.Trim(),
                TrFreightForwarder = freightForwarder,
                TrStatusCode       = "INVIP",
                TrCreatedBy        = "VIP Auto-Import",
                TrCreatedAt        = now
            });
        }

        _db.TrackingOrders.AddRange(newOrders);
        await _db.SaveChangesAsync(ct);

        // Record initial status history for each imported order
        var histories = newOrders.Select(o => new TrackingStatusHistory
        {
            TshTrackingId = o.TrId,
            TshPoNo       = o.TrPoNo,
            TshStatusCode = "INVIP",
            TshStatusDate = now,
            TshComments   = "Auto-imported from VIP (PHSTAT=0).",
            TshChangedBy  = "VIP Auto-Import"
        }).ToList();

        _db.TrackingStatusHistories.AddRange(histories);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Auto-imported {Count} tracking orders from VIP (PHSTAT=0).", newOrders.Count);
        return Ok(ApiResponse<object>.Ok(new { Imported = newOrders.Count },
            $"{newOrders.Count} order(s) auto-imported from VIP."));

        } // end try
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoImportVip failed.");
            return StatusCode(500, ApiResponse.Fail($"Auto-import failed: {ex.Message}"));
        }
    }

    // ── Sync VIP data for a tracking order (refresh auto-filled fields) ───────
    [HttpPost("{id:int}/sync-vip")]
    public async Task<IActionResult> SyncVip(int id, CancellationToken ct)
    {
        var order = await _db.TrackingOrders.FindAsync([id], ct);
        if (order is null) return NotFound(ApiResponse.Fail($"Tracking order {id} not found."));

        var po = await _dhw.PoHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.PhPoNo.StartsWith(order.TrPoNo.Trim()), ct);
        if (po is null) return NotFound(ApiResponse.Fail($"PO {order.TrPoNo} not found in VIP."));

        // Refresh Freight Forwarder from VIP SHIPPER_MASTER via POHDRT.PHSHIP
        var shipperIdRawS = po.PhShip?.Trim();
        var shipperS = !string.IsNullOrWhiteSpace(shipperIdRawS)
            ? await _dhw.ShipperMasters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ShipperId.StartsWith(shipperIdRawS), ct)
            : null;

        var supplierNameS = await GetSupplierNameAsync(po.PhBrvr, ct);
        var vendorBrandS  = await GetVendorBrandAsync(order.TrPoNo, ct);

        order.TrWarehouse        = po.PhWhse?.Trim();
        order.TrSupplier         = po.PhOvrNo?.Trim();
        order.TrSupplierCode     = po.PhBrvr?.Trim();
        order.TrSupplierName     = supplierNameS ?? po.PhOvrNo?.Trim();
        order.TrVendorBrand      = vendorBrandS ?? order.TrVendorBrand;
        order.TrBorw             = po.PhBorw?.Trim();
        order.TrOrderDate        = (int?)po.PhOrdt;
        order.TrVipShipDate      = (int?)po.PhShdt;
        order.TrVipArrivalDate   = (int?)po.PhArdt;
        order.TrTotalCases       = po.PhOqt;
        order.TrVipWeight        = po.PhWeig;
        order.TrVipLiters        = po.PhLtrs;
        order.TrVipTotalAmount   = po.PhTotAmt;
        order.TrVipTotalLines    = (int?)po.PhLine;
        order.TrVipStatus        = po.PhStat?.Trim();
        order.TrContainerNumber  = po.PhConNo?.Trim();
        if (shipperS?.ShipperName != null)
            order.TrFreightForwarder = shipperS.ShipperName.Trim();
        order.TrUpdatedAt      = DateTime.UtcNow;
        order.TrUpdatedBy      = CurrentUser;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<TrackingOrderDto>.Ok(new TrackingOrderDto(order), "VIP data synced."));
    }
}

// ── DTO with calculated fields ─────────────────────────────────────────────────
public class TrackingOrderDto
{
    public TrackingOrder Order { get; }
    public int? DaysOverContainer { get; }
    public ICollection<TrackingStatusHistory> StatusHistory { get; }
    public ReceiptSummaryDto? ReceiptSummary { get; init; }

    public TrackingOrderDto(TrackingOrder t)
    {
        Order = t;
        StatusHistory = t.StatusHistory;
        if (t.TrDateContainerArrivedLicores.HasValue && t.TrReturnDateContainer.HasValue)
            DaysOverContainer = (int)(t.TrReturnDateContainer.Value - t.TrDateContainerArrivedLicores.Value).TotalDays;
    }
}

public class ReceiptSummaryDto
{
    public decimal TotalOrdered  { get; init; }
    public decimal TotalReceived { get; init; }
    public decimal Difference    { get; init; }   // negative = shortage, positive = overage
    public string  Status        { get; init; } = "PENDING";

    public static ReceiptSummaryDto Calculate(decimal ordered, decimal received)
    {
        var diff   = received - ordered;
        var status = ordered == 0      ? "PENDING"
                   : received == 0     ? "PENDING"
                   : diff == 0         ? "COMPLETE"
                   : diff < 0          ? "SHORTAGE"
                                       : "OVERAGE";
        return new ReceiptSummaryDto
        {
            TotalOrdered  = ordered,
            TotalReceived = received,
            Difference    = diff,
            Status        = status
        };
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record CreateTrackingDto(
    string PoNo,
    string? FreightForwarder,
    string? StatusCode,
    string? Comments
);

public record PoDetailLineDto(
    decimal  Line,
    string?  ItemCode,
    string?  ItemDesc,
    string?  BrandCode,
    string?  BrandDesc,
    string?  SupplierCode,
    string?  SupplierName,
    decimal? QtyOrdered,
    decimal? QtyReceived,
    decimal? UnitPrice,
    decimal? UnitsPerCase,
    string?  SupplierItem,
    string?  BeerOrWine,
    string?  Class,
    string?  Status,
    decimal? ReceiveDate
);

public record UpdateTrackingDto(
    string? StatusCode,
    string? StatusComment,
    string? Comments,
    DateTime? LastUpdateDate,
    DateTime? RequestedEta,
    bool? AcknowledgeOrder,
    DateTime? DateLoadingShipper,
    string? ShippingLine,
    string? ShippingAgent,
    string? Vessel,
    string? ContainerNumber,
    string? ConsolidationRef,
    string? ContainerSize,
    DateTime? DateProFormaReceived,
    decimal? QtyProForma,
    DateTime? FactoryReadyDate,
    DateTime? EstDepartureDate,
    DateTime? EstArrivalDate,
    string? TransitTime,
    bool? BijlageDone,
    DateTime? DateArrivalInvoice,
    string? InvoiceNumber,
    DateTime? DateArrivalBol,
    string? Remarks,
    DateTime? DateArrivalNoteReceived,
    DateTime? DateManifestReceived,
    DateTime? DateCopiesToDeclarant,
    DateTime? DateCustomsPapersReady,
    DateTime? DateCustomsPapersAsycuda,
    DateTime? DateContainerAtCps,
    DateTime? ExpirationDateCps,
    DateTime? DateCustomsPapersToCps,
    DateTime? DateContainerArrivedLicores,
    DateTime? DateContainerOpenedCustoms,
    DateTime? DateContainerUnloadReady,
    DateTime? ReturnDateContainer,
    DateTime? DateUnloadPapersAdmin,
    string? SadNumber,
    string? BcNumberOrders,
    string? ExitNoteNumber,
    string? IssuesComments,
    string? ReceiptStatus,
    decimal? QtyShortage,
    decimal? QtyDamages,
    string? ReceiptComments,
    string? SupplierName,
    string? VendorBrand,
    string? Country,
    string? FreightForwarder,
    DateTime? ActualDeliveryDate
);
