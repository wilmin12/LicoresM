using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.SysConfig;

[ApiController]
[Route("api/system/module-approvers")]
[Authorize(Roles = "SuperAdmin,Admin")]
[Produces("application/json")]
public sealed class ModuleApproversController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ModuleApproversController(ApplicationDbContext db) => _db = db;

    /// <summary>Returns all module approver configurations.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var rows = await _db.ModuleApproverEmails
            .AsNoTracking()
            .OrderBy(m => m.MaeId)
            .ToListAsync(ct);

        return Ok(ApiResponse<List<ModuleApproverEmail>>.Ok(rows));
    }

    /// <summary>Updates the approver emails for a given module key.</summary>
    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, [FromBody] ModuleApproverDto dto, CancellationToken ct)
    {
        var row = await _db.ModuleApproverEmails
            .FirstOrDefaultAsync(m => m.MaeModuleKey == key.ToUpper(), ct);

        if (row is null)
            return NotFound(ApiResponse.Fail($"Module key '{key}' not found."));

        row.MaeEmails    = (dto.Emails ?? string.Empty).Trim();
        row.MaeUpdatedAt = DateTime.UtcNow;
        row.MaeUpdatedBy = User.Identity?.Name;

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ModuleApproverEmail>.Ok(row, "Saved successfully."));
    }
}

public record ModuleApproverDto(string? Emails);
