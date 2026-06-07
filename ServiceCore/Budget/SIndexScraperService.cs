using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DALCore;
using DALCore.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ServiceCore.Budget
{
    public class SyncResultaat
    {
        public bool Geslaagd { get; set; }
        public int AantalNieuw { get; set; }
        public int AantalBijgewerkt { get; set; }
        public string? Fout { get; set; }
        public string? Debug { get; set; }

        public string Samenvatting => Geslaagd
            ? $"{AantalNieuw} nieuw, {AantalBijgewerkt} bijgewerkt"
              + (Debug != null ? $" — {Debug}" : "")
            : $"Mislukt: {Fout}";
    }

    public class SIndexScraperService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _url;

        public SIndexScraperService(UnitOfWorkCore uow, IHttpClientFactory httpFactory, IConfiguration config)
        {
            _uow = uow;
            _httpFactory = httpFactory;
            _url = config["Bouwindexen:SIndexUrl"] ?? "https://www.arch-index.be/indexen/waarden-s";
        }

        public async Task<SyncResultaat> ScrapeAsync()
        {
            var resultaat = new SyncResultaat();
            try
            {
                var client = _httpFactory.CreateClient("SIndexScraper");
                var html = await client.GetStringAsync(_url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Zoek de tbody met de data (bevat th "Geldig vanaf")
                var tabel = doc.DocumentNode.SelectSingleNode(
                    "//table[.//th[contains(translate(text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'geldig')]]");

                if (tabel == null)
                    return new SyncResultaat { Fout = $"Tabel met 'Geldig vanaf' niet gevonden op {_url}" };

                // Lees categorieheaders uit eerste <th>-rij
                var headerRij = tabel.SelectSingleNode(".//tr[th]");
                var catHeaders = headerRij?
                    .SelectNodes(".//th")
                    ?.Select(n => n.InnerText.Trim())
                    .Skip(1)   // sla "Geldig vanaf" over
                    .ToList()
                    ?? new List<string> { "Categorie A", "Categorie B", "Categorie C", "Categorie D" };

                var bestaande = await _uow.BouwIndex.GetNoTracking()
                    .Where(x => x.IndexType == "S")
                    .ToListAsync();

                var diagnostics = new StringBuilder();
                int aantalRijen = 0;

                foreach (var rij in tabel.SelectNodes(".//tr") ?? Enumerable.Empty<HtmlNode>())
                {
                    // Lees alle td-cellen (datarijen); th-rijen zijn headers/jaarlabels
                    var tdCellen = rij.SelectNodes(".//td");
                    if (tdCellen == null || tdCellen.Count < 2) continue;

                    var cellen = tdCellen.Select(c =>
                        c.InnerText.Replace("&nbsp;", " ").Replace(" ", " ").Trim()
                    ).ToList();

                    aantalRijen++;
                    if (aantalRijen <= 5)
                        diagnostics.AppendLine($"Rij {aantalRijen}: [{string.Join(" | ", cellen)}]");

                    // Lege scheidingsrij (class="bigline")
                    if (cellen.All(c => string.IsNullOrWhiteSpace(c))) continue;

                    // Parse eerste kolom: datum + optionele subcategorie
                    if (!TryParseDatumEnSubCat(cellen[0], out var datum, out var subCat)) continue;

                    // Alleen rijen met subcategorie "2B" (één waarde per maand)
                    if (!"2B".Equals(subCat, StringComparison.OrdinalIgnoreCase)) continue;

                    // Alleen Categorie A (eerste datakolom)
                    if (!TryParseWaarde(cellen[1], out var waarde)) continue;
                    var categorie = catHeaders.Count > 0 ? catHeaders[0] : "Categorie A";

                    {
                        var bestaand = bestaande.FirstOrDefault(x =>
                            x.Jaar == datum.Year &&
                            x.Maand == datum.Month &&
                            x.GeldigVanaf == datum);

                        if (bestaand == null)
                        {
                            _uow.BouwIndex.Add(new BouwIndex
                            {
                                IndexType    = "S",
                                Jaar         = datum.Year,
                                Maand        = datum.Month,
                                GeldigVanaf  = datum,
                                IndexWaarde  = waarde,
                                Categorie    = categorie,
                                SubCategorie = subCat,
                                IsActief     = false,
                                Bron         = "arch-index.be"
                            });
                            resultaat.AantalNieuw++;
                        }
                        else
                        {
                            var tracked = await _uow.BouwIndex.GetNormal()
                                .FirstAsync(x => x.Id == bestaand.Id);
                            tracked.IndexWaarde = waarde;
                            tracked.GeldigVanaf  = datum;
                            resultaat.AantalBijgewerkt++;
                        }
                    }
                }

                if (resultaat.AantalNieuw == 0 && resultaat.AantalBijgewerkt == 0)
                    resultaat.Debug = $"{aantalRijen} rijen gescanned, 0 opgeslagen. Eerste rijen: {diagnostics}";
                else
                    await _uow.SaveChangesAsync();

                resultaat.Geslaagd = true;
            }
            catch (Exception ex)
            {
                resultaat.Fout = $"{ex.Message} (URL: {_url})";
            }
            return resultaat;
        }

        // "01-04-2026 (2A+)"              → datum=2026-04-01, subCat="2A+"
        // "01-07-2025 tot 31-12-2025 (2A)"→ datum=2025-07-01, subCat="2A"
        // "01-04-2016"                     → datum=2016-04-01, subCat=null
        private static bool TryParseDatumEnSubCat(string tekst, out DateTime datum, out string? subCat)
        {
            datum = default;
            subCat = null;

            if (string.IsNullOrWhiteSpace(tekst)) return false;

            // Haal subcategorie op uit haakjes
            var parenMatch = Regex.Match(tekst, @"\(([^)]+)\)");
            if (parenMatch.Success)
                subCat = parenMatch.Groups[1].Value.Trim();

            // Verwijder alles tussen haakjes en trim
            var zonder = Regex.Replace(tekst, @"\s*\([^)]+\)", "").Trim();

            // Datumbereik "01-07-2025 tot 31-12-2025" → neem startdatum
            if (zonder.Contains(" tot "))
                zonder = zonder.Split(new[] { " tot " }, StringSplitOptions.None)[0].Trim();

            // Verwijder ook "/" varianten: "01-07/2010" → "01-07-2010"
            zonder = zonder.Replace("/", "-");

            var formaten = new[] { "dd-MM-yyyy", "d-M-yyyy", "dd-MM-yy" };
            foreach (var fmt in formaten)
                if (DateTime.TryParseExact(zonder, fmt, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out datum) && datum.Year >= 2000)
                    return true;

            return false;
        }

        // Belgisch decimaalformaat: komma = decimaalteken, punt = duizendtallenscheiding (of vice versa)
        // Voorbeelden: "39,539" → 39.539 | "38.724" → 38.724 | "1.234,56" → 1234.56
        private static bool TryParseWaarde(string tekst, out decimal waarde)
        {
            waarde = 0;
            var s = tekst.Replace(" ", "").Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(s)) return false;

            // Bepaal decimaalteken: als komma aanwezig is, is komma het decimaalteken
            if (s.Contains(','))
            {
                // Verwijder punten (duizendtalscheiding), vervang komma door punt
                s = s.Replace(".", "").Replace(",", ".");
            }
            // Anders: punt is het decimaalteken (laat ongewijzigd)

            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out waarde) && waarde > 0;
        }
    }
}
