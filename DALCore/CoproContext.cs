using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Proxies;

namespace DALCore
{
    public partial class TestdbEntities : DbContext
    {
        public TestdbEntities(string connstring) : base(GetOptions(connstring))
        {

        }

        public TestdbEntities(bool detectchanges)
        {
            ChangeTracker.AutoDetectChangesEnabled = detectchanges;
        }
        public TestdbEntities(bool detectchanges, string connstring) : base(GetOptions(connstring))
        {
            ChangeTracker.AutoDetectChangesEnabled = detectchanges;
        }
        private static DbContextOptions GetOptions(string connectionString)
        {
            
            return SqlServerDbContextOptionsExtensions.UseSqlServer(new DbContextOptionsBuilder(), connectionString).Options;
        }
    }
}
