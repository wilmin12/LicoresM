using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

/// <summary>
/// Inland Freight quotes - 4-level hierarchy:
/// Header → Region → Region Type → Region Type Detail
/// </summary>
[ApiController]
[Route("api/freight/inland")]
[Authorize]
[Produces("application/json")]
public sealed class InlandFreightController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public InlandFreightController(ApplicationDbContext db) { _db = db; }

    // ──────────────────────────────────────────────────────────────────────────
    // HEADERS
    // ──────────────────────────────────────────────────────────────────────────

    [HttpGet("headers")]
    public async Task<IActionResult> GetHeaders(
        [FromQuery] string? forwarder = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var q = _db.InlandFreightHeaders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(forwarder))
            q = q.Where(x => x.FqihForwarder == forwarder);
        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(x => x.FqihStartDate)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return Ok(PagedResponse<InlandFreightHeader>.Ok(data, page, pageSize, total));
    }

    [HttpGet("headers/{id:int}")]
    public async Task<IActionResult> GetHeader(int id, CancellationToken ct)
    {
        var e = await _db.InlandFreightHeaders
            .Include(x => x.Regions)
                .ThenInclude(r => r.RegionTypes)
                    .ThenInclude(rt => rt.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FqihId == id, ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"Inland freight header {id} not found."))
            : Ok(ApiResponse<InlandFreightHeader>.Ok(e));
    }

    [HttpPost("headers")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateHeader([FromBody] InlandFreightHeaderDto dto, CancellationToken ct)
    {
        if (await _db.InlandFreightHeaders.AnyAsync(
                x => x.FqihForwarder == dto.Forwarder && x.FqihQuoteNumber == dto.QuoteNumber, ct))
            return Conflict(ApiResponse.Fail("Quote number already exists for this forwarder."));

        var entity = new InlandFreightHeader
        {
            FqihForwarder   = dto.Forwarder,
            FqihQuoteNumber = dto.QuoteNumber,
            FqihStartDate   = dto.StartDate,
            FqihEndDate     = dto.EndDate,
            FqihRemarks     = dto.Remarks
        };
        _db.InlandFreightHeaders.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetHeader), new { id = entity.FqihId },
            ApiResponse<InlandFreightHeader>.Ok(entity, "Inland freight header created."));
    }

    [HttpPut("headers/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateHeader(int id, [FromBody] InlandFreightHeaderDto dto, CancellationToken ct)
    {
        var entity = await _db.InlandFreightHeaders.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Header {id} not found."));
        entity.FqihForwarder   = dto.Forwarder;
        entity.FqihQuoteNumber = dto.QuoteNumber;
        entity.FqihStartDate   = dto.StartDate;
        entity.FqihEndDate     = dto.EndDate;
        entity.FqihRemarks     = dto.Remarks;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<InlandFreightHeader>.Ok(entity, "Header updated."));
    }

    [HttpDelete("headers/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteHeader(int id, CancellationToken ct)
    {
        var entity = await _db.InlandFreightHeaders.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Header {id} not found."));
        _db.InlandFreightHeaders.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Header deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // REGIONS
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("headers/{headerId:int}/regions")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddRegion(int headerId, [FromBody] InlandRegionDto dto, CancellationToken ct)
    {
        var header = await _db.InlandFreightHeaders.FindAsync([headerId], ct);
        if (header is null) return NotFound(ApiResponse.Fail($"Header {headerId} not found."));

        var entity = new InlandFreightRegion
        {
            FqirHeaderId    = headerId,
            FqirForwarder   = header.FqihForwarder,
            FqirQuoteNumber = header.FqihQuoteNumber,
            FqirRegion      = dto.Region
        };
        _db.InlandFreightRegions.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<InlandFreightRegion>.Ok(entity, "Region added."));
    }

    [HttpPut("regions/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateRegion(int id, [FromBody] InlandRegionDto dto, CancellationToken ct)
    {
        var entity = await _db.InlandFreightRegions.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Region {id} not found."));
        entity.FqirRegion = dto.Region;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<InlandFreightRegion>.Ok(entity, "Region updated."));
    }

    [HttpDelete("regions/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteRegion(int id, CancellationToken ct)
    {
        var entity = await _db.InlandFreightRegions.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Region {id} not found."));
        _db.InlandFreightRegions.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Region deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // REGION TYPES
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("regions/{regionId:int}/types")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddRegionType(int regionId, [FromBody] InlandRegionTypeDto dto, CancellationToken ct)
    {
        var region = await _db.InlandFreightRegions.FindAsync([regionId], ct);
        if (region is null) return NotFound(ApiResponse.Fail($"Region {regionId} not found."));

        var entity = new InlandFreightRegionType
        {
            FqirtRegionId    = regionId,
            FqirtForwarder   = region.FqirForwarder,
            FqirtQuoteNumber = region.FqirQuoteNumber,
            FqirtRegion      = region.FqirRegion,
            FqirtChargeType  = dto.ChargeType,
            FqirtAmountMin   = dto.AmountMin,
            FqirtAmountMax   = dto.AmountMax,
            FqirtCurrency    = dto.Currency
        };
        _db.InlandFreightRegionTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<InlandFreightRegionType>.Ok(entity, "Region type added."));
    }

    [HttpPut("region-types/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateRegionType(int id, [FromBody] InlandRegionTypeDto dto, CancellationToken ct)
    {
        var entity = await _db.InlandFreightRegionTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Region type {id} not found."));
        entity.FqirtChargeType = dto.ChargeType;
        entity.FqirtAmountMin  = dto.AmountMin;
        entity.FqirtAmountMax  = dto.AmountMax;
        entity.FqirtCurrency   = dto.Currency;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<InlandFreightRegionType>.Ok(entity, "Region type updated."));
    }

    [HttpDelete("region-types/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteRegionType(int id, CancellationToken ct)
    {
        var entity = await _db.InlandFreightRegionTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Region type {id} not found."));
        _db.InlandFreightRegionTypes.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Region type deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // REGION TYPE DETAILS
    // ──────────────────────────────────────────────────────────────────────────

    [HttpGet("region-types/{regionTypeId:int}/details")]
    public async Task<IActionResult> GetDetails(int regionTypeId, CancellationToken ct)
    {
        var data = await _db.InlandFreightRegionTypeDets
            .Where(x => x.FqirtdRegionTypeId == regionTypeId)
            .AsNoTracking()
            .OrderBy(x => x.FqirtdFrom)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<InlandFreightRegionTypeDet>>.Ok(data));
    }

    [HttpPost("region-types/{regionTypeId:int}/details")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddDetail(int regionTypeId, [FromBody] InlandRegionTypeDetDto dto, CancellationToken ct)
    {
        var rt = await _db.InlandFreightRegionTypes.FindAsync([regionTypeId], ct);
        if (rt is null) return NotFound(ApiResponse.Fail($"Region type {regionTypeId} not found."));

        var entity = new InlandFreightRegionTypeDet
        {
            FqirtdRegionTypeId = regionTypeId,
            FqirtdForwarder    = rt.FqirtForwarder,
            FqirtdQuoteNumber  = rt.FqirtQuoteNumber,
            FqirtdRegion       = rt.FqirtRegion,
            FqirtdChargeType   = rt.FqirtChargeType,
            FqirtdFrom         = dto.From,
            FqirtdTo           = dto.To,
            FqirtdPrice        = dto.Price,
            FqirtdPriceType    = dto.PriceType,
            FqirtdAmountMin    = dto.AmountMin,
            FqirtdAmountMax    = dto.AmountMax
        };
        _db.InlandFreightRegionTypeDets.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<InlandFreightRegionTypeDet>.Ok(entity, "Detail added."));
    }

    [HttpPut("details/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateDetail(int id, [FromBody] InlandRegionTypeDetDto dto, CancellationToken ct)
    {
        var entity = await _db.InlandFreightRegionTypeDets.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Detail {id} not found."));
        entity.FqirtdFrom      = dto.From;
        entity.FqirtdTo        = dto.To;
        entity.FqirtdPrice     = dto.Price;
        entity.FqirtdPriceType = dto.PriceType;
        entity.FqirtdAmountMin = dto.AmountMin;
        entity.FqirtdAmountMax = dto.AmountMax;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<InlandFreightRegionTypeDet>.Ok(entity, "Detail updated."));
    }

    [HttpDelete("details/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteDetail(int id, CancellationToken ct)
    {
        var entity = await _db.InlandFreightRegionTypeDets.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Detail {id} not found."));
        _db.InlandFreightRegionTypeDets.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Detail deleted."));
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record InlandFreightHeaderDto(
    string   Forwarder,
    string   QuoteNumber,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string?  Remarks
);
public sealed record InlandRegionDto(string Region);
public sealed record InlandRegionTypeDto(
    string   ChargeType,
    decimal? AmountMin,
    decimal? AmountMax,
    string?  Currency
);
public sealed record InlandRegionTypeDetDto(
    decimal? From,
    decimal? To,
    decimal? Price,
    string?  PriceType,
    decimal? AmountMin,
    decimal? AmountMax
);
