using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore.IncomingInvoices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace CPMCore.Controllers
{
    /// <summary>
    /// TIJDELIJK debug-endpoint voor verrijkingspipeline inspectie.
    /// Gebruik: /InvoiceEnrichmentDebug/Inspect?externalId=21_A1_99
    /// Verwijderen na afloop van debug-sessie.
    /// </summary>
    [Authorize]
    public class InvoiceEnrichmentDebugController : Controller
    {
        private readonly cpmRunningContext _db;
        private readonly IProjectMatchingService _projectMatcher;

        public InvoiceEnrichmentDebugController(
            cpmRunningContext db,
            IProjectMatchingService projectMatcher)
        {
            _db = db;
            _projectMatcher = projectMatcher;
        }

        [HttpGet]
        public async Task<IActionResult> Inspect(string externalId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return Json(new { error = "externalId is verplicht." });

            var invoice = await _db.IncommingInvoices
                .Include(i => i.IncomingInvoiceAttachments)
                .Include(i => i.IncommingInvoiceDetail)
                .FirstOrDefaultAsync(i => i.OctopusExternalId == externalId
                                       || i.ExternalId == externalId, ct);

            if (invoice == null)
                return Json(new { error = $"Geen factuur gevonden met OctopusExternalId of ExternalId = '{externalId}'." },
                    new JsonSerializerOptions { WriteIndented = true });

            // ── 1. Basis ──────────────────────────────────────────────────────────
            var pdfAtt = invoice.IncomingInvoiceAttachments
                .FirstOrDefault(a => a.AttachmentType == "PDF");
            var ublAtt = invoice.IncomingInvoiceAttachments
                .FirstOrDefault(a => a.AttachmentType == "UBL");

            var basis = new
            {
                InvoiceId           = invoice.Id,
                ExternalId          = invoice.ExternalId,
                OctopusExternalId   = invoice.OctopusExternalId,
                SupplierName        = invoice.SupplierName,
                SupplierVatNr   = invoice.SupplierVatNumber,
                IssuerCompanyId = invoice.IssuerCompanyId,
                DocumentType    = invoice.DocumentType,
                IsCreditNote    = invoice.DocumentType == "CreditNote",
                Date            = invoice.Date.ToString("dd/MM/yyyy"),
                DueDate         = invoice.DueDate?.ToString("dd/MM/yyyy"),
                Price           = invoice.Price,
                PdfAttachmentId = pdfAtt?.Id,
                PdfHasContent   = pdfAtt?.Content?.Length > 0,
                PdfSize         = pdfAtt?.Content?.Length,
                UblAttachmentId = ublAtt?.Id,
                UblHasContent   = ublAtt?.Content?.Length > 0
            };

            // ── 2. UBL / DB detail lijnen ─────────────────────────────────────────
            var detailLines = invoice.IncommingInvoiceDetail.ToList();
            var detailInfo = detailLines.Select(d => new
            {
                d.Id,
                d.Description,
                d.Price,
                Type = d.Type
            }).ToList();

            var (weakUbl, weakUblReason) = CheckWeakUbl(invoice);

            // ── 3. PDF tekst ──────────────────────────────────────────────────────
            string rawPdfText = null;
            List<object> pdfLineObjects = null;
            object pdfTextInfo = null;

            if (pdfAtt?.Content != null)
            {
                rawPdfText = DebugExtractPdfText(pdfAtt.Content);
                var splitLines = rawPdfText?
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList() ?? new List<string>();

                pdfLineObjects = splitLines
                    .Take(50)
                    .Select((l, i) => (object)new { Index = i, Line = l })
                    .ToList();

                pdfTextInfo = new
                {
                    TotalLines        = splitLines.Count,
                    First50Lines      = pdfLineObjects,
                    ContainsWonnegaarde  = (rawPdfText ?? "").Contains("Wonnegaarde", StringComparison.OrdinalIgnoreCase),
                    ContainsWerf         = (rawPdfText ?? "").ToLowerInvariant().Contains("werf"),
                    ContainsChipperen    = (rawPdfText ?? "").Contains("Chipperen van gevelstenen", StringComparison.OrdinalIgnoreCase)
                };
            }
            else
            {
                pdfTextInfo = new { Error = "Geen PDF-bijlage of lege content." };
            }

            // ── 4. CombinedText bronnen ───────────────────────────────────────────
            var ublText = ublAtt?.Content != null ? DebugExtractUblText(ublAtt.Content) : null;
            var dbDetailText = detailLines
                .Select(d => d.Description)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .ToList();
            var octopusRef = string.Join(" ",
                new[] { invoice.ExternalId, invoice.SupplierName, invoice.SupplierVatNumber }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

            var combinedParts = new Dictionary<string, string>
            {
                ["UblText"]      = ublText ?? "",
                ["PdfText"]      = rawPdfText ?? "",
                ["DbDetailText"] = string.Join(" ", dbDetailText),
                ["OctopusRef"]   = octopusRef
            };

            var combinedText = string.Join(" ",
                combinedParts.Values.Where(s => !string.IsNullOrWhiteSpace(s)));
            var combinedLower = combinedText.ToLowerInvariant();

            var combinedInfo = new
            {
                TotalLength       = combinedText.Length,
                Preview1000       = combinedText.Length > 1000 ? combinedText[..1000] + "…" : combinedText,
                ContainsWonnegaarde  = combinedLower.Contains("wonnegaarde"),
                ContainsWerfWonnegaarde = combinedLower.Contains("werf wonnegaarde"),
                Sources = new
                {
                    UblHeader     = !string.IsNullOrWhiteSpace(ublText),
                    UblTextLength = ublText?.Length ?? 0,
                    DbDetailLines = dbDetailText,
                    PdfTextLength = rawPdfText?.Length ?? 0,
                    OcrText       = "(niet uitgevoerd in debug)"
                }
            };

            // ── 5. Projectmatching ────────────────────────────────────────────────
            // Kandidaten VOOR bouwheerfilter (alle actieve projecten)
            // Alle projecten (geen statusfilter) voor vergelijking voor/na bouwheerfilter
            var allProjects = await _db.Project
                .Where(p => p.ProjectName != null)
                .Select(p => new
                {
                    p.ProjectId,
                    p.ProjectName,
                    p.Street,
                    p.IssuerCompanyIdBuilder,
                    p.StatusId,
                    StatusName = p.Status != null ? p.Status.StatusName : null,
                    Gemeente   = p.PostalCode != null ? p.PostalCode.Gemeente : null
                })
                .AsNoTracking()
                .ToListAsync(ct);

            var projectsBeforeFilter = allProjects.Select(p => new
            {
                p.ProjectId,
                p.ProjectName,
                p.Street,
                City                = p.Gemeente,
                p.IssuerCompanyIdBuilder,
                p.StatusId,
                StatusName          = p.StatusName ?? p.StatusId?.ToString(),
                IsInactive          = p.StatusId == (int)BOCore.ProjectStatusType.Opgeleverd
                                   || p.StatusId == (int)BOCore.ProjectStatusType.Stopgezet,
                AllowedByFilter     = p.IssuerCompanyIdBuilder == invoice.IssuerCompanyId,
                BlockReason         = p.IssuerCompanyIdBuilder != invoice.IssuerCompanyId
                    ? $"IssuerCompanyIdBuilder={p.IssuerCompanyIdBuilder} ≠ {invoice.IssuerCompanyId}"
                    : null
            }).ToList();

            // Kandidaten NA bouwheerfilter (via IProjectMatchingService)
            var matchResults = await _projectMatcher.FindMatchesAsync(
                combinedText, invoice.IssuerCompanyId, ct);

            // Werf-context tokens
            var normalizedCombined = DebugNormalize(combinedText);
            var werfTokens = DebugExtractWerfContextTokens(normalizedCombined);

            // Wonnegaarde specifiek: zoek het project op en analyseer
            var wonnegaardeProject = allProjects.FirstOrDefault(p =>
                p.ProjectName?.Contains("Wonnegaarde", StringComparison.OrdinalIgnoreCase) == true);

            object wonnegaardeAnalysis = null;
            if (wonnegaardeProject != null)
            {
                var normName = DebugNormalize(wonnegaardeProject.ProjectName);
                var nameTokens = DebugGetSignificantWords(normName);
                var tokenHits = werfTokens
                    .Where(t => nameTokens.Contains(t)
                             || (normName.Length >= 5 && DebugContainsWholeWord(t, normName))
                             || (t.Length >= 5 && DebugContainsWholeWord(normName, t)))
                    .ToList();

                wonnegaardeAnalysis = new
                {
                    ProjectId             = wonnegaardeProject.ProjectId,
                    ProjectName           = wonnegaardeProject.ProjectName,
                    NormalizedName        = normName,
                    NameTokens            = nameTokens,
                    WerfTokens            = werfTokens,
                    TokenHits             = tokenHits,
                    WerfTokenMatchResult  = tokenHits.Any() ? $"MATCH op token(s): {string.Join(", ", tokenHits)}" : "GEEN MATCH",
                    ExactNameInText       = DebugContainsWholeWord(normalizedCombined, normName),
                    AllowedByBouwheerFilter = wonnegaardeProject.IssuerCompanyIdBuilder == invoice.IssuerCompanyId
                };
            }
            else
            {
                wonnegaardeAnalysis = new { Info = "Geen project met 'Wonnegaarde' gevonden in actieve projecten." };
            }

            // ── 6. PDF-lijnparsing ────────────────────────────────────────────────
            object pdfLineParsingInfo = null;

            if (pdfAtt?.Content != null)
            {
                var rawLines = DebugExtractPdfLinesRaw(pdfAtt.Content);
                var lineParsingResults = new List<object>();

                foreach (var (rawLine, idx) in rawLines.Select((l, i) => (l, i)))
                {
                    var skipCheck = DebugIsSkipLine(rawLine);
                    var hasNumbers = DebugHasAtLeastTwoNumbers(rawLine);
                    ProposedInvoiceLine parsed = null;
                    string failReason = null;

                    if (!skipCheck && hasNumbers)
                    {
                        (parsed, failReason) = DebugTryParseInvoiceLine(rawLine);
                    }

                    lineParsingResults.Add(new
                    {
                        Index          = idx,
                        RawLine        = rawLine,
                        IsSkipped      = skipCheck,
                        HasTwoNumbers  = hasNumbers,
                        ParseSuccess   = parsed != null,
                        FailReason     = failReason,
                        Description    = parsed?.Description,
                        Quantity       = parsed?.Quantity,
                        Unit           = parsed?.Unit,
                        UnitPrice      = parsed?.UnitPrice,
                        VatRate        = parsed?.VatRate,
                        LineTotal      = parsed?.LineTotal
                    });
                }

                var proposedLines = lineParsingResults
                    .Where(x => ((dynamic)x).ParseSuccess == true)
                    .ToList();

                var proposedSum = proposedLines
                    .Sum(x => (decimal?)((dynamic)x).LineTotal ?? 0m);

                // Ook plain-text fallback uitvoeren
                var plainTextLines = DebugExtractLinesPlainText(pdfAtt.Content);
                var plainTextSum = plainTextLines.Sum(l => l.LineTotal ?? 0);

                pdfLineParsingInfo = new
                {
                    Method1_PositionBased = new
                    {
                        TotalRawLines      = rawLines.Count,
                        LineParsingResults = lineParsingResults,
                        ProposedCount      = proposedLines.Count,
                        ProposedSum        = proposedSum,
                        InvoiceTotal       = invoice.Price,
                        Difference         = Math.Abs(proposedSum - invoice.Price)
                    },
                    Method2_PlainTextFallback = new
                    {
                        ProposedCount = plainTextLines.Count,
                        ProposedSum   = plainTextSum,
                        InvoiceTotal  = invoice.Price,
                        Difference    = Math.Abs(plainTextSum - invoice.Price),
                        Lines         = plainTextLines.Select(l => new
                        {
                            l.Description,
                            l.Quantity,
                            l.Unit,
                            l.UnitPrice,
                            l.VatRate,
                            l.LineTotal
                        })
                    }
                };
            }
            else
            {
                pdfLineParsingInfo = new { Error = "Geen PDF aanwezig." };
            }

            // ── 7. Opgeslagen enrichment resultaat ────────────────────────────────
            var enrichment = await _db.IncomingInvoiceEnrichment
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IncomingInvoiceId == invoice.Id, ct);

            object enrichmentResult = null;
            if (enrichment != null)
            {
                List<object> proposedPdfLines = null;
                if (!string.IsNullOrWhiteSpace(enrichment.ExtractionResultJson))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(enrichment.ExtractionResultJson);
                        if (doc.RootElement.TryGetProperty("ProposedPdfLines", out var linesProp)
                            && linesProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            proposedPdfLines = JsonSerializer
                                .Deserialize<List<ProposedInvoiceLine>>(linesProp.GetRawText())
                                ?.Select(l => (object)new
                                {
                                    l.Description,
                                    l.Quantity,
                                    l.Unit,
                                    l.UnitPrice,
                                    l.VatRate,
                                    l.LineTotal,
                                    l.Source,
                                    l.Confidence
                                })
                                .ToList();
                        }
                    }
                    catch { }
                }

                enrichmentResult = new
                {
                    EnrichedAt             = enrichment.EnrichedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    SelectedProjectId      = enrichment.SuggestedProjectId,
                    SelectedProjectName    = enrichment.SuggestedProjectName,
                    SelectedProjectScore   = enrichment.SuggestedProjectScore,
                    SelectedProjectConfidence = enrichment.SuggestedProjectConfidence,
                    SelectedProjectMatchReason = enrichment.SuggestedProjectMatchReason,
                    SelectedContractId     = enrichment.SuggestedContractId,
                    SuggestedAccount       = enrichment.SuggestedAccountingCode,
                    ReasonFlags            = enrichment.ReasonFlags,
                    ProposedPdfLines       = proposedPdfLines,
                    AltProjectCandidatesJson = enrichment.AltProjectCandidatesJson,
                    ExtractionResultJsonPreview = enrichment.ExtractionResultJson?.Length > 2000
                        ? enrichment.ExtractionResultJson[..2000] + "…(afgekapt)"
                        : enrichment.ExtractionResultJson
                };
            }
            else
            {
                enrichmentResult = new { Info = "Nog geen enrichment opgeslagen — voer eerst Verrijken uit." };
            }

            var warnings = await _db.IncomingInvoiceWarning
                .AsNoTracking()
                .Where(w => w.IncomingInvoiceId == invoice.Id)
                .Select(w => new { w.WarningCode, w.WarningMessage, w.Severity, w.Source, w.IsResolved })
                .ToListAsync(ct);

            // ── Samenvoegen ───────────────────────────────────────────────────────
            var result = new
            {
                GeneratedAt           = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                Basis                 = basis,
                UblDbDetails          = new
                {
                    Lines         = detailInfo,
                    HasWeakUblLines  = weakUbl,
                    WeakUblReason    = weakUblReason
                },
                PdfText               = pdfTextInfo,
                CombinedText          = combinedInfo,
                ProjectMatching       = new
                {
                    AllProjectsBeforeFilter = projectsBeforeFilter,
                    TotalBeforeFilter   = projectsBeforeFilter.Count,
                    TotalAfterFilter    = projectsBeforeFilter.Count(p => p.AllowedByFilter),
                    WerfContextTokens   = werfTokens,
                    NormalizedCombinedPreview300 = normalizedCombined.Length > 300
                        ? normalizedCombined[..300] + "…" : normalizedCombined,
                    MatchResults        = matchResults.Select(r => new
                    {
                        r.ProjectId,
                        r.ProjectName,
                        r.Score,
                        r.ConfidenceLevel,
                        r.MatchReason,
                        r.MatchedFields
                    }),
                    WonnegaardeAnalysis = wonnegaardeAnalysis
                },
                PdfLineParsing        = pdfLineParsingInfo,
                StoredEnrichment      = enrichmentResult,
                Warnings              = warnings
            };

            return Json(result, new JsonSerializerOptions { WriteIndented = true });
        }

        // ── Gekopieerde helpers (zelfstandige debug-implementaties) ───────────────

        private static (bool isWeak, string reason) CheckWeakUbl(IncommingInvoices invoice)
        {
            var lines = invoice.IncommingInvoiceDetail?.ToList();
            if (lines == null || !lines.Any())
                return (true, "Geen IncommingInvoiceDetail regels.");
            if (lines.Count > 1)
                return (false, $"{lines.Count} regels aanwezig — niet zwak.");

            var desc = lines[0].Description?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(desc))
                return (true, "Één regel met lege omschrijving.");
            if (string.Equals(desc, "Invoice Line", StringComparison.OrdinalIgnoreCase)
                || string.Equals(desc, "InvoiceLine", StringComparison.OrdinalIgnoreCase)
                || string.Equals(desc, "Factuurlijn", StringComparison.OrdinalIgnoreCase))
                return (true, $"Één generieke lijn: '{desc}'.");
            if (lines[0].Price.HasValue && invoice.Price > 0
                && Math.Abs(lines[0].Price.Value - invoice.Price) < 0.05m)
                return (true, $"Lijnbedrag ({lines[0].Price}) ≈ factuurtotaal ({invoice.Price}) — inhoudsloos.");

            return (false, $"Één zinvolle regel: '{desc}'.");
        }

        private static string DebugExtractPdfText(byte[] pdfBytes)
        {
            try
            {
                using var doc = PdfDocument.Open(pdfBytes);
                var sb = new StringBuilder();
                foreach (var page in doc.GetPages())
                    sb.AppendLine(string.Join(" ", page.GetWords().Select(w => w.Text)));
                return sb.ToString();
            }
            catch (Exception ex) { return $"[FOUT: {ex.Message}]"; }
        }

        private static string DebugExtractUblText(byte[] xmlContent)
        {
            try
            {
                var xml = Encoding.UTF8.GetString(xmlContent);
                var xdoc = System.Xml.Linq.XDocument.Parse(xml);
                System.Xml.Linq.XNamespace cbc =
                    "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
                var parts = xdoc.Descendants()
                    .Where(e => !e.HasElements && !string.IsNullOrWhiteSpace(e.Value))
                    .Select(e => e.Value.Trim())
                    .Where(v => v.Length > 1)
                    .Take(100)
                    .ToList();
                return string.Join(" ", parts);
            }
            catch (Exception ex) { return $"[FOUT: {ex.Message}]"; }
        }

        private static List<string> DebugExtractPdfLinesRaw(byte[] pdfBytes)
        {
            var result = new List<string>();
            try
            {
                using var doc = PdfDocument.Open(pdfBytes);
                foreach (var page in doc.GetPages())
                {
                    var wordsByLine = page.GetWords()
                        .GroupBy(w => (int)(w.BoundingBox.Bottom / 4) * 4)
                        .OrderByDescending(g => g.Key);

                    foreach (var grp in wordsByLine)
                    {
                        var lineText = string.Join(" ",
                            grp.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                        if (!string.IsNullOrWhiteSpace(lineText))
                            result.Add(lineText);
                    }
                }
            }
            catch (Exception ex) { result.Add($"[FOUT: {ex.Message}]"); }
            return result;
        }

        private static bool DebugIsSkipLine(string line)
        {
            var lower = line.ToLowerInvariant().Trim();
            if (lower.Length < 5) return true;
            if (lower.StartsWith("totaal") || lower.StartsWith("total")
                || lower.StartsWith("subtotaal") || lower.StartsWith("subtotal")
                || lower.StartsWith("grand total") || lower.StartsWith("montant total")
                || lower.StartsWith("bedrag")) return true;
            if (lower.StartsWith("btw") || lower.StartsWith("tva")
                || lower.StartsWith("vat ") || lower.StartsWith("tax ")) return true;
            if (lower is "beschrijving" or "omschrijving" or "description"
                      or "designation" or "libelle" or "artikel" or "item"
                      or "ref" or "reference") return true;
            if (lower.Contains("excl") || lower.Contains("incl")
                || lower.Contains("hors tva") || lower.Contains("tvac")
                || lower.Contains("htva")) return true;
            if (lower.StartsWith("pagina") || lower.StartsWith("page ")
                || lower.StartsWith("blad ")) return true;
            if (Regex.IsMatch(lower, @"^\d{1,2}[/\-\.]\d{1,2}[/\-\.]\d{2,4}$")) return true;
            return false;
        }

        private static bool DebugHasAtLeastTwoNumbers(string line)
        {
            var matches = Regex.Matches(line, @"\d[\d\s.,]*\d|\b\d\b");
            return matches.Count >= 2;
        }

        private static decimal? DebugParseBelgian(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            if (s.Contains(','))
            {
                var cleaned = Regex.Replace(s, @"[.\s]", "").Replace(",", ".");
                return decimal.TryParse(cleaned,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
            }
            else
            {
                var cleaned = s.Replace(" ", "");
                return decimal.TryParse(cleaned,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
            }
        }

        private static readonly Regex DebugUnitPattern =
            new(@"(?<![a-z0-9])(m²|m2|m³|m3|ml|hl|ltr?|kg|ton|stk?|uur|dag|wk|ls|ft|m)(?![a-z0-9])",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const string DebugNumPattern =
            @"(\d{1,3}(?:[.\s]\d{3})*(?:,\d{1,5})?|\d+(?:,\d{1,5})?)";

        private static (ProposedInvoiceLine result, string failReason) DebugTryParseInvoiceLine(string rawLine)
        {
            var line = rawLine.Trim();

            var endMatch = Regex.Match(line, DebugNumPattern + @"\s*$");
            if (!endMatch.Success) return (null, "Eindigt niet op een getal.");

            var lineTotal = DebugParseBelgian(endMatch.Groups[1].Value);
            if (!lineTotal.HasValue || lineTotal <= 0)
                return (null, $"LineTotal ongeldig: '{endMatch.Groups[1].Value}'");
            var rest = line[..endMatch.Index].TrimEnd();

            decimal? vatRate = null;
            var vatMatch = Regex.Match(rest, @"(\d+(?:,\d+)?)\s*%\s*$");
            if (vatMatch.Success)
            {
                vatRate = DebugParseBelgian(vatMatch.Groups[1].Value);
                rest = rest[..vatMatch.Index].TrimEnd();
            }

            decimal? unitPrice = null;
            if (vatRate.HasValue)
            {
                var priceMatch = Regex.Match(rest, DebugNumPattern + @"\s*$");
                if (priceMatch.Success)
                {
                    unitPrice = DebugParseBelgian(priceMatch.Groups[1].Value);
                    rest = rest[..priceMatch.Index].TrimEnd();
                }
            }

            string unit = null;
            var unitMatch = DebugUnitPattern.Match(rest);
            if (unitMatch.Success && unitMatch.Index >= rest.Length - 15)
            {
                unit = unitMatch.Value.Trim();
                rest = rest[..unitMatch.Index].TrimEnd();
            }

            decimal? quantity = null;
            var qtyMatch = Regex.Match(rest, DebugNumPattern + @"\s*$");
            if (qtyMatch.Success)
            {
                var qtyVal = DebugParseBelgian(qtyMatch.Groups[1].Value);
                if (qtyVal is > 0 and < 1_000_000)
                {
                    quantity = qtyVal;
                    rest = rest[..qtyMatch.Index].TrimEnd();
                }
            }

            var desc = rest.TrimEnd(':', ' ', '-').Trim();
            if (string.IsNullOrWhiteSpace(desc) || desc.Length < 4)
                return (null, $"Beschrijving te kort of leeg: '{desc}'");

            if (unitPrice == null && quantity is > 0)
                unitPrice = Math.Round(lineTotal.Value / quantity.Value, 4);

            return (new ProposedInvoiceLine
            {
                Description = desc,
                Quantity    = quantity,
                Unit        = unit,
                UnitPrice   = unitPrice,
                VatRate     = vatRate,
                LineTotal   = lineTotal,
                Source      = "PdfText",
                Confidence  = "Medium"
            }, null);
        }

        private static List<ProposedInvoiceLine> DebugExtractLinesPlainText(byte[] pdfBytes)
        {
            var allText = DebugExtractPdfText(pdfBytes);
            var parsed = new List<ProposedInvoiceLine>();
            if (string.IsNullOrWhiteSpace(allText)) return parsed;

            foreach (var rawLine in allText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = rawLine.Trim();
                if (DebugIsSkipLine(trimmed)) continue;
                if (!DebugHasAtLeastTwoNumbers(trimmed)) continue;
                var (line, _) = DebugTryParseInvoiceLine(trimmed);
                if (line != null)
                {
                    line.Source = "PdfText";
                    parsed.Add(line);
                }
            }
            return parsed;
        }

        private static string DebugNormalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.ToLowerInvariant();
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            s = sb.ToString().Normalize(NormalizationForm.FormC);
            s = Regex.Replace(s, @"[-/_\\]", " ");
            s = Regex.Replace(s, @"[^\w\s]", " ");
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s;
        }

        private static bool DebugContainsWholeWord(string text, string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length < 3) return false;
            return Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b");
        }

        private static List<string> DebugGetSignificantWords(string normalizedName)
        {
            return normalizedName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 4 && !DebugIsStopWord(w))
                .ToList();
        }

        private static bool DebugIsStopWord(string word) =>
            word is "voor" or "van" or "naar" or "met" or "zonder" or "door"
                 or "over" or "onder" or "boven" or "zijn" or "hebben"
                 or "worden" or "kunnen" or "moet" or "project" or "werf"
                 or "woning" or "woningen" or "appartement" or "appartementen"
                 or "villa" or "huis" or "gebouw" or "kantoor" or "kmo"
                 or "nieuwbouw" or "renovatie" or "verbouwing"
                 or "straat" or "laan" or "plein" or "square" or "avenue"
                 or "chantier" or "projet" or "maison" or "immeuble" or "batiment"
                 or "pour" or "avec" or "dans" or "sur" or "sous" or "par" or "lieu"
                 or "site" or "location" or "building" or "house" or "office";

        private static List<string> DebugExtractWerfContextTokens(string normalizedText)
        {
            if (string.IsNullOrWhiteSpace(normalizedText)) return new List<string>();

            const string kwPattern =
                @"(?:werf|bouwplaats|locatie|dossier|chantier|projet|lieu|site|project|location)\s+" +
                @"([a-z0-9][a-z0-9 \-]{2,60})";

            var tokens = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(normalizedText, kwPattern))
            {
                var ctx = m.Groups[1].Value;
                var stopIdx = ctx.IndexOfAny(new[] { '\n', '\r', ',', '/' });
                if (stopIdx > 0) ctx = ctx[..stopIdx];
                ctx = ctx.Trim().TrimEnd('.', ';', ' ');

                foreach (var part in Regex.Split(ctx, @"[\s,\-/\\]+"))
                {
                    var t = part.Trim();
                    if (t.Length >= 3 && !DebugIsStopWord(t))
                        tokens.Add(t);
                }
            }
            return tokens.ToList();
        }
    }
}
