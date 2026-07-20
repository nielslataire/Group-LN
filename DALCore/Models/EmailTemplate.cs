#nullable disable
using System;

namespace DALCore.Models;

public partial class EmailTemplate
{
    public int Id { get; set; }

    public string Naam { get; set; }

    public string Onderwerp { get; set; }

    public string BodyHtml { get; set; }

    public bool IsActief { get; set; }

    public DateTime AangemaaktOp { get; set; }

    public DateTime GewijzigdOp { get; set; }
}
