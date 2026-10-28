using System.Globalization;
using CPMCore.Services.MijnTaken;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

// gl-v2 — Mijn taken als persoonlijke assistent (design-handoff punt 2, "CRM Mijn taken en Deadlines"): Vandaag, Week, Inbox en Overzicht
// met een timer en snel starten. De klassieke lijst blijft bereikbaar via ?classic=true. Schrijfacties geven JSON terug ({ ok, bericht, ... }).
public partial class MijnTakenController
{
    private bool IsV2 => ViewData["UseGlV2Layout"] as bool? == true;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static DateOnly? Week(string? datum) =>
        DateOnly.TryParseExact(datum, "yyyy-MM-dd", Inv, DateTimeStyles.None, out var d) ? MijnTakenAssistent.Maandag(d) : null;

    private static DateTime? Tijd(string? s) =>
        DateTime.TryParseExact(s, "yyyy-MM-dd'T'HH:mm", Inv, DateTimeStyles.None, out var d) ? d : null;

    private JsonResult Fout(string bericht) => Json(new { ok = false, bericht });

    private async Task<IActionResult> VandaagV2()
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        ViewData["HelpScreenId"] = "mijn-taken-vandaag";
        return View("VandaagV2", await _assistent.Vandaag(c));
    }

    [HttpGet("Week")]
    public async Task<IActionResult> WeekV2(string? datum = null)
    {
        SetPageHeader("ph ph-list-checks", "Mijn taken");
        ViewData["UseGlV2Layout"] = true;
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        ViewData["HelpScreenId"] = "mijn-taken-week";
        var maandag = Week(datum) ?? MijnTakenAssistent.Maandag(DateOnly.FromDateTime(DateTime.Today));
        return View("WeekV2", await _assistent.Week(c, maandag));
    }

    [HttpGet("Inbox")]
    public async Task<IActionResult> InboxV2()
    {
        SetPageHeader("ph ph-list-checks", "Mijn taken");
        ViewData["UseGlV2Layout"] = true;
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        ViewData["HelpScreenId"] = "mijn-taken-inbox";
        return View("InboxV2", await _assistent.Inbox(c));
    }

    [HttpGet("Overzicht")]
    public async Task<IActionResult> OverzichtV2(string? datum = null)
    {
        SetPageHeader("ph ph-list-checks", "Mijn taken");
        ViewData["UseGlV2Layout"] = true;
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        ViewData["HelpScreenId"] = "mijn-taken-overzicht";
        var maandag = Week(datum) ?? MijnTakenAssistent.Maandag(DateOnly.FromDateTime(DateTime.Today));
        return View("OverzichtV2", await _assistent.Overzicht(c, maandag));
    }

    // ── Vastleggen en verwerken ──────────────────────────────────────────────────────────────────

    [HttpPost("MailAfgehandeld"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> MailAfgehandeld(int id)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        return await _assistent.MailAfgehandeld(c, id) ? Json(new { ok = true }) : Fout("Die mail werd niet gevonden.");
    }

    [HttpPost("Leg"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> Leg(string titel)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var id = await _assistent.Leg(c, titel);
        return id == 0 ? Fout("Typ eerst wat je wilt vastleggen.") : Json(new { ok = true, id });
    }

    [HttpPost("Verwerk"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> Verwerk(int id, string actie, int? projectId, string? context, int? schatting, string? wanneer, string? wachtOp, string? wachtOpDatum)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        DateTime? wachtDatum = DateTime.TryParseExact(wachtOpDatum, "yyyy-MM-dd", Inv, DateTimeStyles.None, out var wd) ? wd : null;
        var fout = await _assistent.Verwerk(c, id, actie, projectId, context, schatting, Tijd(wanneer), wachtOp, wachtDatum);
        return fout == null ? Json(new { ok = true }) : Fout(fout);
    }

    // ── Plannen en afvinken ─────────────────────────────────────────────────────────────────────

    [HttpPost("Plan"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> Plan(int id, string start, int? minuten)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var tijd = Tijd(start);
        if (tijd == null) return Fout("Ongeldig tijdstip.");
        var fout = await _assistent.Plan(c, id, tijd.Value, minuten);
        return fout == null ? Json(new { ok = true }) : Fout(fout);
    }

    [HttpPost("PlanWijzig"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> PlanWijzig(int id, string? titel, string start, int minuten)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var tijd = Tijd(start);
        if (tijd == null) return Fout("Ongeldig tijdstip.");
        var fout = await _assistent.PlanWijzig(c, id, titel, tijd.Value, minuten);
        return fout == null ? Json(new { ok = true }) : Fout(fout);
    }

    [HttpPost("NieuwBlok"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> NieuwBlok(string? titel, string start, int minuten)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var tijd = Tijd(start);
        if (tijd == null) return Fout("Ongeldig tijdstip.");
        var (id, fout) = await _assistent.MaakBlok(c, titel, tijd.Value, minuten);
        return fout == null ? Json(new { ok = true, id }) : Fout(fout);
    }

    [HttpPost("AgendaWijzig"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> AgendaWijzig(string eventId, string? titel, string start, int minuten)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var tijd = Tijd(start);
        if (tijd == null) return Fout("Ongeldig tijdstip.");
        var fout = await _assistent.AgendaWijzig(c, eventId, titel, tijd.Value, minuten);
        return fout == null ? Json(new { ok = true }) : Fout(fout);
    }

    [HttpPost("Ontplan"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> Ontplan(int id)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var fout = await _assistent.Ontplan(c, id);
        return fout == null ? Json(new { ok = true }) : Fout(fout);
    }

    [HttpPost("Afvinken"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> Afvinken(int id, bool klaar = true)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var fout = await _assistent.Afvink(c, id, klaar);
        return fout == null ? Json(new { ok = true }) : Fout(fout);
    }

    // ── Timer ───────────────────────────────────────────────────────────────────────────────────

    [HttpPost("TimerStart"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> TimerStart(int? taakId, string? titel, int? projectId, string? context, int terugMinuten = 0)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var (fout, vorige) = await _assistent.Start(c, taakId, titel, projectId, context, terugMinuten);
        return fout == null ? Json(new { ok = true, vorige }) : Fout(fout);
    }

    [HttpPost("TimerPauze"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> TimerPauze()
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        await _assistent.Pauze(c);
        return Json(new { ok = true });
    }

    [HttpPost("TimerHervat"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> TimerHervat()
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var fout = await _assistent.Hervat(c);
        return fout == null ? Json(new { ok = true }) : Fout(fout);
    }

    [HttpPost("TimerKlaar"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> TimerKlaar()
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var fout = await _assistent.Klaar(c);
        return fout == null ? Json(new { ok = true }) : Fout(fout);
    }

    /// <summary>Het aantal items in de Inbox, voor de teller die zichzelf bijwerkt zonder dat de pagina ververst.</summary>
    [HttpGet("InboxAantal")]
    public async Task<IActionResult> InboxAantal()
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        return Json(new { ok = true, aantal = await _assistent.InboxAantal(c) });
    }

    /// <summary>Snel starten: bestaande taken die bij de getypte woorden passen, of de recente taken als er niets getypt is.</summary>
    [HttpGet("Snel")]
    public async Task<IActionResult> Snel(string? q)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var taken = string.IsNullOrWhiteSpace(q) ? await _assistent.Recent(c) : await _assistent.Zoek(c, q);
        return Json(new
        {
            ok = true, recent = string.IsNullOrWhiteSpace(q),
            taken = taken.Select(t => new { t.Id, t.Titel, t.Project, t.Kleur }),
            projecten = c.Projecten.Values.Select(p => new { p.Id, p.Naam, p.Kleur })
        });
    }

    // ── Deadlines en review ─────────────────────────────────────────────────────────────────────

    [HttpPost("TaakVanMijlpaal"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> TaakVanMijlpaal(int mijlpaalId)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var (ok, fout) = await _assistent.MaakTaakVanMijlpaal(c, mijlpaalId);
        return ok ? Json(new { ok = true }) : Fout(fout ?? "Niet gelukt.");
    }

    [HttpPost("ReviewZet"), ValidateAntiForgeryToken, CPMCore.Filters.PermissionWrite(BOCore.PermissionCodes.MijnTaken)]
    public async Task<IActionResult> ReviewZet(string datum, int punt, bool aan)
    {
        var c = await _assistent.Context(User);
        if (c == null) return Forbid();
        var week = Week(datum);
        if (week == null) return Fout("Ongeldige week.");
        await _assistent.ZetReview(c, week.Value, punt, aan);
        return Json(new { ok = true });
    }
}
