using FacadeCore;
using System;
using System.Collections.Generic;

namespace CPMCore.Models.Inhoudingsplicht
{
    /// <summary>Pagina Inhoudingsplicht (design-handoff 57): aannemers kiezen, extra btw-nummers plakken en eerdere controlelijsten.</summary>
    public class InhoudingsplichtIndexVm
    {
        public bool Configured { get; set; }
        public bool CanWrite { get; set; }
        public bool Simulation { get; set; }

        public List<SupplierRow> Suppliers { get; set; } = new();
        /// <summary>Werven in uitvoering, met het aantal aannemers dat er werkt.</summary>
        public List<ActiveSite> ActiveSites { get; set; } = new();
        public IReadOnlyList<RetainmentBatchSummaryVm> Lists { get; set; } = Array.Empty<RetainmentBatchSummaryVm>();

        /// <summary>Voorselectie (Opnieuw selecteren).</summary>
        public HashSet<int> SelectedCompanyIds { get; set; } = new();
        public string ExtraText { get; set; }
        public string ListName { get; set; }
        public string Source { get; set; } = "actief";
        /// <summary>Controlelijst die meteen in het zijpaneel opent (na het bewaren).</summary>
        public Guid? OpenBatchId { get; set; }

        public class ActiveSite
        {
            public int ProjectId { get; set; }
            public string Name { get; set; }
            public int Count { get; set; }
        }

        public class SiteRef
        {
            public int ProjectId { get; set; }
            public string Name { get; set; }
            public bool Active { get; set; }
        }

        public class SupplierRow
        {
            public int CompanyId { get; set; }
            public string Name { get; set; }
            public string Activity { get; set; }
            /// <summary>Genormaliseerd nummer (0123456789) of buitenlands nummer; null als er geen is.</summary>
            public string Identifier { get; set; }
            public string IdentifierDisplay { get; set; }
            public List<SiteRef> Sites { get; set; } = new();
            public DateTime? LastInvoice { get; set; }
            public RetainmentCheckVm Last { get; set; }
        }
    }
}
