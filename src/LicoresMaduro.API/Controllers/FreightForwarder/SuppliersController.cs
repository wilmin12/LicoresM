using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/suppliers")]
[Authorize]
[Produces("application/json")]
public sealed class SuppliersController : ControllerBase
{
    private readonly DhwDbContext _dhw;

    public SuppliersController(DhwDbContext dhw) { _dhw = dhw; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search       = null,
        [FromQuery] bool    includeDeleted = false,
        [FromQuery] int     page         = 1,
        [FromQuery] int     pageSize     = 100,
        CancellationToken   ct           = default)
    {
        var q = _dhw.Suppliers.AsNoTracking();

        if (!includeDeleted)
            q = q.Where(x => x.DeleteFlag != "Y");

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x =>
                x.Supplier!.Contains(search) ||
                x.SupplierName!.Contains(search));

        var total = await q.CountAsync(ct);
        var data  = await q
            .OrderBy(x => x.SupplierName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Identity,
                Code         = x.Supplier!.Trim(),
                Name         = x.SupplierName!.Trim(),
                x.ApVendor,
                IsDeleted    = x.DeleteFlag == "Y"
            })
            .ToListAsync(ct);

        return Ok(PagedResponse<object>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var e = await _dhw.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Identity == id, ct);

        if (e is null) return NotFound(ApiResponse.Fail($"Supplier {id} not found."));

        return Ok(ApiResponse<object>.Ok(new
        {
            e.Identity,
            Code      = e.Supplier?.Trim(),
            Name      = e.SupplierName?.Trim(),
            e.ApVendor,
            IsDeleted = e.DeleteFlag == "Y"
        }));
    }
}
