using BOCore;

namespace CPMCore.Services.Security;

/// <summary>
/// Hulpmiddelen voor het gl-v2-rechtenscherm van Instellingen › Gebruikers en rollen (design-handoff punt 49): de boom van functies, het
/// niveau per functie (Geen · Lezen · Bewerken · Verwijderen) en het omrekenen tussen dat niveau en de drie vlaggen die de database
/// bewaart (CanRead/CanWrite/CanDelete). Elk niveau bevat het vorige: Verwijderen = lezen + bewerken + verwijderen.
/// De effectieve rechten volgen exact <c>PermissionService</c>: een rechtstreekse rij is definitief, anders erft een code van de code
/// erboven (punt-notatie: "Customers.Company.5" erft van "Customers").
/// </summary>
public static class PermissionTreeV2
{
    public const string CustomerCompanyPrefix = "Customers.Company.";
    public const string SupplierCompanyPrefix = "Suppliers.Company.";
    public const string InvoicingCompanyPrefix = "InvoicingByBillingCompany.Company.";

    /// <summary>Codes die niet als aparte functie in de boom staan: ze volgen hun ouder mee (zelfde regel als het oude rechtenpaneel).</summary>
    public static readonly string[] HiddenCodes =
    {
        PermissionCodes.CustomersAll, PermissionCodes.CustomersByBillingCompany,
        PermissionCodes.SuppliersAll, PermissionCodes.SuppliersByBillingCompany,
        PermissionCodes.InvoicingByBillingCompany, PermissionCodes.DocumentCenterByBillingCompany
    };

    /// <summary>Functies waarvan de rechten per facturatiebedrijf gelden (de bedrijfstoegang van een rol of gebruiker beperkt ze).</summary>
    public static readonly string[] PerCompanyCodes =
    {
        PermissionCodes.Customers, PermissionCodes.Suppliers, PermissionCodes.Invoicing, PermissionCodes.DocumentCenter
    };

    /// <summary>De ouder-functie van elke verborgen code, zodat die bij het opslaan mee cascadeert.</summary>
    public static readonly Dictionary<string, string[]> HiddenChildren = new(StringComparer.OrdinalIgnoreCase)
    {
        [PermissionCodes.Suppliers] = new[] { PermissionCodes.SuppliersAll, PermissionCodes.SuppliersByBillingCompany },
        [PermissionCodes.Customers] = new[] { PermissionCodes.CustomersAll, PermissionCodes.CustomersByBillingCompany },
        [PermissionCodes.Invoicing] = new[] { PermissionCodes.InvoicingByBillingCompany },
        [PermissionCodes.DocumentCenter] = new[] { PermissionCodes.DocumentCenterByBillingCompany }
    };

    /// <summary>Codes die de rol Admin altijd moet houden, zodat niemand zich buitensluit uit Gebruikers en rollen.</summary>
    public static readonly string[] AdminProtectedCodes =
    {
        PermissionCodes.Settings, PermissionCodes.SettingsTeamEnToegang, PermissionCodes.SettingsUsers, PermissionCodes.SettingsRoles
    };

    /// <summary>Korte uitleg onder een functie in de rechtenboom, voor functies waarvan de naam makkelijk verkeerd begrepen wordt.</summary>
    public static readonly Dictionary<string, string> Info = new(StringComparer.OrdinalIgnoreCase)
    {
        [PermissionCodes.Signing] = "Voor medewerkers: ondertekeningen opvolgen en starten. Klanten tekenen via een link en hebben hier geen recht voor nodig."
    };

    public sealed record Item(string Code, string Name, string? Parent, int Sort, bool PerCompany);

    public readonly record struct Flags(bool Read, bool Write, bool Delete)
    {
        public static readonly Flags None = new(false, false, false);
        public int Level => Delete ? 3 : Write ? 2 : Read ? 1 : 0;
        public static Flags FromLevel(int level) => new(level >= 1, level >= 2, level >= 3);
    }

    public static bool IsCompanyCode(string code) =>
        code.StartsWith(CustomerCompanyPrefix, StringComparison.OrdinalIgnoreCase) ||
        code.StartsWith(SupplierCompanyPrefix, StringComparison.OrdinalIgnoreCase) ||
        code.StartsWith(InvoicingCompanyPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>De zichtbare functies uit de catalogus, in volgorde van de boom (ouder vóór kinderen).</summary>
    public static List<Item> Items()
    {
        var all = PermissionCatalog.All
            .Where(x => !HiddenCodes.Contains(x.Code, StringComparer.OrdinalIgnoreCase))
            .Select(x => new Item(x.Code, x.Name, x.ParentCode, x.SortOrder, PerCompanyCodes.Contains(x.Code, StringComparer.OrdinalIgnoreCase)))
            .ToList();
        var byParent = all.ToLookup(i => i.Parent ?? "", StringComparer.OrdinalIgnoreCase);
        var ordered = new List<Item>();
        void Walk(string parent)
        {
            foreach (var i in byParent[parent].OrderBy(i => i.Sort).ThenBy(i => i.Name))
            {
                ordered.Add(i);
                Walk(i.Code);
            }
        }
        Walk("");
        return ordered;
    }

    /// <summary>Alle codes onder <paramref name="code"/> in de boom (de code zelf niet).</summary>
    public static List<string> Descendants(List<Item> items, string code)
    {
        var result = new List<string>();
        void Walk(string parent)
        {
            foreach (var i in items.Where(i => string.Equals(i.Parent, parent, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(i.Code);
                Walk(i.Code);
            }
        }
        Walk(code);
        return result;
    }

    /// <summary>Effectieve vlaggen van een code: rechtstreekse rij, anders erven van de code erboven (punt-notatie).</summary>
    public static Flags Effective(IReadOnlyDictionary<string, Flags> direct, string code)
    {
        var current = code;
        while (true)
        {
            if (direct.TryGetValue(current, out var f)) return f;
            var dot = current.LastIndexOf('.');
            if (dot <= 0) return Flags.None;
            current = current[..dot];
        }
    }

    public static Dictionary<string, Flags> NewMap() => new(StringComparer.OrdinalIgnoreCase);
}
