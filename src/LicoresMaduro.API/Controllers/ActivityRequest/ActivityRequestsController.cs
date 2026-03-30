using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/requests")]
[Authorize]
[Produces("application/json")]
public sealed class ActivityRequestsController : ControllerBase
{
    private readonly ApplicationDbContext                 _db;
    private readonly ILogger<ActivityRequestsController> _logger;

    // Valid status workflow transitions
    private static readonly Dictionary<string, string[]> _allowedTransitions = new()
    {
        ["DRAFT"]     = ["PENDING"],
        ["PENDING"]   = ["APPROVED", "DENIED"],
        ["APPROVED"]  = ["INPROCESS"],
        ["INPROCESS"] = ["READY"],
        ["READY"]     = ["INVOICED"],
        ["INVOICED"]  = [],
        ["DENIED"]    = ["DRAFT"]   // allow reopen
    };

    public ActivityRequestsController(ApplicationDbContext db, ILogger<ActivityRequestsController> logger)
    { _db = db; _logger = logger; }

    // ── GET /api/activity/requests ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int?    year            = null,
        [FromQuery] string? status          = null,
        [FromQuery] string? search          = null,
        [FromQuery] bool    includeInactive = false,
        [FromQuery] int     page            = 1,
        [FromQuery] int     pageSize        = 50,
        CancellationToken   ct              = default)
    {
        var q = _db.ActivityRequests.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (year.HasValue)    q = q.Where(x => x.ArYear == year.Value);
        if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
            q = q.Where(x => x.ArStatus == status);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x =>
                x.ArNumber.Contains(search)                              ||
                (x.ArSupplierName != null && x.ArSupplierName.Contains(search)) ||
                (x.ArBrand        != null && x.ArBrand.Contains(search))        ||
                (x.ArDescription  != null && x.ArDescription.Contains(search)));

        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(x => x.ArId)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync(ct);
        return Ok(PagedResponse<ActivityRequestHeader>.Ok(data, page, pageSize, total));
    }

    // ── GET /api/activity/requests/{id} ───────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.ActivityRequests.FindAsync([id], ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"Activity request {id} not found."))
            : Ok(ApiResponse<ActivityRequestHeader>.Ok(e));
    }

    // ── GET /api/activity/requests/years ──────────────────────────────────────
    [HttpGet("years")]
    public async Task<IActionResult> GetYears(CancellationToken ct)
    {
        var years = await _db.ActivityRequests
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.ArYear)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<int>>.Ok(years));
    }

    // ── GET /api/activity/requests/summary ────────────────────────────────────
    // Returns count per status for the given year (for KPI cards)
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int? year = null, CancellationToken ct = default)
    {
        var q = _db.ActivityRequests.AsNoTracking().Where(x => x.IsActive);
        if (year.HasValue) q = q.Where(x => x.ArYear == year.Value);

        var counts = await q
            .GroupBy(x => x.ArStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = counts.Sum(x => x.Count);
        var dict  = counts.ToDictionary(x => x.Status, x => x.Count);
        var result = new
        {
            Total     = total,
            Draft     = dict.GetValueOrDefault("DRAFT"),
            Pending   = dict.GetValueOrDefault("PENDING"),
            Approved  = dict.GetValueOrDefault("APPROVED"),
            InProcess = dict.GetValueOrDefault("INPROCESS"),
            Ready     = dict.GetValueOrDefault("READY"),
            Invoiced  = dict.GetValueOrDefault("INVOICED"),
            Denied    = dict.GetValueOrDefault("DENIED")
        };
        return Ok(ApiResponse<object>.Ok(result));
    }

    // ── POST /api/activity/requests ───────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ActivityRequestDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var currentUser = GetCurrentUser();
        var arNumber    = await GenerateNumber(dto.ArYear, ct);

        var entity = new ActivityRequestHeader
        {
            ArNumber               = arNumber,
            ArYear                 = dto.ArYear,
            ArStatus               = "DRAFT",
            ArSupplierCode         = dto.ArSupplierCode,
            ArSupplierName         = dto.ArSupplierName,
            ArBrand                = dto.ArBrand,
            ArActivityTypeCode     = dto.ArActivityTypeCode,
            ArActivityTypeDesc     = dto.ArActivityTypeDesc,
            ArDescription          = dto.ArDescription,
            ArStartDate            = dto.ArStartDate,
            ArEndDate              = dto.ArEndDate,
            ArLocationCode         = dto.ArLocationCode,
            ArLocationName         = dto.ArLocationName,
            ArBudget               = dto.ArBudget,
            ArSegmentCode          = dto.ArSegmentCode,
            ArTargetGroupCode      = dto.ArTargetGroupCode,
            ArSalesGroupCode       = dto.ArSalesGroupCode,
            ArNonClientCode        = dto.ArNonClientCode,
            ArNonClientName        = dto.ArNonClientName,
            ArFacilitatorCode      = dto.ArFacilitatorCode,
            ArFacilitatorName      = dto.ArFacilitatorName,
            ArSponsoringTypeCode   = dto.ArSponsoringTypeCode,
            ArEntertainmentTypeCode= dto.ArEntertainmentTypeCode,
            ArNotes                = dto.ArNotes,
            ArCreatedBy            = currentUser?.UserId,
            ArCreatedByName        = currentUser?.FullName ?? currentUser?.Username,
            IsActive               = true,
            CreatedAt              = DateTime.UtcNow
        };

        _db.ActivityRequests.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("ActivityRequest '{Number}' created (id={Id})", entity.ArNumber, entity.ArId);
        return CreatedAtAction(nameof(GetById), new { id = entity.ArId },
            ApiResponse<ActivityRequestHeader>.Ok(entity, "Activity request created."));
    }

    // ── PUT /api/activity/requests/{id} ───────────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ActivityRequestDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var entity = await _db.ActivityRequests.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Activity request {id} not found."));
        if (entity.ArStatus != "DRAFT")
            return BadRequest(ApiResponse.Fail("Only DRAFT requests can be edited."));

        entity.ArYear                  = dto.ArYear;
        entity.ArSupplierCode          = dto.ArSupplierCode;
        entity.ArSupplierName          = dto.ArSupplierName;
        entity.ArBrand                 = dto.ArBrand;
        entity.ArActivityTypeCode      = dto.ArActivityTypeCode;
        entity.ArActivityTypeDesc      = dto.ArActivityTypeDesc;
        entity.ArDescription           = dto.ArDescription;
        entity.ArStartDate             = dto.ArStartDate;
        entity.ArEndDate               = dto.ArEndDate;
        entity.ArLocationCode          = dto.ArLocationCode;
        entity.ArLocationName          = dto.ArLocationName;
        entity.ArBudget                = dto.ArBudget;
        entity.ArSegmentCode           = dto.ArSegmentCode;
        entity.ArTargetGroupCode       = dto.ArTargetGroupCode;
        entity.ArSalesGroupCode        = dto.ArSalesGroupCode;
        entity.ArNonClientCode         = dto.ArNonClientCode;
        entity.ArNonClientName         = dto.ArNonClientName;
        entity.ArFacilitatorCode       = dto.ArFacilitatorCode;
        entity.ArFacilitatorName       = dto.ArFacilitatorName;
        entity.ArSponsoringTypeCode    = dto.ArSponsoringTypeCode;
        entity.ArEntertainmentTypeCode = dto.ArEntertainmentTypeCode;
        entity.ArNotes                 = dto.ArNotes;

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ActivityRequestHeader>.Ok(entity, "Activity request updated."));
    }

    // ── PATCH /api/activity/requests/{id}/status ──────────────────────────────
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusDto dto, CancellationToken ct)
    {
        var entity = await _db.ActivityRequests.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Activity request {id} not found."));

        var newStatus = dto.Status.ToUpper();
        if (!_allowedTransitions.TryGetValue(entity.ArStatus, out var allowed) || !allowed.Contains(newStatus))
            return BadRequest(ApiResponse.Fail($"Cannot transition from {entity.ArStatus} to {newStatus}."));

        var currentUser = GetCurrentUser();
        entity.ArStatus = newStatus;

        if (newStatus == "APPROVED")
        {
            entity.ArApprovedBy     = currentUser?.UserId;
            entity.ArApprovedByName = currentUser?.FullName ?? currentUser?.Username;
            entity.ArApprovedAt     = DateTime.UtcNow;
        }
        else if (newStatus == "DENIED")
        {
            entity.ArDeniedBy       = currentUser?.UserId;
            entity.ArDeniedByName   = currentUser?.FullName ?? currentUser?.Username;
            entity.ArDeniedAt       = DateTime.UtcNow;
            entity.ArDenialReason   = dto.Reason;
        }
        else if (newStatus == "DRAFT")
        {
            // Reopened from DENIED — clear denial info
            entity.ArDeniedBy     = null;
            entity.ArDeniedByName = null;
            entity.ArDeniedAt     = null;
            entity.ArDenialReason = null;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("ActivityRequest {Id} status → {Status}", id, newStatus);
        return Ok(ApiResponse<ActivityRequestHeader>.Ok(entity, $"Status changed to {newStatus}."));
    }

    // ── DELETE /api/activity/requests/{id} ────────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.ActivityRequests.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Activity request {id} not found."));
        if (entity.ArStatus != "DRAFT")
            return BadRequest(ApiResponse.Fail("Only DRAFT requests can be deleted."));

        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("ActivityRequest {Id} ({Number}) deleted", id, entity.ArNumber);
        return Ok(ApiResponse.Ok("Activity request deleted."));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private LmUser? GetCurrentUser()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId)) return null;
        return _db.LmUsers.AsNoTracking().FirstOrDefault(u => u.UserId == userId);
    }

    private async Task<string> GenerateNumber(int year, CancellationToken ct)
    {
        var count = await _db.ActivityRequests
            .AsNoTracking()
            .CountAsync(x => x.ArYear == year, ct);
        return $"AR-{year}-{(count + 1):D4}";
    }

    // ── Brands sub-table ──────────────────────────────────────────────────────

    [HttpGet("{id:int}/brands")]
    public async Task<IActionResult> GetBrands(int id, CancellationToken ct)
    {
        var items = await _db.ActivityRqBrands.AsNoTracking()
            .Where(x => x.ArbArId == id && x.IsActive)
            .OrderBy(x => x.ArbId)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<ActivityRqBrand>>.Ok(items));
    }

    [HttpPost("{id:int}/brands")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddBrand(int id, [FromBody] ActivityRqBrandDto dto, CancellationToken ct)
    {
        var ar = await _db.ActivityRequests.FindAsync([id], ct);
        if (ar is null) return NotFound(ApiResponse.Fail("Activity request not found."));

        var entity = new ActivityRqBrand
        {
            ArbArId         = id,
            ArbSupplierCode = dto.ArbSupplierCode,
            ArbSupplierName = dto.ArbSupplierName,
            ArbBrand        = dto.ArbBrand,
            ArbBudget       = dto.ArbBudget,
            ArbNotes        = dto.ArbNotes,
            IsActive        = true,
            CreatedAt       = DateTime.UtcNow
        };
        _db.ActivityRqBrands.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ActivityRqBrand>.Ok(entity, "Brand added."));
    }

    [HttpDelete("{id:int}/brands/{brandId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemoveBrand(int id, int brandId, CancellationToken ct)
    {
        var entity = await _db.ActivityRqBrands.FirstOrDefaultAsync(x => x.ArbId == brandId && x.ArbArId == id, ct);
        if (entity is null) return NotFound(ApiResponse.Fail("Brand row not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Brand removed."));
    }

    // ── Products sub-table ────────────────────────────────────────────────────

    [HttpGet("{id:int}/products")]
    public async Task<IActionResult> GetProducts(int id, CancellationToken ct)
    {
        var items = await _db.ActivityRqProducts.AsNoTracking()
            .Where(x => x.ArpArId == id && x.IsActive)
            .OrderBy(x => x.ArpId)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<ActivityRqProduct>>.Ok(items));
    }

    [HttpPost("{id:int}/products")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddProduct(int id, [FromBody] ActivityRqProductDto dto, CancellationToken ct)
    {
        var ar = await _db.ActivityRequests.FindAsync([id], ct);
        if (ar is null) return NotFound(ApiResponse.Fail("Activity request not found."));

        var entity = new ActivityRqProduct
        {
            ArpArId       = id,
            ArpProductCode= dto.ArpProductCode,
            ArpProductName= dto.ArpProductName,
            ArpQuantity   = dto.ArpQuantity,
            ArpUnit       = dto.ArpUnit,
            ArpNotes      = dto.ArpNotes,
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };
        _db.ActivityRqProducts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ActivityRqProduct>.Ok(entity, "Product added."));
    }

    [HttpDelete("{id:int}/products/{productId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemoveProduct(int id, int productId, CancellationToken ct)
    {
        var entity = await _db.ActivityRqProducts.FirstOrDefaultAsync(x => x.ArpId == productId && x.ArpArId == id, ct);
        if (entity is null) return NotFound(ApiResponse.Fail("Product row not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Product removed."));
    }

    // ── Generic sub-table helper ───────────────────────────────────────────────
    private static async Task<IActionResult?> ValidateAr(ApplicationDbContext db, int id, CancellationToken ct, bool draftOnly = true)
    {
        var ar = await db.ActivityRequests.FindAsync([id], ct);
        if (ar is null) return new NotFoundObjectResult(ApiResponse.Fail("Activity request not found."));
        if (draftOnly && ar.ArStatus != "DRAFT") return new BadRequestObjectResult(ApiResponse.Fail("Operation only allowed on DRAFT requests."));
        return null;
    }

    // ── Cash ──────────────────────────────────────────────────────────────────
    [HttpGet("{id:int}/cash")]
    public async Task<IActionResult> GetCash(int id, CancellationToken ct)
    {
        var items = await _db.ActivityRqCashes.AsNoTracking().Where(x => x.ArcArId == id && x.IsActive).OrderBy(x => x.ArcId).ToListAsync(ct);
        return Ok(ApiResponse<List<ActivityRqCash>>.Ok(items));
    }
    [HttpPost("{id:int}/cash"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddCash(int id, [FromBody] ArCashDto dto, CancellationToken ct)
    {
        var err = await ValidateAr(_db, id, ct); if (err is not null) return err;
        var e = new ActivityRqCash { ArcArId=id, ArcType=dto.ArcType, ArcAmount=dto.ArcAmount, ArcReference=dto.ArcReference, ArcNotes=dto.ArcNotes, IsActive=true, CreatedAt=DateTime.UtcNow };
        _db.ActivityRqCashes.Add(e); await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ActivityRqCash>.Ok(e, "Added."));
    }
    [HttpDelete("{id:int}/cash/{rowId:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemoveCash(int id, int rowId, CancellationToken ct)
    {
        var e = await _db.ActivityRqCashes.FirstOrDefaultAsync(x => x.ArcId == rowId && x.ArcArId == id, ct);
        if (e is null) return NotFound(ApiResponse.Fail("Row not found.")); e.IsActive=false; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Removed."));
    }

    // ── Print ─────────────────────────────────────────────────────────────────
    [HttpGet("{id:int}/print")]
    public async Task<IActionResult> GetPrint(int id, CancellationToken ct)
    {
        var items = await _db.ActivityRqPrints.AsNoTracking().Where(x => x.ArprArId == id && x.IsActive).OrderBy(x => x.ArprId).ToListAsync(ct);
        return Ok(ApiResponse<List<ActivityRqPrint>>.Ok(items));
    }
    [HttpPost("{id:int}/print"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddPrint(int id, [FromBody] ArPrintDto dto, CancellationToken ct)
    {
        var err = await ValidateAr(_db, id, ct); if (err is not null) return err;
        var e = new ActivityRqPrint { ArprArId=id, ArprPublication=dto.ArprPublication, ArprFormat=dto.ArprFormat, ArprSize=dto.ArprSize, ArprCost=dto.ArprCost, ArprNotes=dto.ArprNotes, IsActive=true, CreatedAt=DateTime.UtcNow };
        _db.ActivityRqPrints.Add(e); await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ActivityRqPrint>.Ok(e, "Added."));
    }
    [HttpDelete("{id:int}/print/{rowId:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemovePrint(int id, int rowId, CancellationToken ct)
    {
        var e = await _db.ActivityRqPrints.FirstOrDefaultAsync(x => x.ArprId == rowId && x.ArprArId == id, ct);
        if (e is null) return NotFound(ApiResponse.Fail("Row not found.")); e.IsActive=false; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Removed."));
    }

    // ── Radio ─────────────────────────────────────────────────────────────────
    [HttpGet("{id:int}/radio")]
    public async Task<IActionResult> GetRadio(int id, CancellationToken ct)
    {
        var items = await _db.ActivityRqRadios.AsNoTracking().Where(x => x.ArrArId == id && x.IsActive).OrderBy(x => x.ArrId).ToListAsync(ct);
        return Ok(ApiResponse<List<ActivityRqRadio>>.Ok(items));
    }
    [HttpPost("{id:int}/radio"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddRadio(int id, [FromBody] ArRadioDto dto, CancellationToken ct)
    {
        var err = await ValidateAr(_db, id, ct); if (err is not null) return err;
        var e = new ActivityRqRadio { ArrArId=id, ArrStation=dto.ArrStation, ArrDuration=dto.ArrDuration, ArrFrequency=dto.ArrFrequency, ArrCost=dto.ArrCost, ArrNotes=dto.ArrNotes, IsActive=true, CreatedAt=DateTime.UtcNow };
        _db.ActivityRqRadios.Add(e); await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ActivityRqRadio>.Ok(e, "Added."));
    }
    [HttpDelete("{id:int}/radio/{rowId:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemoveRadio(int id, int rowId, CancellationToken ct)
    {
        var e = await _db.ActivityRqRadios.FirstOrDefaultAsync(x => x.ArrId == rowId && x.ArrArId == id, ct);
        if (e is null) return NotFound(ApiResponse.Fail("Row not found.")); e.IsActive=false; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Removed."));
    }

    // ── POS Materials ─────────────────────────────────────────────────────────
    [HttpGet("{id:int}/pos-mat")]
    public async Task<IActionResult> GetPosMat(int id, CancellationToken ct)
    {
        var items = await _db.ActivityRqPosMats.AsNoTracking().Where(x => x.ArpmArId == id && x.IsActive).OrderBy(x => x.ArpmId).ToListAsync(ct);
        return Ok(ApiResponse<List<ActivityRqPosMat>>.Ok(items));
    }
    [HttpPost("{id:int}/pos-mat"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddPosMat(int id, [FromBody] ArPosMatDto dto, CancellationToken ct)
    {
        var err = await ValidateAr(_db, id, ct); if (err is not null) return err;
        var e = new ActivityRqPosMat { ArpmArId=id, ArpmCode=dto.ArpmCode, ArpmName=dto.ArpmName, ArpmQuantity=dto.ArpmQuantity, ArpmUnit=dto.ArpmUnit, ArpmNotes=dto.ArpmNotes, IsActive=true, CreatedAt=DateTime.UtcNow };
        _db.ActivityRqPosMats.Add(e); await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ActivityRqPosMat>.Ok(e, "Added."));
    }
    [HttpDelete("{id:int}/pos-mat/{rowId:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemovePosMat(int id, int rowId, CancellationToken ct)
    {
        var e = await _db.ActivityRqPosMats.FirstOrDefaultAsync(x => x.ArpmId == rowId && x.ArpmArId == id, ct);
        if (e is null) return NotFound(ApiResponse.Fail("Row not found.")); e.IsActive=false; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Removed."));
    }

    // ── Promotions ────────────────────────────────────────────────────────────
    [HttpGet("{id:int}/promotions")]
    public async Task<IActionResult> GetPromotions(int id, CancellationToken ct)
    {
        var items = await _db.ActivityRqPromotions.AsNoTracking().Where(x => x.ArpoArId == id && x.IsActive).OrderBy(x => x.ArpoId).ToListAsync(ct);
        return Ok(ApiResponse<List<ActivityRqPromotion>>.Ok(items));
    }
    [HttpPost("{id:int}/promotions"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddPromotion(int id, [FromBody] ArPromotionDto dto, CancellationToken ct)
    {
        var err = await ValidateAr(_db, id, ct); if (err is not null) return err;
        var e = new ActivityRqPromotion { ArpoArId=id, ArpoType=dto.ArpoType, ArpoDescription=dto.ArpoDescription, ArpoCost=dto.ArpoCost, ArpoNotes=dto.ArpoNotes, IsActive=true, CreatedAt=DateTime.UtcNow };
        _db.ActivityRqPromotions.Add(e); await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ActivityRqPromotion>.Ok(e, "Added."));
    }
    [HttpDelete("{id:int}/promotions/{rowId:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemovePromotion(int id, int rowId, CancellationToken ct)
    {
        var e = await _db.ActivityRqPromotions.FirstOrDefaultAsync(x => x.ArpoId == rowId && x.ArpoArId == id, ct);
        if (e is null) return NotFound(ApiResponse.Fail("Row not found.")); e.IsActive=false; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Removed."));
    }

    // ── Others ────────────────────────────────────────────────────────────────
    [HttpGet("{id:int}/others")]
    public async Task<IActionResult> GetOthers(int id, CancellationToken ct)
    {
        var items = await _db.ActivityRqOthers.AsNoTracking().Where(x => x.AroArId == id && x.IsActive).OrderBy(x => x.AroId).ToListAsync(ct);
        return Ok(ApiResponse<List<ActivityRqOther>>.Ok(items));
    }
    [HttpPost("{id:int}/others"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddOther(int id, [FromBody] ArOtherDto dto, CancellationToken ct)
    {
        var err = await ValidateAr(_db, id, ct); if (err is not null) return err;
        var e = new ActivityRqOther { AroArId=id, AroDescription=dto.AroDescription, AroCost=dto.AroCost, AroNotes=dto.AroNotes, IsActive=true, CreatedAt=DateTime.UtcNow };
        _db.ActivityRqOthers.Add(e); await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ActivityRqOther>.Ok(e, "Added."));
    }
    [HttpDelete("{id:int}/others/{rowId:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemoveOther(int id, int rowId, CancellationToken ct)
    {
        var e = await _db.ActivityRqOthers.FirstOrDefaultAsync(x => x.AroId == rowId && x.AroArId == id, ct);
        if (e is null) return NotFound(ApiResponse.Fail("Row not found.")); e.IsActive=false; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Removed."));
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────
public sealed record ActivityRequestDto(
    int       ArYear,
    string?   ArSupplierCode,
    string?   ArSupplierName,
    string?   ArBrand,
    string?   ArActivityTypeCode,
    string?   ArActivityTypeDesc,
    string?   ArDescription,
    DateOnly? ArStartDate,
    DateOnly? ArEndDate,
    string?   ArLocationCode,
    string?   ArLocationName,
    decimal?  ArBudget,
    string?   ArSegmentCode,
    string?   ArTargetGroupCode,
    string?   ArSalesGroupCode,
    string?   ArNonClientCode,
    string?   ArNonClientName,
    string?   ArFacilitatorCode,
    string?   ArFacilitatorName,
    string?   ArSponsoringTypeCode,
    string?   ArEntertainmentTypeCode,
    string?   ArNotes
);

public sealed record ChangeStatusDto(string Status, string? Reason = null);

public sealed record ActivityRqBrandDto(
    string?  ArbSupplierCode,
    string?  ArbSupplierName,
    string?  ArbBrand,
    decimal? ArbBudget,
    string?  ArbNotes
);

public sealed record ActivityRqProductDto(
    string?  ArpProductCode,
    string?  ArpProductName,
    decimal? ArpQuantity,
    string?  ArpUnit,
    string?  ArpNotes
);

public sealed record ArCashDto(string? ArcType, decimal? ArcAmount, string? ArcReference, string? ArcNotes);
public sealed record ArPrintDto(string? ArprPublication, string? ArprFormat, string? ArprSize, decimal? ArprCost, string? ArprNotes);
public sealed record ArRadioDto(string? ArrStation, string? ArrDuration, int? ArrFrequency, decimal? ArrCost, string? ArrNotes);
public sealed record ArPosMatDto(string? ArpmCode, string? ArpmName, int? ArpmQuantity, string? ArpmUnit, string? ArpmNotes);
public sealed record ArPromotionDto(string? ArpoType, string? ArpoDescription, decimal? ArpoCost, string? ArpoNotes);
public sealed record ArOtherDto(string? AroDescription, decimal? AroCost, string? AroNotes);
