using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

public partial class cpmRunningContext
{
    public virtual DbSet<EmailTemplate> EmailTemplate { get; set; }
    public virtual DbSet<UserEmailSignature> UserEmailSignature { get; set; }
    public virtual DbSet<EmailSendLog> EmailSendLog { get; set; }
}
