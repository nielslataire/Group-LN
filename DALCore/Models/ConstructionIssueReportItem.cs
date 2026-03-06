using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALCore.Models
{
    public partial class ConstructionIssueReportItem
    {
        public int ReportId { get; set; }
        public int IssueId { get; set; }

        public virtual ConstructionIssueReport Report { get; set; } = null!;
        public virtual ConstructionIssue Issue { get; set; } = null!;
    }
}
