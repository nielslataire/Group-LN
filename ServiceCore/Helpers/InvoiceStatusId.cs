namespace ServiceCore.Helpers
{
    public enum InvoiceStatusId : byte
    {
        Unknown = 0,
        Draft = 1,
        Issued = 2,
        Sent = 3,
        PartiallyPaid = 4,
        Paid = 5,
        Overdue = 6,
        Cancelled = 7,
        Booked = 8,
        Generating = 9
    }
}