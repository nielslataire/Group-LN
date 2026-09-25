using DALCore.Models;

namespace ServiceCore.Documents;

/// <summary>Zuivere regels van de documentenmodule (geen database) — apart zodat ze testbaar zijn.</summary>
public static class DocumentRules
{
    public const int ExpiryWarningDays = 90;

    /// <summary>Revisienummer 1,2,3 → letter A,B,C (na Z: AA, AB, ...).</summary>
    public static string RevisionLabel(int no)
    {
        if (no <= 0) return "—";
        var s = "";
        var n = no;
        while (n > 0)
        {
            n--;
            s = (char)('A' + n % 26) + s;
            n /= 26;
        }
        return s;
    }

    /// <summary>Bestandsextensie in hoofdletters zonder punt ("dwg", "PDF" → "DWG"); leeg bij onbekend.</summary>
    public static string Ext(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return "";
        var i = filename.LastIndexOf('.');
        if (i < 0 || i == filename.Length - 1) return "";
        var e = filename[(i + 1)..].ToUpperInvariant();
        return e.Length > 5 ? e[..5] : e;
    }

    public static string SizeText(long? bytes)
    {
        if (bytes is null or <= 0) return "";
        var b = (double)bytes.Value;
        if (b >= 1024 * 1024) return (b / 1024 / 1024).ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("nl-BE")) + " MB";
        return Math.Max(1, (int)Math.Round(b / 1024)).ToString() + " kB";
    }

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(new[] { ' ', '-', '–' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return (parts[0][0].ToString() + parts[^1][0]).ToUpperInvariant();
    }

    /// <summary>Map (code) voor een oud ProjectDocType — dezelfde tabel als de backfill in migratie 049.</summary>
    public static string FolderCodeForLegacyType(int? type) => type switch
    {
        1 => "verkoop",
        16 => "plannen",
        2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14 or 15 or 17 => "keuringen",
        _ => "overige"
    };

    /// <summary>De publieke site (WWWCOPRO) toont ProjectDocs met Type = 1 en zonder ClientAccountId. Een NIEUW document
    /// krijgt dat alleen als het een goedgekeurd, niet-gekoppeld verkoopdocument is — zo lekt een concept of
    /// een klantdocument nooit naar de website. Geeft het (eventueel ongewijzigde) Type terug.</summary>
    public static int? LegacyTypeAfterChange(int? currentType, string folderCode, byte status, int linkCount, bool hasClientLink)
    {
        if (currentType.HasValue) return currentType;
        if (folderCode == "verkoop" && status == DocumentStatus.Goedgekeurd && linkCount == 0 && !hasClientLink) return 1;
        return null;
    }

    public static string StatusKey(byte status) => status switch
    {
        DocumentStatus.Concept => "concept",
        DocumentStatus.TerGoedkeuring => "pending",
        DocumentStatus.Goedgekeurd => "ok",
        DocumentStatus.Getekend => "signed",
        DocumentStatus.TerOndertekening => "signing",
        DocumentStatus.Ingediend => "submitted",
        DocumentStatus.Gegund => "awarded",
        DocumentStatus.NietGegund => "notawarded",
        _ => "ok"
    };

    public static string StatusLabel(byte status) => status switch
    {
        DocumentStatus.Concept => "Concept",
        DocumentStatus.TerGoedkeuring => "Ter goedkeuring",
        DocumentStatus.Goedgekeurd => "Goedgekeurd",
        DocumentStatus.Getekend => "Getekend",
        DocumentStatus.TerOndertekening => "Ter ondertekening",
        DocumentStatus.Ingediend => "Ingediend",
        DocumentStatus.Gegund => "Gegund",
        DocumentStatus.NietGegund => "Niet gegund",
        _ => "Goedgekeurd"
    };

    /// <summary>Vervalstatus: expired | expiring | ok | none.</summary>
    public static string ExpiryState(DateOnly? expiresOn, DateOnly today)
    {
        if (expiresOn is null) return "none";
        if (expiresOn.Value < today) return "expired";
        if (expiresOn.Value <= today.AddDays(ExpiryWarningDays)) return "expiring";
        return "ok";
    }

    /// <summary>Een getekend document (of één met een geplaatste handtekening) is bevroren: geen nieuwe revisies meer.</summary>
    public static bool IsFrozen(byte status, IEnumerable<byte> signatureStatuses) =>
        status == DocumentStatus.Getekend || signatureStatuses.Any(s => s == 2);

    /// <summary>Geeft de vervaldatum uit documentdatum + jaren (bv. keuring elektriciteit 25 jaar).</summary>
    public static DateOnly? ExpiryFromYears(DateOnly? docDate, int? years) =>
        docDate.HasValue && years is > 0 ? docDate.Value.AddYears(years.Value) : null;

    /// <summary>Volgorde-onafhankelijke samenvatting van een ondertekening: (getekend, totaal).</summary>
    public static (int Signed, int Total) SignatureProgress(IEnumerable<byte> statuses)
    {
        var l = statuses.ToList();
        return (l.Count(s => s == 2), l.Count);
    }
}
