using LicoresMaduro.API.Data;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Services;

public interface IPriceCalculationService
{
    Task ComputeAndPersistAsync(int calcNumber, string? approvedBy, CancellationToken ct = default);
}

public sealed class PriceCalculationService : IPriceCalculationService
{
    private readonly ApplicationDbContext _db;
    private readonly DhwDbContext         _dhw;
    private readonly ILogger<PriceCalculationService> _logger;

    public PriceCalculationService(ApplicationDbContext db, DhwDbContext dhw,
        ILogger<PriceCalculationService> logger)
    { _db = db; _dhw = dhw; _logger = logger; }

    public async Task ComputeAndPersistAsync(int calcNumber, string? approvedBy, CancellationToken ct = default)
    {
        try
        {
            var sysCfg = await _db.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct);
            var poHeads = await _db.CcCalcPoHeads
                .Include(p => p.Details)
                .Where(p => p.CcphCalcNumber == calcNumber)
                .AsNoTracking()
                .ToListAsync(ct);

            var itemCodes = poHeads.SelectMany(p => p.Details)
                .Select(d => d.CcpdItemNo).Distinct().ToList();

            var pricesMap = (await _dhw.Ranker553
                .Where(r => itemCodes.Contains(r.Item))
                .AsNoTracking()
                .ToListAsync(ct))
                .GroupBy(r => r.Item)
                .ToDictionary(g => g.Key, g => g.First());

            var all99T = await _dhw.Ranker99T
                .Where(r => itemCodes.Contains(r.Item))
                .AsNoTracking()
                .ToListAsync(ct);


            var allowedMarginMap = all99T
                .GroupBy(r => r.Item)
                .ToDictionary(
                    g => g.Key,
                    g => g.FirstOrDefault(r => (r.Whse ?? "").Trim() == "11010")?.Cost99
                         ?? g.FirstOrDefault(r => r.Cost99.HasValue)?.Cost99);

            var allowedMarginMap11060 = all99T
                .GroupBy(r => r.Item)
                .ToDictionary(
                    g => g.Key,
                    g => g.FirstOrDefault(r => (r.Whse ?? "").Trim() == "11060")?.Cost99);

            _logger.LogInformation("PriceCalc #{CalcNumber}: Ranker99T rows={Rows}, allowedMarginMap keys={Keys}, sample Whse values={Whse}",
                calcNumber, all99T.Count, allowedMarginMap.Count,
                string.Join(",", all99T.Take(3).Select(r => $"'{r.Whse}'")));

            decimal norsaPerc    = (sysCfg?.CompStoreNorsaPerc    ?? 0m) / 100m;
            decimal retailPerc   = (sysCfg?.CompStoreRetailPerc   ?? 0m) / 100m;
            decimal alliancePerc = (sysCfg?.CompStoreAlliancePerc ?? 0m) / 100m;

            var toUpsert = new List<CcPriceConfirmation>();

            foreach (var po in poHeads)
            foreach (var d in po.Details)
            {
                pricesMap.TryGetValue(d.CcpdItemNo, out var op);

                int qty = Math.Max((int)(d.CcpdOrdQty ?? 1m), 1);
                decimal newCostCase11010 = (d.CcpdFinalCost ?? 0m) / qty;
                decimal newCostCase11060 = ((d.CcpdFinalCost ?? 0m) - d.CcpdDuties - d.CcpdEconSurch - d.CcpdOb) / qty;

                decimal oldCost11010 = op?.DutyPd ?? 0m;
                decimal oldCost11060 = op?.DutyFr ?? 0m;

                decimal mPR01 = Margin(op?.Pr01, oldCost11010);
                decimal mPR06 = Margin(op?.Pr06, oldCost11060);
                decimal mPR07 = Margin(op?.Pr07, oldCost11060);
                decimal mPR08 = Margin(op?.Pr08, oldCost11060);
                decimal mPR09 = Margin(op?.Pr09, oldCost11060);
                decimal mPR11 = Margin(op?.Pr11, oldCost11060);

                decimal nPR01 = Price(newCostCase11010, mPR01);
                decimal nPR06 = Price(newCostCase11060, mPR06);
                decimal nPR07 = Price(newCostCase11060, mPR07);
                decimal nPR08 = Price(newCostCase11060, mPR08);
                decimal nPR09 = Price(newCostCase11060, mPR09);
                decimal nPR11 = Price(newCostCase11060, mPR11);
                decimal nPR03 = nPR01 * (1m + norsaPerc);
                decimal nPR04 = nPR01 * (1m + retailPerc);
                decimal nPR05 = nPR01 * (1m + alliancePerc);
                decimal nPR10 = nPR01 * 0.90m;

                toUpsert.Add(new CcPriceConfirmation
                {
                    CcpcCalcNumber    = calcNumber,
                    CcpcPoNo          = po.CcphLmPoNo,
                    CcpcItemNo        = d.CcpdItemNo,
                    CcpcWarehouse     = po.CcphWhse,
                    CcpcDescription   = d.CcpdItemDescr,
                    CcpcOrdQty        = d.CcpdOrdQty,
                    CcpcFobFc         = d.CcpdFobPriceUsd,
                    CcpcAllowedMargin = allowedMarginMap.TryGetValue(d.CcpdItemNo, out var am) ? am : null,
                    CcpcAllowedMargin11060 = allowedMarginMap11060.TryGetValue(d.CcpdItemNo, out var am60) ? am60 : null,
                    CcpcNewCost11010  = newCostCase11010,
                    CcpcNewCost11060  = newCostCase11060,
                    CcpcOldCost11010  = oldCost11010,
                    CcpcOldCost11060  = oldCost11060,
                    CcpcNewPricePr01  = nPR01,  CcpcNewMarginPr01  = Margin(nPR01, newCostCase11010),
                    CcpcOldPricePr01  = op?.Pr01, CcpcOldMarginPr01 = mPR01,
                    CcpcNewPricePr03  = nPR03,  CcpcNewMarginPr03  = Margin(nPR03, newCostCase11010),
                    CcpcOldPricePr03  = op?.Pr03, CcpcOldMarginPr03 = Margin(op?.Pr03, oldCost11010),
                    CcpcNewPricePr04  = nPR04,  CcpcNewMarginPr04  = Margin(nPR04, newCostCase11010),
                    CcpcOldPricePr04  = op?.Pr04, CcpcOldMarginPr04 = Margin(op?.Pr04, oldCost11010),
                    CcpcNewPricePr05  = nPR05,  CcpcNewMarginPr05  = Margin(nPR05, newCostCase11010),
                    CcpcOldPricePr05  = op?.Pr05, CcpcOldMarginPr05 = Margin(op?.Pr05, oldCost11010),
                    CcpcNewPricePr06  = nPR06,  CcpcNewMarginPr06  = Margin(nPR06, newCostCase11060),
                    CcpcOldPricePr06  = op?.Pr06, CcpcOldMarginPr06 = mPR06,
                    CcpcNewPricePr07  = nPR07,  CcpcNewMarginPr07  = Margin(nPR07, newCostCase11060),
                    CcpcOldPricePr07  = op?.Pr07, CcpcOldMarginPr07 = mPR07,
                    CcpcNewPricePr08  = nPR08,  CcpcNewMarginPr08  = Margin(nPR08, newCostCase11060),
                    CcpcOldPricePr08  = op?.Pr08, CcpcOldMarginPr08 = mPR08,
                    CcpcNewPricePr09  = nPR09,  CcpcNewMarginPr09  = Margin(nPR09, newCostCase11060),
                    CcpcOldPricePr09  = op?.Pr09, CcpcOldMarginPr09 = mPR09,
                    CcpcNewPricePr10  = nPR10,  CcpcNewMarginPr10  = Margin(nPR10, newCostCase11010),
                    CcpcOldPricePr10  = op?.Pr10, CcpcOldMarginPr10 = Margin(op?.Pr10, oldCost11010),
                    CcpcNewPricePr11  = nPR11,  CcpcNewMarginPr11  = Margin(nPR11, newCostCase11060),
                    CcpcOldPricePr11  = op?.Pr11, CcpcOldMarginPr11 = mPR11,
                    CcpcApprovedBy    = approvedBy,
                    CcpcCreatedAt     = DateTime.UtcNow
                });
            }

            // Preserve manager decisions (price-change flags) across recalculations
            var existing = await _db.CcPriceConfirmations
                .Where(x => x.CcpcCalcNumber == calcNumber)
                .ToListAsync(ct);
            var flagMap = existing
                .Where(x => x.CcpcPriceChangeFlag != null)
                .ToDictionary(x => (x.CcpcPoNo, x.CcpcItemNo), x => x.CcpcPriceChangeFlag);
            foreach (var row in toUpsert)
                if (flagMap.TryGetValue((row.CcpcPoNo, row.CcpcItemNo), out var oldFlag))
                    row.CcpcPriceChangeFlag = oldFlag;
            _db.CcPriceConfirmations.RemoveRange(existing);
            _db.CcPriceConfirmations.AddRange(toUpsert);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("PriceCalc #{CalcNumber}: persisted {Count} rows", calcNumber, toUpsert.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PriceCalc #{CalcNumber} failed", calcNumber);
        }
    }

    private static decimal Margin(decimal? price, decimal cost)
    {
        if (price == null || price == 0m) return 0m;
        return Math.Round(((price.Value - cost) / price.Value) * 100m, 2);
    }

    private static decimal Price(decimal cost, decimal margin)
    {
        if (margin >= 100m) return 0m;
        return cost / (1m - margin / 100m);
    }
}
