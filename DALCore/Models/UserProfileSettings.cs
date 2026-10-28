#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Persoonlijke instellingen van een gebruiker (Account › Mijn profiel). Eén rij per gebruiker, aangemaakt bij het eerste opslaan.</summary>
public partial class UserProfileSettings
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string VastNummer { get; set; }

    /// <summary>Bitmask: ma=1, di=2, wo=4, do=8, vr=16, za=32, zo=64.</summary>
    public int WerkDagen { get; set; } = 31;

    public string WerkVan { get; set; } = "08:00";

    public string WerkTot { get; set; } = "17:00";

    public string PauzeVan { get; set; } = "12:00";

    public string PauzeTot { get; set; } = "12:45";

    public bool AfwezigAan { get; set; }

    public DateTime? AfwezigVan { get; set; }

    public DateTime? AfwezigTot { get; set; }

    public int? BackupUserId { get; set; }

    /// <summary>Bitmask: 1 = taken en werfpunten naar back-up, 2 = afwezigheidsbericht, 4 = facturen ter goedkeuring naar back-up.</summary>
    public int AfwezigOpties { get; set; } = 3;

    /// <summary>JSON: { "taak-toegewezen": [inCrm, email], ... }; ontbrekende sleutels volgen de standaard.</summary>
    public string MeldingenJson { get; set; }

    public bool DagSamenvatting { get; set; } = true;

    public string SamenvattingTijd { get; set; } = "07:30";

    public bool StilleUren { get; set; } = true;

    public bool DringendDoorlaten { get; set; } = true;

    /// <summary>"1d", "3d", "1w" of "2w".</summary>
    public string DeadlineHerinnering { get; set; } = "3d";

    public DateTime GewijzigdOp { get; set; }

    public virtual Users User { get; set; }

    public virtual Users BackupUser { get; set; }
}
