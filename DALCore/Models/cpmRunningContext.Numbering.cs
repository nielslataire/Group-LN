using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

// Nummering van offertes/wijzigingsopdrachten (migratie 070): elke nieuwe ChangeOrder krijgt vlak voor het
// opslaan zijn publieke nummer, ongeacht welke service/controller de rij aanmaakt.
public partial class cpmRunningContext
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ChangeOrderNumbering.AssignPending(this);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeOrderNumbering.AssignPending(this);
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
