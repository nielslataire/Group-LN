// Partial extension — adds UserCoachmarkState to the EF context.
// The main cpmRunningContext.cs is auto-generated; never edit it directly.
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

public partial class cpmRunningContext
{
    public virtual DbSet<UserCoachmarkState> UserCoachmarkState { get; set; }
}
