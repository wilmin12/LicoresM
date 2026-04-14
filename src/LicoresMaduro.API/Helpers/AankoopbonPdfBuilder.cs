using LicoresMaduro.API.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LicoresMaduro.API.Helpers;

/// <summary>
/// Generates the Aankoopbon purchase-order document as a PDF byte array.
/// </summary>
public static class AankoopbonPdfBuilder
{
    private const string WineColor = "#A52535";

    /// <param name="h">Order header (Details must be loaded).</param>
    /// <param name="logoPath">Absolute path to the company logo image (optional).</param>
    /// <param name="copyLabel">Optional copy label rendered top-right, e.g. "Original" or "Office Copy".</param>
    public static byte[] Generate(AbOrderHeader h, string? logoPath = null, string? copyLabel = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        byte[]? logoBytes = null;
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            logoBytes = File.ReadAllBytes(logoPath);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily(Fonts.Arial));

                page.Header().Element(c => Header(c, logoBytes, copyLabel));
                page.Content().PaddingTop(10).Element(c => Content(c, h, copyLabel));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Pagina ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(" van ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    // ── Page header ───────────────────────────────────────────────────────────
    private static void Header(IContainer c, byte[]? logoBytes, string? copyLabel)
    {
        c.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Left: logo + company name
                row.RelativeItem().Row(inner =>
                {
                    if (logoBytes is { Length: > 0 })
                    {
                        inner.ConstantItem(60).Height(45)
                            .Image(logoBytes).FitArea();
                        inner.ConstantItem(8); // spacer
                    }

                    inner.RelativeItem().AlignMiddle().Column(txt =>
                    {
                        txt.Item().Text("LICORES MADURO")
                            .Bold().FontSize(14).FontColor(WineColor);
                        txt.Item().Text("Aankoopbon / Purchase Order")
                            .FontSize(10).FontColor(Colors.Grey.Darken2);
                    });
                });

                // Right: copy label + document type + status
                row.ConstantItem(130).AlignRight().AlignMiddle().Column(inner =>
                {
                    if (!string.IsNullOrWhiteSpace(copyLabel))
                    {
                        var labelColor = copyLabel.Equals("Original", StringComparison.OrdinalIgnoreCase)
                            ? "#1e40af"   // blue for Original
                            : WineColor;  // wine/red for Office Copy

                        inner.Item().AlignRight()
                            .Background(labelColor)
                            .Padding(3).PaddingHorizontal(8)
                            .Text(copyLabel.ToUpper())
                            .Bold().FontSize(8).FontColor(Colors.White);
                        inner.Item().Height(4); // spacer
                    }

                    inner.Item().Text("AANKOOPBON").Bold().FontSize(13).FontColor(WineColor);
                    inner.Item().Text(t =>
                    {
                        t.Span("Status: ").Bold().FontSize(9);
                        t.Span("APPROVED").Bold().FontSize(9).FontColor(Colors.Green.Darken2);
                    });
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(WineColor);
        });
    }

    // ── Page content ──────────────────────────────────────────────────────────
    private static void Content(IContainer c, AbOrderHeader h, string? copyLabel = null)
    {
        c.Column(col =>
        {
            col.Spacing(10);

            // ── Info grid ──
            col.Item().Table(tbl =>
            {
                tbl.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(80);  // label
                    cd.RelativeColumn();    // value
                    cd.ConstantColumn(80);  // label
                    cd.RelativeColumn();    // value
                });

                Row2(tbl, "Bon Nr:",        h.AohBonNr,
                          "Datum:",         h.AohOrderDate.ToString("yyyy-MM-dd"));
                Row2(tbl, "Aanvrager:",     h.AohRequestor ?? "–",
                          "Afdeling:",      h.AohDepartment ?? "–");
                Row1(tbl, "Leverancier:",   h.AohVendorName ?? "–");

                if (!string.IsNullOrWhiteSpace(h.AohVendorAddress))
                    Row1(tbl, "Adres:", h.AohVendorAddress);

                Row2(tbl, "Kostentype:", h.AohCostType ?? "–",
                          "Offerte Nr:", h.AohQuotationNr ?? "–");

                if (!string.IsNullOrWhiteSpace(h.AohRemarks))
                    Row1(tbl, "Opmerkingen:", h.AohRemarks);

                if (h.AohVehicleId.HasValue)
                {
                    var model = string.Join(" ",
                        new[] { h.AohVehicleType, h.AohVehicleModel }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));
                    Row2(tbl, "Voertuig:", h.AohVehicleLicense ?? "–",
                              "Model:",    model);
                }
            });

            // ── Delivery method ──
            var delivery = h.AohMeegeven  ? "Wilt u meegeven"
                         : h.AohOntvangen ? "Hierbij ontvangt u"
                         : h.AohZenden    ? "Wilt u zenden"
                         : h.AohAndere    ? "Andere"
                         :                  "–";
            col.Item().Text(t =>
            {
                t.Span("Bezorging: ").Bold();
                t.Span(delivery);
            });

            // ── Line items ──
            if (h.Details is { Count: > 0 })
            {
                col.Item().Text("Bestelregels").Bold().FontSize(10);
                col.Item().Table(tbl =>
                {
                    tbl.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(18);   // #
                        cd.ConstantColumn(60);   // Code
                        cd.RelativeColumn(2);    // Omschrijving
                        cd.RelativeColumn(2);    // Aanv. Omschrijving
                        cd.ConstantColumn(38);   // Aantal
                        cd.ConstantColumn(38);   // Eenheid
                    });

                    tbl.Header(hdr =>
                    {
                        static IContainer ThStyle(IContainer cell) =>
                            cell.Background(Colors.Grey.Lighten3).Padding(4);

                        hdr.Cell().Element(ThStyle).Text("#").Bold().FontSize(8);
                        hdr.Cell().Element(ThStyle).Text("Code").Bold().FontSize(8);
                        hdr.Cell().Element(ThStyle).Text("Omschrijving").Bold().FontSize(8);
                        hdr.Cell().Element(ThStyle).Text("Aanv. Omschrijving").Bold().FontSize(8);
                        hdr.Cell().Element(ThStyle).Text("Aantal").Bold().FontSize(8);
                        hdr.Cell().Element(ThStyle).Text("Eenheid").Bold().FontSize(8);
                    });

                    var i = 1;
                    foreach (var d in h.Details)
                    {
                        var bg = i % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                        tbl.Cell().Background(bg).Padding(3).Text($"{i}").FontSize(8);
                        tbl.Cell().Background(bg).Padding(3).Text(d.AodProductCode ?? "").FontSize(8);
                        tbl.Cell().Background(bg).Padding(3).Text(d.AodProductDesc).FontSize(8);
                        tbl.Cell().Background(bg).Padding(3).Text(d.AodAdditionalDesc ?? "").FontSize(8);
                        tbl.Cell().Background(bg).Padding(3).Text(d.AodQuantity.ToString("G29")).FontSize(8);
                        tbl.Cell().Background(bg).Padding(3).Text(d.AodUnit ?? "").FontSize(8);
                        i++;
                    }
                });
            }

            // ── Amount + approval info ──
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text(t =>
                    {
                        t.Span("Goedgekeurd door: ").Bold();
                        t.Span(h.AohApprovedByName ?? "–");
                    });
                    inner.Item().Text(t =>
                    {
                        t.Span("Goedkeuringsdatum: ").Bold();
                        t.Span(h.AohApprovedAt?.ToString("yyyy-MM-dd") ?? "–");
                    });
                });
                row.ConstantItem(170).AlignRight().Column(inner =>
                {
                    inner.Item().Text(t =>
                    {
                        t.Span("Totaalbedrag: ").Bold().FontSize(11);
                        t.Span($"{h.AohAmount:N2} NAF").Bold().FontSize(11).FontColor(WineColor);
                    });
                });
            });

            // ── Signature block ──
            // Original: only Ontvangen door
            // Office Copy: all three blocks
            var isOriginal = "Original".Equals(copyLabel, StringComparison.OrdinalIgnoreCase);
            var receiverLabel = string.IsNullOrWhiteSpace(h.AohReceiverIdDoc)
                ? h.AohReceiverName
                : $"{h.AohReceiverName} (ID: {h.AohReceiverIdDoc})";

            col.Item().PaddingTop(25).Row(row =>
            {
                if (!isOriginal)
                {
                    SigBlock(row, "Aangevraagd door", h.AohRequestor);
                    row.ConstantItem(25);
                    SigBlock(row, "Goedgekeurd door", h.AohApprovedByName);
                    row.ConstantItem(25);
                }
                SigBlock(row, "Ontvangen door", receiverLabel);
            });
        });
    }

    // ── Table row helpers ─────────────────────────────────────────────────────

    private static void Row2(TableDescriptor tbl,
        string lbl1, string val1, string lbl2, string val2)
    {
        tbl.Cell().PaddingVertical(3).PaddingRight(4).Text(lbl1).Bold().FontSize(8);
        tbl.Cell().PaddingVertical(3).Text(val1).FontSize(9);
        tbl.Cell().PaddingVertical(3).PaddingRight(4).Text(lbl2).Bold().FontSize(8);
        tbl.Cell().PaddingVertical(3).Text(val2).FontSize(9);
    }

    private static void Row1(TableDescriptor tbl, string label, string value)
    {
        tbl.Cell().PaddingVertical(3).PaddingRight(4).Text(label).Bold().FontSize(8);
        tbl.Cell().ColumnSpan(3).PaddingVertical(3).Text(value).FontSize(9);
    }

    /// <summary>Signature line with role label and pre-printed name below.</summary>
    private static void SigBlock(RowDescriptor row, string role, string? name)
    {
        row.RelativeItem().Column(c =>
        {
            // Pre-printed name above the line (if available)
            c.Item().AlignCenter()
                .Text(name ?? "")
                .FontSize(8).FontColor(Colors.Grey.Darken3);

            c.Item().PaddingTop(18).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

            c.Item().PaddingTop(3).AlignCenter()
                .Text(role)
                .FontSize(8).FontColor(Colors.Grey.Darken2).Bold();
        });
    }
}
