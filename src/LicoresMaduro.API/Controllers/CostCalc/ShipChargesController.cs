using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/calculations/{calcId:int}/ship-charges")]
[Authorize]
[Produces("application/json")]
public sealed class ShipChargesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ShipChargesController(ApplicationDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll(int calcId, CancellationToken ct)
    {
        var data = await _db.CcShipCharges.AsNoTracking()
            .Where(x => x.ScCalcNumber == calcId)
            .OrderBy(x => x.ScChargeCode)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<CcShipCharge>>.Ok(data));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create(int calcId, [FromBody] ShipChargeDto dto, CancellationToken ct)
    {
        if (!await _db.CcCalcHeaders.AnyAsync(x => x.CcCalcNumber == calcId, ct))
            return NotFound(ApiResponse.Fail($"Calculation {calcId} not found."));
        var item = new CcShipCharge
        {
            ScCalcNumber  = calcId,
            ScChargeCode  = dto.ChargeCode,
            ScDescription = dto.Description,
            ScAmount      = dto.Amount,
            ScCurrency    = dto.Currency,
            ScRate        = dto.Rate,
            CreatedAt     = DateTime.UtcNow
        };
        _db.CcShipCharges.Add(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CcShipCharge>.Ok(item, "Created."));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int calcId, int id, [FromBody] ShipChargeDto dto, CancellationToken ct)
    {
        var item = await _db.CcShipCharges.FirstOrDefaultAsync(x => x.ScId == id && x.ScCalcNumber == calcId, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Ship charge {id} not found."));
        item.ScChargeCode  = dto.ChargeCode;
        item.ScDescription = dto.Description;
        item.ScAmount      = dto.Amount;
        item.ScCurrency    = dto.Currency;
        item.ScRate        = dto.Rate;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CcShipCharge>.Ok(item, "Updated."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int calcId, int id, CancellationToken ct)
    {
        var item = await _db.CcShipCharges.FirstOrDefaultAsync(x => x.ScId == id && x.ScCalcNumber == calcId, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Ship charge {id} not found."));
        _db.CcShipCharges.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }
}

public record ShipChargeDto(string ChargeCode, string? Description, decimal Amount, string? Currency, decimal? Rate);
