using BOCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// Zoekt de e-mailadressen bij een interne rol. Projectleider en Verkoper volgen de directe koppeling op het project.
/// Projectontwikkelaar, CEO / CFO (de bestuurders) en Boekhouder hebben geen koppeling op het project: zij worden
/// herkend aan het dashboardtype van de gebruiker (Users.DashboardType, instelbaar bij het beheer van gebruikers),
/// en het is dan elke actieve gebruiker met dat type en een e-mailadres.
/// </summary>
internal static class RolOntvangers
{
    // CPMCore.Models.DashboardType: Boekhouding = 1, CeoCfo = 2, Ontwikkelaar = 4. ServiceCore kent dat enum niet, daarom de getallen.
    private static int? DashboardTypeVoor(InterneRol rol) => rol switch
    {
        InterneRol.Boekhouder => 1,
        InterneRol.CeoCfo => 2,
        InterneRol.Projectontwikkelaar => 4,
        _ => null
    };

    /// <summary>Alle e-mailadressen (zonder dubbels) die bij deze rol horen voor dit project. Leeg = niemand gevonden.</summary>
    public static async Task<List<string>> ZoekAsync(cpmRunningContext db, InterneRol rol, Project project, CancellationToken ct)
    {
        string? directeUserId = rol switch
        {
            InterneRol.Projectleider => project.AspNetUserId,
            InterneRol.Verkoper => project.SalesResponsibleAspNetUserId,
            _ => null
        };
        if (!string.IsNullOrWhiteSpace(directeUserId))
        {
            var mail = await db.Users.Where(u => u.UserId == directeUserId).Select(u => u.Email).FirstOrDefaultAsync(ct);
            return string.IsNullOrWhiteSpace(mail) ? new List<string>() : new List<string> { mail.Trim() };
        }

        var dashboardType = DashboardTypeVoor(rol);
        if (dashboardType == null) return new List<string>();

        var mails = await db.Users
            .Where(u => u.IsActive && u.DashboardType == dashboardType && u.Email != null && u.Email != "")
            .Select(u => u.Email!)
            .ToListAsync(ct);
        return mails.Select(m => m.Trim()).Where(m => m.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
