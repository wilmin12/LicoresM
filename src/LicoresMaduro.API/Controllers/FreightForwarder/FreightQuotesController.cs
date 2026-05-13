using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

/// <summary>
/// Applied Freight Quotation — one quote per Forwarder + Port + Route.
/// Hierarchy: Header → OceanCharges / InlandRegions(→Types→Dets) / InlandPortAdds
/// </summary>
[ApiController]
[Route("api/freight/quotes")]
[Authorize]
[Produces("application/json")]
public sealed class FreightQuotesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public FreightQuotesController(ApplicationDbContext db) { _db = db; }

    // ──────────────────────────────────────────────────────────────────────────
    // HEADERS (quote list)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetQuotes(
        [FromQuery] string? forwarder = null,
        [FromQuery] string? search    = null,
        [FromQuery] string? tab       = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var q = _db.FreightQuoteHeaders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(forwarder))
            q = q.Where(x => x.FqhForwarder == forwarder);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(x => x.FqhForwarder.ToLower().Contains(s)
                           || (x.FqhPort  != null && x.FqhPort.ToLower().Contains(s))
                           || (x.FqhRoute != null && x.FqhRoute.ToLower().Contains(s)));
        }

        q = tab switch
        {
            "ocean"   => q.Where(x => x.FqhFreightType == "ocean"),
            "inland"  => q.Where(x => x.FqhFreightType == "inland"),
            "portadd" => q.Where(x => x.FqhFreightType == "portadd"),
            "lcl"     => q.Where(x => x.FqhFreightType == "lcl"),
            _         => q
        };

        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(x => x.FqhQuoteNumber)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);

        return Ok(PagedResponse<FreightQuoteHeader>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetQuote(int id, CancellationToken ct)
    {
        try
        {
            var e = await _db.FreightQuoteHeaders
                .Include(x => x.OceanPorts)
                    .ThenInclude(p => p.ShippingLines)
                        .ThenInclude(sl => sl.Charges)
                .Include(x => x.InlandRegions)
                    .ThenInclude(r => r.RegionTypes)
                        .ThenInclude(rt => rt.Details)
                .Include(x => x.InlandPortAdds)
                .Include(x => x.LclPorts)
                    .ThenInclude(p => p.PortTypes)
                        .ThenInclude(pt => pt.Details)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FqhId == id, ct);

            return e is null
                ? NotFound(ApiResponse.Fail($"Quote {id} not found."))
                : Ok(ApiResponse<FreightQuoteHeader>.Ok(e));
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, ApiResponse.Fail(msg));
        }
    }

    [HttpGet("next-number")]
    public async Task<IActionResult> GetNextNumber([FromQuery] string? freightType, CancellationToken ct)
    {
        var max = await _db.FreightQuoteHeaders.AsNoTracking()
                           .Where(x => x.FqhFreightType == freightType)
                           .MaxAsync(x => (int?)x.FqhQuoteNumber, ct) ?? 0;
        return Ok(ApiResponse<int>.Ok(max + 1));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateQuote([FromBody] FreightQuoteHeaderDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

            // Quote number is per FreightType
            var max = await _db.FreightQuoteHeaders
                               .Where(x => x.FqhFreightType == dto.FreightType)
                               .MaxAsync(x => (int?)x.FqhQuoteNumber, ct) ?? 0;

            var entity = new FreightQuoteHeader
            {
                FqhQuoteNumber = max + 1,
                FqhForwarder   = dto.Forwarder,
                FqhFreightType = dto.FreightType,
                FqhPort        = dto.Port,
                FqhRoute       = dto.Route,
                FqhTransitDays = dto.TransitDays,
                FqhStartDate   = dto.StartDate.HasValue ? DateOnly.FromDateTime(dto.StartDate.Value) : null,
                FqhEndDate     = dto.EndDate.HasValue ? DateOnly.FromDateTime(dto.EndDate.Value) : null
            };

            // Server-side Deep Clone logic
            if (dto.CloneFromId.HasValue)
            {
                var source = await _db.FreightQuoteHeaders
                    .Include(h => h.OceanPorts).ThenInclude(p => p.ShippingLines).ThenInclude(s => s.Charges)
                    .Include(h => h.InlandRegions).ThenInclude(r => r.RegionTypes).ThenInclude(rt => rt.Details)
                    .Include(h => h.InlandPortAdds)
                    .Include(h => h.LclPorts).ThenInclude(p => p.PortTypes).ThenInclude(pt => pt.Details)
                    .FirstOrDefaultAsync(h => h.FqhId == dto.CloneFromId.Value, ct);

                if (source != null)
                {
                    if (dto.FreightType == "ocean")
                    {
                        foreach (var sp in source.OceanPorts)
                        {
                            var np = new FreightQuoteOceanPort { FqopPort = sp.FqopPort, FqopRemarks = sp.FqopRemarks };
                            foreach (var sl in sp.ShippingLines)
                            {
                                var nsl = new FreightQuoteOceanPortSLine { FqopsShippingLine = sl.FqopsShippingLine, FqopsRoute = sl.FqopsRoute, FqopsDays = sl.FqopsDays };
                                foreach (var sc in sl.Charges)
                                {
                                    nsl.Charges.Add(new FreightQuoteOceanCharge { FqocChargeType = sc.FqocChargeType, FqocContainerType = sc.FqocContainerType, FqocAmount = sc.FqocAmount, FqocCurrency = sc.FqocCurrency });
                                }
                                np.ShippingLines.Add(nsl);
                            }
                            entity.OceanPorts.Add(np);
                        }
                    }
                    else if (dto.FreightType == "inland")
                    {
                        foreach (var sr in source.InlandRegions)
                        {
                            var nr = new FreightQuoteInlRegion { FqerRegion = sr.FqerRegion };
                            foreach (var rt in sr.RegionTypes)
                            {
                                var nrt = new FreightQuoteInlRegionType { FqertChargeType = rt.FqertChargeType, FqertAmountMin = rt.FqertAmountMin, FqertAmountMax = rt.FqertAmountMax, FqertCurrency = rt.FqertCurrency };
                                foreach (var det in rt.Details)
                                {
                                    nrt.Details.Add(new FreightQuoteInlRegionTypeDet { FqertdFrom = det.FqertdFrom, FqertdTo = det.FqertdTo, FqertdPrice = det.FqertdPrice, FqertdPriceType = det.FqertdPriceType, FqertdAmountMin = det.FqertdAmountMin, FqertdAmountMax = det.FqertdAmountMax });
                                }
                                nr.RegionTypes.Add(nrt);
                            }
                            entity.InlandRegions.Add(nr);
                        }
                    }
                    else if (dto.FreightType == "portadd")
                    {
                        foreach (var pa in source.InlandPortAdds)
                        {
                            entity.InlandPortAdds.Add(new FreightQuoteInlPortAdd
                            {
                                FqipaChargeType = pa.FqipaChargeType,
                                FqipaLoadType   = pa.FqipaLoadType,
                                FqipaAmount     = pa.FqipaAmount,
                                FqipaCurrency   = pa.FqipaCurrency,
                                FqipaAction     = pa.FqipaAction,
                                FqipaChargeOver = pa.FqipaChargeOver,
                                FqipaChargePer  = pa.FqipaChargePer,
                                FqipaFrom       = pa.FqipaFrom,
                                FqipaTo         = pa.FqipaTo,
                                FqipaAmountMin  = pa.FqipaAmountMin,
                                FqipaAmountMax  = pa.FqipaAmountMax
                            });
                        }
                    }
                    else if (dto.FreightType == "lcl")
                    {
                        foreach (var sp in source.LclPorts)
                        {
                            var np = new FreightQuoteLclPort { FqlcpPort = sp.FqlcpPort, FqlcpRemarks = sp.FqlcpRemarks };
                            foreach (var pt in sp.PortTypes)
                            {
                                var npt = new FreightQuoteLclPortType { FqlcptChargeType = pt.FqlcptChargeType, FqlcptAmountMin = pt.FqlcptAmountMin, FqlcptAmountMax = pt.FqlcptAmountMax, FqlcptCurrency = pt.FqlcptCurrency };
                                foreach (var det in pt.Details)
                                {
                                    npt.Details.Add(new FreightQuoteLclPortTypeDet { FqlcptdFrom = det.FqlcptdFrom, FqlcptdTo = det.FqlcptdTo, FqlcptdPrice = det.FqlcptdPrice, FqlcptdOver = det.FqlcptdOver, FqlcptdPriceType = det.FqlcptdPriceType, FqlcptdAmountMin = det.FqlcptdAmountMin, FqlcptdAmountMax = det.FqlcptdAmountMax });
                                }
                                np.PortTypes.Add(npt);
                            }
                            entity.LclPorts.Add(np);
                        }
                    }
                }
            }

            _db.FreightQuoteHeaders.Add(entity);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetQuote), new { id = entity.FqhId },
                ApiResponse<FreightQuoteHeader>.Ok(entity, "Quote created."));
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, ApiResponse.Fail($"Error creating quote: {msg}"));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateQuote(int id, [FromBody] FreightQuoteHeaderDto dto, CancellationToken ct)
    {
        try
        {
            var entity = await _db.FreightQuoteHeaders.FindAsync([id], ct);
            if (entity is null) return NotFound(ApiResponse.Fail($"Quote {id} not found."));

            // Allow changing quote number only if it doesn't collide within the same FreightType
            if (dto.QuoteNumber != entity.FqhQuoteNumber &&
                await _db.FreightQuoteHeaders.AnyAsync(x => x.FqhQuoteNumber == dto.QuoteNumber && x.FqhFreightType == entity.FqhFreightType && x.FqhId != id, ct))
                return Conflict(ApiResponse.Fail($"Quote number {dto.QuoteNumber} already exists for type {entity.FqhFreightType}."));

            entity.FqhQuoteNumber = dto.QuoteNumber;
            entity.FqhForwarder   = dto.Forwarder;
            entity.FqhPort        = dto.Port;
            entity.FqhRoute       = dto.Route;
            entity.FqhTransitDays = dto.TransitDays;
            entity.FqhStartDate   = dto.StartDate.HasValue ? DateOnly.FromDateTime(dto.StartDate.Value) : null;
            entity.FqhEndDate     = dto.EndDate.HasValue ? DateOnly.FromDateTime(dto.EndDate.Value) : null;

            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<FreightQuoteHeader>.Ok(entity, "Quote updated."));
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, ApiResponse.Fail($"Error updating quote: {msg}"));
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteQuote(int id, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteHeaders.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Quote {id} not found."));
        _db.FreightQuoteHeaders.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Quote deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // OCEAN PORTS (level 1 of 3)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("{quoteId:int}/ocean-ports")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddOceanPort(int quoteId, [FromBody] FreightQuoteOceanPortDto dto, CancellationToken ct)
    {
        if (!await _db.FreightQuoteHeaders.AnyAsync(x => x.FqhId == quoteId, ct))
            return NotFound(ApiResponse.Fail($"Quote {quoteId} not found."));
        var entity = new FreightQuoteOceanPort { FqopHeaderId = quoteId, FqopPort = dto.Port, FqopRemarks = dto.Remarks };
        _db.FreightQuoteOceanPorts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteOceanPort>.Ok(entity, "Ocean port added."));
    }

    [HttpPut("ocean-ports/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateOceanPort(int id, [FromBody] FreightQuoteOceanPortDto dto, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteOceanPorts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Ocean port {id} not found."));
        entity.FqopPort    = dto.Port;
        entity.FqopRemarks = dto.Remarks;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteOceanPort>.Ok(entity, "Ocean port updated."));
    }

    [HttpDelete("ocean-ports/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteOceanPort(int id, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteOceanPorts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Ocean port {id} not found."));
        _db.FreightQuoteOceanPorts.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Ocean port deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // OCEAN SHIPPING LINES (level 2 of 3)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("ocean-ports/{portId:int}/slines")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddOceanSLine(int portId, [FromBody] FreightQuoteOceanSLineDto dto, CancellationToken ct)
    {
        try
        {
            if (!await _db.FreightQuoteOceanPorts.AnyAsync(x => x.FqopId == portId, ct))
                return NotFound(ApiResponse.Fail($"Ocean port {portId} not found."));
            var entity = new FreightQuoteOceanPortSLine
            {
                FqopsPortId       = portId,
                FqopsShippingLine = dto.ShippingLine,
                FqopsRoute        = dto.Route,
                FqopsDays         = dto.Days
            };
            _db.FreightQuoteOceanPortSLines.Add(entity);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<FreightQuoteOceanPortSLine>.Ok(entity, "Shipping line added."));
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, ApiResponse.Fail($"Error adding shipping line: {msg}"));
        }
    }

    [HttpPut("ocean-slines/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateOceanSLine(int id, [FromBody] FreightQuoteOceanSLineDto dto, CancellationToken ct)
    {
        try
        {
            var entity = await _db.FreightQuoteOceanPortSLines.FindAsync([id], ct);
            if (entity is null) return NotFound(ApiResponse.Fail($"Shipping line {id} not found."));
            entity.FqopsShippingLine = dto.ShippingLine;
            entity.FqopsRoute        = dto.Route;
            entity.FqopsDays         = dto.Days;
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<FreightQuoteOceanPortSLine>.Ok(entity, "Shipping line updated."));
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, ApiResponse.Fail($"Error updating shipping line: {msg}"));
        }
    }

    [HttpDelete("ocean-slines/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteOceanSLine(int id, CancellationToken ct)
    {
        try
        {
            var entity = await _db.FreightQuoteOceanPortSLines.FindAsync([id], ct);
            if (entity is null) return NotFound(ApiResponse.Fail($"Shipping line {id} not found."));
            _db.FreightQuoteOceanPortSLines.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse.Ok("Shipping line deleted."));
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, ApiResponse.Fail($"Error deleting shipping line: {msg}"));
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // OCEAN CHARGES (level 3 of 3)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("ocean-slines/{slineId:int}/charges")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddOceanCharge(int slineId, [FromBody] FreightQuoteOceanChargeDto dto, CancellationToken ct)
    {
        try
        {
            if (!await _db.FreightQuoteOceanPortSLines.AnyAsync(x => x.FqopsId == slineId, ct))
                return NotFound(ApiResponse.Fail($"Shipping line {slineId} not found."));
            var entity = new FreightQuoteOceanCharge
            {
                FqocSLineId       = slineId,
                FqocChargeType    = dto.ChargeType,
                FqocContainerType = dto.ContainerType,
                FqocAmount        = dto.Amount,
                FqocCurrency      = dto.Currency
            };
            _db.FreightQuoteOceanCharges.Add(entity);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<FreightQuoteOceanCharge>.Ok(entity, "Ocean charge added."));
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, ApiResponse.Fail($"Error adding ocean charge: {msg}"));
        }
    }

    [HttpPut("ocean-charges/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateOceanCharge(int id, [FromBody] FreightQuoteOceanChargeDto dto, CancellationToken ct)
    {
        try
        {
            var entity = await _db.FreightQuoteOceanCharges.FindAsync([id], ct);
            if (entity is null) return NotFound(ApiResponse.Fail($"Ocean charge {id} not found."));
            entity.FqocChargeType    = dto.ChargeType;
            entity.FqocContainerType = dto.ContainerType;
            entity.FqocAmount        = dto.Amount;
            entity.FqocCurrency      = dto.Currency;
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<FreightQuoteOceanCharge>.Ok(entity, "Ocean charge updated."));
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, ApiResponse.Fail($"Error updating ocean charge: {msg}"));
        }
    }

    [HttpDelete("ocean-charges/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteOceanCharge(int id, CancellationToken ct)
    {
        try
        {
            var entity = await _db.FreightQuoteOceanCharges.FindAsync([id], ct);
            if (entity is null) return NotFound(ApiResponse.Fail($"Ocean charge {id} not found."));
            _db.FreightQuoteOceanCharges.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse.Ok("Ocean charge deleted."));
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, ApiResponse.Fail($"Error deleting ocean charge: {msg}"));
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // INLAND REGIONS (level 1 of 3)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("{quoteId:int}/inland-regions")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddInlandRegion(int quoteId, [FromBody] FreightQuoteInlRegionDto dto, CancellationToken ct)
    {
        if (!await _db.FreightQuoteHeaders.AnyAsync(x => x.FqhId == quoteId, ct))
            return NotFound(ApiResponse.Fail($"Quote {quoteId} not found."));

        var entity = new FreightQuoteInlRegion
        {
            FqerHeaderId = quoteId,
            FqerRegion   = dto.Region
        };
        _db.FreightQuoteInlRegions.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteInlRegion>.Ok(entity, "Inland region added."));
    }

    [HttpDelete("inland-regions/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteInlandRegion(int id, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteInlRegions.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Inland region {id} not found."));
        _db.FreightQuoteInlRegions.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Inland region deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // INLAND REGION TYPES (level 2 of 3)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("inland-regions/{regionId:int}/types")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddInlandRegionType(int regionId, [FromBody] FreightQuoteInlRegionTypeDto dto, CancellationToken ct)
    {
        if (!await _db.FreightQuoteInlRegions.AnyAsync(x => x.FqerId == regionId, ct))
            return NotFound(ApiResponse.Fail($"Inland region {regionId} not found."));

        var entity = new FreightQuoteInlRegionType
        {
            FqertRegionId   = regionId,
            FqertChargeType = dto.ChargeType,
            FqertAmountMin  = dto.AmountMin,
            FqertAmountMax  = dto.AmountMax,
            FqertCurrency   = dto.Currency
        };
        _db.FreightQuoteInlRegionTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteInlRegionType>.Ok(entity, "Region type added."));
    }

    [HttpPut("inland-region-types/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateInlandRegionType(int id, [FromBody] FreightQuoteInlRegionTypeDto dto, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteInlRegionTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Region type {id} not found."));
        entity.FqertChargeType = dto.ChargeType;
        entity.FqertAmountMin  = dto.AmountMin;
        entity.FqertAmountMax  = dto.AmountMax;
        entity.FqertCurrency   = dto.Currency;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteInlRegionType>.Ok(entity, "Region type updated."));
    }

    [HttpDelete("inland-region-types/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteInlandRegionType(int id, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteInlRegionTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Region type {id} not found."));
        _db.FreightQuoteInlRegionTypes.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Region type deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // INLAND REGION TYPE DETAILS / ESCALONAMIENTO (level 3 of 3)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("inland-region-types/{typeId:int}/details")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddInlandDetail(int typeId, [FromBody] FreightQuoteInlRegionTypeDetDto dto, CancellationToken ct)
    {
        if (!await _db.FreightQuoteInlRegionTypes.AnyAsync(x => x.FqertId == typeId, ct))
            return NotFound(ApiResponse.Fail($"Region type {typeId} not found."));

        var entity = new FreightQuoteInlRegionTypeDet
        {
            FqertdRegionTypeId = typeId,
            FqertdFrom         = dto.From,
            FqertdTo           = dto.To,
            FqertdPrice        = dto.Price,
            FqertdPriceType    = dto.PriceType,
            FqertdAmountMin    = dto.AmountMin,
            FqertdAmountMax    = dto.AmountMax
        };
        _db.FreightQuoteInlRegionTypeDets.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteInlRegionTypeDet>.Ok(entity, "Detail added."));
    }

    [HttpPut("inland-details/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateInlandDetail(int id, [FromBody] FreightQuoteInlRegionTypeDetDto dto, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteInlRegionTypeDets.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Detail {id} not found."));
        entity.FqertdFrom      = dto.From;
        entity.FqertdTo        = dto.To;
        entity.FqertdPrice     = dto.Price;
        entity.FqertdPriceType = dto.PriceType;
        entity.FqertdAmountMin = dto.AmountMin;
        entity.FqertdAmountMax = dto.AmountMax;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteInlRegionTypeDet>.Ok(entity, "Detail updated."));
    }

    [HttpDelete("inland-details/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteInlandDetail(int id, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteInlRegionTypeDets.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Detail {id} not found."));
        _db.FreightQuoteInlRegionTypeDets.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Detail deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // INLAND PORT ADDITIONAL CHARGES
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("{quoteId:int}/port-adds")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddPortAdd(int quoteId, [FromBody] FreightQuoteInlPortAddDto dto, CancellationToken ct)
    {
        if (!await _db.FreightQuoteHeaders.AnyAsync(x => x.FqhId == quoteId, ct))
            return NotFound(ApiResponse.Fail($"Quote {quoteId} not found."));

        var entity = new FreightQuoteInlPortAdd
        {
            FqipaHeaderId   = quoteId,
            FqipaChargeType = dto.ChargeType,
            FqipaLoadType   = dto.LoadType,
            FqipaAmount     = dto.Amount,
            FqipaAction     = dto.Action,
            FqipaChargeOver = dto.ChargeOver,
            FqipaChargePer  = dto.ChargePer,
            FqipaFrom       = dto.From,
            FqipaTo         = dto.To,
            FqipaAmountMin  = dto.AmountMin,
            FqipaAmountMax  = dto.AmountMax,
            FqipaCurrency   = dto.Currency
        };
        _db.FreightQuoteInlPortAdds.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteInlPortAdd>.Ok(entity, "Port additional charge added."));
    }

    [HttpPut("port-adds/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdatePortAdd(int id, [FromBody] FreightQuoteInlPortAddDto dto, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteInlPortAdds.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port additional charge {id} not found."));
        entity.FqipaChargeType = dto.ChargeType;
        entity.FqipaLoadType   = dto.LoadType;
        entity.FqipaAmount     = dto.Amount;
        entity.FqipaAction     = dto.Action;
        entity.FqipaChargeOver = dto.ChargeOver;
        entity.FqipaChargePer  = dto.ChargePer;
        entity.FqipaFrom       = dto.From;
        entity.FqipaTo         = dto.To;
        entity.FqipaAmountMin  = dto.AmountMin;
        entity.FqipaAmountMax  = dto.AmountMax;
        entity.FqipaCurrency   = dto.Currency;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteInlPortAdd>.Ok(entity, "Port additional charge updated."));
    }

    [HttpDelete("port-adds/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeletePortAdd(int id, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteInlPortAdds.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port additional charge {id} not found."));
        _db.FreightQuoteInlPortAdds.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Port additional charge deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // LCL PORTS (level 1 of 3)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("{quoteId:int}/lcl-ports")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddLclPort(int quoteId, [FromBody] FreightQuoteLclPortDto dto, CancellationToken ct)
    {
        if (!await _db.FreightQuoteHeaders.AnyAsync(x => x.FqhId == quoteId, ct))
            return NotFound(ApiResponse.Fail($"Quote {quoteId} not found."));
        var entity = new FreightQuoteLclPort { FqlcpHeaderId = quoteId, FqlcpPort = dto.Port, FqlcpRemarks = dto.Remarks };
        _db.FreightQuoteLclPorts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteLclPort>.Ok(entity, "LCL port added."));
    }

    [HttpPut("lcl-ports/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateLclPort(int id, [FromBody] FreightQuoteLclPortDto dto, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteLclPorts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"LCL port {id} not found."));
        entity.FqlcpPort    = dto.Port;
        entity.FqlcpRemarks = dto.Remarks;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteLclPort>.Ok(entity, "LCL port updated."));
    }

    [HttpDelete("lcl-ports/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteLclPort(int id, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteLclPorts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"LCL port {id} not found."));
        _db.FreightQuoteLclPorts.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("LCL port deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // LCL PORT TYPES / SHIPPING (level 2 of 3)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("lcl-ports/{portId:int}/types")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddLclPortType(int portId, [FromBody] FreightQuoteLclPortTypeDto dto, CancellationToken ct)
    {
        if (!await _db.FreightQuoteLclPorts.AnyAsync(x => x.FqlcpId == portId, ct))
            return NotFound(ApiResponse.Fail($"LCL port {portId} not found."));
        var entity = new FreightQuoteLclPortType
        {
            FqlcptPortId     = portId,
            FqlcptChargeType = dto.ChargeType,
            FqlcptAmountMin  = dto.AmountMin,
            FqlcptAmountMax  = dto.AmountMax,
            FqlcptCurrency   = dto.Currency
        };
        _db.FreightQuoteLclPortTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteLclPortType>.Ok(entity, "LCL port type added."));
    }

    [HttpPut("lcl-port-types/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateLclPortType(int id, [FromBody] FreightQuoteLclPortTypeDto dto, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteLclPortTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"LCL port type {id} not found."));
        entity.FqlcptChargeType = dto.ChargeType;
        entity.FqlcptAmountMin  = dto.AmountMin;
        entity.FqlcptAmountMax  = dto.AmountMax;
        entity.FqlcptCurrency   = dto.Currency;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteLclPortType>.Ok(entity, "LCL port type updated."));
    }

    [HttpDelete("lcl-port-types/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteLclPortType(int id, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteLclPortTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"LCL port type {id} not found."));
        _db.FreightQuoteLclPortTypes.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("LCL port type deleted."));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // LCL PORT TYPE DETAILS (level 3 of 3)
    // ──────────────────────────────────────────────────────────────────────────

    [HttpPost("lcl-port-types/{typeId:int}/details")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddLclDetail(int typeId, [FromBody] FreightQuoteLclPortTypeDetDto dto, CancellationToken ct)
    {
        if (!await _db.FreightQuoteLclPortTypes.AnyAsync(x => x.FqlcptId == typeId, ct))
            return NotFound(ApiResponse.Fail($"LCL port type {typeId} not found."));
        var entity = new FreightQuoteLclPortTypeDet
        {
            FqlcptdPortTypeId = typeId,
            FqlcptdFrom       = dto.From,
            FqlcptdTo         = dto.To,
            FqlcptdPrice      = dto.Price,
            FqlcptdOver       = dto.Over,
            FqlcptdPriceType  = dto.PriceType,
            FqlcptdAmountMin  = dto.AmountMin,
            FqlcptdAmountMax  = dto.AmountMax
        };
        _db.FreightQuoteLclPortTypeDets.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteLclPortTypeDet>.Ok(entity, "LCL detail added."));
    }

    [HttpPut("lcl-details/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateLclDetail(int id, [FromBody] FreightQuoteLclPortTypeDetDto dto, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteLclPortTypeDets.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"LCL detail {id} not found."));
        entity.FqlcptdFrom      = dto.From;
        entity.FqlcptdTo        = dto.To;
        entity.FqlcptdPrice     = dto.Price;
        entity.FqlcptdOver      = dto.Over;
        entity.FqlcptdPriceType = dto.PriceType;
        entity.FqlcptdAmountMin = dto.AmountMin;
        entity.FqlcptdAmountMax = dto.AmountMax;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FreightQuoteLclPortTypeDet>.Ok(entity, "LCL detail updated."));
    }

    [HttpDelete("lcl-details/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteLclDetail(int id, CancellationToken ct)
    {
        var entity = await _db.FreightQuoteLclPortTypeDets.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"LCL detail {id} not found."));
        _db.FreightQuoteLclPortTypeDets.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("LCL detail deleted."));
    }

    // ── Forwarders that have at least one quote ───────────────────────────────

    [HttpGet("forwarders-with-quotes")]
    public async Task<IActionResult> GetForwardersWithQuotes(CancellationToken ct)
    {
        var data = await _db.FreightQuoteHeaders
            .AsNoTracking()
            .GroupBy(x => x.FqhForwarder)
            .Select(g => new { Code = g.Key, QuoteCount = g.Count() })
            .OrderBy(x => x.Code)
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(data));
    }

    // ── Pre-Calculation: latest Ocean + Inland rates for a Forwarder ──────────

    [HttpGet("latest-rates/{forwarderCode}")]
    public async Task<IActionResult> GetLatestRates(string forwarderCode, CancellationToken ct)
    {
        var oceanHeader = await _db.FreightQuoteHeaders
            .AsNoTracking()
            .Where(x => x.FqhForwarder == forwarderCode && x.FqhFreightType == "ocean")
            .OrderByDescending(x => x.FqhQuoteNumber)
            .Include(x => x.OceanPorts)
                .ThenInclude(p => p.ShippingLines)
                    .ThenInclude(sl => sl.Charges)
            .FirstOrDefaultAsync(ct);

        decimal? oceanTotal = null;
        string?  oceanCurr  = null;
        if (oceanHeader is not null)
        {
            var charges = oceanHeader.OceanPorts
                .SelectMany(p => p.ShippingLines)
                .SelectMany(sl => sl.Charges)
                .Where(c => c.FqocAmount.HasValue)
                .ToList();
            if (charges.Count > 0)
            {
                oceanTotal = charges.Sum(c => c.FqocAmount!.Value);
                oceanCurr  = charges.FirstOrDefault(c => c.FqocCurrency != null)?.FqocCurrency;
            }
        }

        var inlandHeader = await _db.FreightQuoteHeaders
            .AsNoTracking()
            .Where(x => x.FqhForwarder == forwarderCode && x.FqhFreightType == "inland")
            .OrderByDescending(x => x.FqhQuoteNumber)
            .Include(x => x.InlandRegions)
                .ThenInclude(r => r.RegionTypes)
                    .ThenInclude(rt => rt.Details)
            .FirstOrDefaultAsync(ct);

        decimal? inlandAmount = null;
        string?  inlandCurr   = null;
        if (inlandHeader is not null)
        {
            var firstType = inlandHeader.InlandRegions
                .SelectMany(r => r.RegionTypes)
                .FirstOrDefault();
            if (firstType is not null)
            {
                inlandCurr = firstType.FqertCurrency;
                // Try AmountMin on the type, then fall back to first detail's AmountMin or Price
                inlandAmount = firstType.FqertAmountMin
                    ?? firstType.Details.FirstOrDefault()?.FqertdAmountMin
                    ?? firstType.Details.FirstOrDefault()?.FqertdPrice;
            }
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            OceanFreight   = oceanTotal,
            OceanCurrency  = oceanCurr,
            OceanQuoteNo   = oceanHeader?.FqhQuoteNumber,
            InlandFreight  = inlandAmount,
            InlandCurrency = inlandCurr,
            InlandQuoteNo  = inlandHeader?.FqhQuoteNumber
        }));
    }

    // ── Clone Quote (copy-on-write versioning) ────────────────────────────────

    [HttpPost("{id:int}/clone")]
    public async Task<IActionResult> CloneQuote(int id, CancellationToken ct)
    {
        var src = await _db.FreightQuoteHeaders
            .Include(x => x.OceanPorts)
                .ThenInclude(p => p.ShippingLines)
                    .ThenInclude(sl => sl.Charges)
            .Include(x => x.InlandRegions)
                .ThenInclude(r => r.RegionTypes)
                    .ThenInclude(rt => rt.Details)
            .Include(x => x.InlandPortAdds)
            .Include(x => x.LclPorts)
                .ThenInclude(p => p.PortTypes)
                    .ThenInclude(pt => pt.Details)
            .FirstOrDefaultAsync(x => x.FqhId == id, ct);

        if (src is null)
            return NotFound(ApiResponse<object>.Fail("Quote not found"));

        var nextNo = (await _db.FreightQuoteHeaders
            .Where(x => x.FqhForwarder == src.FqhForwarder && x.FqhFreightType == src.FqhFreightType)
            .MaxAsync(x => (int?)x.FqhQuoteNumber, ct) ?? 0) + 1;

        var clone = new FreightQuoteHeader
        {
            FqhQuoteNumber = nextNo,
            FqhForwarder   = src.FqhForwarder,
            FqhFreightType = src.FqhFreightType,
            FqhPort        = src.FqhPort,
            FqhRoute       = src.FqhRoute,
            FqhTransitDays = src.FqhTransitDays,
            FqhStartDate   = src.FqhStartDate,
            FqhEndDate     = src.FqhEndDate,
        };

        foreach (var port in src.OceanPorts)
        {
            var np = new FreightQuoteOceanPort { FqopPort = port.FqopPort, FqopRemarks = port.FqopRemarks };
            foreach (var sl in port.ShippingLines)
            {
                var nsl = new FreightQuoteOceanPortSLine
                {
                    FqopsShippingLine = sl.FqopsShippingLine,
                    FqopsRoute        = sl.FqopsRoute,
                    FqopsDays         = sl.FqopsDays,
                };
                foreach (var ch in sl.Charges)
                    nsl.Charges.Add(new FreightQuoteOceanCharge
                    {
                        FqocChargeType    = ch.FqocChargeType,
                        FqocContainerType = ch.FqocContainerType,
                        FqocAmount        = ch.FqocAmount,
                        FqocCurrency      = ch.FqocCurrency,
                    });
                np.ShippingLines.Add(nsl);
            }
            clone.OceanPorts.Add(np);
        }

        foreach (var reg in src.InlandRegions)
        {
            var nr = new FreightQuoteInlRegion { FqerRegion = reg.FqerRegion };
            foreach (var rt in reg.RegionTypes)
            {
                var nrt = new FreightQuoteInlRegionType
                {
                    FqertChargeType = rt.FqertChargeType,
                    FqertAmountMin  = rt.FqertAmountMin,
                    FqertAmountMax  = rt.FqertAmountMax,
                    FqertCurrency   = rt.FqertCurrency,
                };
                foreach (var det in rt.Details)
                    nrt.Details.Add(new FreightQuoteInlRegionTypeDet
                    {
                        FqertdFrom      = det.FqertdFrom,
                        FqertdTo        = det.FqertdTo,
                        FqertdPrice     = det.FqertdPrice,
                        FqertdPriceType = det.FqertdPriceType,
                        FqertdAmountMin = det.FqertdAmountMin,
                        FqertdAmountMax = det.FqertdAmountMax,
                    });
                nr.RegionTypes.Add(nrt);
            }
            clone.InlandRegions.Add(nr);
        }

        foreach (var pa in src.InlandPortAdds)
            clone.InlandPortAdds.Add(new FreightQuoteInlPortAdd
            {
                FqipaChargeType = pa.FqipaChargeType,
                FqipaLoadType   = pa.FqipaLoadType,
                FqipaAmount     = pa.FqipaAmount,
                FqipaAction     = pa.FqipaAction,
                FqipaChargeOver = pa.FqipaChargeOver,
                FqipaChargePer  = pa.FqipaChargePer,
                FqipaFrom       = pa.FqipaFrom,
                FqipaTo         = pa.FqipaTo,
                FqipaAmountMin  = pa.FqipaAmountMin,
                FqipaAmountMax  = pa.FqipaAmountMax,
                FqipaCurrency   = pa.FqipaCurrency,
            });

        foreach (var lp in src.LclPorts)
        {
            var nlp = new FreightQuoteLclPort { FqlcpPort = lp.FqlcpPort, FqlcpRemarks = lp.FqlcpRemarks };
            foreach (var pt in lp.PortTypes)
            {
                var npt = new FreightQuoteLclPortType
                {
                    FqlcptChargeType = pt.FqlcptChargeType,
                    FqlcptAmountMin  = pt.FqlcptAmountMin,
                    FqlcptAmountMax  = pt.FqlcptAmountMax,
                    FqlcptCurrency   = pt.FqlcptCurrency,
                };
                foreach (var det in pt.Details)
                    npt.Details.Add(new FreightQuoteLclPortTypeDet
                    {
                        FqlcptdFrom      = det.FqlcptdFrom,
                        FqlcptdTo        = det.FqlcptdTo,
                        FqlcptdPrice     = det.FqlcptdPrice,
                        FqlcptdOver      = det.FqlcptdOver,
                        FqlcptdPriceType = det.FqlcptdPriceType,
                        FqlcptdAmountMin = det.FqlcptdAmountMin,
                        FqlcptdAmountMax = det.FqlcptdAmountMax,
                    });
                nlp.PortTypes.Add(npt);
            }
            clone.LclPorts.Add(nlp);
        }

        _db.FreightQuoteHeaders.Add(clone);
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { Id = clone.FqhId, QuoteNumber = clone.FqhQuoteNumber }));
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record FreightQuoteHeaderDto(
    int QuoteNumber,
    string Forwarder,
    string FreightType,
    string? Port,
    string? Route,
    int? TransitDays,
    DateTime? StartDate,
    DateTime? EndDate,
    int? CloneFromId = null);

