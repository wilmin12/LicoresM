using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FF = LicoresMaduro.API.Data.FreightForwarder;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/forwarders")]
[Authorize]
[Produces("application/json")]
public sealed class FreightForwardersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly DhwDbContext         _dhw;
    private readonly ILogger<FreightForwardersController> _logger;

    public FreightForwardersController(ApplicationDbContext db, DhwDbContext dhw, ILogger<FreightForwardersController> logger)
    { _db = db; _dhw = dhw; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var q = _db.FreightForwarders.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.FfCode.Contains(search) || x.FfName.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.FfName)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return Ok(PagedResponse<FF>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.FreightForwarders.FindAsync([id], ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"Freight forwarder {id} not found."))
            : Ok(ApiResponse<FF>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] FreightForwarderDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        if (await _db.FreightForwarders.AnyAsync(x => x.FfCode == dto.FfCode, ct))
            return Conflict(ApiResponse.Fail($"Code '{dto.FfCode}' already exists."));

        var entity = new FF
        {
            FfCode     = dto.FfCode,
            FfName     = dto.FfName,
            FfAddress1 = dto.FfAddress1,
            FfAddress2 = dto.FfAddress2,
            FfCity     = dto.FfCity,
            FfCountry  = dto.FfCountry,
            FfPhone1   = dto.FfPhone1,
            FfPhone2   = dto.FfPhone2,
            FfEmail    = dto.FfEmail,
            FfContact  = dto.FfContact,
            FfCurrency = dto.FfCurrency,
            IsActive   = true,
            CreatedAt  = DateTime.UtcNow
        };
        _db.FreightForwarders.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("FreightForwarder '{Code}' created", dto.FfCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.FfId },
            ApiResponse<FF>.Ok(entity, "Freight forwarder created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] FreightForwarderDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var entity = await _db.FreightForwarders.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Freight forwarder {id} not found."));

        if (entity.FfCode != dto.FfCode &&
            await _db.FreightForwarders.AnyAsync(x => x.FfCode == dto.FfCode && x.FfId != id, ct))
            return Conflict(ApiResponse.Fail($"Code '{dto.FfCode}' already exists."));

        entity.FfCode     = dto.FfCode;
        entity.FfName     = dto.FfName;
        entity.FfAddress1 = dto.FfAddress1;
        entity.FfAddress2 = dto.FfAddress2;
        entity.FfCity     = dto.FfCity;
        entity.FfCountry  = dto.FfCountry;
        entity.FfPhone1   = dto.FfPhone1;
        entity.FfPhone2   = dto.FfPhone2;
        entity.FfEmail    = dto.FfEmail;
        entity.FfContact  = dto.FfContact;
        entity.FfCurrency = dto.FfCurrency;

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FF>.Ok(entity, "Freight forwarder updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.FreightForwarders.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Freight forwarder {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Freight forwarder {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.FreightForwarders.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Freight forwarder {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("FreightForwarder {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Freight forwarder deleted."));
    }

    [HttpPost("sync-vip")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SyncFromVip(CancellationToken ct)
    {
        var shippers = await _dhw.ShipperMasters
            .AsNoTracking()
            .ToListAsync(ct);

        var existing = await _db.FreightForwarders
            .ToDictionaryAsync(x => x.FfCode, ct);

        int inserted = 0, updated = 0;

        foreach (var s in shippers)
        {
            var code    = s.ShipperId.Trim();
            var name    = s.ShipperName?.Trim() ?? code;
            var city    = s.IdCity.HasValue ? s.IdCity.Value.ToString() : null;
            var phone1  = s.TelephoneNumber.HasValue ? s.TelephoneNumber.Value.ToString("0") : null;
            var phone2  = s.FaxNumber.HasValue ? s.FaxNumber.Value.ToString("0") : null;
            var active  = !string.Equals(s.DeleteFlag?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);

            if (existing.TryGetValue(code, out var ff))
            {
                ff.FfName     = name;
                ff.FfAddress1 = s.Address1?.Trim();
                ff.FfAddress2 = s.Address2?.Trim();
                ff.FfCity     = city;
                ff.FfPhone1   = phone1;
                ff.FfPhone2   = phone2;
                ff.IsActive   = active;
                updated++;
            }
            else
            {
                _db.FreightForwarders.Add(new FF
                {
                    FfCode     = code,
                    FfName     = name,
                    FfAddress1 = s.Address1?.Trim(),
                    FfAddress2 = s.Address2?.Trim(),
                    FfCity     = city,
                    FfPhone1   = phone1,
                    FfPhone2   = phone2,
                    IsActive   = active,
                    CreatedAt  = s.CreateTimestamp?.ToUniversalTime() ?? DateTime.UtcNow
                });
                inserted++;
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("FreightForwarder sync-vip: {Inserted} inserted, {Updated} updated", inserted, updated);
        return Ok(ApiResponse.Ok($"Sync complete. {inserted} inserted, {updated} updated."));
    }
}

public sealed record FreightForwarderDto(
    string  FfCode,
    string  FfName,
    string? FfAddress1,
    string? FfAddress2,
    string? FfCity,
    string? FfCountry,
    string? FfPhone1,
    string? FfPhone2,
    string? FfEmail,
    string? FfContact,
    string? FfCurrency
);
