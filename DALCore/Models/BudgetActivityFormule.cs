#nullable disable
using System;
namespace DALCore.Models;
public partial class BudgetActivityFormule {
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string Formule { get; set; }
    public string Omschrijving { get; set; }
    public bool Actief { get; set; }
    public DateTime LaatstGewijzigd { get; set; }
    public virtual Activity Activity { get; set; }
}
