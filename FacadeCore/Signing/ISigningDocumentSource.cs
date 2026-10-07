namespace FacadeCore.Signing;

/// <summary>
/// Het document dat ondertekend wordt (ONDERTEKENEN_VOORSTEL.md §4.2). Eén implementatie per
/// documenttype — dit is de ENIGE plek waar de signingmodule iets over wijzigingsopdrachten,
/// contracten of bestelbonnen weet. Meermaals geregistreerd in DI, gekozen op <see cref="DocumentType"/>.
/// </summary>
public interface ISigningDocumentSource
{
    /// <summary>Sleutel die ook in <c>SigningPolicy.DocumentType</c> staat, bv. "ChangeOrder".</summary>
    string DocumentType { get; }

    /// <summary>Weergavenaam voor pagina's en mails, bv. "Wijzigingsopdracht".</summary>
    string DisplayName { get; }

    /// <summary>Bouwt de definitieve PDF en verzamelt metadata, bedragen, bijlagen en de voorgestelde ondertekenaars.</summary>
    Task<SigningDocumentPackage> BuildAsync(int sourceEntityId, int byUserId, CancellationToken ct = default);

    /// <summary>Het dossier werd aangeboden: vergrendel de bron (§9.2).</summary>
    Task OnCaseOpenedAsync(int sourceEntityId, int caseId, CancellationToken ct = default);

    /// <summary>Alle vereiste handtekeningen zijn gezet: werk de bron bij (bv. akkoorddatum).</summary>
    Task OnCaseCompletedAsync(int sourceEntityId, int caseId, DateTime completedAtUtc, CancellationToken ct = default);

    /// <summary>Het dossier is gesloten zonder voltooiing (geannuleerd/geweigerd/verlopen): ontgrendel de bron.</summary>
    Task OnCaseClosedAsync(int sourceEntityId, int caseId, int caseStatus, CancellationToken ct = default);

    /// <summary>
    /// Vangnet naast de vergrendeling: een stabiele vingerafdruk (SHA-256 hex) van alles wat de
    /// inhoud van het document bepaalt. De service bewaart ze bij het aanmaken
    /// (<c>SigningCase.SourceFingerprint</c>) en vergelijkt ze opnieuw op het moment van ondertekenen;
    /// verschilt ze, dan wordt de handtekening geweigerd. Null = de bron kan geen vingerafdruk
    /// leveren (dan geldt enkel de vergrendeling). Gekozen i.p.v. een "gewijzigd sinds"-tijdstip omdat
    /// bv. <c>ChangeOrder</c> geen wijzigingstijdstip bijhoudt.
    /// </summary>
    Task<string?> ComputeFingerprintAsync(int sourceEntityId, CancellationToken ct = default);

    /// <summary>Projectnaam voor pagina's en mails (null als het document niet bij een project hoort).</summary>
    Task<string?> GetProjectNameAsync(int sourceEntityId, CancellationToken ct = default);

    /// <summary>Eenheid waar het document over gaat (bv. "Woning Lot 2"), voor de ondertekenpagina; null als er geen is.
    /// Standaard null, zodat bronnen die er geen hebben niets hoeven te doen.</summary>
    Task<string?> GetUnitLabelAsync(int sourceEntityId, CancellationToken ct = default) => Task.FromResult<string?>(null);

    /// <summary>Naam van het bedrijf dat het document uitgeeft (bv. "BCO"), voor de verificatiepagina; standaard null.</summary>
    Task<string?> GetIssuerNameAsync(int sourceEntityId, CancellationToken ct = default) => Task.FromResult<string?>(null);

    /// <summary>Actuele bedragen van het document (excl., btw, incl.) voor de ondertekenpagina; null = gebruik wat in het dossier staat.</summary>
    Task<(decimal Excl, decimal Vat, decimal Incl)?> GetAmountsAsync(int sourceEntityId, CancellationToken ct = default) => Task.FromResult<(decimal Excl, decimal Vat, decimal Incl)?>(null);

    /// <summary>Interne e-mailadressen die verwittigd worden bij voltooiing/weigering (bv. de projectleider).</summary>
    Task<IReadOnlyList<SigningMailRecipient>> GetInternalNotificationRecipientsAsync(int sourceEntityId, CancellationToken ct = default);
}
