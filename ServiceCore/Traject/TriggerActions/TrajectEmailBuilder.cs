using System.Net;
using DALCore.Models;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>Minimale HTML-e-mail voor trajecttriggers (kloon van de stijl in ServiceCore/Issues/IssueEmailHtmlBuilder).</summary>
internal static class TrajectEmailBuilder
{
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
