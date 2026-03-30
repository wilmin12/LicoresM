using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/pos-materials")]
[Authorize]
[Produces("application/json")]
public sealed class PosMaterialsController : ControllerBase
{
    private readonly ApplicationDbContext          _db;
    private readonly ILogger<PosMaterialsController> _logger;

    public PosMaterialsController(ApplicationDbContext db, ILogger<PosMaterialsController> logger)
    {
        _db = db; _logger = logger;
    }

    // ── GET /api/activity/pos-materials ───────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search          = null,
        [FromQuery] string? categoryCode    = null,
        [FromQuery] bool    includeInactive = false,
        [FromQuery] int     page            = 1,
        [FromQuery] int     pageSize        = 50,
        CancellationToken   ct              = default)
    {
        var q = _db.PosMaterials.AsNoTracking();

        if (!includeInactive) q = q.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.PmCode.Contains(search) || x.PmName.Contains(search));

        if (!string.IsNullOrWhiteSpace(categoryCode))
            q = q.Where(x => x.PmCategoryCode == categoryCode);

        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.PmCode)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync(ct);

        return Ok(PagedResponse<PosMaterial>.Ok(data, page, pageSize, total));
    }

    // ── GET /api/activity/pos-materials/categories ────────────────────────────
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var cats = await _db.PosMaterials.AsNoTracking()
            .Where(x => x.IsActive && x.PmCategoryCode != null)
            .Select(x => new { code = x.PmCategoryCode, desc = x.PmCategoryDesc })
            .Distinct()
            .OrderBy(x => x.code)
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(cats));
    }

    // ── GET /api/activity/pos-materials/{id} ──────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.PosMaterials.FindAsync([id], ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"POS material {id} not found."))
            : Ok(ApiResponse<PosMaterial>.Ok(e));
    }

    // ── POST /api/activity/pos-materials ──────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] PosMaterialDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var exists = await _db.PosMaterials.AnyAsync(x => x.PmCode == dto.PmCode && x.IsActive, ct);
        if (exists)
            return Conflict(ApiResponse.Fail($"A POS material with code '{dto.PmCode}' already exists."));

        var entity = new PosMaterial
        {
            PmCode           = dto.PmCode,
            PmName           = dto.PmName,
            PmCategoryCode   = dto.PmCategoryCode,
            PmCategoryDesc   = dto.PmCategoryDesc,
            PmDescription    = dto.PmDescription,
            PmUnit           = dto.PmUnit,
            PmStockTotal     = dto.PmStockTotal     ?? 0,
            PmStockAvailable = dto.PmStockAvailable ?? 0,
            PmNotes          = dto.PmNotes,
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow
        };

        _db.PosMaterials.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("PosMaterial {Code} created", entity.PmCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.PmId }, ApiResponse<PosMaterial>.Ok(entity, "POS material created."));
    }

    // ── PUT /api/activity/pos-materials/{id} ──────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] PosMaterialDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var entity = await _db.PosMaterials.FindAsync([id], ct);
        if (entity is null)
            return NotFound(ApiResponse.Fail($"POS material {id} not found."));

        var codeConflict = await _db.PosMaterials.AnyAsync(x => x.PmCode == dto.PmCode && x.PmId != id && x.IsActive, ct);
        if (codeConflict)
            return Conflict(ApiResponse.Fail($"Another POS material already uses code '{dto.PmCode}'."));

        entity.PmCode           = dto.PmCode;
        entity.PmName           = dto.PmName;
        entity.PmCategoryCode   = dto.PmCategoryCode;
        entity.PmCategoryDesc   = dto.PmCategoryDesc;
        entity.PmDescription    = dto.PmDescription;
        entity.PmUnit           = dto.PmUnit;
        entity.PmStockTotal     = dto.PmStockTotal     ?? entity.PmStockTotal;
        entity.PmStockAvailable = dto.PmStockAvailable ?? entity.PmStockAvailable;
        entity.PmNotes          = dto.PmNotes;

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<PosMaterial>.Ok(entity, "POS material updated."));
    }

    // ── PATCH /api/activity/pos-materials/{id}/toggle ─────────────────────────
    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var entity = await _db.PosMaterials.FindAsync([id], ct);
        if (entity is null)
            return NotFound(ApiResponse.Fail($"POS material {id} not found."));

        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"POS material {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    // ── DELETE /api/activity/pos-materials/{id} ───────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.PosMaterials.FindAsync([id], ct);
        if (entity is null)
            return NotFound(ApiResponse.Fail($"POS material {id} not found."));

        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("PosMaterial {Id} ({Code}) soft-deleted", id, entity.PmCode);
        return Ok(ApiResponse.Ok("POS material deleted."));
    }
}

// ── DTO ──────────────────────────────────────────────────────────────────────
public sealed record PosMaterialDto(
    string  PmCode,
    string  PmName,
    string? PmCategoryCode,
    string? PmCategoryDesc,
    string? PmDescription,
    string? PmUnit,
    int?    PmStockTotal,
    int?    PmStockAvailable,
    string? PmNotes
);
