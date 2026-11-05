#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Migratie 131 — een bijlage bij een offerte (plan, fiche, ...). Het bestand staat in de
/// DocStorage-map "quote-attachments"; de offertemail stuurt ze mee naar de klant.</summary>
public partial class ChangeOrderAttachment
{
    public int Id { get; set; }

    public int ChangeOrderId { get; set; }

    public string FileName { get; set; }

    public string StoragePath { get; set; }

    public string ContentType { get; set; }

    public long SizeBytes { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual ChangeOrder ChangeOrder { get; set; }
}
