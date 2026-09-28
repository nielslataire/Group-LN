namespace FacadeCore.Signing;

/// <summary>
/// Bewijs- en auditgegevens (ONDERTEKENEN_VOORSTEL.md §4.6). De enige weg naar <c>SigningEvent</c>:
/// berekent per event de hash-ketting (§3.7) en schrijft append-only. Niemand anders schrijft in
/// die tabel, zodat de ketting nooit een gat krijgt.
/// </summary>
public interface ISigningEvidenceStore
{
    /// <summary>Voegt een event toe binnen de lopende transactie van de aanroeper (of zonder, als er geen is) en geeft het id terug.</summary>
    Task<long> AppendAsync(SigningEventDraft draft, CancellationToken ct = default);

    Task<IReadOnlyList<SigningEventView>> ListAsync(int caseId, CancellationToken ct = default);

    /// <summary>Herberekent de ketting van begin tot einde en meldt het eerste event waar ze breekt.</summary>
    Task<ChainVerification> VerifyAsync(int caseId, CancellationToken ct = default);
}
