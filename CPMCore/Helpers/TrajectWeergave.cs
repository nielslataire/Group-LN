using BOCore;
using CPMCore.Models.Traject;
using DALCore.Models;

namespace CPMCore.Helpers;

/// <summary>Weergave-afleidingen voor de gl-v2 trajectpagina (design-handoff 30): één plek voor "is dit te laat",
/// "hoeveel dagen" en "wie is verantwoordelijk", zodat tijdlijn, tabel, matrix en kalender nooit uiteenlopen.</summary>
public static class TrajectWeergave
{
    /// <summary>Wat telt als streefdatum: de handmatige/relatieve datum, anders de uit het sjabloon berekende.</summary>
    public static DateOnly? Streef(Mijlpaal m) => m.Doeldatum ?? m.DoeldatumBerekend;

    public static bool IsAfgehandeld(Mijlpaal m) =>
        m.Status == (int)MijlpaalStatus.Bereikt || m.Status == (int)MijlpaalStatus.NietVanToepassing;

    public static bool IsTeLaat(Mijlpaal m, DateOnly vandaag) =>
        !IsAfgehandeld(m) && Streef(m) is DateOnly d && d < vandaag;

    public static int DagenTeLaat(Mijlpaal m, DateOnly vandaag) =>
        IsTeLaat(m, vandaag) ? vandaag.DayNumber - Streef(m)!.Value.DayNumber : 0;

    /// <summary>done · nvt · late · bezig · geblokkeerd · open — de vorm van het bolletje.</summary>
    public static string Soort(Mijlpaal m, DateOnly vandaag)
    {
        if (m.Status == (int)MijlpaalStatus.Bereikt) return "done";
        if (m.Status == (int)MijlpaalStatus.NietVanToepassing) return "nvt";
        if (IsTeLaat(m, vandaag)) return "late";
        if (m.Status == (int)MijlpaalStatus.Geblokkeerd) return "geblokkeerd";
        if (m.Status == (int)MijlpaalStatus.Bezig) return "bezig";
        return "open";
    }

    public static string RolNaam(int? rol) =>
        rol is int r && r > 0 && Enum.IsDefined(typeof(InterneRol), r) ? ((InterneRol)r).GetDisplayName() : "";

    public static string TypeNaam(int type) =>
        Enum.IsDefined(typeof(MijlpaalType), type) ? ((MijlpaalType)type).GetDisplayName() : "";

    /// <summary>Naam + initialen van de verantwoordelijke: de toegewezen gebruiker, anders de rol.</summary>
    public static (string Naam, string Initialen) Verantwoordelijke(Mijlpaal m, TrajectIndexVm vm)
    {
        if (!string.IsNullOrEmpty(m.VerantwoordelijkeUserId) && vm.Gebruikers.TryGetValue(m.VerantwoordelijkeUserId, out var u) && u.Naam.Length > 0)
            return (u.Naam, u.Initialen);
        var rol = RolNaam(m.VerantwoordelijkeRol);
        if (rol.Length == 0) return ("", "");
        var woorden = rol.Split(new[] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
        var init = woorden.Length >= 2 ? $"{woorden[0][0]}{woorden[^1][0]}" : rol[..Math.Min(2, rol.Length)];
        return (rol, init.ToUpperInvariant());
    }

    public static string Datum(DateOnly? d) => d?.ToString("dd/MM/yyyy") ?? "—";

    public static string Dagen(int n) => n == 1 ? "1 dag" : n + " dagen";
}
