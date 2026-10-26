using BOCore;
using CPMCore.Helpers;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

// gl-v2 — uitnodigen, importeren en koppelen met Entra (design-handoff punt 49a): "Gebruiker uitnodigen", "Importeren uit Entra" en
// "Koppelingen nakijken". Entra is een koppeling, geen aparte lijst: niet-gekoppelde Entra-accounts verschijnen enkel in de nakijklijst met
// een voorstel op basis van het e-mailadres. Zonder Graph-configuratie (AzureAd-instellingen) zijn de Entra-lijsten leeg.
public partial class UserAdminController
{
    private static string NormEmail(string? e) => (e ?? "").Trim().ToLowerInvariant();

    private static (string voornaam, string naam) SplitName(string? displayName)
    {
        var d = (displayName ?? "").Trim();
        var i = d.IndexOf(' ');
        return i < 0 ? (d, "") : (d[..i], d[(i + 1)..].Trim());
    }

    private async Task<int> NextUserIdAsync() => await _db.Users.AnyAsync() ? await _db.Users.MaxAsync(u => u.Id) + 1 : 1;

    private async Task SetSingleRole(int userId, int? roleId)
    {
        if (!roleId.HasValue || !await _db.SecurityRole.AnyAsync(r => r.RoleId == roleId.Value && r.IsActive)) return;
        _db.SecurityUserRole.RemoveRange(await _db.SecurityUserRole.Where(x => x.UserId == userId).ToListAsync());
        _db.SecurityUserRole.Add(new SecurityUserRole { UserId = userId, RoleId = roleId.Value });
        await _db.SaveChangesAsync();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> GebruikerUitnodigenV2(string? voornaam, string? naam, string? email, string? type, int? roleId)
    {
        var mail = (email ?? "").Trim();
        if (mail.Length == 0 || !mail.Contains('@')) return Fout("Geef een geldig e-mailadres in.");
        if (await _db.Users.AnyAsync(u => u.Email == mail || u.UserId == mail)) return Fout("Dit e-mailadres is al in gebruik.");
        var gast = string.Equals(type, "gast", StringComparison.OrdinalIgnoreCase);

        var user = new Users
        {
            Id = await NextUserIdAsync(),
            UserId = mail,
            Email = mail,
            Voornaam = voornaam?.Trim() ?? "",
            Familienaam = naam?.Trim() ?? "",
            Functie = "",
            Gsm = "",
            Password = "",
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await SetSingleRole(user.Id, roleId);

        string? waarschuwing = null;
        if (gast)
        {
            var result = await _guestInvitationService.InviteGuestAsync(user.Id, User.GetCpmUserId(), $"{Request.Scheme}://{Request.Host}");
            if (!result.Success) waarschuwing = result.ErrorMessage ?? "De uitnodiging kon niet verstuurd worden. Je kunt ze opnieuw proberen vanuit het menu van de gebruiker.";
        }
        return Json(new { ok = true, id = user.Id, bericht = waarschuwing });
    }

    /// <summary>Uitnodiging versturen, opnieuw versturen, resetten of de gastkoppeling verbreken — zelfde service als de oude knoppen, maar met JSON.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> UitnodigingV2(int userId, string actie)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var by = User.GetCpmUserId();
        var result = actie switch
        {
            "uitnodigen" => await _guestInvitationService.InviteGuestAsync(userId, by, baseUrl),
            "opnieuw" => await _guestInvitationService.ResendInvitationAsync(userId, by, baseUrl),
            "reset" => await _guestInvitationService.ResetRedemptionAsync(userId, by),
            "ontkoppel" => await _guestInvitationService.UnlinkGuestAsync(userId, by),
            _ => null
        };
        if (result == null) return Fout("Onbekende actie.");
        return Json(new { ok = result.Success, bericht = result.Success ? null : result.ErrorMessage ?? "Dat is niet gelukt." });
    }

    [HttpGet]
    public async Task<IActionResult> EntraKandidatenV2()
    {
        try
        {
            var entra = await LoadEntraUsersAsync();
            var bekendeIds = (await _db.Users.AsNoTracking().Where(u => u.EntraObjectId != null).Select(u => u.EntraObjectId!).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var bekendeMails = (await _db.Users.AsNoTracking().Select(u => u.Email).ToListAsync()).Select(NormEmail).ToHashSet();
            var kandidaten = entra.Where(e => !bekendeIds.Contains(e.Id) && !bekendeMails.Contains(NormEmail(string.IsNullOrWhiteSpace(e.Email) ? e.UserPrincipalName : e.Email)))
                .Select(e => new { id = e.Id, naam = e.DisplayName, email = string.IsNullOrWhiteSpace(e.Email) ? e.UserPrincipalName : e.Email, gast = e.IsGuest });
            return Json(new { ok = true, beschikbaar = entra.Count > 0, kandidaten });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Entra-gebruikers konden niet geladen worden.");
            return Json(new { ok = false, bericht = "Entra is niet bereikbaar. Controleer de koppeling en probeer het opnieuw." });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> EntraImporterenV2(string[]? ids, int? roleId)
    {
        if (ids == null || ids.Length == 0) return Fout("Kies minstens één Entra-account.");
        try
        {
            var entra = (await LoadEntraUsersAsync()).ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);
            var aantal = 0;
            foreach (var id in ids.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!entra.TryGetValue(id, out var e)) continue;
                var mail = string.IsNullOrWhiteSpace(e.Email) ? e.UserPrincipalName ?? "" : e.Email;
                if (mail.Length == 0 || await _db.Users.AnyAsync(u => u.EntraObjectId == e.Id || u.Email == mail || u.UserId == mail)) continue;
                var (voornaam, naam) = SplitName(e.DisplayName);
                var user = new Users
                {
                    Id = await NextUserIdAsync(), UserId = mail, Email = mail, Voornaam = voornaam, Familienaam = naam,
                    Functie = "", Gsm = "", Password = "", IsActive = true, EntraObjectId = e.Id
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
                await SetSingleRole(user.Id, roleId);
                aantal++;
            }
            return Json(new { ok = true, aantal });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Importeren uit Entra mislukt.");
            return Fout("Importeren uit Entra is mislukt. Probeer het opnieuw.");
        }
    }

    /// <summary>Entra-accounts die nog aan geen enkele gebruiker hangen, met een voorstel (gebruiker met hetzelfde e-mailadres).</summary>
    [HttpGet]
    public async Task<IActionResult> KoppelingenV2()
    {
        try
        {
            var entra = await LoadEntraUsersAsync();
            var users = await _db.Users.AsNoTracking().ToListAsync();
            var gekoppeld = users.Where(u => !string.IsNullOrWhiteSpace(u.EntraObjectId)).Select(u => u.EntraObjectId!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var invites = (await _db.UserGuestInvitation.AsNoTracking().Where(i => i.ExternalObjectId != null).Select(i => new { i.UserId, i.ExternalObjectId }).ToListAsync());
            foreach (var i in invites) gekoppeld.Add(i.ExternalObjectId!);
            var aannemers = (await _db.UserCompanyAccess.AsNoTracking().Select(a => a.UserId).Distinct().ToListAsync()).ToHashSet();
            var ongekoppeld = entra.Where(e => e.IsGuest && !gekoppeld.Contains(e.Id)).ToList();
            var voorstellen = ongekoppeld.Select(e =>
            {
                var mail = NormEmail(string.IsNullOrWhiteSpace(e.Email) ? e.UserPrincipalName : e.Email);
                var match = users.FirstOrDefault(u => NormEmail(u.Email) == mail && string.IsNullOrWhiteSpace(u.EntraObjectId));
                return new
                {
                    entraId = e.Id,
                    entraNaam = e.DisplayName,
                    entraEmail = string.IsNullOrWhiteSpace(e.Email) ? e.UserPrincipalName : e.Email,
                    gebruikerId = match?.Id,
                    gebruikerNaam = match == null ? null : DisplayName(match),
                    aannemer = match != null && aannemers.Contains(match.Id)
                };
            }).ToList();
            return Json(new { ok = true, beschikbaar = entra.Count > 0, aantal = voorstellen.Count, metAannemer = voorstellen.Count(v => v.aannemer), voorstellen });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Entra-koppelingen konden niet nagekeken worden.");
            return Json(new { ok = false, bericht = "Entra is niet bereikbaar. Controleer de koppeling en probeer het opnieuw." });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> KoppelenV2(int userId, string? entraId)
    {
        var id = entraId?.Trim();
        if (string.IsNullOrWhiteSpace(id)) return Fout("Geen Entra-account gekozen.");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Fout("Deze gebruiker bestaat niet meer.");
        if (await _db.Users.AnyAsync(u => u.Id != userId && u.EntraObjectId == id)) return Fout("Dit Entra-account is al aan een andere gebruiker gekoppeld.");
        user.EntraObjectId = id;
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }
}
