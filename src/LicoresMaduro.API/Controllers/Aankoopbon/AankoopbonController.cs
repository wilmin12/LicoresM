using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MimeKit.Utils;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/orders")]
[Authorize]
[Produces("application/json")]
public sealed class AankoopbonController : ControllerBase
{
    private readonly ApplicationDbContext          _db;
    private readonly ILogger<AankoopbonController> _log;
    private readonly IWebHostEnvironment           _env;

    public AankoopbonController(ApplicationDbContext db, ILogger<AankoopbonController> log, IWebHostEnvironment env)
    { _db = db; _log = log; _env = env; }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int? CurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private string CurrentUserName()
        => User.FindFirst("fullName")?.Value
        ?? User.FindFirst("username")?.Value
        ?? "Unknown";

    private bool IsAdminOrSuperAdmin()
        => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");

    private async Task<string> NextBonNrAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"{year}/";
        var last = await _db.AbOrderHeaders
            .Where(h => h.AohBonNr.StartsWith(prefix))
            .OrderByDescending(h => h.AohBonNr)
            .Select(h => h.AohBonNr)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && int.TryParse(last.Split('/').Last(), out var n))
            seq = n + 1;

        return $"{year}/{seq:D3}";
    }

    // ── GET /api/aankoopbon/orders ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status   = null,
        [FromQuery] string? search   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 50,
        CancellationToken ct = default)
    {
        var myId = CurrentUserId();
        if (myId is null) return Unauthorized();

        var q = _db.AbOrderHeaders.AsNoTracking().Where(h => h.IsActive);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(h => h.AohStatus == status.ToUpper());

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(h => h.AohBonNr.Contains(search)
                           || (h.AohVendorName  != null && h.AohVendorName.Contains(search))
                           || (h.AohRequestor   != null && h.AohRequestor.Contains(search))
                           || (h.AohDepartment  != null && h.AohDepartment.Contains(search))
                           || (h.AohRemarks     != null && h.AohRemarks.Contains(search)));

        var total = await q.CountAsync(ct);
        var data  = await q
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(h => new
            {
                h.AohId, h.AohBonNr, h.AohStatus, h.AohOrderDate,
                h.AohRequestor, h.AohVendorName, h.AohDepartment,
                h.AohCostType, h.AohRemarks, h.AohAmount,
                h.AohVehicleLicense, h.AohVehicleType, h.AohVehicleModel,
                h.AohCreatedBy, h.AohCreatedByName, h.CreatedAt,
                HasInvoice      = h.AohInvoiceNr != null,
                HasQuotationPdf = h.AohQuotationPdfPath != null,
                h.AohClosedAt, h.AohClosedByName
            })
            .ToListAsync(ct);

        return Ok(PagedResponse<object>.Ok(data, page, pageSize, total));
    }

    // ── GET /api/aankoopbon/orders/{id} ───────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var h = await _db.AbOrderHeaders
            .AsNoTracking()
            .Include(x => x.Details.Where(d => d.IsActive))
            .FirstOrDefaultAsync(x => x.AohId == id && x.IsActive, ct);

        return h is null
            ? NotFound(ApiResponse.Fail($"Aankoopbon {id} not found."))
            : Ok(ApiResponse<AbOrderHeader>.Ok(h));
    }

    // ── GET /api/aankoopbon/orders/vehicle/{license} ──────────────────────────
    [HttpGet("vehicle/{license}")]
    public async Task<IActionResult> VehicleHistory(string license, CancellationToken ct)
    {
        var history = await _db.AbOrderHeaders
            .AsNoTracking()
            .Where(h => h.AohVehicleLicense == license && h.IsActive)
            .OrderByDescending(h => h.AohOrderDate)
            .Select(h => new
            {
                h.AohId, h.AohBonNr, h.AohStatus, h.AohOrderDate,
                h.AohVendorName, h.AohRemarks, h.AohCostType
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(history));
    }

    // ── POST /api/aankoopbon/orders ── Create (DRAFT) ─────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AankoopbonCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        if (dto.Details is null or { Count: 0 })
            return BadRequest(ApiResponse.Fail("At least one detail line is required."));

        var header = new AbOrderHeader
        {
            AohBonNr        = await NextBonNrAsync(ct),
            AohStatus       = "DRAFT",
            AohOrderDate    = DateTime.UtcNow,
            AohRequestor    = dto.AohRequestor,
            AohVendorId     = dto.AohVendorId,
            AohVendorName   = dto.AohVendorName,
            AohVendorAddress= dto.AohVendorAddress,
            AohDepartment   = dto.AohDepartment,
            AohCostType     = dto.AohCostType,
            AohRemarks      = dto.AohRemarks,
            AohVehicleId    = dto.AohVehicleId,
            AohVehicleLicense = dto.AohVehicleLicense,
            AohVehicleType  = dto.AohVehicleType,
            AohVehicleModel = dto.AohVehicleModel,
            AohQuotationNr  = dto.AohQuotationNr,
            AohAmount       = dto.AohAmount,
            AohCreatedBy    = userId.Value,
            AohCreatedByName= CurrentUserName(),
        };

        _db.AbOrderHeaders.Add(header);
        await _db.SaveChangesAsync(ct);

        // Add details
        if (dto.Details is { Count: > 0 })
        {
            var lineNr = 1;
            foreach (var d in dto.Details)
            {
                _db.AbOrderDetails.Add(new AbOrderDetail
                {
                    AodHeaderId    = header.AohId,
                    AodLineNr      = lineNr++,
                    AodProductCode = d.AodProductCode,
                    AodProductDesc = d.AodProductDesc,
                    AodQuantity    = d.AodQuantity,
                    AodUnit        = d.AodUnit
                });
            }
            await _db.SaveChangesAsync(ct);
        }

        _log.LogInformation("Aankoopbon {BonNr} created by user {UserId}", header.AohBonNr, userId);
        return CreatedAtAction(nameof(GetById), new { id = header.AohId },
            ApiResponse<object>.Ok(new { header.AohId, header.AohBonNr }, "Aankoopbon created."));
    }

    // ── PUT /api/aankoopbon/orders/{id} ── Update (only DRAFT) ───────────────
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AankoopbonCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values
                .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var header = await _db.AbOrderHeaders
            .Include(h => h.Details)
            .FirstOrDefaultAsync(h => h.AohId == id && h.IsActive, ct);

        if (header is null) return NotFound(ApiResponse.Fail($"Aankoopbon {id} not found."));
        if (!IsAdminOrSuperAdmin() && header.AohCreatedBy != CurrentUserId())
            return Forbid();
        if (header.AohStatus != "DRAFT")
            return BadRequest(ApiResponse.Fail("Only DRAFT aankoopbonnen can be edited."));
        if (dto.Details is null or { Count: 0 })
            return BadRequest(ApiResponse.Fail("At least one detail line is required."));

        header.AohRequestor    = dto.AohRequestor;
        header.AohVendorId     = dto.AohVendorId;
        header.AohVendorName   = dto.AohVendorName;
        header.AohVendorAddress= dto.AohVendorAddress;
        header.AohDepartment   = dto.AohDepartment;
        header.AohCostType     = dto.AohCostType;
        header.AohRemarks      = dto.AohRemarks;
        header.AohVehicleId    = dto.AohVehicleId;
        header.AohVehicleLicense = dto.AohVehicleLicense;
        header.AohVehicleType  = dto.AohVehicleType;
        header.AohVehicleModel = dto.AohVehicleModel;
        header.AohQuotationNr  = dto.AohQuotationNr;
        header.AohAmount       = dto.AohAmount;

        // Replace details
        _db.AbOrderDetails.RemoveRange(header.Details);
        var lineNr = 1;
        foreach (var d in dto.Details ?? [])
        {
            _db.AbOrderDetails.Add(new AbOrderDetail
            {
                AodHeaderId    = header.AohId,
                AodLineNr      = lineNr++,
                AodProductCode = d.AodProductCode,
                AodProductDesc = d.AodProductDesc,
                AodQuantity    = d.AodQuantity,
                AodUnit        = d.AodUnit
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Aankoopbon updated."));
    }

    // ── PATCH /api/aankoopbon/orders/{id}/close ── → PENDING + email ─────────
    [HttpPatch("{id:int}/close")]
    public async Task<IActionResult> Close(int id, [FromBody] CloseDto dto, CancellationToken ct)
    {
        var header = await _db.AbOrderHeaders
            .FirstOrDefaultAsync(h => h.AohId == id && h.IsActive, ct);

        if (header is null) return NotFound(ApiResponse.Fail($"Aankoopbon {id} not found."));
        if (!IsAdminOrSuperAdmin() && header.AohCreatedBy != CurrentUserId())
            return Forbid();
        if (header.AohStatus != "DRAFT")
            return BadRequest(ApiResponse.Fail("Only DRAFT aankoopbonnen can be closed."));
        if (string.IsNullOrEmpty(header.AohQuotationPdfPath) || !System.IO.File.Exists(header.AohQuotationPdfPath))
            return BadRequest(ApiResponse.Fail("A quotation PDF must be attached before submitting for approval."));

        header.AohStatus      = "PENDING";
        header.AohMeegeven    = dto.Meegeven;
        header.AohOntvangen   = dto.Ontvangen;
        header.AohZenden      = dto.Zenden;
        header.AohAndere      = dto.Andere;
        header.AohReceiverId  = dto.ReceiverId;
        header.AohReceiverName   = dto.ReceiverName;
        header.AohReceiverIdDoc  = dto.ReceiverIdDoc;

        await _db.SaveChangesAsync(ct);

        // Send email to manager
        await _sendEmailAsync(header, "pending", ct);

        // In-app notifications for all Admin / SuperAdmin users
        await _notifyApproversAsync(header, ct);

        _log.LogInformation("Aankoopbon {BonNr} closed → PENDING", header.AohBonNr);
        return Ok(ApiResponse.Ok("Aankoopbon sent for approval."));
    }

    // ── PATCH /api/aankoopbon/orders/{id}/approve ──────────────────────────────
    [HttpPatch("{id:int}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var header = await _db.AbOrderHeaders
            .Include(h => h.Details)
            .FirstOrDefaultAsync(h => h.AohId == id && h.IsActive, ct);

        if (header is null) return NotFound(ApiResponse.Fail($"Aankoopbon {id} not found."));
        if (header.AohStatus != "PENDING")
            return BadRequest(ApiResponse.Fail("Only PENDING aankoopbonnen can be approved."));

        header.AohStatus        = "APPROVED";
        header.AohApprovedBy    = CurrentUserId();
        header.AohApprovedByName= CurrentUserName();
        header.AohApprovedAt    = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _sendEmailAsync(header, "approved", ct);

        _log.LogInformation("Aankoopbon {BonNr} approved by {User}", header.AohBonNr, CurrentUserName());
        return Ok(ApiResponse<object>.Ok(new { header.AohId, header.AohBonNr, header.AohStatus }, "Aankoopbon approved."));
    }

    // ── PATCH /api/aankoopbon/orders/{id}/reject ───────────────────────────────
    [HttpPatch("{id:int}/reject")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(ApiResponse.Fail("Rejection reason is required."));

        var header = await _db.AbOrderHeaders
            .FirstOrDefaultAsync(h => h.AohId == id && h.IsActive, ct);

        if (header is null) return NotFound(ApiResponse.Fail($"Aankoopbon {id} not found."));
        if (header.AohStatus != "PENDING")
            return BadRequest(ApiResponse.Fail("Only PENDING aankoopbonnen can be rejected."));

        header.AohStatus          = "REJECTED";
        header.AohRejectedBy      = CurrentUserId();
        header.AohRejectedByName  = CurrentUserName();
        header.AohRejectedAt      = DateTime.UtcNow;
        header.AohRejectionReason = dto.Reason.Trim();

        await _db.SaveChangesAsync(ct);

        await _sendEmailAsync(header, "rejected", ct, rejectionReason: dto.Reason.Trim());

        _log.LogInformation("Aankoopbon {BonNr} rejected by {User}: {Reason}",
            header.AohBonNr, CurrentUserName(), dto.Reason);
        return Ok(ApiResponse.Ok("Aankoopbon rejected."));
    }

    // ── PATCH /api/aankoopbon/orders/{id}/resubmit ── REJECTED → DRAFT ──────────
    [HttpPatch("{id:int}/resubmit")]
    public async Task<IActionResult> Resubmit(int id, CancellationToken ct)
    {
        var header = await _db.AbOrderHeaders
            .FirstOrDefaultAsync(h => h.AohId == id && h.IsActive, ct);

        if (header is null) return NotFound(ApiResponse.Fail($"Aankoopbon {id} not found."));
        if (!IsAdminOrSuperAdmin() && header.AohCreatedBy != CurrentUserId())
            return Forbid();
        if (header.AohStatus != "REJECTED")
            return BadRequest(ApiResponse.Fail("Only REJECTED aankoopbonnen can be resubmitted."));

        header.AohStatus          = "DRAFT";
        header.AohRejectedBy      = null;
        header.AohRejectedByName  = null;
        header.AohRejectedAt      = null;
        header.AohRejectionReason = null;

        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Aankoopbon {BonNr} resubmitted to DRAFT by {User}", header.AohBonNr, CurrentUserName());
        return Ok(ApiResponse.Ok("Aankoopbon returned to DRAFT for editing."));
    }

    // ── DELETE /api/aankoopbon/orders/{id} ── soft delete (only DRAFT) ────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var header = await _db.AbOrderHeaders.FirstOrDefaultAsync(h => h.AohId == id && h.IsActive, ct);
        if (header is null) return NotFound(ApiResponse.Fail($"Aankoopbon {id} not found."));
        if (!IsAdminOrSuperAdmin() && header.AohCreatedBy != CurrentUserId())
            return Forbid();
        if (header.AohStatus != "DRAFT")
            return BadRequest(ApiResponse.Fail("Only DRAFT aankoopbonnen can be deleted."));

        header.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Aankoopbon deleted."));
    }

    // ── POST /api/aankoopbon/orders/{id}/upload-pdf ───────────────────────────
    [HttpPost("{id:int}/upload-pdf")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPdf(int id, IFormFile file, CancellationToken ct)
    {
        var header = await _db.AbOrderHeaders.FindAsync([id], ct);
        if (header is null) return NotFound(ApiResponse.Fail($"Aankoopbon {id} not found."));
        if (!IsAdminOrSuperAdmin() && header.AohCreatedBy != CurrentUserId())
            return Forbid();
        if (header.AohStatus != "DRAFT")
            return BadRequest(ApiResponse.Fail("PDF can only be uploaded on DRAFT aankoopbonnen."));
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("No file provided."));
        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Fail("Only PDF files are allowed."));

        var uploadDir = Path.Combine(_env.ContentRootPath, "uploads", "aankoopbon");
        Directory.CreateDirectory(uploadDir);

        // Delete previous file if it exists
        if (!string.IsNullOrEmpty(header.AohQuotationPdfPath) && System.IO.File.Exists(header.AohQuotationPdfPath))
            System.IO.File.Delete(header.AohQuotationPdfPath);

        var safeBonNr = header.AohBonNr.Replace('/', '-');
        var fileName  = $"{safeBonNr}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var filePath  = Path.Combine(uploadDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream, ct);

        header.AohQuotationPdfPath = filePath;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Aankoopbon {BonNr} — quotation PDF uploaded: {File}", header.AohBonNr, fileName);
        return Ok(ApiResponse.Ok("Quotation PDF uploaded successfully."));
    }

    // ── GET /api/aankoopbon/orders/{id}/quotation-pdf ─────────────────────────
    [HttpGet("{id:int}/quotation-pdf")]
    public async Task<IActionResult> GetQuotationPdf(int id, CancellationToken ct)
    {
        var h = await _db.AbOrderHeaders.AsNoTracking()
            .Where(x => x.AohId == id && x.IsActive)
            .Select(x => new { x.AohQuotationPdfPath, x.AohCreatedBy })
            .FirstOrDefaultAsync(ct);

        if (h is null) return NotFound(ApiResponse.Fail("Aankoopbon not found."));
        if (!IsAdminOrSuperAdmin() && h.AohCreatedBy != CurrentUserId())
            return Forbid();
        if (string.IsNullOrEmpty(h.AohQuotationPdfPath) || !System.IO.File.Exists(h.AohQuotationPdfPath))
            return NotFound(ApiResponse.Fail("No quotation PDF found for this aankoopbon."));

        return PhysicalFile(h.AohQuotationPdfPath, "application/pdf");
    }

    // ── PATCH /api/aankoopbon/orders/{id}/invoice ── Save invoice + close ───────
    [HttpPatch("{id:int}/invoice")]
    public async Task<IActionResult> UpdateInvoice(int id, [FromBody] InvoiceDto dto, CancellationToken ct)
    {
        var header = await _db.AbOrderHeaders.FindAsync([id], ct);
        if (header is null) return NotFound(ApiResponse.Fail($"Aankoopbon {id} not found."));
        if (!IsAdminOrSuperAdmin() && header.AohCreatedBy != CurrentUserId())
            return Forbid();
        if (header.AohStatus == "CLOSED")
            return BadRequest(ApiResponse.Fail("This aankoopbon is already closed and cannot be modified."));
        if (header.AohStatus != "APPROVED")
            return BadRequest(ApiResponse.Fail("Invoice data can only be entered on APPROVED aankoopbonnen."));

        if (string.IsNullOrWhiteSpace(dto.InvoiceNr))
            return BadRequest(ApiResponse.Fail("Invoice number is required to close the aankoopbon."));
        if (dto.InvoiceDate is null)
            return BadRequest(ApiResponse.Fail("Invoice date is required to close the aankoopbon."));
        if (dto.InvoiceAmount is null or <= 0)
            return BadRequest(ApiResponse.Fail("Invoice amount is required to close the aankoopbon."));

        header.AohInvoiceNr     = dto.InvoiceNr.Trim();
        header.AohInvoiceDate   = dto.InvoiceDate;
        header.AohInvoiceAmount = dto.InvoiceAmount;

        // Transition to CLOSED — no further modifications allowed
        header.AohStatus        = "CLOSED";
        header.AohClosedBy      = CurrentUserId();
        header.AohClosedByName  = CurrentUserName();
        header.AohClosedAt      = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Aankoopbon {BonNr} closed (invoice entered) by {User}", header.AohBonNr, CurrentUserName());
        return Ok(ApiResponse.Ok("Invoice data saved. Aankoopbon is now closed."));
    }

    // ── In-app notifications ──────────────────────────────────────────────────

    private async Task _notifyApproversAsync(AbOrderHeader header, CancellationToken ct)
    {
        try
        {
            // Find all active Admin and SuperAdmin users (role name is in LM_Roles)
            var approverRoles = new[] { "Admin", "SuperAdmin" };
            var approvers = await _db.LmUsers.AsNoTracking()
                .Where(u => u.IsActive && u.Role != null && approverRoles.Contains(u.Role.RoleName))
                .Select(u => u.UserId)
                .ToListAsync(ct);

            if (approvers.Count == 0) return;

            var url = $"/pages/aankoopbon/orders.html";
            var notifications = approvers.Select(uid => new LmNotification
            {
                NtfUserId  = uid,
                NtfTitle   = "Nueva solicitud de compra",
                NtfMessage = $"Bon {header.AohBonNr} — {header.AohRequestor} solicitó a {header.AohVendorName}. Monto: {header.AohAmount:N2} NAF.",
                NtfType    = "WARNING",
                NtfUrl     = url,
                NtfRefId   = header.AohId,
                NtfRefType = "AANKOOPBON",
            }).ToList();

            _db.LmNotifications.AddRange(notifications);
            await _db.SaveChangesAsync(ct);

            _log.LogInformation("Aankoopbon {BonNr} — {Count} in-app notification(s) created.", header.AohBonNr, notifications.Count);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to create in-app notifications for aankoopbon {BonNr}", header.AohBonNr);
        }
    }

    // ── Email helpers ─────────────────────────────────────────────────────────

    private async Task<string?> GetCreatorEmailAsync(AbOrderHeader header, CancellationToken ct)
    {
        var email = await _db.LmUsers.AsNoTracking()
            .Where(u => u.UserId == header.AohCreatedBy)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);
        return string.IsNullOrWhiteSpace(email) ? null : email;
    }

    private async Task<string?> GetRequestorEmailAsync(string? requestorName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestorName)) return null;
        var name  = requestorName.Trim();
        var email = await _db.Requestors.AsNoTracking()
            .Where(r => r.ReqName.Trim() == name && r.IsActive)
            .Select(r => r.ReqEmail)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(email))
            _log.LogWarning("Aankoopbon email: requestor '{Name}' has no email in REQUESTORS catalog.", name);
        return string.IsNullOrWhiteSpace(email) ? null : email;
    }

    /// <summary>
    /// type = "pending"  → manager receives approval request + quotation PDF
    /// type = "approved" → creator AND requestor receive confirmation + aankoopbon PDF
    /// type = "rejected" → creator receives rejection reason
    /// </summary>
    private async Task _sendEmailAsync(AbOrderHeader header, string type,
        CancellationToken ct, string? rejectionReason = null)
    {
        try
        {
            var cfg = await _db.LmEmailConfig.AsNoTracking().FirstOrDefaultAsync(ct);
            if (cfg is null || !cfg.IsEnabled) return;

            var orderTable = $@"
<table style='border-collapse:collapse;font-family:sans-serif;font-size:14px;'>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Bon Nr:</td><td>{header.AohBonNr}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Date:</td><td>{header.AohOrderDate:yyyy-MM-dd}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Requestor:</td><td>{header.AohRequestor}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Vendor:</td><td>{header.AohVendorName}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Department:</td><td>{header.AohDepartment}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Remarks:</td><td>{header.AohRemarks}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Amount:</td><td>{header.AohAmount:N2} NAF</td></tr>
</table>";

            // ── Build recipient list and message content ────────────────────
            var recipients  = new List<string>();
            string subject;
            string body;
            byte[]? pdfAttachment = null;
            string? quotationPath = null;

            if (type == "pending")
            {
                var approverCfg = await _db.ModuleApproverEmails.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MaeModuleKey == "AANKOOPBON", ct);
                var managers = approverCfg?.MaeEmails
                    ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList() ?? [];
                recipients.AddRange(managers);

                subject       = $"[Aankoopbon] {header.AohBonNr} — Pending Approval";
                body          = $"<p>A new aankoopbon requires your approval.</p>{orderTable}<p>Please log in to approve or reject.</p>";
                quotationPath = header.AohQuotationPdfPath;
            }
            else if (type == "approved")
            {
                // Both the creator (logged-in user) and the requestor (catalog) receive the approval
                var creatorEmail   = await GetCreatorEmailAsync(header, ct);
                var requestorEmail = await GetRequestorEmailAsync(header.AohRequestor, ct);

                _log.LogInformation("Aankoopbon {BonNr} approval emails — creator: {C}, requestor: {R}",
                    header.AohBonNr, creatorEmail ?? "none", requestorEmail ?? "none");

                if (!string.IsNullOrEmpty(creatorEmail))
                    recipients.Add(creatorEmail);

                // Add requestor only if different from creator (avoid duplicate)
                if (!string.IsNullOrEmpty(requestorEmail) &&
                    !string.Equals(requestorEmail, creatorEmail, StringComparison.OrdinalIgnoreCase))
                    recipients.Add(requestorEmail);

                subject = $"[Aankoopbon] {header.AohBonNr} — Approved ✔";
                body    = $@"<p>The aankoopbon below has been <b style='color:green;'>approved</b> by {header.AohApprovedByName}.</p>
{orderTable}
<p>The purchase order document is attached to this email.</p>";

                // Generate aankoopbon PDF
                try
                {
                    if (header.Details is null or { Count: 0 })
                        await _db.Entry(header).Collection(h => h.Details).LoadAsync(ct);

                    var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "LogoLicores.png");
                    pdfAttachment = AankoopbonPdfBuilder.Generate(header, logoPath);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Could not generate aankoopbon PDF for {BonNr}", header.AohBonNr);
                }
            }
            else // rejected
            {
                var creatorEmail = await GetCreatorEmailAsync(header, ct);
                if (!string.IsNullOrEmpty(creatorEmail)) recipients.Add(creatorEmail);

                subject = $"[Aankoopbon] {header.AohBonNr} — Rejected ✖";
                body    = $@"<p>Your aankoopbon has been <b style='color:red;'>rejected</b> by {header.AohRejectedByName}.</p>
{orderTable}
<p><b>Reason:</b> {rejectionReason}</p>
<p>Please contact your manager for more information.</p>";
            }

            if (recipients.Count == 0) return;

            // ── Build MimeMessage ──────────────────────────────────────────
            var bb = new BodyBuilder { HtmlBody = body };

            if (!string.IsNullOrEmpty(quotationPath) && System.IO.File.Exists(quotationPath))
                await bb.Attachments.AddAsync(quotationPath, ct);

            if (pdfAttachment is { Length: > 0 })
            {
                var safeName = header.AohBonNr.Replace('/', '-');
                bb.Attachments.Add($"Aankoopbon_{safeName}.pdf", pdfAttachment,
                    new MimeKit.ContentType("application", "pdf"));
            }

            var messageBody = bb.ToMessageBody();

            // ── Open one SMTP connection and send to all recipients ─────────
            using var client = new SmtpClient();
            var sslOption = cfg.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
            await client.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort, sslOption, ct);
            await client.AuthenticateAsync(cfg.SenderEmail, cfg.SenderPassword, ct);

            foreach (var to in recipients)
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(cfg.SenderName, cfg.SenderEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Body    = messageBody;
                await client.SendAsync(message, ct);
                _log.LogInformation("Aankoopbon {BonNr} — email '{Type}' sent to {To}", header.AohBonNr, type, to);
            }

            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to send '{Type}' email for aankoopbon {BonNr}", type, header.AohBonNr);
        }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed class AankoopbonDetailDto
{
    public string?  AodProductCode { get; set; }
    public string   AodProductDesc { get; set; } = string.Empty;
    public decimal  AodQuantity    { get; set; } = 1;
    public string?  AodUnit        { get; set; }
}

