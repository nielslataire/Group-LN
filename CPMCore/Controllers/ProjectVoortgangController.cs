using CPMCore.Services;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CPMCore.Controllers
{
    /// <summary>
    /// Handmatige trigger voor voortgang-herberekening (debug/beheer).
    /// De dagelijkse herberekening loopt automatisch via VoortgangHostedService.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/voortgang")]
    public class ProjectVoortgangController : Controller
    {
        private readonly VoortgangHostedService _hostedService;
        private readonly IProjectVoortgangService _service;

        public ProjectVoortgangController(
            VoortgangHostedService hostedService,
            IProjectVoortgangService service)
        {
            _hostedService = hostedService;
            _service = service;
        }

        /// <summary>Herbereken voortgang voor alle actieve projecten (handmatige trigger).</summary>
        [HttpGet("herbereken-alles")]
        public IActionResult HerberekenAlles()
        {
            _ = _hostedService.RunAsync();
            return Ok(new { status = "gestart", timestamp = System.DateTime.UtcNow });
        }

        /// <summary>Herbereken voortgang voor één project.</summary>
        [HttpGet("herbereken/{projectId:int}")]
        public IActionResult Herbereken(int projectId)
        {
            var result = _service.CalculateAndSave(projectId);
            if (!result.Success)
                return BadRequest(new { errors = result.Messages.Select(m => m.Message) });
            return Ok(result.Value);
        }

        /// <summary>Geef de opgeslagen voortgang terug voor één project.</summary>
        [HttpGet("{projectId:int}")]
        public IActionResult Get(int projectId)
        {
            var result = _service.GetByProjectId(projectId);
            if (!result.Success)
                return NotFound(new { errors = result.Messages.Select(m => m.Message) });
            return Ok(result.Value);
        }
    }
}
