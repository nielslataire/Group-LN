#nullable disable
using System;

namespace DALCore.Models;

public partial class UserEmailSignature
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string SignatureHtml { get; set; }

    public string SignatureFormat { get; set; }

    public DateTime GewijzigdOp { get; set; }
}
