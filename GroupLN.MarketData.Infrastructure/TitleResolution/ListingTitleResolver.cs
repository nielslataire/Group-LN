using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Enums;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.TitleResolution;

public static class ListingTitleResolver
{
    // ── Regex ─────────────────────────────────────────────────────────────────

    private static readonly Regex StrongTagRegex = new(
        @"<strong[^>]*>(.*?)</strong>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex HtmlTagRegex = new(
        @"<[^>]+>",
        RegexOptions.Compiled);

    private static readonly Regex MultiSpaceRegex = new(
        @"\s{2,}",
        RegexOptions.Compiled);

    // Stopt bij leesteken, dubbele spatie of einde (lazy: min. tekens)
    private static readonly Regex ResidentieInlineRegex = new(
        @"(?i)\b(?:Residentie|Res\.)\s+([A-Za-zÀ-ÿ][A-Za-zÀ-ÿ0-9\-' ]{1,40}?)(?=\s*[:\-–|,.]|\s{2,}|$)",
        RegexOptions.Compiled);

    private static readonly Regex YearInBracketsRegex = new(
        @"\s*\(\d{4}\)", RegexOptions.Compiled);

    // Verwijdert bekende generieke suffixen na scheidingsteken (bv. "BOEVRIE-Appartementen")
    private static readonly Regex GenericSuffixAfterSeparatorRegex = new(
        @"\s*[-–|:,]\s*(?:Appartementen|Woningen|Huizen|Project|Nieuwbouw)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Verwijdert marketing-suffix als linkerdeel ≥2 woorden heeft of begint met projectkeyword.
    // Voorbeeld: "Residentie De Gendarmerie biedt naast" → "Residentie De Gendarmerie"
    private static readonly Regex MarketingSuffixRegex = new(
        @"\s+(?:biedt(?:\s+\w+)?|omvat|bestaat\s+uit|nabij|in\s+het\s+hart\s+van|energiezuinige?\b|modern\b|stijlvol\b|exclusief\b).*$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly string[] MarketingSuffixProjectPrefixes =
        ["residentie", "res.", "hof", "park", "villa", "kaai", "green", "linum", "mona"];

    // Patronen voor scan van volledige description-tekst
    private static readonly (Regex Pattern, string Label)[] DescriptionPatterns =
    [
        (new Regex(@"(?i)\b(Residentie\s+[A-Za-zÀ-ÿ0-9][A-Za-zÀ-ÿ0-9\-']*(?:\s+[A-Za-zÀ-ÿ0-9][A-Za-zÀ-ÿ0-9\-']*){0,3})",
            RegexOptions.Compiled), "Residentie"),
        (new Regex(@"(?i)\b(Woonproject\s+[A-Za-zÀ-ÿ0-9][A-Za-zÀ-ÿ0-9\-']*(?:\s+[A-Za-zÀ-ÿ0-9][A-Za-zÀ-ÿ0-9\-']*){0,4})",
            RegexOptions.Compiled), "Woonproject"),
        (new Regex(@"(?i)\b(Nieuwbouwproject\s+[A-Za-zÀ-ÿ0-9][A-Za-zÀ-ÿ0-9\-']*(?:\s+[A-Za-zÀ-ÿ0-9][A-Za-zÀ-ÿ0-9\-']*){0,3})",
            RegexOptions.Compiled), "Nieuwbouwproject"),
        (new Regex(@"(?i)\b(Verkaveling\s+[A-Za-zÀ-ÿ0-9][A-Za-zÀ-ÿ0-9\-']*(?:\s+[A-Za-zÀ-ÿ0-9][A-Za-zÀ-ÿ0-9\-']*){0,3})",
            RegexOptions.Compiled), "Verkaveling"),
        (new Regex(@"(?i)\b(Domein\s+[A-Za-zÀ-ÿ0-9][A-Za-zÀ-ÿ0-9\-']*(?:\s+[A-Za-zÀ-ÿ0-9][A-Za-zÀ-ÿ0-9\-']*){0,3})",
            RegexOptions.Compiled), "Domein"),
    ];

    // Enkelvoudige generieke woorden — mogen nooit als projectnaam gebruikt worden
    private static readonly HashSet<string> GenericWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Nieuwbouwproject", "Appartementen", "Woningen", "Huizen", "Huis",
        "Appartement", "Woning", "Te koop", "Prachtig", "Modern", "Project",
        "Nieuwbouw", "Verkoop", "Gebouw", "Kwaliteitsvolle", "Karaktervolle",
        "Instapklaar", "Luxueus", "Woonproject",
    };

    // Tekst die begint met een generiek type-woord (altijd afwijzen)
    private static readonly Regex StartsWithGenericWordRegex = new(
        @"^(?:Nieuwbouwproject|Appartementen|Woningen|Huizen|Huis|Appartement|Woning|Te\s+koop|Project|Nieuwbouw)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── Blacklist: marketing- en zinspatronen die nooit projectnaam zijn ──────

    private static readonly Regex[] ProjectNameBlacklistPatterns =
    [
        // Werkwoorden als opener (marketing tekst)
        new Regex(@"^Wonen\s+in\b",        RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Thuiskomen\b",         RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Ontdek\b",             RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Geniet\b",             RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Investeren\b",         RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Exclusief\s+wonen\b",  RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Gelegen\b",            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Ideaal\b",             RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Uniek\b",              RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Woonproject/Nieuwbouwproject gevolgd door marketing (werkwoord, voegwoord, lidwoord)
        new Regex(@"^Woonproject\s+(?:waar|omvat|met|dat|voor|biedt|om|die|het|een|de|bij|aan|is|was|heeft|geeft|staat|ligt|laat|omvat|voorziet|combineert|telt|kent|bevat|bevindt)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Nieuwbouwproject\s+(?:met|dat|voor|biedt|waar|omvat|die|het|een|de|bij|aan|is|was|heeft|geeft|staat|ligt|telt|kent|bevat|voorziet)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Aanwijzende voornaamwoorden
        new Regex(@"^Dit\b",   RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Deze\b",  RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Dat\b",   RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Lidwoorden gevolgd door bijvoeglijk naamwoord
        new Regex(@"^Een\s+(?:uniek|prachtig|exclusief|modern|stijlvol|mooi|luxe|nieuw|hedendaags|eigentijds|kwalitatief|uitzonderlijk|bijzonder)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Overige marketing-openers
        new Regex(@"^Prachtig\s+(?:gelegen|nieuwbouw|appartement|woning|project)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Strategisch\s+gelegen\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^Centraal\s+gelegen\b",    RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"^In\s+(?:het\s+hart|de\s+buurt|het\s+centrum)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    // Keywords die een eerste tekstlijn als mogelijke projectnaam kwalificeren
    private static readonly HashSet<string> ProjectKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Hof", "Park", "Residentie", "Res", "Domein", "Site", "Wijk",
        "Gardens", "Court", "Square", "House", "Plaza", "Tuin", "Villa",
        "Kasteel", "Hoeve", "Kaai", "Dijk", "Laan", "Straat", "Plein",
        "Haven", "Berg", "Bos", "Veld", "Zicht", "Heem",
    };

    // Bekende typefouten / technische namen → correcte naam
    private static readonly Dictionary<string, string> ProjectNameCorrections =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["BOEVRIE"]  = "Boeverie",
            ["BOEVERIE"] = "Boeverie",
        };

    // Verdieping labels (int → string)
    private static readonly Dictionary<int, string> FloorLabels = new()
    {
        [-2] = "2e kelderverdieping",
        [-1] = "1e kelderverdieping",
        [0]  = "Gelijkvloers",
        [1]  = "1e verdieping",
        [2]  = "2e verdieping",
        [3]  = "3e verdieping",
        [4]  = "4e verdieping",
        [5]  = "5e verdieping",
        [6]  = "6e verdieping",
        [7]  = "7e verdieping",
        [8]  = "8e verdieping",
        [9]  = "9e verdieping",
        [10] = "10e verdieping",
    };

    // ── Publieke API ──────────────────────────────────────────────────────────

    /// <summary>Bepaalt de best mogelijke titel voor een gewone listing of projectgroep.</summary>
    public static TitleResolutionResult Resolve(
        ListingDto dto, PropertyType mappedType, ILogger? logger = null)
    {
        var rawTitle      = dto.Title ?? string.Empty;
        var addressFallback = BuildAddressFallback(dto.Street, dto.HouseNumber);
        var typeLabel     = GetDisplayPropertyType(dto.PropertyTypeRaw, dto.PropertySubTypeRaw, mappedType, PropertySubType.Unknown);
        var candidates    = new List<(string Source, string? Value, bool Accepted)>();

        var result = mappedType == PropertyType.ProjectGroup
            ? ResolveProjectGroup(dto, addressFallback, candidates)
            : ResolveRegularListing(dto, addressFallback, candidates);

        if (result.Title != rawTitle)
            logger?.LogInformation(
                "[TitleResolved] {ExternalId} | Old='{OldTitle}' | New='{NewTitle}' | Source={Source}",
                dto.ExternalId, rawTitle, result.Title, result.TitleSource);

        AppendToDebugFile(dto.ExternalId,
            isProjectGroup: mappedType == PropertyType.ProjectGroup,
            isProjectUnit: false,
            oldTitle: rawTitle,
            addressFallback: addressFallback,
            typeLabel: typeLabel,
            result: result);

        AppendCandidatesDebugFile(dto.ExternalId, candidates, result);

        return result;
    }

    /// <summary>
    /// Bepaalt de best mogelijke titel voor een project-unit.
    /// <paramref name="resolvedParentTitle"/> overschrijft de raw <see cref="ProjectGroupUnitDto.ParentProjectName"/>
    /// met de al-opgekuiste titel van het parent project.
    /// </summary>
    public static TitleResolutionResult ResolveForUnit(
        ProjectGroupUnitDto unit,
        string? resolvedParentTitle = null,
        ILogger? logger = null)
    {
        var typeLabel = GetDisplayPropertyType(
            unit.RawGroupType, unit.RawSubType,
            unit.MappedPropertyType, unit.MappedPropertySubType);

        // Gebruik de meest bruikbare naam: resolvedParentTitle > ParentProjectName
        var rawParentName  = resolvedParentTitle ?? unit.ParentProjectName;
        var projectName    = NormalizeProjectName(rawParentName);
        string title;
        string source;

        if (!string.IsNullOrEmpty(projectName) && IsValidProjectName(projectName))
        {
            var detail = BuildUnitDetail(typeLabel, unit.Floor, unit.BedroomCount,
                unit.MappedPropertyType, unit.MappedPropertySubType);
            title  = $"{projectName} - {detail}";
            source = "ParentProject";
        }
        else
        {
            // Fallback: geen bruikbare projectnaam beschikbaar → "Unit X" als laatste redmiddel
            title  = $"Unit {unit.UnitId}";
            source = "TechnicalFallback";
        }

        logger?.LogInformation(
            "[TitleResolved] {UnitId} | Old='Unit {UnitId2}' | New='{NewTitle}' | Source={Source}",
            unit.UnitId, unit.UnitId, title, source);

        var result = new TitleResolutionResult(title, source, DetectedProjectName: projectName);
        AppendToDebugFile(unit.UnitId, isProjectGroup: false, isProjectUnit: true,
            oldTitle: null, addressFallback: null, typeLabel: typeLabel, result: result);

        return result;
    }

    /// <summary>
    /// True als de titel een technische fallback is die vervangen mag worden.
    /// Detecteert ook titels die raw technische termen bevatten (bv. "Vaartstraat – COMMERCIAL_PREMISES").
    /// </summary>
    public static bool IsTechnicalFallbackTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return true;
        var upper = title.Trim().ToUpperInvariant();

        return upper.StartsWith("HOUSE_GROUP")
            || upper.StartsWith("APARTMENT_GROUP")
            || upper.StartsWith("HOUSE IN ")
            || upper.StartsWith("APARTMENT IN ")
            || upper.Contains("HOUSE_GROUP")
            || upper.Contains("APARTMENT_GROUP")
            || upper.Contains("COMMERCIAL_PREMISES")
            || upper == "HOUSE"
            || upper == "APARTMENT"
            || upper.StartsWith("UNIT ")
            || Regex.IsMatch(upper, @"^PROJECT\s+\d+$")
            || Regex.IsMatch(upper, @"^LISTING\s+\d+$");
    }

    /// <summary>
    /// Geeft een kwaliteitsscore [0–100] voor een projecttitel.
    /// Hoge score: korte naam, "Residentie"-prefix, geen werkwoorden.
    /// Lage score: marketing-tekst, te lang, technische fallback.
    /// </summary>
    public static int TitleQualityScore(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return 0;
        if (IsTechnicalFallbackTitle(title)) return 0;
        var t = title.Trim();
        var score = 50;

        // Positief
        if (Regex.IsMatch(t, @"^\s*Residentie\b", RegexOptions.IgnoreCase)) score += 20;
        if (t.Length is > 3 and <= 30) score += 10;
        else if (t.Length <= 45) score += 5;

        // Negatief: marketing-werkwoorden
        if (t.Contains("biedt",  StringComparison.OrdinalIgnoreCase)) score -= 25;
        if (t.Contains("omvat",  StringComparison.OrdinalIgnoreCase)) score -= 20;
        if (Regex.IsMatch(t, @"\bwaar\b",             RegexOptions.IgnoreCase)) score -= 15;
        if (Regex.IsMatch(t, @"^Woonproject\b",       RegexOptions.IgnoreCase)) score -= 20;
        if (Regex.IsMatch(t, @"^Nieuwbouwproject\b",  RegexOptions.IgnoreCase)) score -= 20;
        if (t.Length > 45) score -= 15;
        if (t.Contains("..."))  score -= 10;
        if (IsBlacklisted(t))   score -= 30;

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// True als newTitle een duidelijke verbetering is over oldTitle.
    /// Gebruikt TitleQualityScore — minimaal 6 punten beter vereist.
    /// </summary>
    public static bool IsBetterTitle(string? oldTitle, string? newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle)) return false;
        if (string.IsNullOrWhiteSpace(oldTitle)) return true;
        if (oldTitle == newTitle) return false;
        if (IsTechnicalFallbackTitle(oldTitle) && !IsTechnicalFallbackTitle(newTitle)) return true;
        if (IsTechnicalFallbackTitle(newTitle)) return false;
        return TitleQualityScore(newTitle) > TitleQualityScore(oldTitle) + 5;
    }

    // ── Type display helper ───────────────────────────────────────────────────

    /// <summary>Geeft een gebruiksvriendelijk Nederlandstalig type-label. Nooit technische enumnamen.</summary>
    public static string GetDisplayPropertyType(
        string? rawType, string? rawSubType,
        PropertyType mappedType, PropertySubType mappedSubType)
    {
        var sub  = (rawSubType ?? string.Empty).Trim().ToUpperInvariant();
        var type = (rawType   ?? string.Empty).Trim().ToUpperInvariant();

        var fromSub = sub switch
        {
            "PENTHOUSE"                              => "Penthouse",
            "STUDIO"                                 => "Studio",
            "DUPLEX"                                 => "Duplex",
            "TRIPLEX"                                => "Triplex",
            "SERVICE_FLAT"                           => "Serviceflat",
            "GROUND_FLOOR"                           => "Benedenverdieping",
            "APARTMENT" or "FLAT"                    => "Appartement",
            "HOUSE"                                  => "Woning",
            "VILLA"                                  => "Villa",
            "BUNGALOW"                               => "Bungalow",
            "TOWN_HOUSE" or "TERRACED_HOUSE"         => "Rijwoning",
            "SEMI_DETACHED_HOUSE"                    => "Halfopen woning",
            "DETACHED_HOUSE"                         => "Vrijstaande woning",
            "COMMERCIAL_PREMISES" or "COMMERCIAL"    => "Handel",
            "OFFICE" or "KANTOOR" or "BUREAU"        => "Kantoor",
            "GARAGE"                                 => "Garage",
            "PARKING"                                => "Parking",
            _                                        => string.Empty
        };
        if (!string.IsNullOrEmpty(fromSub)) return fromSub;

        var fromType = type switch
        {
            "APARTMENT" or "FLAT"                    => "Appartement",
            "APARTMENT_GROUP"                        => "Appartementen",
            "HOUSE"                                  => "Woning",
            "HOUSE_GROUP"                            => "Woningen",
            "PENTHOUSE"                              => "Penthouse",
            "DUPLEX"                                 => "Duplex",
            "STUDIO"                                 => "Studio",
            "COMMERCIAL" or "COMMERCIAL_PREMISES"    => "Handel",
            "OFFICE" or "KANTOOR"                    => "Kantoor",
            "GARAGE"                                 => "Garage",
            "PARKING"                                => "Parking",
            _                                        => string.Empty
        };
        if (!string.IsNullOrEmpty(fromType)) return fromType;

        return mappedType switch
        {
            PropertyType.Apartment          => "Appartement",
            PropertyType.House              => "Woning",
            PropertyType.ProjectGroup       => "Nieuwbouwproject",
            PropertyType.CommercialProperty => "Handel",
            PropertyType.Garage             => "Garage",
            PropertyType.Land               => "Grond",
            _                               => string.Empty
        };
    }

    // ── Publieke extractie-helpers (testbaar) ─────────────────────────────────

    /// <summary>Geeft de inhoud van de eerste &lt;strong&gt;-tag in de description.</summary>
    public static string? ExtractStrongTitle(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        var m = StrongTagRegex.Match(description);
        if (!m.Success) return null;
        var content = WebUtility.HtmlDecode(m.Groups[1].Value).Trim();
        return string.IsNullOrEmpty(content) ? null : content;
    }

    /// <summary>
    /// Extraheert een bruikbare projectnaam uit een strong title:
    /// 1. "Residentie X" / "Res. X" → normaliseert naar "Residentie X".
    /// 2. Korte naam vóór " - " / " – " / " | " / ": " (max 4 woorden, niet generiek/blacklist).
    /// Geeft null terug als geen bruikbare naam gevonden.
    /// </summary>
    public static string? ExtractProjectNameFromStrongTitle(string? strongText)
    {
        if (string.IsNullOrWhiteSpace(strongText)) return null;

        var text = WebUtility.HtmlDecode(strongText).Trim();

        // 1. Residentie X / Res. X patroon
        var m = ResidentieInlineRegex.Match(text);
        if (m.Success)
        {
            var suffix = m.Groups[1].Value.Trim().TrimEnd(':', '-', '–', '|', ',', '.');
            return $"Residentie {suffix}";
        }

        // 2. Naam vóór scheidingsteken (max 4 woorden, niet generiek/blacklist)
        foreach (var sep in new[] { " – ", " - ", " | ", ": " })
        {
            var idx = text.IndexOf(sep, StringComparison.Ordinal);
            if (idx <= 0) continue;

            var left = text[..idx].Trim();
            if (string.IsNullOrEmpty(left)) continue;

            var wordCount = left.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount < 1 || wordCount > 4) continue;
            if (!IsValidProjectName(left)) continue;

            return left;
        }

        return null;
    }

    /// <summary>
    /// Zoekt een projectnaam in de volledige description:
    /// 1. Bekende patronen (Residentie X, Woonproject X, …) — blacklist gefilterd.
    /// 2. Eerste 10 niet-lege regels gescand op korte eigennamen.
    /// </summary>
    public static string? ExtractProjectNameFromDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;

        var plain = HtmlTagRegex.Replace(description, "\n");
        plain = WebUtility.HtmlDecode(plain);

        // 1. Bekende patronen in volledige tekst
        foreach (var (pattern, _) in DescriptionPatterns)
        {
            var m = pattern.Match(plain);
            if (!m.Success) continue;

            var name = m.Groups[1].Value.Trim().TrimEnd(',', '.', ';', ':');
            if (name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 6
                && IsValidProjectName(name))
                return name;
        }

        // 2. Eerste 10 niet-lege regels controleren op korte eigennamen
        var lines = plain.Split('\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var linesChecked = 0;
        foreach (var line in lines)
        {
            if (linesChecked >= 10) break;
            var trimmed = MultiSpaceRegex.Replace(line, " ").Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            linesChecked++;

            if (IsProjectNameCandidate(trimmed))
                return trimmed;
        }

        return null;
    }

    /// <summary>
    /// Normaliseert een projectnaam: HTML-entities, tags, ellipsis, separator-verwerking,
    /// generieke suffixen, jaar-haakjes, bekende correcties, "Res." → "Residentie".
    /// </summary>
    public static string NormalizeProjectName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var result = WebUtility.HtmlDecode(name);
        result = HtmlTagRegex.Replace(result, " ");
        result = MultiSpaceRegex.Replace(result, " ").Trim();

        // Ellipsis
        result = result.Replace("...", string.Empty).Trim();

        // Marketing suffix: "biedt ...", "omvat ...", etc.
        result = StripMarketingSuffix(result);

        // Jaar tussen haakjes: "(2025)"
        result = YearInBracketsRegex.Replace(result, string.Empty).Trim();

        // Intelligente separator-verwerking
        result = ResolveSeparator(result);

        // Generieke suffix na scheidingsteken (bv. "BOEVRIE-Appartementen" → "BOEVRIE")
        result = GenericSuffixAfterSeparatorRegex.Replace(result, string.Empty).Trim();

        // Technische underscores
        result = result.Replace("_", " ");
        result = MultiSpaceRegex.Replace(result, " ").Trim();

        // Bekende correcties
        if (ProjectNameCorrections.TryGetValue(result, out var corrected))
            return corrected;

        // "Res." → "Residentie "
        result = Regex.Replace(result, @"\bRes\.\s+", "Residentie ", RegexOptions.IgnoreCase);

        return result.Trim();
    }

    /// <summary>Basisnormalisering voor niet-projectnamen (HTML decode, tags, spaties).</summary>
    public static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        var result = WebUtility.HtmlDecode(title);
        result = HtmlTagRegex.Replace(result, " ");
        result = MultiSpaceRegex.Replace(result, " ").Trim();
        return result;
    }

    // ── Projectgroep resolutie ────────────────────────────────────────────────

    private static TitleResolutionResult ResolveProjectGroup(
        ListingDto dto,
        string? addressFallback,
        List<(string Source, string? Value, bool Accepted)> candidates)
    {
        // 1. cluster.projectInfo.projectName
        if (!string.IsNullOrWhiteSpace(dto.ProjectName))
        {
            var cleaned = NormalizeProjectName(dto.ProjectName);
            candidates.Add(("ProjectInfo", cleaned, IsValidProjectName(cleaned)));
            if (IsValidProjectName(cleaned))
                return new TitleResolutionResult(cleaned, "ProjectInfoCleaned", DetectedProjectName: cleaned);
        }

        var strongRaw = ExtractStrongTitle(dto.Description);

        // 2-3. Projectnaam extraheren uit eerste <strong>-tag
        if (strongRaw != null)
        {
            var fromStrong = ExtractProjectNameFromStrongTitle(strongRaw);
            if (fromStrong != null)
            {
                var name = NormalizeProjectName(fromStrong);
                var valid = IsValidProjectName(name);
                candidates.Add(("StrongTitle", name, valid));
                if (valid)
                {
                    var src = ResidentieInlineRegex.IsMatch(strongRaw) ? "StrongResidentie" : "StrongDescription";
                    return new TitleResolutionResult(name, src, RawStrongTitle: strongRaw, DetectedProjectName: name);
                }
            }
            else
            {
                candidates.Add(("StrongTitle", strongRaw, false));
            }
        }

        // 4. Projectnaam uit volledige description
        var fromDesc = ExtractProjectNameFromDescription(dto.Description);
        if (fromDesc != null)
        {
            var name  = NormalizeProjectName(fromDesc);
            var valid = IsValidProjectName(name);
            candidates.Add(("Description", name, valid));
            if (valid)
                return new TitleResolutionResult(name, "ResidentiePattern",
                    RawStrongTitle: strongRaw, DetectedProjectName: name);
        }

        // 5. Adres fallback
        if (!string.IsNullOrEmpty(addressFallback))
        {
            candidates.Add(("AddressFallback", addressFallback, true));
            return new TitleResolutionResult(addressFallback, "AddressFallback");
        }

        // 6. Technische fallback
        var fallback = $"Project {dto.ExternalId}";
        candidates.Add(("TechnicalFallback", fallback, true));
        return new TitleResolutionResult(fallback, "TechnicalFallback");
    }

    // ── Gewone listing resolutie ──────────────────────────────────────────────

    private static TitleResolutionResult ResolveRegularListing(
        ListingDto dto,
        string? addressFallback,
        List<(string Source, string? Value, bool Accepted)> candidates)
    {
        var strongRaw = ExtractStrongTitle(dto.Description);

        // 1-2. Strong title → residentie of korte naam
        if (strongRaw != null)
        {
            var fromStrong = ExtractProjectNameFromStrongTitle(strongRaw);
            if (fromStrong != null)
            {
                var name  = NormalizeProjectName(fromStrong);
                var valid = IsValidProjectName(name);
                candidates.Add(("StrongTitle", name, valid));
                if (valid)
                    return new TitleResolutionResult(name, "StrongDescription", RawStrongTitle: strongRaw);
            }
            else
            {
                candidates.Add(("StrongTitle", strongRaw, false));
            }
        }

        // 3. Patroon in description
        var fromDesc = ExtractProjectNameFromDescription(dto.Description);
        if (fromDesc != null)
        {
            var name  = NormalizeProjectName(fromDesc);
            var valid = IsValidProjectName(name);
            candidates.Add(("Description", name, valid));
            if (valid)
                return new TitleResolutionResult(name, "ResidentiePattern",
                    RawStrongTitle: strongRaw, DetectedProjectName: fromDesc);
        }

        // 4. Adres — GEEN type toevoegen
        if (!string.IsNullOrEmpty(addressFallback))
        {
            candidates.Add(("AddressFallback", addressFallback, true));
            return new TitleResolutionResult(addressFallback, "AddressFallback");
        }

        // 5. Technische fallback
        var fallback = $"Listing {dto.ExternalId}";
        candidates.Add(("TechnicalFallback", fallback, true));
        return new TitleResolutionResult(fallback, "TechnicalFallback");
    }

    // ── Privé helpers ─────────────────────────────────────────────────────────

    private static string? BuildAddressFallback(string? street, string? houseNumber)
    {
        if (string.IsNullOrWhiteSpace(street)) return null;
        var s = street.Trim();
        return string.IsNullOrEmpty(houseNumber) ? s : $"{s} {houseNumber.Trim()}";
    }

    /// <summary>
    /// Bouwt het detail-gedeelte van een unit-titel op:
    /// type + verdieping (appartement) of type + slaapkamers (woning).
    /// </summary>
    private static string BuildUnitDetail(
        string typeLabel, int? floor, int? bedroomCount,
        PropertyType propertyType, PropertySubType propertySubType)
    {
        if (propertySubType == PropertySubType.Penthouse || typeLabel == "Penthouse")
            return "Penthouse";

        if (propertyType == PropertyType.CommercialProperty)
            return string.IsNullOrEmpty(typeLabel) ? "Handel" : typeLabel;

        if (propertyType == PropertyType.Apartment)
        {
            if (floor.HasValue)
            {
                var lbl = FloorLabels.TryGetValue(floor.Value, out var l) ? l : $"{floor.Value}e verdieping";
                return $"Appartement - {lbl}";
            }
            return "Appartement";
        }

        if (propertyType == PropertyType.House)
        {
            return bedroomCount.HasValue
                ? $"Woning - {bedroomCount.Value} slaapkamers"
                : "Woning";
        }

        return string.IsNullOrEmpty(typeLabel) ? "Eenheid" : typeLabel;
    }

    /// <summary>
    /// Intelligente separator-verwerking:
    /// - Links = stad (1-2 woorden, uppercase, geen cijfers) + rechts niet-generiek → rechts
    /// - Rechts generiek/blacklisted + links niet-generiek → links
    /// - Standaard → links (strip marketing van rechts)
    /// </summary>
    private static string ResolveSeparator(string name)
    {
        foreach (var sep in new[] { " – ", " - ", " | ", ": " })
        {
            var idx = name.IndexOf(sep, StringComparison.Ordinal);
            if (idx <= 0) continue;

            var left  = name[..idx].Trim();
            var right = name[(idx + sep.Length)..].Trim();

            if (string.IsNullOrEmpty(right)) return left;

            // Stedelijk/locatie prefix: 1-2 woorden, beginnen met hoofdletter, geen cijfers
            var leftWords = left.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (leftWords.Length <= 2
                && leftWords.All(w => w.Length > 0 && char.IsUpper(w[0]) && !w.Any(char.IsDigit))
                && !IsGenericOrBlacklisted(right)
                && !IsGenericOrBlacklisted(left))
            {
                return right;
            }

            // Marketing suffix rechts is onbruikbaar → links bewaren
            if (IsGenericOrBlacklisted(right) && !IsGenericOrBlacklisted(left))
                return left;

            // Standaard: links gebruiken
            if (!IsGenericOrBlacklisted(left))
                return left;

            break;
        }

        return name;
    }

    /// <summary>
    /// Combineert alle validatiechecks: niet leeg, niet technisch, niet generiek, niet blacklisted.
    /// </summary>
    private static bool IsValidProjectName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim();
        return !IsTechnicalFallbackTitle(n) && !IsGenericTitle(n) && !IsBlacklisted(n);
    }

