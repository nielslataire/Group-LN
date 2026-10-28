using System.Text.RegularExpressions;
using CPMCore.Helpers;
using CPMCore.Models.Account;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

// gl-v2 — Account › Mijn profiel (design-handoff "CRM Mijn profiel.dc.html"): Profiel, Handtekening, Meldingen en Wachtwoord en beveiliging
// op één pagina met één Opslaan. De oude pagina Mijn handtekening blijft bestaan voor ?classic=true.
public partial class AccountController
{
    private static readonly Regex Uur = new(@"^([01]\d|2[0-3]):[0-5]\d$", RegexOptions.Compiled);
    private static readonly string[] Tabs = { "profiel", "handtekening", "meldingen", "beveiliging" };

    [HttpGet]
    public async Task<IActionResult> MijnProfiel(string? tab = null, CancellationToken ct = default)
    {
        var userId = User.GetCpmUserId();
        if (userId == null)
            return Forbid();

        var vm = await LaadProfielAsync(userId.Value, ct);
        if (vm == null)
            return NotFound();
        vm.Tab = Tabs.Contains(tab ?? "") ? tab! : "profiel";
        return RenderProfiel(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MijnProfiel(MijnProfielVM vm, CancellationToken ct = default)
    {
        var userId = User.GetCpmUserId();
        if (userId == null)
            return Forbid();

        Valideer(vm);
        if (!ModelState.IsValid)
        {
            var basis = await LaadProfielAsync(userId.Value, ct);
            if (basis == null) return NotFound();
            // Enkel weergavevelden komen uit de database; wat de gebruiker intikte blijft staan.
            vm.Email = basis.Email; vm.RolNaam = basis.RolNaam; vm.AantalProjecten = basis.AantalProjecten;
            vm.AanmeldMethode = basis.AanmeldMethode; vm.Collegas = basis.Collegas; vm.Outlook = basis.Outlook;
            // Open het tabblad van de eerste fout, zodat de melding niet achter een ander tabblad verdwijnt.
            var eerste = ModelState.Where(k => k.Value?.Errors.Count > 0).Select(k => k.Key).FirstOrDefault() ?? "";
            vm.Tab = eerste.StartsWith("Samenvatting") || eerste.StartsWith("Deadline") ? "meldingen" : "profiel";
            return RenderProfiel(vm);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user == null)
            return NotFound();

        user.Voornaam = vm.Voornaam!.Trim();
        user.Familienaam = vm.Familienaam?.Trim();
        user.Functie = string.IsNullOrWhiteSpace(vm.Functie) ? null : vm.Functie.Trim();
        user.Gsm = string.IsNullOrWhiteSpace(vm.Gsm) ? null : vm.Gsm.Trim();

        var inst = await _db.UserProfileSettings.FirstOrDefaultAsync(s => s.UserId == userId.Value, ct);
        if (inst == null)
        {
            inst = new UserProfileSettings { UserId = userId.Value };
            _db.UserProfileSettings.Add(inst);
        }
        inst.VastNummer = string.IsNullOrWhiteSpace(vm.VastNummer) ? null : vm.VastNummer.Trim();
        inst.WerkDagen = vm.WerkDagen & 127;
        inst.WerkVan = vm.WerkVan; inst.WerkTot = vm.WerkTot;
        inst.PauzeVan = string.IsNullOrWhiteSpace(vm.PauzeVan) ? null : vm.PauzeVan;
        inst.PauzeTot = string.IsNullOrWhiteSpace(vm.PauzeTot) ? null : vm.PauzeTot;
        inst.AfwezigAan = vm.AfwezigAan;
        inst.AfwezigVan = vm.AfwezigVan?.Date; inst.AfwezigTot = vm.AfwezigTot?.Date;
        inst.BackupUserId = vm.BackupUserId; inst.AfwezigOpties = vm.AfwezigOpties & 7;
        inst.MeldingenJson = MeldingCatalogus.Schoon(vm.MeldingenJson);
        inst.DagSamenvatting = vm.DagSamenvatting; inst.SamenvattingTijd = vm.SamenvattingTijd;
        inst.StilleUren = vm.StilleUren; inst.DringendDoorlaten = vm.DringendDoorlaten;
        inst.DeadlineHerinnering = vm.DeadlineHerinnering;
        inst.GewijzigdOp = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var response = _userSignatureService.Save(userId.Value, vm.SignatureHtml ?? string.Empty, vm.Format);
        if (response.HasErrors)
            AddMessage("error", "Je handtekening kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Je profiel is opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(MijnProfiel), new { tab = Tabs.Contains(vm.Tab) ? vm.Tab : "profiel" });
    }

    // Outlook-koppeling (spec #2): aan/uit gaat meteen, los van Opslaan. Standaard uit; enkel de gebruiker zelf zet ze aan.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OutlookAan(CancellationToken ct = default)
    {
        var userId = User.GetCpmUserId();
        if (userId == null)
            return Forbid();
        await HttpContext.RequestServices.GetRequiredService<CPMCore.Services.OutlookSync.OutlookSyncService>().ZetAanAsync(userId.Value, DateTime.UtcNow, ct);
        AddMessage("success", "De Outlook-koppeling staat aan. De eerste synchronisatie volgt binnen enkele minuten.", "Outlook");
        return RedirectToAction(nameof(MijnProfiel), new { tab = "profiel" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OutlookUit(CancellationToken ct = default)
    {
        var userId = User.GetCpmUserId();
        if (userId == null)
            return Forbid();
        await HttpContext.RequestServices.GetRequiredService<CPMCore.Services.OutlookSync.OutlookSyncService>().ZetUitAsync(userId.Value, DateTime.UtcNow, ct);
        AddMessage("success", "De Outlook-koppeling staat uit. CPM leest je Outlook niet meer.", "Outlook");
        return RedirectToAction(nameof(MijnProfiel), new { tab = "profiel" });
    }

    private IActionResult RenderProfiel(MijnProfielVM vm)
    {
        SetPageHeader("bx bx-user", "Mijn profiel");
        // Mijn profiel bestaat enkel op gl-v2; wie de oude shell heeft, krijgt alsnog de gl-v2-pagina.
        ViewData["UseGlV2Layout"] = true;
        return View("MijnProfielV2", vm);
    }

    private void Valideer(MijnProfielVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Voornaam))
            ModelState.AddModelError(nameof(vm.Voornaam), "Vul je voornaam in.");
        foreach (var (naam, waarde, verplicht) in new[]
        {
            (nameof(vm.WerkVan), vm.WerkVan, true), (nameof(vm.WerkTot), vm.WerkTot, true),
            (nameof(vm.PauzeVan), vm.PauzeVan, false), (nameof(vm.PauzeTot), vm.PauzeTot, false),
            (nameof(vm.SamenvattingTijd), vm.SamenvattingTijd, true)
        })
        {
            if (string.IsNullOrWhiteSpace(waarde) ? verplicht : !Uur.IsMatch(waarde))
                ModelState.AddModelError(naam, "Geef een uur als uu:mm, bv. 08:30.");
        }
        if (ModelState.IsValid && string.CompareOrdinal(vm.WerkTot, vm.WerkVan) <= 0)
            ModelState.AddModelError(nameof(vm.WerkTot), "Je werkuren eindigen na het begin.");
        if (!MeldingCatalogus.Herinneringen.Any(h => h.Id == vm.DeadlineHerinnering))
            ModelState.AddModelError(nameof(vm.DeadlineHerinnering), "Kies een herinnering.");
        if (vm.AfwezigAan)
        {
            if (vm.AfwezigVan == null || vm.AfwezigTot == null)
                ModelState.AddModelError(nameof(vm.AfwezigVan), "Vul de periode van je afwezigheid in.");
            else if (vm.AfwezigTot < vm.AfwezigVan)
                ModelState.AddModelError(nameof(vm.AfwezigTot), "De einddatum ligt vóór de begindatum.");
            if ((vm.AfwezigOpties & 5) != 0 && vm.BackupUserId == null)
                ModelState.AddModelError(nameof(vm.BackupUserId), "Kies een back-up of zet die opties uit.");
        }
    }

    private async Task<MijnProfielVM?> LaadProfielAsync(int userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return null;
        var inst = await _db.UserProfileSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct) ?? new UserProfileSettings();
        var handtekening = _userSignatureService.GetByUserId(userId).Value;
        var rol = await _db.SecurityUserRole.AsNoTracking().Where(r => r.UserId == userId).Select(r => r.Role.Name).FirstOrDefaultAsync(ct);
        var aantalProjecten = await _db.ProjectUserAccess.AsNoTracking().Where(a => a.UserId == userId).Select(a => a.ProjectId).Distinct().CountAsync(ct);
        var collegas = await _db.Users.AsNoTracking().Where(u => u.IsActive && u.Id != userId && u.Email != null)
            .OrderBy(u => u.Voornaam).ThenBy(u => u.Familienaam).Select(u => new { u.Id, u.Voornaam, u.Familienaam }).ToListAsync(ct);

        return new MijnProfielVM
        {
            Voornaam = user.Voornaam, Familienaam = user.Familienaam, Functie = user.Functie, Gsm = user.Gsm, Email = user.Email ?? "",
            VastNummer = inst.VastNummer, WerkDagen = inst.WerkDagen, WerkVan = inst.WerkVan, WerkTot = inst.WerkTot, PauzeVan = inst.PauzeVan, PauzeTot = inst.PauzeTot,
            AfwezigAan = inst.AfwezigAan, AfwezigVan = inst.AfwezigVan, AfwezigTot = inst.AfwezigTot, BackupUserId = inst.BackupUserId, AfwezigOpties = inst.AfwezigOpties,
            MeldingenJson = MeldingCatalogus.Schoon(inst.MeldingenJson), DagSamenvatting = inst.DagSamenvatting, SamenvattingTijd = inst.SamenvattingTijd,
            StilleUren = inst.StilleUren, DringendDoorlaten = inst.DringendDoorlaten, DeadlineHerinnering = inst.DeadlineHerinnering,
            SignatureHtml = handtekening?.SignatureHtml,
            Format = string.IsNullOrWhiteSpace(handtekening?.SignatureFormat) ? "Visual" : handtekening.SignatureFormat,
            RolNaam = rol ?? "", AantalProjecten = aantalProjecten,
            AanmeldMethode = User.IsGoogleLogin() ? "Google" : "Microsoft",
            Collegas = collegas.Select(c => (c.Id, $"{c.Voornaam} {c.Familienaam}".Trim())).ToList(),
            Outlook = await HttpContext.RequestServices.GetRequiredService<CPMCore.Services.OutlookSync.OutlookSyncService>().WeergaveVoorAsync(userId, DateTime.UtcNow, ct)
        };
    }
}
