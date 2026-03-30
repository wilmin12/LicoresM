using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/pos-lend-out")]
[Authorize]
[Produces("application/json")]
public sealed class PosLendOutController : ControllerBase
{
    private readonly ApplicationDbContext        _db;
    private readonly ILogger<PosLendOutController> _logger;

    private static readonly Dictionary<string, string[]> _allowedTransitions = new()
    {
        ["DRAFT"]    = ["LENT"],
        ["LENT"]     = ["RETURNED", "PARTIAL"],
        ["PARTIAL"]  = ["RETURNED"],
        ["RETURNED"] = []
    };

    public PosLendOutController(ApplicationDbContext db, ILogger<PosLendOutController> logger)
    {
        _db = db; _logger = logger;
    }

    // ── GET /api/activity/pos-lend-out ────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search          = null,
        [FromQuery] string? status          = null,
        [FromQuery] int?    year            = null,
        [FromQuery] bool    includeInactive = false,
        [FromQuery] int     page            = 1,
        [FromQuery] int     pageSize        = 50,
        CancellationToken   ct              = default)
    {
        var q = _db.PosLendOuts.AsNoTracking();

        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.PloStatus == status);
        if (year.HasValue) q = q.Where(x => x.PloYear == year.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.PloNumber.Contains(search) ||
                              (x.PloClientName != null && x.PloClientName.Contains(search)) ||
                              (x.PloClientCode != null && x.PloClientCode.Contains(search)));

        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(x => x.PloId)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync(ct);

        return Ok(PagedResponse<PosLendOut>.Ok(data, page, pageSize, total));
    }

    // ── GET /api/activity/pos-lend-out/years ──────────────────────────────────
    [HttpGet("years")]
    public async Task<IActionResult> GetYears(CancellationToken ct)
    {
        var years = await _db.PosLendOuts.AsNoTracking()
            .Select(x => x.PloYear).Distinct().OrderByDescending(y => y).ToListAsync(ct);
        return Ok(ApiResponse<List<int>>.Ok(years));
    }

    // ── GET /api/activity/pos-lend-out/summary ────────────────────────────────
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int? year = null, CancellationToken ct = default)
    {
        var q = _db.PosLendOuts.AsNoTracking().Where(x => x.IsActive);
        if (year.HasValue) q = q.Where(x => x.PloYear == year.Value);

        var counts = await q.GroupBy(x => x.PloStatus)
                            .Select(g => new { status = g.Key, count = g.Count() })
                            .ToListAsync(ct);

        var result = new Dictionary<string, int>
        {
            ["DRAFT"]    = 0, ["LENT"] = 0,
            ["PARTIAL"]  = 0, ["RETURNED"] = 0
        };
        foreach (var c in counts)
            if (result.ContainsKey(c.status)) result[c.status] = c.count;

        return Ok(ApiResponse<Dictionary<string, int>>.Ok(result));
    }

    // ── GET /api/activity/pos-lend-out/{id} ───────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.PosLendOuts.FindAsync([id], ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"Lend out {id} not found."))
            : Ok(ApiResponse<PosLendOut>.Ok(e));
    }

    // ── POST /api/activity/pos-lend-out ───────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] PosLendOutDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var user   = GetCurrentUser();
        var year   = dto.PloYear ?? DateTime.UtcNow.Year;
        var number = await GenerateNumber(year, ct);

        var entity = new PosLendOut
        {
            PloNumber        = number,
            PloYear          = year,
            PloStatus        = "DRAFT",
            PloDate          = dto.PloDate,
            PloExpectedReturn= dto.PloExpectedReturn,
            PloClientCode    = dto.PloClientCode,
            PloClientName    = dto.PloClientName,
            PloContactName   = dto.PloContactName,
            PloContactPhone  = dto.PloContactPhone,
            PloNotes         = dto.PloNotes,
            PloCreatedById   = user?.UserId,
            PloCreatedByName = user?.FullName,
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow
        };

        _db.PosLendOuts.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("PosLendOut {Number} created", entity.PloNumber);
        return CreatedAtAction(nameof(GetById), new { id = entity.PloId }, ApiResponse<PosLendOut>.Ok(entity, "Lend out created."));
    }

    // ── PUT /api/activity/pos-lend-out/{id} ───────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] PosLendOutDto dto, CancellationToken ct)
    {
        var entity = await _db.PosLendOuts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Lend out {id} not found."));
        if (entity.PloStatus != "DRAFT")
            return BadRequest(ApiResponse.Fail("Only DRAFT records can be edited."));

        entity.PloDate          = dto.PloDate;
        entity.PloExpectedReturn= dto.PloExpectedReturn;
        entity.PloClientCode    = dto.PloClientCode;
        entity.PloClientName    = dto.PloClientName;
        entity.PloContactName   = dto.PloContactName;
        entity.PloContactPhone  = dto.PloContactPhone;
        entity.PloNotes         = dto.PloNotes;

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<PosLendOut>.Ok(entity, "Lend out updated."));
    }

    // ── PATCH /api/activity/pos-lend-out/{id}/status ──────────────────────────
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] PloChangeStatusDto dto, CancellationToken ct)
    {
        var entity = await _db.PosLendOuts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Lend out {id} not found."));

        if (!_allowedTransitions.TryGetValue(entity.PloStatus, out var allowed) || !allowed.Contains(dto.Status))
            return BadRequest(ApiResponse.Fail($"Cannot transition from {entity.PloStatus} to {dto.Status}."));

        if (dto.Status == "RETURNED")
            entity.PloActualReturn = dto.ActualReturn ?? DateOnly.FromDateTime(DateTime.UtcNow);

        entity.PloStatus = dto.Status;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<PosLendOut>.Ok(entity, $"Status changed to {dto.Status}."));
    }

    // ── DELETE /api/activity/pos-lend-out/{id} ────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.PosLendOuts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Lend out {id} not found."));
        if (entity.PloStatus != "DRAFT")
            return BadRequest(ApiResponse.Fail("Only DRAFT records can be deleted."));

        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("PosLendOut {Id} ({Number}) deleted", id, entity.PloNumber);
        return Ok(ApiResponse.Ok("Lend out deleted."));
    }

    // ── GET /api/activity/pos-lend-out/{id}/items ────────────────────────────
    [HttpGet("{id:int}/items")]
    public async Task<IActionResult> GetItems(int id, CancellationToken ct)
    {
        var items = await _db.PosLendOutItems.AsNoTracking()
            .Where(x => x.PloiPloId == id && x.IsActive)
            .OrderBy(x => x.PloiId)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<PosLendOutItem>>.Ok(items));
    }

    // ── POST /api/activity/pos-lend-out/{id}/items ───────────────────────────
    [HttpPost("{id:int}/items")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddItem(int id, [FromBody] PosLendOutItemDto dto, CancellationToken ct)
    {
        var header = await _db.PosLendOuts.FindAsync([id], ct);
        if (header is null) return NotFound(ApiResponse.Fail("Lend out not found."));
        if (header.PloStatus != "DRAFT")
            return BadRequest(ApiResponse.Fail("Items can only be added to DRAFT records."));

        var item = new PosLendOutItem
        {
            PloiPloId          = id,
            PloiPmCode         = dto.PloiPmCode,
            PloiPmName         = dto.PloiPmName,
            PloiQuantityLent   = dto.PloiQuantityLent,
            PloiQuantityReturned = 0,
            PloiNotes          = dto.PloiNotes,
            IsActive           = true,
            CreatedAt          = DateTime.UtcNow
        };
        _db.PosLendOutItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<PosLendOutItem>.Ok(item, "Item added."));
    }

    // ── PATCH /api/activity/pos-lend-out/{id}/items/{itemId}/return ──────────
    [HttpPatch("{id:int}/items/{itemId:int}/return")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ReturnItem(int id, int itemId, [FromBody] PloReturnItemDto dto, CancellationToken ct)
    {
        var item = await _db.PosLendOutItems.FirstOrDefaultAsync(x => x.PloiId == itemId && x.PloiPloId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail("Item not found."));

        if (dto.QuantityReturned < 0 || dto.QuantityReturned > item.PloiQuantityLent)
            return BadRequest(ApiResponse.Fail("Invalid return quantity."));

        item.PloiQuantityReturned = dto.QuantityReturned;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<PosLendOutItem>.Ok(item, "Return quantity updated."));
    }

    // ── DELETE /api/activity/pos-lend-out/{id}/items/{itemId} ────────────────
    [HttpDelete("{id:int}/items/{itemId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemoveItem(int id, int itemId, CancellationToken ct)
    {
        var item = await _db.PosLendOutItems.FirstOrDefaultAsync(x => x.PloiId == itemId && x.PloiPloId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail("Item not found."));

        item.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Item removed."));
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
        var count = await _db.PosLendOuts.AsNoTracking().CountAsync(x => x.PloYear == year, ct);
        return $"LO-{year}-{(count + 1):D4}";
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record PosLendOutDto(
    int?      PloYear,
    DateOnly? PloDate,
    DateOnly? PloExpectedReturn,
    string?   PloClientCode,
    string?   PloClientName,
    string?   PloContactName,
    string?   PloContactPhone,
    string?   PloNotes
);

public sealed record PloChangeStatusDto(string Status, DateOnly? ActualReturn = null);

public sealed record PosLendOutItemDto(
    string? PloiPmCode,
    string? PloiPmName,
    int     PloiQuantityLent,
    string? PloiNotes
);

public sealed record PloReturnItemDto(int QuantityReturned);
