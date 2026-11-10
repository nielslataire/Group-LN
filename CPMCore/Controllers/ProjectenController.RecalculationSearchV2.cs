using BOCore;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>Nacalculatie — "Factuur zoeken" in de topbalk (design-handoff 42a + 42m). Eén zoekvak dat raadt
    /// wat je typt (nummer, leverancier of categorie, datum, bedrag) en de beste facturen van het project
    /// teruggeeft. Elk woord moet ergens passen; een los getal zoekt nummer én bedrag.</summary>
    public partial class ProjectenController
    {
        private static readonly string[][] NlMonths =
        {
            new[] { "jan", "januari" }, new[] { "feb", "februari" }, new[] { "mrt", "maart" }, new[] { "apr", "april" },
            new[] { "mei" }, new[] { "jun", "juni" }, new[] { "jul", "juli" }, new[] { "aug", "augustus" },
            new[] { "sep", "sept", "september" }, new[] { "okt", "oktober" }, new[] { "nov", "november" }, new[] { "dec", "december" },
        };

        private sealed class SearchToken
        {
            public string Raw = "";
            public int? Day, Month, Year;
            public decimal? Amount;
            public bool IsDate, IsAmount, IsPlainNumber, IsYear, IsNumberLike;
        }

        private static SearchToken ParseSearchToken(string raw)
        {
            var t = new SearchToken { Raw = raw };
            var s = raw.Trim().ToLowerInvariant();
            if (s.Length == 0) return t;
            var nl = CultureInfo.GetCultureInfo("nl-BE");

            // Bedrag met €-teken: altijd een bedrag.
            if (s.StartsWith("€"))
            {
                var num = s.TrimStart('€').Replace(".", "").Replace(',', '.');
                if (decimal.TryParse(num, NumberStyles.Number, CultureInfo.InvariantCulture, out var a)) { t.IsAmount = true; t.Amount = a; return t; }
            }
            // Datum 21/10, 21/10/2026, 10/2026
            var m = Regex.Match(s, @"^(\d{1,2})/(\d{1,2})(?:/(\d{2,4}))?$");
            if (m.Success)
            {
                var a = int.Parse(m.Groups[1].Value); var b = int.Parse(m.Groups[2].Value);
                if (a is >= 1 and <= 31 && b is >= 1 and <= 12)
                {
                    t.IsDate = true; t.Day = a; t.Month = b;
                    if (m.Groups[3].Success) { var y = int.Parse(m.Groups[3].Value); t.Year = y < 100 ? 2000 + y : y; }
                    return t;
                }
            }
            m = Regex.Match(s, @"^(\d{1,2})/(\d{4})$");
            if (m.Success && int.Parse(m.Groups[1].Value) is >= 1 and <= 12) { t.IsDate = true; t.Month = int.Parse(m.Groups[1].Value); t.Year = int.Parse(m.Groups[2].Value); return t; }
            // Maandnaam
            for (var i = 0; i < NlMonths.Length; i++)
                if (NlMonths[i].Contains(s)) { t.IsDate = true; t.Month = i + 1; return t; }
            // Jaar
            if (Regex.IsMatch(s, @"^(19|20)\d{2}$")) { t.IsYear = true; t.Year = int.Parse(s); t.IsNumberLike = true; t.IsPlainNumber = true; t.Amount = t.Year; return t; }
            // Bedrag zonder teken: 7670 · 7.670,00 · 7670,5
            if (Regex.IsMatch(s, @"^\d{1,3}(\.\d{3})+(,\d+)?$|^\d+(,\d+)?$"))
            {
                var num = s.Replace(".", "").Replace(',', '.');
                if (decimal.TryParse(num, NumberStyles.Number, CultureInfo.InvariantCulture, out var a)) { t.IsPlainNumber = true; t.Amount = a; t.IsNumberLike = true; return t; }
            }
            if (s.Any(char.IsDigit)) t.IsNumberLike = true;   // F26/0861 e.d.: nummer
            return t;
        }

        private static string NormNr(string? s) => Regex.Replace((s ?? "").ToLowerInvariant(), @"[^a-z0-9]", "");

        [HttpGet]
        public async Task<IActionResult> RecalculationInvoiceSearch(int projectid, string? q, int take = 6)
        {
            take = Math.Clamp(take, 1, 25);
            var nl = CultureInfo.GetCultureInfo("nl-BE");

            var invoices = await _db.IncommingInvoices.AsNoTracking()
                .Where(i => i.ProjectId == projectid || (i.Contract != null && i.Contract.ProjectId == projectid))
                .Include(i => i.Company)
                .Include(i => i.Contract).ThenInclude(c => c.Company)
                .Include(i => i.IncommingInvoiceDetail).ThenInclude(d => d.Act).ThenInclude(a => a.Group)
                .Include(i => i.IncommingInvoiceDetail).ThenInclude(d => d.ContractAct).ThenInclude(ca => ca.Activity).ThenInclude(a => a.Group)
                .OrderByDescending(i => i.Date).ThenByDescending(i => i.Id)
                .ToListAsync();

            string Supplier(IncommingInvoices i) => !string.IsNullOrWhiteSpace(i.SupplierName) ? i.SupplierName! : i.Company?.BedrijfsNaam ?? i.Contract?.Company?.BedrijfsNaam ?? "";
            string Intern(IncommingInvoices i) => i.OctopusDocumentSequenceNr?.ToString() ?? i.Id.ToString();
            string Extern(IncommingInvoices i) => string.IsNullOrWhiteSpace(i.ExternalId) ? $"#{i.Id}" : i.ExternalId!;
            decimal Amount(IncommingInvoices i)
            {
                var v = i.NetAmount ?? i.Price;
                return i.InvoiceKind == (byte)IncommingInvoiceKind.Creditnota ? -Math.Abs(v) : v;
            }
            (string label, string tone) TypeOf(IncommingInvoices i)
            {
                var d = i.IncommingInvoiceDetail.FirstOrDefault();
                var type = d != null ? (int)d.Type : (i.ContractId.HasValue ? 1 : 4);
                return type switch
                {
                    1 => ("Contract", "is-positive"),
                    2 => ("Meerwerk algemeen", "is-neutral"),
                    3 => ("Meerwerk klant", "is-attention"),
                    _ => ("Geen contract", "is-blocked"),
                };
            }
            IEnumerable<string> Categories(IncommingInvoices i) => i.IncommingInvoiceDetail
                .SelectMany(d => new[] { d.Act?.Omschrijving, d.Act?.Group?.Name, d.ContractAct?.Activity?.Omschrijving, d.ContractAct?.Activity?.Group?.Name })
                .Where(s => !string.IsNullOrWhiteSpace(s))!;

            object Shape(IncommingInvoices i, bool dMatch, bool aMatch)
            {
                var (label, tone) = TypeOf(i);
                var amt = Amount(i);
                return new
                {
                    id = i.Id,
                    url = Url.Action("IncommingInvoiceDetail", "Projecten", new { projectid, invoiceid = i.Id }),
                    nummer = Extern(i),
                    intern = Intern(i),
                    leverancier = Supplier(i),
                    datum = i.Date.ToString("dd/MM/yyyy", nl),
                    bedrag = (amt < 0 ? "− € " : "€ ") + Math.Abs(amt).ToString("N2", nl),
                    type = label,
                    tone,
                    dMatch,
                    aMatch,
                };
            }

            q = (q ?? "").Trim();
            if (q.Length == 0)
            {
                // Leeg vak: voorbeelden om te proberen, uit de echte facturen van dit project.
                var tips = new List<object>();
                var first = invoices.FirstOrDefault();
                var sup = invoices.Select(Supplier).Where(s => s.Length > 0).GroupBy(s => s).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
                if (sup != null) tips.Add(new { q = sup.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant(), l = "leverancier" });
                var nr = invoices.Select(Extern).FirstOrDefault(s => !s.StartsWith("#"));
                if (nr != null) tips.Add(new { q = nr, l = "nummer" });
                if (first != null) tips.Add(new { q = first.Date.ToString("MM/yyyy", nl), l = "datum" });
                if (first != null) tips.Add(new { q = Math.Round(Math.Abs(Amount(first))).ToString("0", nl), l = "bedrag" });
                return Json(new { chips = Array.Empty<object>(), items = Array.Empty<object>(), total = 0, suppliers = Array.Empty<object>(), tips, textTerms = Array.Empty<string>() });
            }

            // € 7.670 → €7.670 (het €-teken hoort bij het getal)
            var cleaned = Regex.Replace(q, @"€\s+", "€");
            var tokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(ParseSearchToken).ToList();

            var chipKinds = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var t in tokens)
            {
                if (t.IsAmount) chipKinds.Add("bedrag");
                else if (t.IsYear) chipKinds.Add("datum");
                else if (t.IsDate) chipKinds.Add("datum");
                else if (t.IsPlainNumber) { chipKinds.Add("nummer"); chipKinds.Add("bedrag"); }
                else if (t.IsNumberLike) chipKinds.Add("nummer");
                else chipKinds.Add("leverancier");
            }
            var chipLabels = new Dictionary<string, (string label, string icon)>
            {
                ["nummer"] = ("Nummer", "M3.4 2.4h7l2.2 2.2v9H3.4z M5.6 6.4h4.8 M5.6 8.8h4.8 M5.6 11.2h2.8"),
                ["leverancier"] = ("Leverancier of categorie", "M1.8 4.6h7.4v6H1.8z M9.2 6.8h2.4l2.6 2.2v1.6H9.2z"),
                ["datum"] = ("Datum", "M2.6 3.8h10.8v9.6H2.6z M2.6 6.6h10.8 M5.4 2.4v2.8 M10.6 2.4v2.8"),
                ["bedrag"] = ("Bedrag", "M8 2.4v11.2 M10.8 5.2C10.4 4.2 9.4 3.7 8 3.7 6.4 3.7 5.4 4.4 5.4 5.6 5.4 8.2 10.8 7.2 10.8 10.4 10.8 11.6 9.6 12.3 8 12.3 6.5 12.3 5.5 11.7 5.1 10.7"),
            };
            var chips = chipKinds.Select(k => new { kind = k, label = chipLabels[k].label, icon = chipLabels[k].icon }).ToList();

            var hits = new List<(IncommingInvoices inv, bool dMatch, bool aMatch)>();
            foreach (var inv in invoices)
            {
                var all = true; var dMatch = false; var aMatch = false;
                var name = Supplier(inv).ToLowerInvariant();
                var nrs = new[] { NormNr(Extern(inv)), NormNr(Intern(inv)) };
                var cats = Categories(inv).Select(c => c!.ToLowerInvariant()).ToList();
                var amt = Math.Abs(Amount(inv));
                foreach (var t in tokens)
                {
                    var ok = false;
                    var raw = t.Raw.Trim().ToLowerInvariant();
                    if (t.IsAmount)
                    {
                        if (Math.Round(amt) == Math.Round(t.Amount!.Value)) { ok = true; aMatch = true; }
                    }
                    else if (t.IsDate)
                    {
                        var d = inv.Date;
                        if ((t.Month == null || d.Month == t.Month) && (t.Day == null || d.Day == t.Day) && (t.Year == null || d.Year == t.Year)) { ok = true; dMatch = true; }
                    }
                    else if (t.IsYear)
                    {
                        if (inv.Date.Year == t.Year) { ok = true; dMatch = true; }
                        else if (nrs.Any(n => n.Contains(raw))) ok = true;
                    }
                    else if (t.IsPlainNumber)
                    {
                        var nrKey = NormNr(raw);
                        if (nrKey.Length > 0 && nrs.Any(n => n.Contains(nrKey))) ok = true;
                        if (Math.Round(amt) == Math.Round(t.Amount!.Value)) { ok = true; aMatch = true; }
                    }
                    else if (t.IsNumberLike)
                    {
                        var nrKey = NormNr(raw);
                        if (nrKey.Length > 0 && nrs.Any(n => n.Contains(nrKey))) ok = true;
                    }
                    else
                    {
                        if (name.Contains(raw) || cats.Any(c => c.Contains(raw))) ok = true;
                    }
                    if (!ok) { all = false; break; }
                }
                if (all) hits.Add((inv, dMatch, aMatch));
            }

            // Leveranciers: alleen als een woord van de zoekopdracht (geen cijfer) op een leveranciersnaam past.
            var words = tokens.Where(t => !t.IsAmount && !t.IsDate && !t.IsPlainNumber && !t.IsNumberLike).Select(t => t.Raw.Trim().ToLowerInvariant()).ToList();
            var suppliers = words.Count == 0 ? new List<object>() : invoices
                .Select(Supplier).Where(s => s.Length > 0 && words.Any(w => s.ToLowerInvariant().Contains(w)))
                .GroupBy(s => s).OrderByDescending(g => g.Count()).Take(2)
                .Select(g => (object)new { name = g.Key, count = g.Count() }).ToList();

            return Json(new
            {
                chips,
                items = hits.Take(take).Select(h => Shape(h.inv, h.dMatch, h.aMatch)).ToList(),
                total = hits.Count,
                suppliers,
                tips = Array.Empty<object>(),
                textTerms = words,
                nrTerms = tokens.Where(t => t.IsNumberLike).Select(t => t.Raw.Trim().TrimStart('€')).ToList(),
            });
        }
    }
}