    private static bool IsGenericTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;
        var trimmed = text.Trim();
        return GenericWords.Contains(trimmed) || StartsWithGenericWordRegex.IsMatch(trimmed);
    }

    private static bool IsBlacklisted(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        foreach (var pattern in ProjectNameBlacklistPatterns)
        {
            if (pattern.IsMatch(trimmed)) return true;
        }
        return false;
    }

    private static bool IsGenericOrBlacklisted(string text) =>
        IsGenericTitle(text) || IsBlacklisted(text);

    /// <summary>
    /// Verwijdert marketing-suffix (biedt/omvat/...) alleen als het resterende deel
    /// ≥2 woorden heeft of begint met een projectkeyword.
    /// </summary>
    private static string StripMarketingSuffix(string name)
    {
        var stripped = MarketingSuffixRegex.Replace(name, string.Empty).Trim();
        if (stripped == name) return name;

        var wordCount = stripped.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount >= 2) return stripped;

        var lower = stripped.ToLowerInvariant();
        foreach (var prefix in MarketingSuffixProjectPrefixes)
        {
            if (lower.StartsWith(prefix, StringComparison.Ordinal)) return stripped;
        }

        return name;
    }

    /// <summary>
    /// True als een korte tekstregel een goede kandidaat-projectnaam is
    /// (voor scan van eerste regels in description).
    /// </summary>
    private static bool IsProjectNameCandidate(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        if (!char.IsUpper(line[0])) return false;

        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2 || words.Length > 5) return false;

        // Geen prijs of oppervlakte
        if (Regex.IsMatch(line, @"[€$]\s*\d|\d+\s*euro|\d+\s*m[²2]", RegexOptions.IgnoreCase)) return false;

        // Geen volledige zin (eindigt op punt bij >3 woorden)
        if (words.Length > 3 && line.TrimEnd().EndsWith('.')) return false;

        // Bevat bekend project-keyword
        if (!words.Any(w => ProjectKeywords.Contains(w.TrimEnd('.', ',', ';', ':')))) return false;

        // Niet generiek of blacklisted
        if (IsGenericOrBlacklisted(line)) return false;

        return true;
    }

    // ── Debug bestanden ───────────────────────────────────────────────────────

    private static void AppendToDebugFile(
        string? externalId,
        bool isProjectGroup,
        bool isProjectUnit,
        string? oldTitle,
        string? addressFallback,
        string? typeLabel,
        TitleResolutionResult result)
    {
        try
        {
            var dir   = Path.Combine(AppContext.BaseDirectory, "debug", "titles");
            Directory.CreateDirectory(dir);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
            var path  = Path.Combine(dir, $"title-resolution-{stamp}.json");

            var entry = new
            {
                externalId,
                isProjectGroup,
                isProjectUnit,
                oldTitle,
                newTitle            = result.Title,
                titleSource         = result.TitleSource,
                rawStrongTitle      = result.RawStrongTitle,
                detectedProjectName = result.DetectedProjectName,
                addressFallback,
                typeLabel,
                timestamp           = DateTime.UtcNow.ToString("O")
            };

            File.AppendAllText(path, JsonSerializer.Serialize(entry) + "\n");
        }
        catch { /* debug schrijven nooit laten crashen */ }
    }

    private static void AppendCandidatesDebugFile(
        string? externalId,
        List<(string Source, string? Value, bool Accepted)> candidates,
        TitleResolutionResult result)
    {
        if (candidates.Count == 0) return;
        try
        {
            var dir   = Path.Combine(AppContext.BaseDirectory, "debug", "titles");
            Directory.CreateDirectory(dir);
            var date  = DateTime.UtcNow.ToString("yyyyMMdd");
            var path  = Path.Combine(dir, $"project-name-candidates-{date}.json");

            var entry = new
            {
                externalId,
                chosenName   = result.Title,
                chosenSource = result.TitleSource,
                candidates   = candidates.Select(c => new
                {
                    source   = c.Source,
                    value    = c.Value,
                    accepted = c.Accepted
                }).ToArray(),
                timestamp = DateTime.UtcNow.ToString("O")
            };

            File.AppendAllText(path, JsonSerializer.Serialize(entry) + "\n");
        }
        catch { /* debug schrijven nooit laten crashen */ }
    }
}
