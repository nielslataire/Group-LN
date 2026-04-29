using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore.IncomingInvoices
{
    /// <summary>
    /// Scoring-gebaseerde projectmatchingservice.
    /// Normaliseert tekst, herkent projectnamen/adressen/aliassen en berekent een score per project.
    ///
    /// Scoremodel:
    ///   +90  token na werf/chantier/site/... sleutelwoord (NL/FR/EN) matcht projectnaam-token
    ///   +25  exacte projectnaam in tekst
    ///   +20  straat + gemeente beide aanwezig
    ///   +15  alias-match
    ///   +10  alle significante woorden uit projectnaam aanwezig
    ///    +5  fuzzy gedeeltelijke projectnaam
    ///
    /// Drempelwaarden:
    ///   >= 80 → High confidence
    ///   50–79 → Medium confidence
    ///    < 50 → Low confidence (enkel suggestie, geen automatische koppeling)
    ///
    /// Hard filter: enkel projecten waar IssuerCompanyIdBuilder == issuerCompanyId.
    /// </summary>
    public class ProjectMatchingService : IProjectMatchingService
    {
        private readonly cpmRunningContext _db;
        private readonly ILogger<ProjectMatchingService> _logger;

        private const int MinWordLength = 4;
        private const int MinProjectNameLength = 5;

        public ProjectMatchingService(cpmRunningContext db, ILogger<ProjectMatchingService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Minimale score voor automatische selectie (auto-fill)
        private const int MinAutoSelectScore = 50;

        // Minimale score voor een geblokkeerde match om toch zichtbaar te zijn
        private const int MinBlockedVisibleScore = 80;

        public async Task<IReadOnlyList<ProjectMatchResult>> FindMatchesAsync(
            string combinedText,
            int? issuerCompanyId = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(combinedText))
                return Array.Empty<ProjectMatchResult>();

            var normalizedText = Normalize(combinedText);

            _logger.LogDebug(
                "Projectmatching start — zoektekst (eerste 300 tekens): '{Preview}'",
                normalizedText.Length > 300 ? normalizedText[..300] + "…" : normalizedText);

            // Extraheer tokens na werf-sleutelwoorden (NL/FR/EN)
            var werfTokens = ExtractWerfContextTokens(normalizedText);

            if (werfTokens.Count > 0)
                _logger.LogDebug("Werf-context tokens gevonden: [{Tokens}]", string.Join(", ", werfTokens));
            else
                _logger.LogDebug("Geen werf-context tokens in zoektekst (lengte={Len}).", normalizedText.Length);

            // Laad ALLE projecten — geen enkel filter in de query.
            // Matching (stap 1) en businessfiltering (stap 2) worden strikt gescheiden:
            // een sterke inhoudelijke match mag nooit verloren gaan door een businessregel.
            var totalInDb = await _db.Project.CountAsync(ct);

            var projects = await _db.Project
                .Where(p => p.ProjectName != null)
                .Select(p => new ProjectCandidate
                {
                    ProjectId              = p.ProjectId,
                    ProjectName            = p.ProjectName,
                    Street                 = p.Street,
                    Gemeente               = p.PostalCode != null ? p.PostalCode.Gemeente : null,
                    PostalCode             = p.PostalCode != null ? p.PostalCode.Postcode : null,
                    StatusId               = p.StatusId,
                    StatusName             = p.Status != null ? p.Status.StatusName : null,
                    IssuerCompanyIdBuilder = p.IssuerCompanyIdBuilder
                })
                .AsNoTracking()
                .ToListAsync(ct);

            _logger.LogDebug(
                "Projectmatching: {Used} project(en) geladen voor matching (totaal in DB: {Total}){Filter}. " +
                "Statusverdeling: {Dist}",
                projects.Count, totalInDb,
                issuerCompanyId.HasValue ? $" — bouwheerfilter op {issuerCompanyId.Value}" : " — geen bouwheerfilter",
                string.Join(", ", projects
                    .GroupBy(p => p.StatusName ?? $"status#{p.StatusId}")
                    .Select(g => $"{g.Key}:{g.Count()}")));

            // Laad aliassen
            var aliases = await _db.InvoiceProjectAlias
                .Where(a => a.IsActive)
                .Select(a => new { a.ProjectId, a.Alias, a.AliasType })
                .AsNoTracking()
                .ToListAsync(ct);

            var aliasByProject = aliases
                .GroupBy(a => a.ProjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var results = new List<ProjectMatchResult>();

            foreach (var p in projects)
            {
                if (p.ProjectName.Length < MinProjectNameLength) continue;

                var normalizedName = Normalize(p.ProjectName);
                if (string.IsNullOrWhiteSpace(normalizedName)) continue;

                var nameTokens = GetSignificantWords(normalizedName);
                var score = 0;
                var reasons = new List<string>();
                var matchedFields = new List<string>();

                // ── 0. Werf-keyword token match (+90) ──────────────────────────
                // Structurele detectie: token na NL/FR/EN werf-sleutelwoord matcht
                // een significante token uit de projectnaam.
                // NIET alleen volledige string — ALTIJD ook losse tokens matchen.
                bool werfMatchFound = false;
                if (werfTokens.Count > 0 && nameTokens.Count > 0)
                {
                    foreach (var token in werfTokens)
                    {
                        bool hit = nameTokens.Contains(token)
                            || (normalizedName.Length >= 5 && ContainsWholeWord(token, normalizedName))
                            || (token.Length >= 5 && ContainsWholeWord(normalizedName, token));

                        if (hit)
                        {
                            score += 90;
                            reasons.Add($"token '{token}' na werf-sleutelwoord");
                            matchedFields.Add("WerfContext");
                            werfMatchFound = true;
                            _logger.LogDebug(
                                "  '{Name}': werf-token '{Token}' → +90", p.ProjectName, token);
                            break;
                        }
                    }
                }

                // ── 1. Exacte projectnaam in tekst (+25) ───────────────────────
                if (!werfMatchFound && ContainsWholeWord(normalizedText, normalizedName))
                {
                    score += 25;
                    reasons.Add("exacte projectnaam");
                    matchedFields.Add("Name");
                    _logger.LogDebug("  '{Name}': exacte naam → +25", p.ProjectName);
                }
                else if (!werfMatchFound)
                {
                    // ── 2. Alle significante woorden aanwezig (+10) ─────────────
                    if (nameTokens.Count >= 2 && nameTokens.All(w => ContainsWholeWord(normalizedText, w)))
                    {
                        score += 10;
                        reasons.Add("alle trefwoorden projectnaam");
                        matchedFields.Add("Name");
                    }
                    else if (nameTokens.Count >= 1)
                    {
                        var matchedWord = nameTokens.FirstOrDefault(
                            w => w.Length >= 6 && ContainsWholeWord(normalizedText, w));
                        if (matchedWord != null)
                        {
                            score += 5;
                            reasons.Add($"gedeeltelijke projectnaam ('{matchedWord}')");
                            matchedFields.Add("Name");
                        }
                    }
                }

                // ── 3. Straat + gemeente (+20) ──────────────────────────────────
                score += ScoreAddress(normalizedText, p.Street, p.Gemeente, reasons, matchedFields);

                // ── 4. Aliassen (+15) ───────────────────────────────────────────
                if (aliasByProject.TryGetValue(p.ProjectId, out var projectAliases))
                {
                    foreach (var alias in projectAliases)
                    {
                        var normalizedAlias = Normalize(alias.Alias);
                        if (string.IsNullOrWhiteSpace(normalizedAlias)) continue;

                        if (ContainsWholeWord(normalizedText, normalizedAlias))
                        {
                            score += 15;
                            reasons.Add($"alias '{alias.Alias}'");
                            matchedFields.Add("Alias");
                            break;
                        }
                    }
                }

                if (score <= 0) continue;

                // ── Stap 2: businessfilter (bouwheer) — NADAT score bepaald is ──
                // Matching en filtering zijn gescheiden: inhoudelijk sterke matches
                // worden altijd bewaard, ook als een businessregel ze blokkeert.
                bool allowedByFilter = true;
                string blockReason = null;

                if (issuerCompanyId.HasValue)
                {
                    if (!p.IssuerCompanyIdBuilder.HasValue)
                    {
                        allowedByFilter = false;
                        blockReason = "MissingIssuerCompanyRelation";
                    }
                    else if (p.IssuerCompanyIdBuilder.Value != issuerCompanyId.Value)
                    {
                        allowedByFilter = false;
                        blockReason = "DifferentIssuerCompany";
                    }
                }

                // Geblokkeerde matches enkel bewaren als ze inhoudelijk sterk zijn
                if (!allowedByFilter && score < MinBlockedVisibleScore) continue;

                var isInactive = p.StatusId == (int)ProjectStatusType.Opgeleverd
                              || p.StatusId == (int)ProjectStatusType.Stopgezet;

                _logger.LogDebug(
                    "  Kandidaat '{Name}' (status={Status}{Inactive}): score={Score}, toegestaan={Allowed}{Block}, reden={Reason}",
                    p.ProjectName, p.StatusName ?? p.StatusId?.ToString() ?? "?",
                    isInactive ? " — INACTIEF" : "",
                    score, allowedByFilter,
                    blockReason != null ? $" [{blockReason}]" : "",
                    string.Join(", ", reasons));

                results.Add(new ProjectMatchResult
                {
                    ProjectId              = p.ProjectId,
                    ProjectName            = p.ProjectName,
                    Score                  = score,
                    ConfidenceLevel        = ScoreToConfidence(score),
                    MatchReason            = string.Join(", ", reasons),
                    MatchedFields          = string.Join(",", matchedFields.Distinct()),
                    Source                 = "Combined",
                    Street                 = p.Street,
                    City                   = p.Gemeente,
                    ProjectStatusId        = p.StatusId,
                    ProjectStatusLabel     = p.StatusName ?? (p.StatusId.HasValue
                        ? ((ProjectStatusType)p.StatusId.Value).ToString()
                        : null),
                    IsInactiveProject      = isInactive,
                    AllowedByFilter        = allowedByFilter,
                    BlockedByBusinessRule  = !allowedByFilter,
                    BlockReason            = blockReason
                });
            }

            // Toegestane matches eerst, daarna geblokkeerde — beide gerangschikt op score.
            // Zo staat de beste toegestane match altijd bovenaan, maar blijven sterke
            // geblokkeerde matches zichtbaar voor de gebruiker.
            var sorted = results
                .OrderByDescending(r => r.AllowedByFilter ? 1 : 0)
                .ThenByDescending(r => r.Score)
                .Take(10)
                .ToList();

            var allowedCount  = sorted.Count(r => r.AllowedByFilter);
            var blockedCount  = sorted.Count(r => r.BlockedByBusinessRule);

            if (sorted.Any())
                _logger.LogInformation(
                    "Projectmatching: {Count} kandidaat/kandidaten ({Allowed} toegestaan, {Blocked} geblokkeerd). Top-3: {Top}",
                    sorted.Count, allowedCount, blockedCount,
                    string.Join(" | ", sorted.Take(3).Select(r =>
                        $"'{r.ProjectName}' score={r.Score}{(r.BlockedByBusinessRule ? $" [GEBLOKKEERD:{r.BlockReason}]" : "")} ({r.MatchReason})")));
            else
                _logger.LogInformation("Projectmatching: geen kandidaten gevonden in zoektekst.");

            return sorted;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private const double FuzzyStreetThreshold = 0.88;

        private int ScoreAddress(
            string normalizedText,
            string street,
            string gemeente,
            List<string> reasons,
            List<string> matchedFields)
        {
            if (string.IsNullOrWhiteSpace(street) || street.Length < 5) return 0;
            if (string.IsNullOrWhiteSpace(gemeente)) return 0;

            // Straatnaam normaliseren: huisnummer afknippen
            var normStreet = Normalize(street);
            var streetName = Regex.Replace(normStreet, @"\s+\d+.*$", "").Trim();
            if (streetName.Length < 4) return 0;

            var normGemeente = Normalize(gemeente);
            var hasGemeente  = ContainsWholeWord(normalizedText, normGemeente);

            // ── Exacte straatmatch ────────────────────────────────────────────
            if (ContainsWholeWord(normalizedText, streetName))
            {
                if (hasGemeente)
                {
                    reasons.Add($"straat '{street}' + gemeente '{gemeente}'");
                    matchedFields.Add("Street");
                    matchedFields.Add("City");
                    _logger.LogDebug(
                        "  Adres: exacte straatmatch '{Street}' + gemeente '{City}' → +20",
                        street, gemeente);
                    return 20;
                }
                reasons.Add($"straat '{street}'");
                matchedFields.Add("Street");
                return 5;
            }

            // ── Fuzzy straatmatch ─────────────────────────────────────────────
            // Vergelijk de projectstraat met elk woord-n-gram in de factuurttekst
            // ter lengte van de projectstraatnaam. Zo worden kleine schrijfvarianten
            // ("Kuiperstraat" vs "Kuipersstraat") toch herkend.
            var textWords   = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var streetWords = streetName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var windowSize  = streetWords.Length;

            double bestSimilarity = 0;
            for (int i = 0; i <= textWords.Length - windowSize; i++)
            {
                var window    = string.Join(" ", textWords, i, windowSize);
                var sim       = StreetSimilarity(streetName, window);
                if (sim > bestSimilarity) bestSimilarity = sim;
            }

            _logger.LogDebug(
                "  Adres fuzzy: projectstraat='{StreetNorm}', beste similarity={Sim:F3}, gemeente={City}",
                streetName, bestSimilarity, hasGemeente ? gemeente : "—");

            if (bestSimilarity >= FuzzyStreetThreshold && hasGemeente)
            {
                reasons.Add($"straat fuzzy match + gemeente '{gemeente}' (similarity {bestSimilarity:F2})");
                matchedFields.Add("Street");
                matchedFields.Add("City");
                _logger.LogDebug(
                    "  Adres: fuzzy straatmatch (sim={Sim:F3}) + gemeente → +40",
                    bestSimilarity);
                return 40;
            }

            return 0;
        }

        /// <summary>
        /// Similarity tussen twee genormaliseerde straatnamen op basis van
        /// Levenshtein-distance: sim = 1 − (editDistance / maxLen).
        /// Geeft 1.0 bij exacte match, 0.0 bij volledig verschillende strings.
        /// </summary>
        private static double StreetSimilarity(string a, string b)
        {
            if (a == b) return 1.0;
            if (a.Length == 0 || b.Length == 0) return 0.0;

            int maxLen = Math.Max(a.Length, b.Length);
            int dist   = LevenshteinDistance(a, b);
            return 1.0 - (double)dist / maxLen;
        }

        private static int LevenshteinDistance(string s, string t)
        {
            int m = s.Length, n = t.Length;
            var dp = new int[m + 1, n + 1];

            for (int i = 0; i <= m; i++) dp[i, 0] = i;
            for (int j = 0; j <= n; j++) dp[0, j] = j;

            for (int i = 1; i <= m; i++)
            for (int j = 1; j <= n; j++)
                dp[i, j] = s[i - 1] == t[j - 1]
                    ? dp[i - 1, j - 1]
                    : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));

            return dp[m, n];
        }

        internal static string Normalize(string input)
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

        private static bool ContainsWholeWord(string text, string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length < 3) return false;
            return Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b");
        }

        private static List<string> GetSignificantWords(string normalizedName)
        {
            return normalizedName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= MinWordLength && !IsStopWord(w))
                .ToList();
        }

        private static bool IsStopWord(string word) =>
            word is
                // NL stopwoorden
                "voor" or "van" or "naar" or "met" or "zonder" or "door"
             or "over" or "onder" or "boven" or "zijn" or "hebben"
             or "worden" or "kunnen" or "moet" or "project" or "werf"
             or "woning" or "woningen" or "appartement" or "appartementen"
             or "villa" or "huis" or "gebouw" or "kantoor" or "kmo"
             or "nieuwbouw" or "renovatie" or "verbouwing"
             or "straat" or "laan" or "plein" or "square" or "avenue"
                // FR stopwoorden
             or "chantier" or "projet" or "maison" or "immeuble" or "batiment"
             or "pour" or "avec" or "dans" or "sur" or "sous" or "par" or "lieu"
                // EN stopwoorden
             or "site" or "location" or "building" or "house" or "office";

        /// <summary>
        /// Extraheert tokens na werf-sleutelwoorden (NL/FR/EN).
        /// Voorbeeld: "werf Wonnegaarde - Otegem" → tokens: ["wonnegaarde", "otegem"]
        /// Voorbeeld: "chantier Wonnegaarde"      → tokens: ["wonnegaarde"]
        ///
        /// Splitst context (tot aan newline/komma/slash) in losse tokens op
        /// spatie/streepje/slash zodat ALTIJD losse woorden gematcht worden.
        /// </summary>
        private static IReadOnlyList<string> ExtractWerfContextTokens(string normalizedText)
        {
            if (string.IsNullOrWhiteSpace(normalizedText))
                return Array.Empty<string>();

            // NL: werf, bouwplaats, locatie, dossier
            // FR: chantier, projet, lieu
            // EN: site, project, location
            const string kwPattern =
                @"(?:werf|bouwplaats|locatie|dossier|chantier|projet|lieu|site|project|location)\s+" +
                @"([a-z0-9][a-z0-9 \-]{2,60})";

            var tokens = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match m in Regex.Matches(normalizedText, kwPattern))
            {
                // Neem tekst tot aan newline, komma of slash
                var ctx = m.Groups[1].Value;
                var stopIdx = ctx.IndexOfAny(new[] { '\n', '\r', ',', '/' });
                if (stopIdx > 0) ctx = ctx[..stopIdx];
                ctx = ctx.Trim().TrimEnd('.', ';', ' ');

                // Split op spatie, streepje, slash, komma → losse tokens
                foreach (var part in Regex.Split(ctx, @"[\s,\-/\\]+"))
                {
                    var t = part.Trim();
                    if (t.Length >= 3 && !IsStopWord(t))
                        tokens.Add(t);
                }
            }

            return tokens.ToList();
        }

        private static string ScoreToConfidence(int score) =>
            score >= 80 ? "High" : score >= 50 ? "Medium" : "Low";

        private sealed class ProjectCandidate
        {
            public int ProjectId { get; set; }
            public string ProjectName { get; set; }
            public string Street { get; set; }
            public string Gemeente { get; set; }
            public string PostalCode { get; set; }
            public int? StatusId { get; set; }
            public string StatusName { get; set; }
            public int? IssuerCompanyIdBuilder { get; set; }
        }
    }
}
