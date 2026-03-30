using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

/// <summary>
/// LCL (Less than Container Load) quotes - 4-level hierarchy:
/// Header → Port → Port Type → Port Type Detail
/// </summary>
[ApiController]
[Route("api/freight/lcl")]
[Authorize]
[Produces("application/json")]
public sealed class LclController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public LclController(ApplicationDbContext db) { _db = db; }

    // ──────────────────────────────────────────────────────────────────────────
    // HEADERS
    // ──────────────────────────────────────────────────────────────────────────

    [HttpGet("headers")]
    public async Task<IActionResult> GetHeaders(
        [FromQuery] string? forwarder = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var q = _db.LclHeaders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(forwarder))
            q = q.Where(x => x.FqlhForwarder == forwarder);
        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(x => x.FqlhStartDate)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return Ok(PagedResponse<LclHeader>.Ok(data, page, pageSize, total));
    }

    [HttpGet("headers/{id:int}")]
    public async Task<IActionResult> GetHeader(int id, CancellationToken ct)
    {
        var e = await _db.LclHeaders
            .Include(x => x.Ports)
                .ThenInclude(p => p.PortTypes)
                    .ThenInclude(pt => pt.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FqlhId == id, ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"LCL header {id} not found."))
            : Ok(ApiResponse<LclHeader>.Ok(e));
    }

    [HttpPost("headers")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateHeader([FromBody] LclHeaderDto dto, CancellationToken ct)
    {
        if (await _db.LclHeaders.AnyAsync(
                x => x.FqlhForwarder == dto.Forwarder && x.FqlhQuoteNumber == dto.QuoteNumber, ct))
            return Conflict(ApiResponse.Fail("Quote number already exists for this forwarder."));

        var entity = new LclHeader
        {
            FqlhForwarder   = dto.Forwarder,
            FqlhQuoteNumber = dto.QuoteNumber,
            FqlhStartDate   = dto.StartDate,
            FqlhEndDate     = dto.EndDate,
            FqlhRemarks     = dto.Remarks
        };
        _db.LclHeaders.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetHeader), new { id = entity.FqlhId },
            ApiResponse<LclHeader>.Ok(entity, "LCL header created."));
    }

    [HttpPut("headers/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateHeader(int id, [FromBody] LclHeaderDto dto, CancellationToken ct)
    {
        var entity = await _db.LclHeaders.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Header {id} not found."));
        entity.FqlhForwarder   = dto.Forwarder;
        entity.FqlhQuoteNumber = dto.QuoteNumber;
        entity.FqlhStartDate   = dto.StartDate;
        entity.FqlhEndDate     = dto.EndDate;
        entity.FqlhRemarks     = dto.Remarks;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LclHeader>.Ok(entity, "Header updated."));
    }

    [HttpDelete("headers/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteHeader(int id, CancellationToken ct)
    {
        var entity = await _db.LclHeaders.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Header {id} not found."));
        _db.LclHeaders.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Header deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PORTS
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("headers/{headerId:int}/ports")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddPort(int headerId, [FromBody] LclPortDto dto, CancellationToken ct)
    {
        var header = await _db.LclHeaders.FindAsync([headerId], ct);
        if (header is null) return NotFound(ApiResponse.Fail($"Header {headerId} not found."));

        var entity = new LclPort
        {
            FqlpHeaderId    = headerId,
            FqlpForwarder   = header.FqlhForwarder,
            FqlpQuoteNumber = header.FqlhQuoteNumber,
            FqlpPort        = dto.Port,
            FqlpRemarks     = dto.Remarks
        };
        _db.LclPorts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LclPort>.Ok(entity, "Port added."));
    }

    [HttpPut("ports/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdatePort(int id, [FromBody] LclPortDto dto, CancellationToken ct)
    {
        var entity = await _db.LclPorts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port {id} not found."));
        entity.FqlpPort    = dto.Port;
        entity.FqlpRemarks = dto.Remarks;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LclPort>.Ok(entity, "Port updated."));
    }

    [HttpDelete("ports/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeletePort(int id, CancellationToken ct)
    {
        var entity = await _db.LclPorts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port {id} not found."));
        _db.LclPorts.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Port deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PORT TYPES
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("ports/{portId:int}/types")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddPortType(int portId, [FromBody] LclPortTypeDto dto, CancellationToken ct)
    {
        var port = await _db.LclPorts.FindAsync([portId], ct);
        if (port is null) return NotFound(ApiResponse.Fail($"Port {portId} not found."));

        var entity = new LclPortType
        {
            FqlptPortId      = portId,
            FqlptForwarder   = port.FqlpForwarder,
            FqlptQuoteNumber = port.FqlpQuoteNumber,
            FqlptPort        = port.FqlpPort,
            FqlptChargeType  = dto.ChargeType,
            FqlptAmountMin   = dto.AmountMin,
            FqlptAmountMax   = dto.AmountMax,
            FqlptCurrency    = dto.Currency
        };
        _db.LclPortTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LclPortType>.Ok(entity, "Port type added."));
    }

    [HttpPut("port-types/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdatePortType(int id, [FromBody] LclPortTypeDto dto, CancellationToken ct)
    {
        var entity = await _db.LclPortTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port type {id} not found."));
        entity.FqlptChargeType = dto.ChargeType;
        entity.FqlptAmountMin  = dto.AmountMin;
        entity.FqlptAmountMax  = dto.AmountMax;
        entity.FqlptCurrency   = dto.Currency;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LclPortType>.Ok(entity, "Port type updated."));
    }

    [HttpDelete("port-types/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeletePortType(int id, CancellationToken ct)
    {
        var entity = await _db.LclPortTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port type {id} not found."));
        _db.LclPortTypes.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Port type deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PORT TYPE DETAILS
    // ──────────────────────────────────────────────────────────────────────────

    [HttpGet("port-types/{portTypeId:int}/details")]
    public async Task<IActionResult> GetDetails(int portTypeId, CancellationToken ct)
    {
        var data = await _db.LclPortTypeDets
            .Where(x => x.FqlptdPortTypeId == portTypeId)
            .AsNoTracking()
            .OrderBy(x => x.FqlptdFrom)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<LclPortTypeDet>>.Ok(data));
    }

    [HttpPost("port-types/{portTypeId:int}/details")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddDetail(int portTypeId, [FromBody] LclPortTypeDetDto dto, CancellationToken ct)
    {
        var pt = await _db.LclPortTypes.FindAsync([portTypeId], ct);
        if (pt is null) return NotFound(ApiResponse.Fail($"Port type {portTypeId} not found."));

        var entity = new LclPortTypeDet
        {
            FqlptdPortTypeId  = portTypeId,
            FqlptdForwarder   = pt.FqlptForwarder,
            FqlptdQuoteNumber = pt.FqlptQuoteNumber,
            FqlptdPort        = pt.FqlptPort,
            FqlptdChargeType  = pt.FqlptChargeType,
            FqlptdFrom        = dto.From,
            FqlptdTo          = dto.To,
            FqlptdPrice       = dto.Price,
            FqlptdOver        = dto.Over,
            FqlptdPriceType   = dto.PriceType,
            FqlptdAmountMin   = dto.AmountMin,
            FqlptdAmountMax   = dto.AmountMax
        };
        _db.LclPortTypeDets.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LclPortTypeDet>.Ok(entity, "Detail added."));
    }

    [HttpPut("details/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateDetail(int id, [FromBody] LclPortTypeDetDto dto, CancellationToken ct)
    {
        var entity = await _db.LclPortTypeDets.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Detail {id} not found."));
        entity.FqlptdFrom      = dto.From;
        entity.FqlptdTo        = dto.To;
        entity.FqlptdPrice     = dto.Price;
        entity.FqlptdOver      = dto.Over;
        entity.FqlptdPriceType = dto.PriceType;
        entity.FqlptdAmountMin = dto.AmountMin;
        entity.FqlptdAmountMax = dto.AmountMax;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LclPortTypeDet>.Ok(entity, "Detail updated."));
    }

    [HttpDelete("details/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteDetail(int id, CancellationToken ct)
    {
        var entity = await _db.LclPortTypeDets.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Detail {id} not found."));
        _db.LclPortTypeDets.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Detail deleted."));
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record LclHeaderDto(
    string   Forwarder,
    string   QuoteNumber,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string?  Remarks
);
public sealed record LclPortDto(string Port, string? Remarks);
public sealed record LclPortTypeDto(
    string   ChargeType,
    decimal? AmountMin,
    decimal? AmountMax,
    string?  Currency
);
public sealed record LclPortTypeDetDto(
    decimal? From,
    decimal? To,
    decimal? Price,
    decimal? Over,
    string?  PriceType,
    decimal? AmountMin,
    decimal? AmountMax
);
