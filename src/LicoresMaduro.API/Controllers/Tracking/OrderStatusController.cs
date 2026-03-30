using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Tracking;

[ApiController]
[Route("api/tracking/order-status")]
[Authorize]
[Produces("application/json")]
public sealed class OrderStatusController : ControllerBase
{
    private readonly ApplicationDbContext          _db;
    private readonly ILogger<OrderStatusController> _logger;

    public OrderStatusController(ApplicationDbContext db, ILogger<OrderStatusController> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // GET api/tracking/order-status
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search          = null,
        [FromQuery] bool    includeInactive = false,
        [FromQuery] int     page            = 1,
        [FromQuery] int     pageSize        = 50,
        CancellationToken   ct              = default)
    {
        var query = _db.OrderStatuses.AsNoTracking();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.OsDescription.Contains(search) || x.OsCode.Contains(search));

        var total   = await query.CountAsync(ct);
        var records = await query.OrderBy(x => x.OsDescription)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(PagedResponse<OrderStatus>.Ok(records, page, pageSize, total));
    }

    // GET api/tracking/order-status/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var entity = await _db.OrderStatuses.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Order status {id} not found."));
        return Ok(ApiResponse<OrderStatus>.Ok(entity));
    }

    // POST api/tracking/order-status
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] OrderStatusDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(ApiResponse.Fail("Code is required."));
        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(ApiResponse.Fail("Description is required."));

        var entity = new OrderStatus
        {
            OsCode        = dto.Code.Trim().ToUpperInvariant(),
            OsDescription = dto.Description.Trim(),
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };
        _db.OrderStatuses.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Conflict(ApiResponse.Fail($"An order status with code '{entity.OsCode}' or description '{entity.OsDescription}' already exists."));
        }

        _logger.LogInformation("OrderStatus '{Code}' created", entity.OsCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.OsId }, ApiResponse<OrderStatus>.Ok(entity, "Order status created."));
    }

    // PUT api/tracking/order-status/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] OrderStatusDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(ApiResponse.Fail("Description is required."));

        var entity = await _db.OrderStatuses.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Order status {id} not found."));

        if (!string.IsNullOrWhiteSpace(dto.Code))
            entity.OsCode = dto.Code.Trim().ToUpperInvariant();
        entity.OsDescription = dto.Description.Trim();
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Conflict(ApiResponse.Fail($"An order status with code '{entity.OsCode}' or description '{entity.OsDescription}' already exists."));
        }

        return Ok(ApiResponse<OrderStatus>.Ok(entity, "Order status updated."));
    }

    // PATCH api/tracking/order-status/{id}/toggle
    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.OrderStatuses.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Order status {id} not found."));

        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Order status {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    // DELETE api/tracking/order-status/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.OrderStatuses.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Order status {id} not found."));

        _db.OrderStatuses.Remove(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("OrderStatus {Id} permanently deleted", id);
        return Ok(ApiResponse.Ok("Order status deleted successfully."));
    }
}

public sealed class OrderStatusDto
{
    public string? Code        { get; set; }
    public string? Description { get; set; }
}
