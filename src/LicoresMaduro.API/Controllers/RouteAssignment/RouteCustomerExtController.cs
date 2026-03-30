using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.RouteAssignment;

[ApiController]
[Route("api/route/customer-ext")]
[Authorize]
[Produces("application/json")]
public sealed class RouteCustomerExtController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RouteCustomerExtController> _logger;

    public RouteCustomerExtController(ApplicationDbContext db, ILogger<RouteCustomerExtController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search   = null,
        [FromQuery] int    page      = 1,
        [FromQuery] int    pageSize  = 50,
        CancellationToken ct = default)
    {
        var q = _db.RouteCustomerExts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.RceAccountNumber.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.RceAccountNumber)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return Ok(PagedResponse<RouteCustomerExt>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.RouteCustomerExts.FindAsync([id], ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"Customer ext record {id} not found."))
            : Ok(ApiResponse<RouteCustomerExt>.Ok(e));
    }

    /// <summary>Upsert by AccountNumber — insert if not found, update if exists.</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Upsert([FromBody] RouteCustomerExtDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var existing = await _db.RouteCustomerExts
            .FirstOrDefaultAsync(x => x.RceAccountNumber == dto.RceAccountNumber, ct);

        if (existing is null)
        {
            existing = new RouteCustomerExt();
            _db.RouteCustomerExts.Add(existing);
        }

        ApplyDto(existing, dto);
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("RouteCustomerExt upserted for account '{Acct}'", dto.RceAccountNumber);
        return Ok(ApiResponse<RouteCustomerExt>.Ok(existing, "Saved."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await _db.RouteCustomerExts.FindAsync([id], ct);
        if (e is null) return NotFound(ApiResponse.Fail($"Customer ext record {id} not found."));
        _db.RouteCustomerExts.Remove(e);
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("RouteCustomerExt {Id} deleted", id);
        return Ok(ApiResponse.Ok("Record deleted."));
    }

    private static void ApplyDto(RouteCustomerExt e, RouteCustomerExtDto d)
    {
        e.RceAccountNumber            = d.RceAccountNumber;
        e.RceRouteNpActive            = d.RceRouteNpActive;
        e.RceRouteOvd5                = d.RceRouteOvd5;
        e.RceRouteOvd6                = d.RceRouteOvd6;
        e.RcePareto1Overall           = d.RcePareto1Overall;
        e.RcePareto2Overall           = d.RcePareto2Overall;
        e.RceParetoOthersOverall      = d.RceParetoOthersOverall;
        e.RcePareto1Beer              = d.RcePareto1Beer;
        e.RcePareto2Beer              = d.RcePareto2Beer;
        e.RceParetoOthersBeer         = d.RceParetoOthersBeer;
        e.RcePareto1Water             = d.RcePareto1Water;
        e.RcePareto2Water             = d.RcePareto2Water;
        e.RceParetoOthersWater        = d.RceParetoOthersWater;
        e.RcePareto1Others            = d.RcePareto1Others;
        e.RcePareto2Others            = d.RcePareto2Others;
        e.RceParetoOthersOthers       = d.RceParetoOthersOthers;
        e.RceProyection               = d.RceProyection;
        e.RceSalesRepActive4          = d.RceSalesRepActive4;
        e.RceSalesRepActive5          = d.RceSalesRepActive5;
        e.RceSalesRepActive6          = d.RceSalesRepActive6;
        e.RceAlternativeSalesRep      = d.RceAlternativeSalesRep;
        e.RceCoolerPolar              = d.RceCoolerPolar;
        e.RceCoolerCorona             = d.RceCoolerCorona;
        e.RceCoolerBrasa              = d.RceCoolerBrasa;
        e.RceCoolerWine               = d.RceCoolerWine;
        e.RcePaintedPolar             = d.RcePaintedPolar;
        e.RceBrandingDwl              = d.RceBrandingDwl;
        e.RceBrandingGreyGoose        = d.RceBrandingGreyGoose;
        e.RceBrandingBacardi          = d.RceBrandingBacardi;
        e.RceBrandingBrasa            = d.RceBrandingBrasa;
        e.RceHighTraffic              = d.RceHighTraffic;
        e.RceIndoorBrandingClaro      = d.RceIndoorBrandingClaro;
        e.RceIndoorBrandingBrasa      = d.RceIndoorBrandingBrasa;
        e.RceIndoorBrandingPolar      = d.RceIndoorBrandingPolar;
        e.RceIndoorBrandingMalta      = d.RceIndoorBrandingMalta;
        e.RceIndoorBrandingCorona     = d.RceIndoorBrandingCorona;
        e.RceIndoorBrandingCarloRossi = d.RceIndoorBrandingCarloRossi;
        e.RceWithRackDisplay          = d.RceWithRackDisplay;
        e.RceWithLightHeader          = d.RceWithLightHeader;
        e.RceWithWallMountedNameboard = d.RceWithWallMountedNameboard;
        e.RceWithBackbar              = d.RceWithBackbar;
        e.RceWithLicoresWineAsHousewine = d.RceWithLicoresWineAsHousewine;
    }
}

public sealed record RouteCustomerExtDto(
    string  RceAccountNumber,
    string? RceRouteNpActive,
    string? RceRouteOvd5,
    string? RceRouteOvd6,
    string? RcePareto1Overall,
    string? RcePareto2Overall,
    string? RceParetoOthersOverall,
    string? RcePareto1Beer,
    string? RcePareto2Beer,
    string? RceParetoOthersBeer,
    string? RcePareto1Water,
    string? RcePareto2Water,
    string? RceParetoOthersWater,
    string? RcePareto1Others,
    string? RcePareto2Others,
    string? RceParetoOthersOthers,
    string? RceProyection,
    string? RceSalesRepActive4,
    string? RceSalesRepActive5,
    string? RceSalesRepActive6,
    string? RceAlternativeSalesRep,
    bool    RceCoolerPolar              = false,
    bool    RceCoolerCorona             = false,
    bool    RceCoolerBrasa              = false,
    bool    RceCoolerWine               = false,
    bool    RcePaintedPolar             = false,
    bool    RceBrandingDwl              = false,
    bool    RceBrandingGreyGoose        = false,
    bool    RceBrandingBacardi          = false,
    bool    RceBrandingBrasa            = false,
    bool    RceHighTraffic              = false,
    bool    RceIndoorBrandingClaro      = false,
    bool    RceIndoorBrandingBrasa      = false,
    bool    RceIndoorBrandingPolar      = false,
    bool    RceIndoorBrandingMalta      = false,
    bool    RceIndoorBrandingCorona     = false,
    bool    RceIndoorBrandingCarloRossi = false,
    bool    RceWithRackDisplay          = false,
    bool    RceWithLightHeader          = false,
    bool    RceWithWallMountedNameboard = false,
    bool    RceWithBackbar              = false,
    bool    RceWithLicoresWineAsHousewine = false
);
