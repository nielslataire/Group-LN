using System.Collections.Generic;
using BOCore;
using Microsoft.AspNetCore.Http;

namespace CPMCore.Models.Projecten
{
    /// <summary>
    /// De velden die het project-toevoegen- (<see cref="ProjectModel"/>) en
    /// project-bewerken-formulier (<see cref="EditProjectDetail"/>) delen, zodat de
    /// _ProjectForm*-partials één keer bestaan i.p.v. per scherm te worden
    /// onderhouden. Beide modellen hadden deze leden al; de interface maakt ze
    /// alleen expliciet deelbaar. Bewerken-specifieke velden (status, publiceren,
    /// SEO, verplichte documenten, coördinatiecontract) blijven in Edit.cshtml.
    /// </summary>
    public interface IProjectFormModel
    {
        ProjectBO Project { get; set; }
        List<IdNameBO> Countries { get; set; }
        int SelectedCountry { get; set; }
        int SelectedPostalcode { get; set; }
        List<ProjectIssuerCompanyOptionVM> IssuerCompanies { get; set; }
        IFormFile? StandardFotoUpload { get; set; }
        List<ProjectContractSliceVM> ContractSlices { get; set; }
        List<ProjectHourlyRateVM> HourlyRates { get; set; }
        List<IdNameBO> AvailableUsers { get; set; }
        IEnumerable<CpmUserOption> Users { get; set; }
    }
}
