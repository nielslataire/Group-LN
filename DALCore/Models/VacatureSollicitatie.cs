#nullable disable
using System;

namespace DALCore.Models;

public partial class VacatureSollicitatie
{
    public int Id { get; set; }

    public int? VacatureId { get; set; }

    public string VacatureTitelSnapshot { get; set; }

    public string Voornaam { get; set; }

    public string Achternaam { get; set; }

    public string Email { get; set; }

    public string Telefoon { get; set; }

    public string Motivatie { get; set; }

    public string CvBestandsnaam { get; set; }

    public string CvBestandType { get; set; }

    public byte[] CvBestand { get; set; }

    public bool IsGelezen { get; set; }

    public DateTime AangemaaktOp { get; set; }

    public virtual Vacature Vacature { get; set; }
}
