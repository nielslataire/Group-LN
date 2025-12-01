namespace CPMCore.Models.Instellingen
{
    public class InvoiceSequenceVM
    {
        public int Id { get; set; }
        public int SeriesId { get; set; }
        public int? BookyearId { get; set; }
        public int? JournalId { get; set; }
        public int FiscalYear { get; set; }
        public int CurrentNumber { get; set; }
        public string? BookyearDescription { get; set; }
        public string? JournalName { get; set; }
    }
}