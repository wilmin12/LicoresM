using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/inland-additional-charges")]
[Authorize]
[Produces("application/json")]
public sealed class InlandAdditionalChargesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public InlandAdditionalChargesController(ApplicationDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? forwarder = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        var q = _db.InlandAdditionalCharges.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(forwarder))
            q = q.Where(x => x.FqiaForwarder == forwarder);
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.FqiaForwarder).ThenBy(x => x.FqiaChargeType)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return Ok(PagedResponse<InlandAdditionalCharge>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.InlandAdditionalCharges.FindAsync([id], ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"Charge {id} not found."))
            : Ok(ApiResponse<InlandAdditionalCharge>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] InlandAdditionalChargeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var entity = new InlandAdditionalCharge
        {
            FqiaForwarder  = dto.Forwarder,
            FqiaChargeType = dto.ChargeType,
            FqiaLoadType   = dto.LoadType,
            FqiaAmount     = dto.Amount,
            FqiaAction     = dto.Action,
            FqiaChargeOver = dto.ChargeOver,
            FqiaChargePer  = dto.ChargePer,
            FqiaFrom       = dto.From,
            FqiaTo         = dto.To,
            FqiaAmountMin  = dto.AmountMin,
            FqiaAmountMax  = dto.AmountMax,
            FqiaCurrency   = dto.Currency
        };
        _db.InlandAdditionalCharges.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.FqiaId },
            ApiResponse<InlandAdditionalCharge>.Ok(entity, "Inland additional charge created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] InlandAdditionalChargeDto dto, CancellationToken ct)
    {
        var entity = await _db.InlandAdditionalCharges.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge {id} not found."));

        entity.FqiaForwarder  = dto.Forwarder;
        entity.FqiaChargeType = dto.ChargeType;
        entity.FqiaLoadType   = dto.LoadType;
        entity.FqiaAmount     = dto.Amount;
        entity.FqiaAction     = dto.Action;
        entity.FqiaChargeOver = dto.ChargeOver;
        entity.FqiaChargePer  = dto.ChargePer;
        entity.FqiaFrom       = dto.From;
        entity.FqiaTo         = dto.To;
        entity.FqiaAmountMin  = dto.AmountMin;
        entity.FqiaAmountMax  = dto.AmountMax;
        entity.FqiaCurrency   = dto.Currency;

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<InlandAdditionalCharge>.Ok(entity, "Charge updated."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.InlandAdditionalCharges.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge {id} not found."));
        _db.InlandAdditionalCharges.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Charge deleted."));
    }
}

public sealed record InlandAdditionalChargeDto(
    string   Forwarder,
    string   ChargeType,
    string?  LoadType,
    decimal? Amount,
    string?  Action,
    string?  ChargeOver,
    string?  ChargePer,
    decimal? From,
    decimal? To,
    decimal? AmountMin,
    decimal? AmountMax,
    string?  Currency
);
