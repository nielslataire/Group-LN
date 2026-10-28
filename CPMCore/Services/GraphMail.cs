namespace CPMCore.Services;

/// <summary>Een mail uit Graph, beperkt tot wat CPM bewaart: metadata en de korte voorvertoning. Nooit bijlagen of de volledige tekst.</summary>
public sealed class GraphMail
{
    public string Id { get; set; } = "";
    public string? InternetMessageId { get; set; }
    public string? ConversationId { get; set; }
    public string? Onderwerp { get; set; }
    public string? VanAdres { get; set; }
    public string? VanNaam { get; set; }
    /// <summary>Aan en cc.</summary>
    public List<string> Ontvangers { get; set; } = new();
    public DateTime OntvangenOpUtc { get; set; }
    public string? Voorvertoning { get; set; }
    public string? WebLink { get; set; }
    /// <summary>"in" (Postvak IN en submappen) of "uit" (Verzonden items).</summary>
    public string Richting { get; set; } = "in";
    /// <summary>Outlook zette de mail in "Overige" (inferenceClassification = other).</summary>
    public bool IsOverige { get; set; }
    public bool IsConcept { get; set; }
}

public sealed class GraphMailResultaat
{
    public GraphToegang Toegang { get; set; } = GraphToegang.Ok;
    public string? Fout { get; set; }
    public List<GraphMail> Nieuw { get; set; } = new();
    /// <summary>Graph-id's van mails die in Outlook verwijderd zijn (delta "@removed").</summary>
    public List<string> VerwijderdeIds { get; set; } = new();
    /// <summary>De deltatokens per map om op te slaan voor de volgende ronde (JSON), enkel bij <see cref="GraphToegang.Ok"/>.</summary>
    public string? NieuweTokensJson { get; set; }
}
