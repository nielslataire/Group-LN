using Microsoft.AspNetCore.Mvc;
using BOCore;
using FacadeCore;
using System.Linq;


namespace CPMCore.Controllers
{
    public class SharedController : Controller
    {
        private readonly IPostalcodeService _postalcodeService;
        private readonly ICountryService _countryService;
        private readonly IUnitService _unitService;

        public SharedController(IPostalcodeService postalcodeService, ICountryService countryService, IUnitService unitService)
        {
            _postalcodeService = postalcodeService;
            _countryService = countryService;
            _unitService = unitService;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<JsonResult> GetPostcodesByCountry(string term, int CountryId)
        {
            var presponse = await _postalcodeService.GetPostalcodeByCountryAndSearchstring(CountryId, term);

            var postalcodeList = presponse.Success
                ? presponse.Values.Select(x => new Select2DTO
                {
                    id = x.PostcodeId.GetValueOrDefault(),
                    text = $"{x.Postcode} - {x.Gemeente}"
                })
                  .ToList()
                : new List<Select2DTO>();

            return Json(postalcodeList);
        }
        [HttpGet]
        public JsonResult GetPostcodeById(int id)
        {
            var presponse = _postalcodeService.GetPostalcodeById(id);
            if(!presponse.Success)
                return Json(null);

            return Json(new
            {
                Value = presponse.Value.PostcodeId,
                Text = presponse.Value.Postcode + " - " + presponse.Value.Gemeente
            });
        }

        [HttpPost]
        public string GetCountryIsoCode(int countryid)
        {
            var presponse = _countryService.GetCountryById(countryid);
            CountryBO iPostcode = new CountryBO();
            if ((presponse.Success))
                iPostcode = presponse.Values.FirstOrDefault();
            return iPostcode.ISOCode;
        }
        [HttpPost]
        public JsonResult GetAvailableUnitsByProjectId(int id)
        {
            var presponse = _unitService.GetAvailableUnitsByProjectId(id);
            List<IdNameBO> iList = new List<IdNameBO>();
            if ((presponse.Success))
                iList = presponse.Values;
            return Json(iList);
        }

        public class Select2DTO
        {
            // as select2 is formed like id and text so we used DTO
            public int id
            {
                get
                {
                    return m_id;
                }
                set
                {
                    m_id = value;
                }
            }
            private int m_id;
            public string text
            {
                get
                {
                    return m_text;
                }
                set
                {
                    m_text = value;
                }
            }
            private string m_text;
        }
    }
}
