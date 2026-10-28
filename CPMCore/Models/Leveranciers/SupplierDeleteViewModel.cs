namespace CPMCore.Models.Leveranciers
{
    public class SupplierDeleteViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>Waarom de leverancier niet verwijderd mag worden (contracten of facturen), of null.</summary>
        public string? BlockReason { get; set; }
    }
}