using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DALCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ServiceCore.Budget
{
    public class I2021SyncService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _url;

        public I2021SyncService(UnitOfWorkCore uow, IHttpClientFactory httpFactory, IConfiguration config)
        {
            _uow = uow;
            _httpFactory = httpFactory;
            _url = config["Bouwindexen:I2021Url"]
                ?? "https://economie.fgov.be/sites/default/files/Files/Entreprises/prix-construction-Indice-I-2021.xlsx";
        }

        public async Task<SyncResultaat> SyncAsync()
        {
            var resultaat = new SyncResultaat();
            try
            {
                var client = _httpFactory.CreateClient("I2021Sync");
                var bytes = await client.GetByteArrayAsync(_url);

                using var ms = new MemoryStream(bytes);
                using var wb = new XLWorkbook(ms);

                // Gebruik bij voorkeur de NL-versie van het werkblad
                var ws = wb.Worksheets.FirstOrDefault(w =>
                        w.Name.IndexOf("NL", StringComparison.OrdinalIgnoreCase) >= 0)
                    ?? wb.Worksheets.First();

                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;

                // Stap 1: zoek de datumrij (bevat meerdere opeenvolgende datumkolommen)
                var datumKolommen = new Dictionary<int, (int jaar, int maand)>();
                int datumRij = -1;

                for (int r = 1; r <= Math.Min(10, lastRow); r++)
                {
                    var gevonden = new Dictionary<int, (int, int)>();
                    for (int c = 1; c <= lastCol; c++)
                    {
                        if (TryParseCelAlsDatum(ws.Cell(r, c), out var j, out var m))
                            gevonden[c] = (j, m);
                    }
                    if (gevonden.Count >= 6) // minstens 6 datumkolommen verwacht
                    {
                        datumRij = r;
                        datumKolommen = gevonden;
                        break;
                    }
                }

                if (datumRij < 0)
                    return new SyncResultaat
                    {
                        Geslaagd = true,
                        Debug = "Geen datumrij gevonden. Controleer of het Excel-bestand de juiste structuur heeft."
                    };

                // Stap 2: zoek de rijen "INDEX I-2021" en "INDEX I+"
                int rijI2021 = -1, rijIPlus = -1;
                for (int r = datumRij + 1; r <= lastRow; r++)
                {
                    for (int c = 1; c <= Math.Min(5, lastCol); c++)
                    {
                        var tekst = ws.Cell(r, c).Value.ToString();
                        if (rijI2021 < 0 && (tekst.Contains("I-2021", StringComparison.OrdinalIgnoreCase)
                                             || tekst.Contains("I 2021", StringComparison.OrdinalIgnoreCase)))
                            rijI2021 = r;
                        if (rijIPlus < 0 && tekst.Contains("I+", StringComparison.OrdinalIgnoreCase)
                                         && !tekst.Contains("Laatste", StringComparison.OrdinalIgnoreCase))
                            rijIPlus = r;
                    }
                    if (rijI2021 > 0 && rijIPlus > 0) break;
                }

                if (rijI2021 < 0 && rijIPlus < 0)
                    return new SyncResultaat
                    {
                        Geslaagd = true,
                        Debug = $"Datumrij {datumRij} gevonden ({datumKolommen.Count} kolommen), "
                              + "maar rijen 'INDEX I-2021' en 'INDEX I+' niet gevonden."
                    };

                // Stap 3: inlezen bestaande records
                var bestaande = await _uow.BouwIndex.GetNoTracking()
                    .Where(x => x.IndexType == "I2021" || x.IndexType == "I+")
                    .ToListAsync();

                // Stap 4: verwerk beide rijen per datumkolom
                var bronRijen = new List<(int rij, string type)>();
                if (rijI2021 > 0) bronRijen.Add((rijI2021, "I2021"));
                if (rijIPlus > 0)  bronRijen.Add((rijIPlus, "I+"));

                foreach (var (rij, type) in bronRijen)
                {
                    foreach (var (col, (jaar, maand)) in datumKolommen)
                    {
                        var cel = ws.Cell(rij, col);
                        var waardeStr = cel.Value.ToString().Trim()
                            .Replace(" ", "")   // non-breaking space
                            .Replace(" ", "")
                            .Replace(",", ".");

                        if (!decimal.TryParse(waardeStr, NumberStyles.Any,
                                CultureInfo.InvariantCulture, out var waarde) || waarde <= 0)
                            continue;

                        var bestaand = bestaande.FirstOrDefault(x =>
                            x.IndexType == type && x.Jaar == jaar && x.Maand == maand);

                        if (bestaand == null)
                        {
                            _uow.BouwIndex.Add(new BouwIndex
                            {
                                IndexType   = type,
                                Jaar        = jaar,
                                Maand       = maand,
                                IndexWaarde = waarde,
                                IsActief    = false,
                                Bron        = "FOD Economie - I2021"
                            });
                            resultaat.AantalNieuw++;
                        }
                        else
                        {
                            var tracked = await _uow.BouwIndex.GetNormal()
                                .FirstAsync(x => x.Id == bestaand.Id);
                            tracked.IndexWaarde = waarde;
                            resultaat.AantalBijgewerkt++;
                        }
                    }
                }

                if (resultaat.AantalNieuw > 0 || resultaat.AantalBijgewerkt > 0)
                    await _uow.SaveChangesAsync();
                else
                    resultaat.Debug = $"Datumrij={datumRij}, {datumKolommen.Count} kolommen, "
                        + $"I-2021 rij={rijI2021}, I+ rij={rijIPlus}, maar geen geldige waarden gevonden.";

                resultaat.Geslaagd = true;
            }
            catch (Exception ex)
            {
                resultaat.Fout = $"{ex.Message} (URL: {_url})";
            }
            return resultaat;
        }

        private static bool TryParseCelAlsDatum(IXLCell cel, out int jaar, out int maand)
        {
            jaar = 0; maand = 0;

            // Excel-datum (numerieke waarde met datumopmaak)
            try
            {
                if (cel.DataType == XLDataType.DateTime)
                {
                    var dt = cel.GetDateTime();
                    if (dt.Year >= 2000 && dt.Year <= 2050)
                    { jaar = dt.Year; maand = dt.Month; return true; }
                }
            }
            catch { /* negeer */ }

            // Tekstrepresentatie "okt-23", "jan-24", "01/2024", "2024-01"
            var tekst = cel.Value.ToString().Trim();
            if (string.IsNullOrEmpty(tekst)) return false;

            var formaten = new[] { "MMM-yy", "MMM yy", "MM/yyyy", "yyyy-MM", "MM-yyyy", "MMMM yyyy", "MMMM-yyyy" };
            var culturen = new[] { new CultureInfo("nl-BE"), new CultureInfo("nl-NL"), new CultureInfo("fr-BE") };

            foreach (var fmt in formaten)
                foreach (var cult in culturen)
                    if (DateTime.TryParseExact(tekst, fmt, cult, DateTimeStyles.None, out var dt)
                        && dt.Year >= 2000 && dt.Year <= 2050)
                    { jaar = dt.Year; maand = dt.Month; return true; }

            return false;
        }
    }
}