public sealed class AankoopbonCreateDto
{
    public string?  AohRequestor     { get; set; }
    public int?     AohVendorId      { get; set; }
    public string?  AohVendorName    { get; set; }
    public string?  AohVendorAddress { get; set; }
    public string?  AohDepartment    { get; set; }
    public string?  AohCostType      { get; set; }
    public string?  AohRemarks       { get; set; }
    public int?     AohVehicleId     { get; set; }
    public string?  AohVehicleLicense{ get; set; }
    public string?  AohVehicleType   { get; set; }
    public string?  AohVehicleModel  { get; set; }
    public string?  AohQuotationNr   { get; set; }
    public decimal? AohAmount        { get; set; }
    public List<AankoopbonDetailDto>? Details { get; set; }
}


public sealed class CloseDto
{
    public bool    Meegeven    { get; set; }
    public bool    Ontvangen   { get; set; }
    public bool    Zenden      { get; set; }
    public bool    Andere      { get; set; }
    public int?    ReceiverId  { get; set; }
    public string? ReceiverName   { get; set; }
    public string? ReceiverIdDoc  { get; set; }
}

public sealed record RejectDto(string Reason);

public sealed class InvoiceDto
{
    public string?   InvoiceNr     { get; set; }
    public DateTime? InvoiceDate   { get; set; }
    public decimal?  InvoiceAmount { get; set; }
}
