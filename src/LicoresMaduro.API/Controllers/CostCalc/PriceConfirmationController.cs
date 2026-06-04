using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/price-confirmations")]
[Authorize]
[Produces("application/json")]
public sealed class PriceConfirmationController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PriceConfirmationController> _logger;

    public PriceConfirmationController(ApplicationDbContext db, ILogger<PriceConfirmationController> logger)
    { _db = db; _logger = logger; }

    // Level 1: POs pending manager price confirmation
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var pos = await _db.CcCalcPoHeads
            .AsNoTracking()
            .Where(p => p.CcphStatus == "PC")
            .OrderBy(p => p.CcphCalcNumber).ThenBy(p => p.CcphLmPoNo)
            .Select(p => new {
                p.CcphCalcNumber,
                p.CcphLmPoNo,
                p.CcphVendNo,
                p.CcphVendName,
                p.CcphCurrCode,
                p.CcphCurrRate,
                p.CcphInvNumber,
                p.CcphInvDate,
                p.CcphTotQty,
                p.CcphTotAmount,
                p.CcphWeight,
                p.CcphInlandFreight
            })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(pos));
    }

    // Level 2: Product analysis for a specific PO
    [HttpGet("{calcId:int}/{poNo}")]
    public async Task<IActionResult> GetPoItems(int calcId, string poNo, CancellationToken ct)
    {
        var items = await _db.CcPriceConfirmations
            .AsNoTracking()
            .Where(x => x.CcpcCalcNumber == calcId && x.CcpcPoNo == poNo)
            .OrderBy(x => x.CcpcItemNo)
            .ToListAsync(ct);
        if (!items.Any()) return NotFound(ApiResponse.Fail($"No price data found for Calc {calcId} / PO {poNo}."));
        return Ok(ApiResponse<object>.Ok(items));
    }

    // Level 3: Full case prices for a specific item
    [HttpGet("{calcId:int}/{poNo}/{itemNo}")]
    public async Task<IActionResult> GetItemPrices(int calcId, string poNo, string itemNo, CancellationToken ct)
    {
        var item = await _db.CcPriceConfirmations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CcpcCalcNumber == calcId
                && x.CcpcPoNo == poNo && x.CcpcItemNo == itemNo, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Item {itemNo} not found in price confirmation."));
        return Ok(ApiResponse<CcPriceConfirmation>.Ok(item));
    }

    // Bidirectional recalc: given price compute margin, or given margin compute price
    [HttpPost("recalc")]
    public IActionResult Recalc([FromBody] RecalcDto dto)
    {
        if (dto.Price.HasValue && dto.Price > 0 && dto.Cost > 0)
        {
            var margin = Math.Round(((dto.Price.Value - dto.Cost) / dto.Price.Value) * 100m, 2);
            return Ok(ApiResponse<object>.Ok(new { margin, price = dto.Price.Value }));
        }
        if (dto.Margin.HasValue && dto.Margin < 100 && dto.Cost > 0)
        {
            var price = dto.Cost / (1m - dto.Margin.Value / 100m);
            var margin = Math.Round(((price - dto.Cost) / price) * 100m, 2);
            return Ok(ApiResponse<object>.Ok(new { margin, price }));
        }
        return BadRequest(ApiResponse.Fail("Provide either Price or Margin (not both), and a valid Cost > 0."));
    }

    // Approve price changes for a PO
    [HttpPost("{calcId:int}/{poNo}/approve")]
    public async Task<IActionResult> ApprovePo(int calcId, string poNo,
        [FromBody] ApprovePoDto dto, CancellationToken ct)
    {
        var po = await _db.CcCalcPoHeads
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.CcphCalcNumber == calcId && p.CcphLmPoNo == poNo, ct);
        if (po is null) return NotFound(ApiResponse.Fail($"PO {poNo} not found."));
        if (po.CcphStatus != "PC") return BadRequest(ApiResponse.Fail("Only PC-status POs can be approved here."));

        if (dto.ItemFlags?.Count > 0)
        {
            var itemNos = dto.ItemFlags.Keys.ToList();
            var rows = await _db.CcPriceConfirmations
                .Where(x => x.CcpcCalcNumber == calcId && x.CcpcPoNo == poNo
                    && itemNos.Contains(x.CcpcItemNo))
                .ToListAsync(ct);
            foreach (var row in rows)
            {
                if (dto.ItemFlags.TryGetValue(row.CcpcItemNo, out var flag))
                {
                    row.CcpcPriceChangeFlag = flag;
                    row.CcpcApprovedBy = User.Identity?.Name;
                    row.CcpcApprovedAt = DateTime.UtcNow;
                }
            }
        }

        foreach (var detail in po.Details)
        {
            var confirmed = await _db.CcPriceConfirmations
                .FirstOrDefaultAsync(x => x.CcpcCalcNumber == calcId
                    && x.CcpcPoNo == poNo && x.CcpcItemNo == detail.CcpdItemNo, ct);
            if (confirmed?.CcpcNewPricePr01 != null)
                detail.CcpdSellingPrice = confirmed.CcpcNewPricePr01;
        }

        po.CcphStatus          = "PD";
        po.CcphReasonCode      = dto.ReasonCode;
        po.CcphPriceChangeDate = dto.PriceChangeDate;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("PriceConfirm: Calc {CalcId} PO {PoNo} approved by {User}",
            calcId, poNo, User.Identity?.Name);
        return Ok(ApiResponse.Ok("Price changes approved."));
    }

    // Reason codes catalog
    [HttpGet("reasons")]
    public async Task<IActionResult> GetReasons(CancellationToken ct)
    {
        var reasons = await _db.CcPriceChangeReasons
            .AsNoTracking()
            .Where(r => r.PcrActive)
            .OrderBy(r => r.PcrCode)
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(reasons));
    }
}

public record RecalcDto(decimal Cost, decimal? Price, decimal? Margin);
public record ApprovePoDto(string? ReasonCode, DateTime? PriceChangeDate, Dictionary<string, bool>? ItemFlags);
