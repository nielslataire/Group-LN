namespace CPMCore.Models.Instellingen
{
    public class InvoiceSequencesPageVM
    {
        public int SeriesId { get; set; }
        public int IssuerId { get; set; }
        public List<InvoiceSequenceVM> Items { get; set; } = new();
    }
}
