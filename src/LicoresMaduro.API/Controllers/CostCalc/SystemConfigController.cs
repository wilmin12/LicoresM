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
    private readonly ApplicationDbContext _db;
    public SystemConfigController(ApplicationDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var cfg = await _db.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct);
        return Ok(ApiResponse<SystemTable>.Ok(cfg ?? new SystemTable { CompCode = "LM" }));
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] SystemTable dto, CancellationToken ct)
    {
        var existing = await _db.SystemTable.FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            dto.CompCode = "LM";
            _db.SystemTable.Add(dto);
        }
        else
        {
            existing.CompName                    = dto.CompName;
            existing.CompAddress                 = dto.CompAddress;
            existing.CompContact                 = dto.CompContact;
            existing.CompTel                     = dto.CompTel;
            existing.CompFax                     = dto.CompFax;
            existing.CompEmail                   = dto.CompEmail;
            existing.CompCurrLocal               = dto.CompCurrLocal;
            existing.CompCurrUsd                 = dto.CompCurrUsd;
            existing.CompRateUsd                 = dto.CompRateUsd;
            existing.CompDocNumber               = dto.CompDocNumber;
            existing.CompDocEmail                = dto.CompDocEmail;
            existing.CompInsurance               = dto.CompInsurance;
            existing.CompTransport               = dto.CompTransport;
            existing.CompUnloading               = dto.CompUnloading;
            existing.CompLocalHandling           = dto.CompLocalHandling;
            existing.CompFwCode                  = dto.CompFwCode;
            existing.CompFwName                  = dto.CompFwName;
            existing.CompFwCurr                  = dto.CompFwCurr;
            existing.CompSm01Code                = dto.CompSm01Code;
            existing.CompSm01Email               = dto.CompSm01Email;
            existing.CompSm02Code                = dto.CompSm02Code;
            existing.CompSm02Email               = dto.CompSm02Email;
            existing.CompSm03Code                = dto.CompSm03Code;
            existing.CompSm03Email               = dto.CompSm03Email;
            existing.CompPcEmail                 = dto.CompPcEmail;
            existing.CompOzFactor                = dto.CompOzFactor;
            existing.CompLiterMultiplier         = dto.CompLiterMultiplier;
            existing.CompPriceChangePerc         = dto.CompPriceChangePerc;
            existing.CompPriceChangeAmnt         = dto.CompPriceChangeAmnt;
            existing.CompStoreRetailPerc         = dto.CompStoreRetailPerc;
            existing.CompStoreAlliancePerc       = dto.CompStoreAlliancePerc;
            existing.CompStoreNorsaPerc          = dto.CompStoreNorsaPerc;
            existing.CompPriceChangeYear         = dto.CompPriceChangeYear;
            existing.CompPriceChangeSeq          = dto.CompPriceChangeSeq;
            existing.CompEmailPurchDep           = dto.CompEmailPurchDep;
            existing.CompEmailApproval           = dto.CompEmailApproval;
            existing.CompPathCostChanges         = dto.CompPathCostChanges;
            existing.CompPathPriceChanges        = dto.CompPathPriceChanges;
            existing.CompPathCostCalc            = dto.CompPathCostCalc;
            existing.CompEmailCostChange         = dto.CompEmailCostChange;
            existing.CompEmailConfirmMngr        = dto.CompEmailConfirmMngr;
            existing.CompEmailPriceChangesFinance = dto.CompEmailPriceChangesFinance;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<SystemTable>.Ok(existing ?? dto));
    }
}
