using BOCore;
using DALCore.Models;

namespace ServiceCore.Traject;

/// <summary>
/// Tweerichtings-koppeling tussen BOCore.NutsChecklistDefaults-codes en de getypeerde datumvelden op
/// ProjectNutsAansluiting. Gedeeld tussen NutsAansluitingService (bewerkformulier -> checklist,
/// bij Opslaan) en ProjectDossierService.ChangeSubstapStatus (checklist-knop "Zet" -> formulier),
/// zodat beide plekken waar een gebruiker deze datums kan zetten elkaar in sync houden — vóór dit
/// bestand liep de spiegeling maar één kant op: een datum gezet via de checklist op de dossierpagina
/// verscheen niet terug in het bewerkformulier.
/// </summary>
internal static class NutsChecklistSpiegel
{
    public static readonly (string Code, Func<ProjectNutsAansluiting, DateOnly?> Get, Action<ProjectNutsAansluiting, DateOnly?> Set)[] Velden =
    {
        ("AANVRAAG", n => n.AanvraagVerstuurdOp, (n, d) => n.AanvraagVerstuurdOp = d),
        ("OFFERTE_ONTVANGEN", n => n.OfferteOntvangenOp, (n, d) => n.OfferteOntvangenOp = d),
        ("OFFERTE_GOEDGEKEURD", n => n.OfferteGoedgekeurdOp, (n, d) => n.OfferteGoedgekeurdOp = d),
        ("UITVOERINGSDATUM_DOORGEGEVEN", n => n.UitvoeringGevraagdOp, (n, d) => n.UitvoeringGevraagdOp = d),
        ("UITGEVOERD", n => n.UitgevoerdOp, (n, d) => n.UitgevoerdOp = d),
    };

    /// <summary>
    /// Leidt DossierStatus af uit de werkstroomdatums i.p.v. een apart handmatig veld — enige
    /// uitzondering is Geannuleerd, want geen enkele datum in dit model betekent "afgeblazen".
    /// Gedeeld tussen dezelfde twee plekken als Velden hierboven: NutsAansluitingService
    /// (bewerkformulier) en ProjectDossierService.ChangeSubstapStatus (checklist-knop "Zet") moeten
    /// hier allebei doorheen, anders herhaalt zich exact de bug die Velden zelf oploste — een
    /// dossier "Afgehandeld" via de ene weg, maar niet via de andere.
    /// </summary>
    public static int BerekenStatus(ProjectNutsAansluiting nuts, bool geannuleerd)
    {
        if (geannuleerd) return (int)DossierStatus.Geannuleerd;
        if (nuts.UitgevoerdOp.HasValue) return (int)DossierStatus.Afgehandeld;
        if (nuts.OfferteOntvangenOp.HasValue || nuts.OfferteGoedgekeurdOp.HasValue || nuts.UitvoeringGevraagdOp.HasValue)
            return (int)DossierStatus.InBehandeling;
        if (nuts.AanvraagVerstuurdOp.HasValue) return (int)DossierStatus.Aangevraagd;
        return (int)DossierStatus.Nieuw;
    }
}
