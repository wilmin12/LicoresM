using System.Text.Json;
using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers;

// ── Base entity marker ─────────────────────────────────────────────────────────

/// <summary>
/// Marker interface so the generic controller can call IsActive + CreatedAt
/// without reflection on every call.
/// </summary>
public interface ICatalogEntity
{
    bool     IsActive  { get; set; }
    DateTime CreatedAt { get; set; }
}

// ── Generic controller ─────────────────────────────────────────────────────────

/// <summary>
/// Reusable CRUD base controller for simple catalog tables.
/// Concrete controllers inherit and specify TEntity + TKey.
/// Provides: GetAll, GetById, Create, Update, Delete (soft) with audit logging.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class GenericCatalogController<TEntity, TKey> : ControllerBase
    where TEntity  : class, ICatalogEntity
    where TKey     : struct
{
    protected readonly ApplicationDbContext   Db;
    protected readonly IHttpContextAccessor   HttpCtx;
    protected readonly ILogger                Logger;

    protected abstract DbSet<TEntity>  EntitySet { get; }
    protected abstract string          TableName { get; }
    protected abstract TKey            GetKey(TEntity entity);
    protected abstract void            ApplyUpdate(TEntity existing, TEntity incoming);

    protected GenericCatalogController(
        ApplicationDbContext db,
        IHttpContextAccessor httpCtx,
        ILogger              logger)
    {
        Db      = db;
        HttpCtx = httpCtx;
        Logger  = logger;
    }

    // ── GET (all) ──────────────────────────────────────────────────────────────

    /// <summary>Return all active records for this catalog.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int  page            = 1,
        [FromQuery] int  pageSize        = 100,
        CancellationToken ct = default)
    {
        var query = EntitySet.AsNoTracking();
        if (!includeInactive) query = query.Where(e => e.IsActive);

        var total   = await query.CountAsync(ct);
        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(PagedResponse<TEntity>.Ok(records, page, pageSize, total));
    }

    // ── GET by id ──────────────────────────────────────────────────────────────

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> GetById(TKey id, CancellationToken ct)
    {
        var entity = await EntitySet.FindAsync([id], ct);
        if (entity is null)
            return NotFound(ApiResponse.Fail($"Record {id} not found in {TableName}."));

        return Ok(ApiResponse<TEntity>.Ok(entity));
    }

    // ── POST ───────────────────────────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<IActionResult> Create([FromBody] TEntity entity, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(
                ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        entity.IsActive  = true;
        entity.CreatedAt = DateTime.UtcNow;

        EntitySet.Add(entity);
        await Db.SaveChangesAsync(ct);

        await WriteAuditAsync("CREATE", GetKey(entity).ToString()!, null,
            JsonSerializer.Serialize(entity), ct);

        Logger.LogInformation("Created record in {Table}: {Id}", TableName, GetKey(entity));

        return CreatedAtAction(nameof(GetById),
            new { id = GetKey(entity) },
            ApiResponse<TEntity>.Ok(entity, "Record created."));
    }

    // ── PUT ────────────────────────────────────────────────────────────────────

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> Update(TKey id, [FromBody] TEntity incoming, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(
                ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var existing = await EntitySet.FindAsync([id], ct);
        if (existing is null)
            return NotFound(ApiResponse.Fail($"Record {id} not found in {TableName}."));

        var oldJson = JsonSerializer.Serialize(existing);
        ApplyUpdate(existing, incoming);
        await Db.SaveChangesAsync(ct);

        await WriteAuditAsync("UPDATE", id.ToString()!, oldJson,
            JsonSerializer.Serialize(existing), ct);

        return Ok(ApiResponse<TEntity>.Ok(existing, "Record updated."));
    }

    // ── DELETE (soft) ──────────────────────────────────────────────────────────

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> Delete(TKey id, CancellationToken ct)
    {
        var existing = await EntitySet.FindAsync([id], ct);
        if (existing is null)
            return NotFound(ApiResponse.Fail($"Record {id} not found in {TableName}."));

        var oldJson = JsonSerializer.Serialize(existing);
        existing.IsActive = false;                      // soft delete
        await Db.SaveChangesAsync(ct);

        await WriteAuditAsync("DELETE", id.ToString()!, oldJson, null, ct);

        Logger.LogWarning("Soft-deleted record {Id} in {Table}", id, TableName);
        return Ok(ApiResponse.Ok("Record deleted."));
    }

    // ── Audit helper ───────────────────────────────────────────────────────────

    protected async Task WriteAuditAsync(
        string    action,
        string    recordId,
        string?   oldValues,
        string?   newValues,
        CancellationToken ct)
    {
        try
        {
            var userIdClaim = HttpCtx.HttpContext?.User
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? HttpCtx.HttpContext?.User.FindFirst("sub")?.Value;

            int.TryParse(userIdClaim, out var userId);

            var ip = HttpCtx.HttpContext?.Items["ClientIp"] as string
                  ?? HttpCtx.HttpContext?.Connection.RemoteIpAddress?.ToString()
                  ?? "unknown";

            Db.LmAuditLogs.Add(new LmAuditLog
            {
                UserId    = userId > 0 ? userId : null,
                Action    = action,
                TableName = TableName,
                RecordId  = recordId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ip,
                CreatedAt = DateTime.UtcNow
            });

            await Db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to write audit log for {Action} on {Table}", action, TableName);
        }
    }
}
