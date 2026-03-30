using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/countries")]
[Authorize]
[Produces("application/json")]
public sealed class CountriesController : ControllerBase
{
    private readonly DhwDbContext _dhw;

    public CountriesController(DhwDbContext dhw) { _dhw = dhw; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 100,
        CancellationToken   ct       = default)
    {
        var q = _dhw.Countries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x =>
                x.Description!.Contains(search) ||
                x.IsoAlpha2!.Contains(search)   ||
                x.IsoAlpha3!.Contains(search));

        var total = await q.CountAsync(ct);
        var data  = await q
            .OrderBy(x => x.Description)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Identity,
                IsoAlpha2   = x.IsoAlpha2!.Trim(),
                IsoAlpha3   = x.IsoAlpha3!.Trim(),
                x.IsoNumeric,
                Description = x.Description!.Trim()
            })
            .ToListAsync(ct);

        return Ok(PagedResponse<object>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var e = await _dhw.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Identity == id, ct);

        if (e is null) return NotFound(ApiResponse.Fail($"Country {id} not found."));

        return Ok(ApiResponse<object>.Ok(new
        {
            e.Identity,
            IsoAlpha2   = e.IsoAlpha2?.Trim(),
            IsoAlpha3   = e.IsoAlpha3?.Trim(),
            e.IsoNumeric,
            Description = e.Description?.Trim()
        }));
    }
}
