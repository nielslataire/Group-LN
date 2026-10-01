using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using CPMCore.Services.InvoiceExtraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Services.QuoteExtraction
{
    /// <summary>Offerte inlezen (design-handoff 20c) — leest de tabelstructuur uit een client-
    /// bijgesneden regio-afbeelding via Azure Document Intelligence, model "prebuilt-layout" (generieke
    /// rij/kolom-detectie, in tegenstelling tot AzureInvoiceAnalysisService se "prebuilt-invoice" dat
    /// hele-document-analyse met semantische velden doet). Herbruikt dezelfde
    /// AzureDocumentIntelligenceOptions (endpoint/API-key) als die service.</summary>
    public class AzureQuoteAnalysisService : IQuoteRegionAnalysisService
    {
        private const string LayoutModelId = "prebuilt-layout";
        private static readonly string[] UnitTokens = { "m²", "m2", "m³", "m3", "lm", "sog", "st", "stuk", "stuks" };

        private readonly AzureDocumentIntelligenceOptions _azure;
        private readonly ILogger<AzureQuoteAnalysisService> _logger;

        public bool IsEnabled => _azure.IsConfigured;

        public AzureQuoteAnalysisService(IOptions<InvoiceExtractionOptions> options, ILogger<AzureQuoteAnalysisService> logger)
        {
            _azure = options.Value.Azure;
            _logger = logger;
        }

        public async Task<QuoteRegionResult> AnalyzeTableAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            if (!IsEnabled)
                return new QuoteRegionResult { ErrorMessage = "Azure Document Intelligence is niet geconfigureerd." };

            try
            {
                var client = new DocumentAnalysisClient(new Uri(_azure.Endpoint), new AzureKeyCredential(_azure.ApiKey));
                using var ms = new MemoryStream(imageBytes);
                var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, LayoutModelId, ms, cancellationToken: ct);

                var table = operation.Value.Tables.OrderByDescending(t => t.Cells.Count).FirstOrDefault();
                if (table is null)
                {
                    _logger.LogInformation("Azure Document Intelligence (prebuilt-layout): geen tabel herkend in de regio.");
                    return new QuoteRegionResult { Success = true };
                }

                var rows = table.Cells
                    .GroupBy(c => c.RowIndex)
                    .OrderBy(g => g.Key)
                    .Select(g => g.OrderBy(c => c.ColumnIndex).Select(c => (c.ColumnIndex, Text: (c.Content ?? "").Trim())).ToList())
                    .ToList();

                var lines = MapTable(rows);
                return new QuoteRegionResult { Success = true, Lines = lines };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Document Intelligence (prebuilt-layout) analyse mislukt.");
                return new QuoteRegionResult { ErrorMessage = ex.Message };
            }
        }

        // Koptekst-herkenning: welke kolom is wat. Trefwoorden in kleine letters, substring-match op de
        // kopcel. "Bedrag"/"totaal" wordt expliciet apart herkend zodat de regel-totaalkolom nooit als
        // eenheidsprijs gelezen wordt.
        private static readonly string[] HeaderDescription = { "omschrijving", "beschrijving", "description", "artikel", "product", "item" };
        private static readonly string[] HeaderUnitPrice = { "prijs/st", "prijs per", "eenheidsprijs", "e.p.", "unit price", "p/st", "stukprijs", "prijs" };
        private static readonly string[] HeaderTotal = { "bedrag", "totaal", "total", "amount" };
        private static readonly string[] HeaderQuantity = { "aantal", "hoeveelheid", "qty", "quantity", "stuks", "aant." };
        private static readonly string[] HeaderUnit = { "eenh", "eenheid", "unit" };
        private static readonly string[] HeaderNr = { "nr", "no.", "pos", "positie", "#", "lijn" };

        private sealed class ColumnMap
        {
            public int? Description, UnitPrice, Total, Quantity, Unit, Nr;
            public bool HasHeader => Description.HasValue || UnitPrice.HasValue || Quantity.HasValue;
        }

        /// <summary>Kolomtoewijzing is een heuristiek, geen semantische AI-herkenning (prebuilt-layout
        /// geeft enkel rij/kolom-cellen terug). Twee lagen:
        /// 1. Staat er een kopregel ("Nr | Omschrijving | Prijs/St. | Aantal | Bedrag"), dan worden de
        ///    kolommen op naam toegewezen — veel betrouwbaarder dan positie raden.
        /// 2. Anders de positionele terugval: kolom 0 = omschrijving, laatste numerieke cel = prijs, de
        ///    cel ervoor = aantal.
        /// Daarbovenop: een rij ZONDER prijs (en zonder nr) is een vervolgrij van de regel erboven —
        /// typische leveranciersoffertes zetten de specificaties (kleur, motor, afmetingen …) als losse
        /// rijen onder het artikel. Die worden in de omschrijving van die regel gevouwen i.p.v. als eigen
        /// regel met de attribuutwaarde ("1840") als prijs. Alles wat niet overtuigend herkend wordt →
        /// NeedsReview.</summary>
        private static List<QuoteRegionLine> MapTable(List<List<(int ColumnIndex, string Text)>> rows)
        {
            var lines = new List<QuoteRegionLine>();
            if (rows.Count == 0) return lines;

            var map = DetectHeader(rows[0]);
            var dataRows = map.HasHeader ? rows.Skip(1).ToList() : rows;

            QuoteRegionLine current = null;
            foreach (var cells in dataRows)
            {
                if (cells.All(c => string.IsNullOrWhiteSpace(c.Text))) continue;

                string Cell(int? col) => col.HasValue ? cells.FirstOrDefault(c => c.ColumnIndex == col.Value).Text ?? "" : "";

                decimal? price, quantity;
                string description, unitText;

                if (map.HasHeader)
                {
                    price = ParseDecimal(Cell(map.UnitPrice));
                    if (!price.HasValue && !map.UnitPrice.HasValue) price = ParseDecimal(Cell(map.Total));
                    quantity = ParseDecimal(Cell(map.Quantity));
                    description = Cell(map.Description);
                    unitText = Cell(map.Unit);
                    var nrText = Cell(map.Nr);

                    // Vervolgrij: geen prijs en geen nr → specificaties van de regel erboven.
                    if (!price.HasValue && string.IsNullOrWhiteSpace(nrText) && current != null)
                    {
                        AppendToDescription(current, cells.Where(c => c.ColumnIndex != map.Nr).Select(c => c.Text));
                        continue;
                    }
                }
                else
                {
                    description = cells[0].Text;
                    var rest = cells.Skip(1).ToList();
                    var unitCol = rest.Where(c => UnitTokens.Contains(c.Text.ToLowerInvariant())).Select(c => (int?)c.ColumnIndex).FirstOrDefault();
                    var numeric = rest.Where(c => c.ColumnIndex != unitCol && ParseDecimal(c.Text).HasValue).ToList();
                    price = numeric.Count > 0 ? ParseDecimal(numeric[^1].Text) : null;
                    quantity = numeric.Count > 1 ? ParseDecimal(numeric[^2].Text) : null;
                    unitText = unitCol.HasValue ? rest.First(c => c.ColumnIndex == unitCol.Value).Text : null;

                    if (!price.HasValue && current != null)
                    {
                        AppendToDescription(current, cells.Select(c => c.Text));
                        continue;
                    }
                }

                current = new QuoteRegionLine
                {
                    Description = description,
                    Unit = string.IsNullOrWhiteSpace(unitText) ? null : unitText,
                    Number = quantity ?? 1m,
                    Price = price,
                    NeedsReview = string.IsNullOrWhiteSpace(description) || !price.HasValue,
                };
                lines.Add(current);
            }

            return lines;
        }

        private static ColumnMap DetectHeader(List<(int ColumnIndex, string Text)> firstRow)
        {
            var map = new ColumnMap();
            foreach (var (col, text) in firstRow)
            {
                var t = text.ToLowerInvariant().Trim();
                if (string.IsNullOrEmpty(t)) continue;
                // Volgorde van belang: "bedrag"/"totaal" vóór "prijs" (anders matcht "totaalprijs" op prijs),
                // en "nr" enkel als korte kop zodat "omschrijving" er nooit op hangt.
                if (!map.Total.HasValue && HeaderTotal.Any(t.Contains)) map.Total = col;
                else if (!map.UnitPrice.HasValue && HeaderUnitPrice.Any(t.Contains)) map.UnitPrice = col;
                else if (!map.Description.HasValue && HeaderDescription.Any(t.Contains)) map.Description = col;
                else if (!map.Quantity.HasValue && HeaderQuantity.Any(t.Contains)) map.Quantity = col;
                else if (!map.Unit.HasValue && HeaderUnit.Any(t.Contains)) map.Unit = col;
                else if (!map.Nr.HasValue && t.Length <= 6 && HeaderNr.Any(t.Contains)) map.Nr = col;
            }
            return map;
        }

        private static void AppendToDescription(QuoteRegionLine line, IEnumerable<string> cellTexts)
        {
            var extra = string.Join(" ", cellTexts.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
            if (string.IsNullOrWhiteSpace(extra)) return;
            // Elke vervolgrij op een eigen regel (geen " · "): de omschrijving is een textarea en de
            // structuur van de offerte (nummering, kenmerk: waarde) blijft zo leesbaar.
            line.Description = string.IsNullOrWhiteSpace(line.Description) ? extra : line.Description + "\n" + extra;
        }

        /// <summary>Getallen zoals ze in Belgische/Nederlandse offertes voorkomen, zonder op één cultuur te
        /// vertrouwen — hetzelfde document mengt gerust "1388,87" (komma = decimaal) met "1.00" (punt =
        /// decimaal). Regel: staan beide scheidingstekens erin, dan is het LAATSTE de decimaalscheider;
        /// staat er maar één, dan is het de decimaalscheider als er 1-2 cijfers op volgen en een
        /// duizendtalscheider als er precies 3 op volgen. Invariant-first parsen (de vorige versie) las
        /// "1388,87" als 138887 — dat was de prijsbug uit de eerste live test.</summary>
        private static decimal? ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var s = text.Replace("€", "").Replace("EUR", "").Replace(" ", "").Replace(" ", "").Trim();
            if (s.Length == 0) return null;

            var lastDot = s.LastIndexOf('.');
            var lastComma = s.LastIndexOf(',');

            string normalized;
            if (lastDot >= 0 && lastComma >= 0)
            {
                var decimalSep = lastDot > lastComma ? '.' : ',';
                var groupSep = decimalSep == '.' ? ',' : '.';
                normalized = s.Replace(groupSep.ToString(), "").Replace(decimalSep, '.');
            }
            else if (lastDot >= 0 || lastComma >= 0)
            {
                var sep = lastDot >= 0 ? '.' : ',';
                var idx = lastDot >= 0 ? lastDot : lastComma;
                var digitsAfter = s.Length - idx - 1;
                var isDecimal = digitsAfter >= 1 && digitsAfter <= 2 || s.IndexOf(sep) != idx; // "1.234.567" → groepen
                if (s.IndexOf(sep) != idx) normalized = s.Replace(sep.ToString(), "");           // meerdere → allemaal groepen
                else normalized = isDecimal ? s.Replace(sep, '.') : s.Replace(sep.ToString(), "");
            }
            else normalized = s;

            return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var v) ? v : null;
        }
    }
}
