using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services;

/// <summary>
/// Wie telt als extern (aannemer of gast) voor de keuzelijsten met interne medewerkers (projectbeheerder, facturatie-gebruikers, ...)?
/// Zelfde regel als Gebruikersbeheer (UserAdminController.UserTypesAsync): een aannemerskoppeling (UserCompanyAccess) of gastuitnodiging is
/// bewijs voor "extern", maar enkel zolang al zijn rollen portaalrollen zijn. Een gebruiker die ook een rol heeft die niet voor externen
/// bedoeld is (een rol met minstens één lid zonder extern bewijs), is intern. Zo verdwijnt een beheerder met een testkoppeling naar
/// een eigen bedrijf niet uit de lijsten.
/// </summary>
public static class ExterneGebruikers
{
    /// <summary>De Users.Id's die als extern gelden.</summary>
    public static HashSet<int> Ids(cpmRunningContext db)
    {
        var contractorIds = db.UserCompanyAccess.AsNoTracking().Select(a => a.UserId).Distinct().ToList().ToHashSet();
        var guestIds = new HashSet<int>();
        try { guestIds = db.UserGuestInvitation.AsNoTracking().Where(i => i.UserType != "internal").Select(i => i.UserId).ToList().ToHashSet(); }
        catch { /* tabel bestaat nog niet */ }
        var evidence = contractorIds.Union(guestIds).ToHashSet();
        if (evidence.Count == 0) return evidence;

        var activeRoles = db.SecurityRole.AsNoTracking().Where(r => r.IsActive).Select(r => r.RoleId).ToList().ToHashSet();
        var userRoles = db.SecurityUserRole.AsNoTracking().ToList().Where(x => activeRoles.Contains(x.RoleId)).ToList();
        var portalRoles = userRoles.GroupBy(x => x.RoleId).Where(g => g.All(x => evidence.Contains(x.UserId))).Select(g => g.Key).ToHashSet();
        var rolesOf = userRoles.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.RoleId).ToList());

        return evidence.Where(id => !rolesOf.TryGetValue(id, out var r) || r.All(portalRoles.Contains)).ToHashSet();
    }
}
