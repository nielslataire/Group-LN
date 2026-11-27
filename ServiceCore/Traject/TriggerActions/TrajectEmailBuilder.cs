using System.Net;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>Minimale HTML-e-mail voor trajecttriggers (kloon van de stijl in ServiceCore/Issues/IssueEmailHtmlBuilder).</summary>
internal static class TrajectEmailBuilder
{
    /// <summary>Onderwerp en inhoud van een trigger-mail, met de eenheid, de opdracht (trigger, standaardtekst of algemene zin) en de link naar het traject.</summary>
    internal static async Task<(string Onderwerp, string Body)> MaakAsync(cpmRunningContext db, string? baseUrl, string voorvoegsel, string aanspreking,
        Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, CancellationToken ct)
    {
        string? eenheid = mijlpaal.UnitId == null ? null : await db.Units.Where(u => u.Id == mijlpaal.UnitId).Select(u => u.Name).FirstOrDefaultAsync(ct);
        var parameters = TriggerParamHelper.Parse(trigger.ActieParametersJson);
        var wat = MijlpaalVerwachting.Wat(mijlpaal, TriggerParamHelper.GetString(parameters, "omschrijving"));
        var waarom = MijlpaalVerwachting.Waarom(mijlpaal, trigger, DateOnly.FromDateTime(DateTime.Today));
        var link = TrajectLink(baseUrl, project.ProjectId, mijlpaal.Id);
        return (Onderwerp(voorvoegsel, mijlpaal, eenheid, project.ProjectName ?? ""),
                BuildActie(project.ProjectName ?? "", mijlpaal, eenheid, waarom, wat, link, aanspreking));
    }

    /// <summary>Basisadres voor de link in de mail (App:BaseUrl); leeg = geen knop.</summary>
    internal static string? TrajectLink(string? baseUrl, int projectId, int mijlpaalId)
        => string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl.TrimEnd('/')}/Projects/{projectId}/Traject?hl={mijlpaalId}";

    /// <summary>Onderwerp met de aanleiding vooraan, zodat de lezer in de inbox al ziet dat er iets van hem verwacht wordt.</summary>
    internal static string Onderwerp(string voorvoegsel, Mijlpaal mijlpaal, string? eenheid, string projectNaam)
        => $"{voorvoegsel}: {mijlpaal.Naam}{(string.IsNullOrWhiteSpace(eenheid) ? "" : " — " + eenheid)} ({projectNaam})";

    /// <summary>
    /// Mail met vier vaste delen: waarom je dit krijgt, wat je moet doen, voor welk project/eenheid, en een knop naar het traject.
    /// </summary>
    internal static string BuildActie(string projectName, Mijlpaal mijlpaal, string? eenheid, string waarom, string wat, string? link, string? aanspreking = null)
    {
        var knop = link == null ? "" : $"""
              <p style="margin:20px 0 4px"><a href="{WebUtility.HtmlEncode(link)}" style="background:#0a5a3b;color:#fff;text-decoration:none;padding:10px 18px;border-radius:8px;display:inline-block;font-weight:600">Open het traject</a></p>
            """;
        var opmerking = string.IsNullOrWhiteSpace(mijlpaal.Opmerking) ? "" :
            $"""<tr><td style="color:#8892a4;padding:2px 12px 2px 0;vertical-align:top">Opmerking</td><td>{WebUtility.HtmlEncode(mijlpaal.Opmerking)}</td></tr>""";
        var eenheidRij = string.IsNullOrWhiteSpace(eenheid) ? "" :
            $"""<tr><td style="color:#8892a4;padding:2px 12px 2px 0">Eenheid</td><td>{WebUtility.HtmlEncode(eenheid)}</td></tr>""";
        var datum = (mijlpaal.Doeldatum ?? mijlpaal.DoeldatumBerekend)?.ToString("dd/MM/yyyy");
        var datumRij = datum == null ? "" :
            $"""<tr><td style="color:#8892a4;padding:2px 12px 2px 0">Streefdatum</td><td>{datum}</td></tr>""";
        return $"""
            <div style="font-family:'Segoe UI',Helvetica,Arial,sans-serif;color:#1f2937;max-width:560px">
              <p style="color:#8892a4;font-size:12px;margin:0 0 4px">{WebUtility.HtmlEncode(aanspreking ?? "Actie nodig")}</p>
              <h2 style="color:#0a5a3b;margin:0 0 4px">{WebUtility.HtmlEncode(mijlpaal.Naam)}</h2>
              <p style="color:#4b5563;margin:0 0 14px">{WebUtility.HtmlEncode(projectName)}</p>
              <p style="margin:0 0 14px">{WebUtility.HtmlEncode(waarom)}</p>
              <div style="background:#eef6f1;border-left:4px solid #0a5a3b;padding:10px 14px;margin:0 0 14px">
                <div style="font-weight:600;margin-bottom:4px">Wat moet je doen?</div>
                <div>{WebUtility.HtmlEncode(wat)}</div>
              </div>
              <table style="border-collapse:collapse;font-size:14px">
                <tr><td style="color:#8892a4;padding:2px 12px 2px 0">Project</td><td>{WebUtility.HtmlEncode(projectName)}</td></tr>
                {eenheidRij}{datumRij}{opmerking}
              </table>
            {knop}
              <p style="color:#8892a4;font-size:12px;margin-top:18px">Automatisch bericht van de trajectopvolging in CPM. Je krijgt het omdat jouw rol bij deze mijlpaal hoort. Is de mijlpaal al afgewerkt, vink ze dan af in het traject, dan komt er geen herinnering meer.</p>
            </div>
            """;
    }

    internal static string Build(string projectName, Mijlpaal mijlpaal, string aanleiding)
    {
        var datum = (mijlpaal.WerkelijkeDatum ?? mijlpaal.Doeldatum ?? mijlpaal.DoeldatumBerekend)?.ToString("dd/MM/yyyy") ?? "—";
        return $"""
            <div style="font-family:'Segoe UI',Helvetica,Arial,sans-serif;color:#1f2937;max-width:520px">
              <h2 style="color:#0a5a3b;margin:0 0 12px">{WebUtility.HtmlEncode(projectName)}</h2>
              <p style="margin:0 0 8px">Mijlpaal <strong>{WebUtility.HtmlEncode(mijlpaal.Naam)}</strong> {WebUtility.HtmlEncode(aanleiding)}.</p>
              <table style="border-collapse:collapse;font-size:14px">
                <tr><td style="color:#8892a4;padding:2px 8px 2px 0">Datum</td><td>{datum}</td></tr>
                <tr><td style="color:#8892a4;padding:2px 8px 2px 0">Opmerking</td><td>{WebUtility.HtmlEncode(mijlpaal.Opmerking ?? "—")}</td></tr>
              </table>
              <p style="color:#8892a4;font-size:12px;margin-top:16px">Automatisch bericht van de trajectopvolging — CPM.</p>
            </div>
            """;
    }
}