public sealed record FreightQuoteOceanPortDto(string Port, string? Remarks);

public sealed record FreightQuoteOceanSLineDto(string ShippingLine, string? Route, int? Days);

public sealed record FreightQuoteOceanChargeDto(
    string   ChargeType,
    string?  ContainerType,
    decimal? Amount,
    string?  Currency
);

public sealed record FreightQuoteInlRegionDto(
    string Region
);

public sealed record FreightQuoteInlRegionTypeDto(
    string   ChargeType,
    decimal? AmountMin,
    decimal? AmountMax,
    string?  Currency
);

public sealed record FreightQuoteInlRegionTypeDetDto(
    decimal? From,
    decimal? To,
    decimal? Price,
    string?  PriceType,
    decimal? AmountMin,
    decimal? AmountMax
);

public sealed record FreightQuoteInlPortAddDto(
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

public sealed record FreightQuoteLclPortDto(string Port, string? Remarks);

public sealed record FreightQuoteLclPortTypeDto(
    string   ChargeType,
    decimal? AmountMin,
    decimal? AmountMax,
    string?  Currency
);

public sealed record FreightQuoteLclPortTypeDetDto(
    decimal? From,
    decimal? To,
    decimal? Price,
    decimal? Over,
    string?  PriceType,
    decimal? AmountMin,
    decimal? AmountMax
);
