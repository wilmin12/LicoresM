using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/products")]
[Authorize]
[Produces("application/json")]
public sealed class AbProductsController : ControllerBase
{
    private readonly ApplicationDbContext          _db;
    private readonly DhwDbContext                  _dhw;
    private readonly ILogger<AbProductsController> _logger;

    public AbProductsController(ApplicationDbContext db, DhwDbContext dhw, ILogger<AbProductsController> logger)
    { _db = db; _dhw = dhw; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.AbProducts.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.ItemKode.Contains(search) || x.Omschrijving.Contains(search) ||
                              (x.VendorCode != null && x.VendorCode.Contains(search)));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.ItemKode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<AbProduct>.Ok(data, page, pageSize, total));
    }

    [HttpGet("item-lookup")]
    public async Task<IActionResult> ItemLookup([FromQuery] string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(ApiResponse.Fail("Item code is required."));

        var cleanCode = code.Trim().ToUpper();
        var description = await _dhw.Database
            .SqlQuery<string>($"SELECT dbo.Description_Items_BEER({cleanCode}) AS Value")
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(description))
            return NotFound(ApiResponse.Fail($"Item '{cleanCode}' not found in DHW database."));

        return Ok(ApiResponse<object>.Ok(new { ItemCode = cleanCode, Description = description.Trim() }));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.AbProducts.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"AB product {id} not found.")) : Ok(ApiResponse<AbProduct>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,PurchaseManager")]
    public async Task<IActionResult> Create([FromBody] AbProductDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        if (await _db.AbProducts.AnyAsync(x => x.ItemKode == dto.ItemKode, ct))
            return Conflict(ApiResponse.Fail($"A product with code '{dto.ItemKode}' already exists."));

        var entity = new AbProduct
        {
            ItemKode     = dto.ItemKode,
            Omschrijving = dto.Omschrijving,
            VendorCode   = dto.VendorCode,
            CostType     = dto.CostType,
            Eenheid      = dto.Eenheid,
            UnitQuantity = dto.UnitQuantity,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
        _db.AbProducts.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating AB product '{ItemKode}'", dto.ItemKode);
            return Conflict(ApiResponse.Fail("Could not save the product. It may already exist or a required field is invalid."));
        }
        _logger.LogInformation("AbProduct '{ItemKode}' created", dto.ItemKode);
        return CreatedAtAction(nameof(GetById), new { id = entity.AbpId }, ApiResponse<AbProduct>.Ok(entity, "AB product created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,PurchaseManager")]
    public async Task<IActionResult> Update(int id, [FromBody] AbProductDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.AbProducts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"AB product {id} not found."));

        if (await _db.AbProducts.AnyAsync(x => x.ItemKode == dto.ItemKode && x.AbpId != id, ct))
            return Conflict(ApiResponse.Fail($"A product with code '{dto.ItemKode}' already exists."));

        entity.ItemKode = dto.ItemKode; entity.Omschrijving = dto.Omschrijving;
        entity.VendorCode = dto.VendorCode; entity.CostType = dto.CostType;
        entity.Eenheid = dto.Eenheid; entity.UnitQuantity = dto.UnitQuantity;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating AB product {Id}", id);
            return Conflict(ApiResponse.Fail("Could not save the product. It may already exist or a required field is invalid."));
        }
        return Ok(ApiResponse<AbProduct>.Ok(entity, "AB product updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.AbProducts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"AB product {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"AB product {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.AbProducts.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"AB product {id} not found."));

        // Block delete if product code has ever been used in any aankoopbon line
        var inUse = await _db.AbOrderDetails
            .AnyAsync(d => d.AodProductCode == entity.ItemKode, ct);
        if (inUse)
            return Conflict(ApiResponse.Fail(
                $"Cannot delete '{entity.ItemKode}': it is linked to one or more aankoopbonnen. Deactivate it instead."));

        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("AbProduct {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("AB product deleted."));
    }
}

public sealed record AbProductDto(
    string  ItemKode,
    string  Omschrijving,
    string? VendorCode,
    string? CostType,
    string? Eenheid,
    double? UnitQuantity
);
