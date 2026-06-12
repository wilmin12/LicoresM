using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Models.Auth;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;

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
    [HttpGet("{calcId:int}")]
    public async Task<IActionResult> GetPoItems(int calcId, [FromQuery] string poNo, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(poNo))
            return BadRequest(ApiResponse.Fail("poNo is required."));
        var items = await _db.CcPriceConfirmations
            .AsNoTracking()
            .Where(x => x.CcpcCalcNumber == calcId && x.CcpcPoNo == poNo)
            .OrderBy(x => x.CcpcItemNo)
            .ToListAsync(ct);
        if (!items.Any()) return NotFound(ApiResponse.Fail($"No price data found for Calc {calcId} / PO {poNo}."));
        return Ok(ApiResponse<object>.Ok(items));
    }

    // Level 3: Full case prices for a specific item
    [HttpGet("{calcId:int}/item")]
    public async Task<IActionResult> GetItemPrices(int calcId, [FromQuery] string poNo, [FromQuery] string itemNo, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(poNo) || string.IsNullOrEmpty(itemNo))
            return BadRequest(ApiResponse.Fail("poNo and itemNo are required."));
        var item = await _db.CcPriceConfirmations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CcpcCalcNumber == calcId
                && x.CcpcPoNo == poNo && x.CcpcItemNo == itemNo, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Item {itemNo} not found in price confirmation."));
        return Ok(ApiResponse<CcPriceConfirmation>.Ok(item));
    }

    // Save manager decision for one item: price-change flag + edited prices/margins
    [HttpPut("{calcId:int}/item")]
    public async Task<IActionResult> SaveItem(int calcId, [FromQuery] string poNo, [FromQuery] string itemNo,
        [FromBody] SaveItemDto dto, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(poNo) || string.IsNullOrEmpty(itemNo))
            return BadRequest(ApiResponse.Fail("poNo and itemNo are required."));
        var row = await _db.CcPriceConfirmations
            .FirstOrDefaultAsync(x => x.CcpcCalcNumber == calcId
                && x.CcpcPoNo == poNo && x.CcpcItemNo == itemNo, ct);
        if (row is null) return NotFound(ApiResponse.Fail($"Item {itemNo} not found in price confirmation."));

        row.CcpcPriceChangeFlag = dto.PriceChangeFlag;
        if (dto.Prices is not null)
        {
            // PR01/PR03/PR04/PR05 are locked (system-calculated); the rest are manager-editable
            var editable = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "Pr06", "Pr07", "Pr08", "Pr09", "Pr10", "Pr11" };
            foreach (var (key, pm) in dto.Prices)
            {
                if (!editable.Contains(key)) continue;
                var priceProp  = typeof(CcPriceConfirmation).GetProperty($"CcpcNewPrice{key}");
                var marginProp = typeof(CcPriceConfirmation).GetProperty($"CcpcNewMargin{key}");
                if (pm.Price.HasValue)  priceProp?.SetValue(row, pm.Price);
                if (pm.Margin.HasValue) marginProp?.SetValue(row, pm.Margin);
            }
        }
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Item saved."));
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
    [HttpPost("{calcId:int}/approve")]
    public async Task<IActionResult> ApprovePo(int calcId, [FromQuery] string poNo,
        [FromBody] ApprovePoDto dto, CancellationToken ct)
    {
        var po = await _db.CcCalcPoHeads
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.CcphCalcNumber == calcId && p.CcphLmPoNo == poNo, ct);
        if (po is null) return NotFound(ApiResponse.Fail($"PO {poNo} not found."));
        if (po.CcphStatus != "PC") return BadRequest(ApiResponse.Fail("Only PC-status POs can be approved here."));

        // 4-eyes: confirmer cannot approve prices
        var currentUser = User.Identity?.Name ?? string.Empty;
        if (!string.IsNullOrEmpty(po.CcphConfirmedBy) &&
            string.Equals(po.CcphConfirmedBy, currentUser, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Fail("The user who confirmed this calculation cannot approve prices. Another user must approve."));

        // All items must be flagged (Price change / No price change) before approval
        var poRows = await _db.CcPriceConfirmations
            .Where(x => x.CcpcCalcNumber == calcId && x.CcpcPoNo == poNo)
            .ToListAsync(ct);
        var unflagged = poRows
            .Where(x => x.CcpcPriceChangeFlag == null
                && (dto.ItemFlags == null || !dto.ItemFlags.ContainsKey(x.CcpcItemNo)))
            .Select(x => x.CcpcItemNo)
            .ToList();
        if (unflagged.Count > 0)
            return BadRequest(ApiResponse.Fail(
                $"All items must be marked Price change / No price change before approving. Pending: {string.Join(", ", unflagged)}"));

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
        po.CcphApprovedBy      = currentUser;

        // Transition calc to AP on first PO approval
        var calc = await _db.CcCalcHeaders.FirstOrDefaultAsync(c => c.CcCalcNumber == calcId, ct);
        if (calc is not null && calc.CcStatus == "CF")
            calc.CcStatus = "AP";

        await _db.SaveChangesAsync(ct);

        // Fire-and-forget: send notification email
        LmEmailConfig? emailCfg = null;
        string? approvalEmail = null;
        try
        {
            emailCfg     = await _db.LmEmailConfig.AsNoTracking().FirstOrDefaultAsync(ct);
            var sysCfg   = await _db.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct);
            approvalEmail = sysCfg?.CompEmailApproval?.Trim();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not pre-load email config for ApprovePo #{CalcId}", calcId); }

        if (emailCfg is not null && !string.IsNullOrEmpty(approvalEmail))
        {
            var calcIdCopy  = calcId;
            var poNoCopy    = poNo;
            var approvedBy  = currentUser;
            LmEmailConfig cfg = emailCfg;
            var recipients  = approvalEmail.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(r => r.Trim()).Where(r => !string.IsNullOrEmpty(r)).ToList();
            _ = Task.Run(async () =>
            {
                try
                {
                    var subject = $"[Price Confirmation] Calc #{calcIdCopy} / PO {poNoCopy} — Prices Approved";
                    var body    = $"<p>The price changes for <b>PO {poNoCopy}</b> in Cost Calculation <b>#{calcIdCopy}</b> have been <b style='color:green;'>approved</b> by <b>{approvedBy}</b>.</p>";
                    var sslOption = cfg.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                    using var client = new SmtpClient();
                    await client.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort, sslOption);
                    await client.AuthenticateAsync(cfg.SenderEmail, cfg.SenderPassword);
                    foreach (var to in recipients)
                    {
                        var msg = new MimeMessage();
                        msg.From.Add(new MailboxAddress(cfg.SenderName, cfg.SenderEmail));
                        msg.To.Add(MailboxAddress.Parse(to));
                        msg.Subject = subject;
                        msg.Body    = new TextPart("html") { Text = body };
                        await client.SendAsync(msg);
                    }
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to send price approval email for Calc #{CalcId} PO {PoNo}", calcIdCopy, poNoCopy); }
            });
        }

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
public record PriceMarginDto(decimal? Price, decimal? Margin);
public record SaveItemDto(bool? PriceChangeFlag, Dictionary<string, PriceMarginDto>? Prices);
