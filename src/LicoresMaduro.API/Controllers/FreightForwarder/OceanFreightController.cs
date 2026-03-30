using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

/// <summary>
/// Ocean Freight quotes - full 4-level hierarchy:
/// Header → Port → Port+ShippingLine → Charges
/// </summary>
[ApiController]
[Route("api/freight/ocean")]
[Authorize]
[Produces("application/json")]
public sealed class OceanFreightController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public OceanFreightController(ApplicationDbContext db) { _db = db; }

    // ──────────────────────────────────────────────────────────────────────────
    // HEADERS
    // ──────────────────────────────────────────────────────────────────────────

    [HttpGet("headers/next-number")]
    public async Task<IActionResult> GetNextQuoteNumber(CancellationToken ct)
    {
        var numbers = await _db.OceanFreightHeaders
            .AsNoTracking()
            .Select(x => x.FqohQuoteNumber)
            .ToListAsync(ct);
        var max = numbers.Select(n => int.TryParse(n, out var v) ? v : 0).DefaultIfEmpty(0).Max();
        return Ok(ApiResponse<int>.Ok(max + 1));
    }

    [HttpPost("headers/{id:int}/copy")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CopyHeader(int id, CancellationToken ct)
    {
        var source = await _db.OceanFreightHeaders
            .Include(x => x.Ports)
                .ThenInclude(p => p.PortCharges)
            .Include(x => x.Ports)
                .ThenInclude(p => p.ShippingLines)
                    .ThenInclude(sl => sl.Charges)
            .FirstOrDefaultAsync(x => x.FqohId == id, ct);

        if (source is null)
            return NotFound(ApiResponse.Fail($"Header {id} not found."));

        var allNumbers = await _db.OceanFreightHeaders
            .AsNoTracking()
            .Select(x => x.FqohQuoteNumber)
            .ToListAsync(ct);
        var newNumber = allNumbers.Select(n => int.TryParse(n, out var v) ? v : 0).DefaultIfEmpty(0).Max() + 1;

        var newHeader = new OceanFreightHeader
        {
            FqohForwarder   = source.FqohForwarder,
            FqohQuoteNumber = newNumber.ToString(),
            FqohStartDate   = source.FqohStartDate,
            FqohEndDate     = source.FqohEndDate,
            FqohRemarks     = source.FqohRemarks
        };

        foreach (var port in source.Ports)
        {
            var newPort = new OceanFreightPort
            {
                FqopForwarder   = port.FqopForwarder,
                FqopQuoteNumber = newNumber.ToString(),
                FqopPort        = port.FqopPort,
                FqopRemarks     = port.FqopRemarks
            };
            foreach (var pc in port.PortCharges)
            {
                newPort.PortCharges.Add(new OceanFreightPortCharge
                {
                    FqopcForwarder     = pc.FqopcForwarder,
                    FqopcQuoteNumber   = newNumber.ToString(),
                    FqopcPort          = pc.FqopcPort,
                    FqopcChargeType    = pc.FqopcChargeType,
                    FqopcContainerType = pc.FqopcContainerType,
                    FqopcAmount        = pc.FqopcAmount,
                    FqopcCurrency      = pc.FqopcCurrency
                });
            }
            foreach (var sl in port.ShippingLines)
            {
                var newSl = new OceanFreightPortSLine
                {
                    FqoplForwarder   = sl.FqoplForwarder,
                    FqoplQuoteNumber = newNumber.ToString(),
                    FqoplPort        = sl.FqoplPort,
                    FqoplShipLine    = sl.FqoplShipLine,
                    FqoplRoute       = sl.FqoplRoute,
                    FqoplDays        = sl.FqoplDays,
                    FqoplRemarks     = sl.FqoplRemarks
                };
                foreach (var ch in sl.Charges)
                {
                    newSl.Charges.Add(new OceanFreightPortSLineCharge
                    {
                        FqoplcForwarder     = ch.FqoplcForwarder,
                        FqoplcQuoteNumber   = newNumber.ToString(),
                        FqoplcPort          = ch.FqoplcPort,
                        FqoplcShipLine      = ch.FqoplcShipLine,
                        FqoplcChargeType    = ch.FqoplcChargeType,
                        FqoplcContainerType = ch.FqoplcContainerType,
                        FqoplcAmount        = ch.FqoplcAmount,
                        FqoplcCurrency      = ch.FqoplcCurrency
                    });
                }
                newPort.ShippingLines.Add(newSl);
            }
            newHeader.Ports.Add(newPort);
        }

        _db.OceanFreightHeaders.Add(newHeader);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightHeader>.Ok(newHeader, $"Quote copied as #{newNumber}."));
    }

    [HttpGet("headers")]
    public async Task<IActionResult> GetHeaders(
        [FromQuery] string? forwarder = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var q = _db.OceanFreightHeaders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(forwarder))
            q = q.Where(x => x.FqohForwarder == forwarder);
        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(x => x.FqohStartDate)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return Ok(PagedResponse<OceanFreightHeader>.Ok(data, page, pageSize, total));
    }

    [HttpGet("headers/{id:int}")]
    public async Task<IActionResult> GetHeader(int id, CancellationToken ct)
    {
        var e = await _db.OceanFreightHeaders
            .Include(x => x.Ports)
                .ThenInclude(p => p.PortCharges)
            .Include(x => x.Ports)
                .ThenInclude(p => p.ShippingLines)
                    .ThenInclude(sl => sl.Charges)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FqohId == id, ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"Ocean freight header {id} not found."))
            : Ok(ApiResponse<OceanFreightHeader>.Ok(e));
    }

    [HttpPost("headers")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateHeader([FromBody] OceanFreightHeaderDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        if (await _db.OceanFreightHeaders.AnyAsync(
                x => x.FqohForwarder == dto.Forwarder && x.FqohQuoteNumber == dto.QuoteNumber, ct))
            return Conflict(ApiResponse.Fail("Quote number already exists for this forwarder."));

        var entity = new OceanFreightHeader
        {
            FqohForwarder   = dto.Forwarder,
            FqohQuoteNumber = dto.QuoteNumber,
            FqohStartDate   = dto.StartDate,
            FqohEndDate     = dto.EndDate,
            FqohRemarks     = dto.Remarks
        };
        _db.OceanFreightHeaders.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetHeader), new { id = entity.FqohId },
            ApiResponse<OceanFreightHeader>.Ok(entity, "Ocean freight header created."));
    }

    [HttpPut("headers/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateHeader(int id, [FromBody] OceanFreightHeaderDto dto, CancellationToken ct)
    {
        var entity = await _db.OceanFreightHeaders.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Header {id} not found."));
        entity.FqohForwarder   = dto.Forwarder;
        entity.FqohQuoteNumber = dto.QuoteNumber;
        entity.FqohStartDate   = dto.StartDate;
        entity.FqohEndDate     = dto.EndDate;
        entity.FqohRemarks     = dto.Remarks;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightHeader>.Ok(entity, "Header updated."));
    }

    [HttpDelete("headers/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteHeader(int id, CancellationToken ct)
    {
        var entity = await _db.OceanFreightHeaders.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Header {id} not found."));
        _db.OceanFreightHeaders.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Header deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PORTS
    // ──────────────────────────────────────────────────────────────────────────

    [HttpGet("headers/{headerId:int}/ports")]
    public async Task<IActionResult> GetPorts(int headerId, CancellationToken ct)
    {
        var data = await _db.OceanFreightPorts
            .Where(x => x.FqopHeaderId == headerId)
            .Include(x => x.ShippingLines).ThenInclude(sl => sl.Charges)
            .AsNoTracking()
            .ToListAsync(ct);
        return Ok(ApiResponse<List<OceanFreightPort>>.Ok(data));
    }

    [HttpPost("headers/{headerId:int}/ports")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddPort(int headerId, [FromBody] OceanFreightPortDto dto, CancellationToken ct)
    {
        if (!await _db.OceanFreightHeaders.AnyAsync(x => x.FqohId == headerId, ct))
            return NotFound(ApiResponse.Fail($"Header {headerId} not found."));

        var header = await _db.OceanFreightHeaders.FindAsync([headerId], ct);
        var entity = new OceanFreightPort
        {
            FqopHeaderId    = headerId,
            FqopForwarder   = header!.FqohForwarder,
            FqopQuoteNumber = header.FqohQuoteNumber,
            FqopPort        = dto.Port,
            FqopRemarks     = dto.Remarks
        };
        _db.OceanFreightPorts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightPort>.Ok(entity, "Port added."));
    }

    [HttpPut("ports/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdatePort(int id, [FromBody] OceanFreightPortDto dto, CancellationToken ct)
    {
        var entity = await _db.OceanFreightPorts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port {id} not found."));
        entity.FqopPort    = dto.Port;
        entity.FqopRemarks = dto.Remarks;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightPort>.Ok(entity, "Port updated."));
    }

    [HttpDelete("ports/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeletePort(int id, CancellationToken ct)
    {
        var entity = await _db.OceanFreightPorts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port {id} not found."));
        _db.OceanFreightPorts.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Port deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PORT + SHIPPING LINES
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("ports/{portId:int}/slines")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddShippingLine(int portId, [FromBody] OceanFreightSLineDto dto, CancellationToken ct)
    {
        var port = await _db.OceanFreightPorts.FindAsync([portId], ct);
        if (port is null) return NotFound(ApiResponse.Fail($"Port {portId} not found."));

        var entity = new OceanFreightPortSLine
        {
            FqoplPortId      = portId,
            FqoplForwarder   = port.FqopForwarder,
            FqoplQuoteNumber = port.FqopQuoteNumber,
            FqoplPort        = port.FqopPort,
            FqoplShipLine    = dto.ShipLine,
            FqoplRoute       = dto.Route,
            FqoplDays        = dto.Days,
            FqoplRemarks     = dto.Remarks
        };
        _db.OceanFreightPortSLines.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightPortSLine>.Ok(entity, "Shipping line added."));
    }

    [HttpPut("slines/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateShippingLine(int id, [FromBody] OceanFreightSLineDto dto, CancellationToken ct)
    {
        var entity = await _db.OceanFreightPortSLines.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Shipping line {id} not found."));
        entity.FqoplShipLine = dto.ShipLine;
        entity.FqoplRoute    = dto.Route;
        entity.FqoplDays     = dto.Days;
        entity.FqoplRemarks  = dto.Remarks;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightPortSLine>.Ok(entity, "Shipping line updated."));
    }

    [HttpDelete("slines/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteShippingLine(int id, CancellationToken ct)
    {
        var entity = await _db.OceanFreightPortSLines.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Shipping line {id} not found."));
        _db.OceanFreightPortSLines.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Shipping line deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CHARGES
    // ──────────────────────────────────────────────────────────────────────────

    [HttpGet("slines/{slineId:int}/charges")]
    public async Task<IActionResult> GetCharges(int slineId, CancellationToken ct)
    {
        var data = await _db.OceanFreightCharges
            .Where(x => x.FqoplcSLineId == slineId)
            .AsNoTracking()
            .ToListAsync(ct);
        return Ok(ApiResponse<List<OceanFreightPortSLineCharge>>.Ok(data));
    }

    [HttpPost("slines/{slineId:int}/charges")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddCharge(int slineId, [FromBody] OceanFreightChargeDto dto, CancellationToken ct)
    {
        var sline = await _db.OceanFreightPortSLines.FindAsync([slineId], ct);
        if (sline is null) return NotFound(ApiResponse.Fail($"Shipping line {slineId} not found."));

        var entity = new OceanFreightPortSLineCharge
        {
            FqoplcSLineId       = slineId,
            FqoplcForwarder     = sline.FqoplForwarder,
            FqoplcQuoteNumber   = sline.FqoplQuoteNumber,
            FqoplcPort          = sline.FqoplPort,
            FqoplcShipLine      = sline.FqoplShipLine,
            FqoplcChargeType    = dto.ChargeType,
            FqoplcContainerType = dto.ContainerType,
            FqoplcAmount        = dto.Amount,
            FqoplcCurrency      = dto.Currency
        };
        _db.OceanFreightCharges.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightPortSLineCharge>.Ok(entity, "Charge added."));
    }

    [HttpPut("charges/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateCharge(int id, [FromBody] OceanFreightChargeDto dto, CancellationToken ct)
    {
        var entity = await _db.OceanFreightCharges.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge {id} not found."));
        entity.FqoplcChargeType    = dto.ChargeType;
        entity.FqoplcContainerType = dto.ContainerType;
        entity.FqoplcAmount        = dto.Amount;
        entity.FqoplcCurrency      = dto.Currency;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightPortSLineCharge>.Ok(entity, "Charge updated."));
    }

    [HttpDelete("charges/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteCharge(int id, CancellationToken ct)
    {
        var entity = await _db.OceanFreightCharges.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge {id} not found."));
        _db.OceanFreightCharges.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Charge deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PORT-LEVEL CHARGES (independent of shipping line)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpGet("ports/{portId:int}/port-charges")]
    public async Task<IActionResult> GetPortCharges(int portId, CancellationToken ct)
    {
        var data = await _db.OceanFreightPortCharges
            .Where(x => x.FqopcPortId == portId)
            .AsNoTracking()
            .ToListAsync(ct);
        return Ok(ApiResponse<List<OceanFreightPortCharge>>.Ok(data));
    }

    [HttpPost("ports/{portId:int}/port-charges")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddPortCharge(int portId, [FromBody] OceanFreightChargeDto dto, CancellationToken ct)
    {
        var port = await _db.OceanFreightPorts.FindAsync([portId], ct);
        if (port is null) return NotFound(ApiResponse.Fail($"Port {portId} not found."));

        var entity = new OceanFreightPortCharge
        {
            FqopcPortId        = portId,
            FqopcForwarder     = port.FqopForwarder,
            FqopcQuoteNumber   = port.FqopQuoteNumber,
            FqopcPort          = port.FqopPort,
            FqopcChargeType    = dto.ChargeType,
            FqopcContainerType = dto.ContainerType,
            FqopcAmount        = dto.Amount,
            FqopcCurrency      = dto.Currency
        };
        _db.OceanFreightPortCharges.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightPortCharge>.Ok(entity, "Port charge added."));
    }

    [HttpPut("port-charges/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdatePortCharge(int id, [FromBody] OceanFreightChargeDto dto, CancellationToken ct)
    {
        var entity = await _db.OceanFreightPortCharges.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port charge {id} not found."));
        entity.FqopcChargeType    = dto.ChargeType;
        entity.FqopcContainerType = dto.ContainerType;
        entity.FqopcAmount        = dto.Amount;
        entity.FqopcCurrency      = dto.Currency;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightPortCharge>.Ok(entity, "Port charge updated."));
    }

    [HttpDelete("port-charges/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeletePortCharge(int id, CancellationToken ct)
    {
        var entity = await _db.OceanFreightPortCharges.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port charge {id} not found."));
        _db.OceanFreightPortCharges.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Port charge deleted."));
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record OceanFreightHeaderDto(
    string   Forwarder,
    string   QuoteNumber,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string?  Remarks
);

public sealed record OceanFreightPortDto(
    string  Port,
    string? Remarks
);

public sealed record OceanFreightSLineDto(
    string  ShipLine,
    string? Route,
    short?  Days,
    string? Remarks
);

public sealed record OceanFreightChargeDto(
    string   ChargeType,
    string?  ContainerType,
    decimal? Amount,
    string?  Currency
);
