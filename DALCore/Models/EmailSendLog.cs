#nullable disable
using System;

namespace DALCore.Models;

public partial class EmailSendLog
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string ContactEmail { get; set; }

    public string ContactNaam { get; set; }

    public int? EmailTemplateId { get; set; }

    public string TemplateNaam { get; set; }

    public string Onderwerp { get; set; }

    public int VerzondenDoorUserId { get; set; }

    public string VerzondenDoorNaam { get; set; }

    public DateTime VerzondenOp { get; set; }
}
