using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/system-config")]
[Authorize]
[Produces("application/json")]
public sealed class SystemConfigController : ControllerBase
{
    private readonly DhwDbContext _dhw;
    public SystemConfigController(DhwDbContext dhw) { _dhw = dhw; }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var cfg = await _dhw.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct);
        if (cfg is null) return NotFound(ApiResponse.Fail("System configuration not found."));
        return Ok(ApiResponse<DhwSystemTable>.Ok(cfg));
    }
}
