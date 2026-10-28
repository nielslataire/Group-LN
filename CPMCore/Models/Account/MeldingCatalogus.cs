namespace CPMCore.Models.Account;

/// <summary>De gebeurtenissen waarover een gebruiker meldingen kan instellen (Mijn profiel › Meldingen). De sleutel is stabiel en wordt in
/// <c>UserProfileSettings.MeldingenJson</c> bewaard; een ontbrekende sleutel volgt de standaard.</summary>
public static class MeldingCatalogus
{
    public record Item(string Key, string Naam, string? Uitleg, bool StandaardInCrm, bool StandaardMail);
    public record Groep(string Naam, Item[] Items);

    public static readonly Groep[] Groepen =
    {
        new("Taken en deadlines", new Item[]
        {
            new("taak-toegewezen", "Taak aan mij toegewezen", null, true, true),
            new("deadline-nadert", "Deadline nadert", "Volgens je herinnering hieronder", true, true),
            new("taak-achterstallig", "Taak of mijlpaal achterstallig", null, true, false),
            new("wachten-antwoord", "Wachten op: antwoord ontvangen", null, true, false),
        }),
        new("Projecten en werf", new Item[]
        {
            new("werfpunt-toegewezen", "Werfpunt aan mij toegewezen", null, true, true),
            new("verslag-klaar", "Werfverslag klaar om te verwerken", null, true, false),
            new("mijlpaal-bereikt", "Mijlpaal bereikt", "In projecten waar ik een rol heb", true, false),
        }),
        new("Facturatie", new Item[]
        {
            new("factuur-goedkeuren", "Inkomende factuur te goedkeuren", null, true, true),
            new("factuur-betaald", "Factuur betaald", null, true, false),
            new("factuur-vervallen", "Factuur vervallen en onbetaald", null, true, true),
        }),
        new("Documenten en verkoop", new Item[]
        {
            new("document-ondertekend", "Document ondertekend in het portaal", null, true, true),
            new("document-bekeken", "Klant heeft document bekeken", null, false, false),
        }),
        new("Vacatures", new Item[]
        {
            new("nieuwe-sollicitatie", "Nieuwe sollicitatie", null, true, false),
        }),
    };

    public static readonly (string Id, string Naam)[] Herinneringen = { ("1d", "1 dag"), ("3d", "3 dagen"), ("1w", "1 week"), ("2w", "2 weken") };

    public static IEnumerable<Item> Alle => Groepen.SelectMany(g => g.Items);

    /// <summary>Leest de bewaarde JSON en vult ontbrekende sleutels met de standaard aan.</summary>
    public static Dictionary<string, bool[]> Lees(string? json)
    {
        var bewaard = new Dictionary<string, bool[]>();
        if (!string.IsNullOrWhiteSpace(json))
        {
            try { bewaard = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool[]>>(json) ?? new(); }
            catch (System.Text.Json.JsonException) { bewaard = new(); }
        }
        return Alle.ToDictionary(i => i.Key, i => bewaard.TryGetValue(i.Key, out var v) && v is { Length: 2 } ? v : new[] { i.StandaardInCrm, i.StandaardMail });
    }

    /// <summary>Filtert ingezonden JSON op bekende sleutels en geeft de nette, bewaarbare vorm terug.</summary>
    public static string Schoon(string? json) => System.Text.Json.JsonSerializer.Serialize(Lees(json));
}
