using BOCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// Zoekt de e-mailadressen bij een interne rol. Projectleider en Verkoper volgen de directe koppeling op het project.
/// Projectontwikkelaar, CEO / CFO en Boekhouder hebben geen koppeling op het project: zij worden herkend aan de rol die de
/// gebruiker heeft bij het beheer van gebruikers (SecurityRole), en het is dan elke actieve gebruiker met die rol en een e-mailadres.
/// De trajectrol CEO / CFO en Projectontwikkelaar horen beide bij de rollen "Bestuurder" en "Admin" (beide zijn bestuurder); Boekhouder bij "Boekhouding".
/// Projectleider (gebruikersrol "Projectmanager") blijft de persoon die op het project staat, zodat niet alle projectmanagers elke mail krijgen.
/// </summary>
internal static class RolOntvangers
{
    /// <summary>Namen van de gebruikersrollen (SecurityRole.Name, hoofdletterongevoelig) die bij een trajectrol horen.</summary>
    internal static string[] GebruikersrollenVoor(InterneRol rol) => rol switch
    {
        InterneRol.Boekhouder => new[] { "Boekhouding" },
        InterneRol.CeoCfo => new[] { "Bestuurder", "Admin" },
        InterneRol.Projectontwikkelaar => new[] { "Bestuurder", "Admin" },
        _ => Array.Empty<string>()
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

        var namen = GebruikersrollenVoor(rol).Select(n => n.ToLower()).ToList();
        if (namen.Count == 0) return new List<string>();

        var mails = await db.SecurityUserRole
            .Where(ur => ur.Role.IsActive && namen.Contains(ur.Role.Name.ToLower())
                         && ur.User.IsActive && ur.User.Email != null && ur.User.Email != "")
            .Select(ur => ur.User.Email!)
            .ToListAsync(ct);
        return mails.Select(m => m.Trim()).Where(m => m.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
