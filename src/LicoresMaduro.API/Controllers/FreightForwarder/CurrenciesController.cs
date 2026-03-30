using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/currencies")]
[Authorize]
[Produces("application/json")]
public sealed class CurrenciesController : ControllerBase
{
    private readonly ApplicationDbContext          _db;
    private readonly ILogger<CurrenciesController> _logger;

    public CurrenciesController(ApplicationDbContext db, ILogger<CurrenciesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.Currencies.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.CurCode.Contains(search) || x.CurDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.CurCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<Currency>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Currencies.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Currency {id} not found.")) : Ok(ApiResponse<Currency>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CurrencyDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new Currency
        {
            CurCode            = dto.CurCode.ToUpper(),
            CurDescription     = dto.CurDescription,
            CurBnkPurchaseRate = dto.CurBnkPurchaseRate,
            CurCustomsRate     = dto.CurCustomsRate,
            IsActive           = true,
            CreatedAt          = DateTime.UtcNow
        };
        _db.Currencies.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Currency '{Code}' created", dto.CurCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.CurId }, ApiResponse<Currency>.Ok(entity, "Currency created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CurrencyDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.Currencies.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Currency {id} not found."));
        entity.CurCode            = dto.CurCode.ToUpper();
        entity.CurDescription     = dto.CurDescription;
        entity.CurBnkPurchaseRate = dto.CurBnkPurchaseRate;
        entity.CurCustomsRate     = dto.CurCustomsRate;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Currency>.Ok(entity, "Currency updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.Currencies.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Currency {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Currency {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Currencies.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Currency {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Currency {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Currency deleted."));
    }
}

public sealed record CurrencyDto(
    string  CurCode,
    string  CurDescription,
    double? CurBnkPurchaseRate,
    double? CurCustomsRate);
