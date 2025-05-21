using Microsoft.AspNetCore.Mvc;
using BOCore;
using CPMCore.Service; 


namespace CPMCore.Controllers
{
    public class SharedController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public JsonResult GetPostcodesByCountry(string term, int CountryId)
        {
            var pservice = ServiceFactory.GetPostalcodeService();
            var presponse = pservice.GetPostalcodeByCountryAndSearchstring(CountryId, term);
            List<PostalCodeBO> iList = new List<PostalCodeBO>();
            if ((presponse.Success))
                iList = presponse.Values;
            List<Select2DTO> PostalcodeList = new List<Select2DTO>();
            Select2DTO singlePostalcode;
            foreach (PostalCodeBO selectedPostalcode in iList)
            {
                singlePostalcode = new Select2DTO();
                singlePostalcode.id = (int)selectedPostalcode.PostcodeId;
                singlePostalcode.text = selectedPostalcode.Postcode + " - " + selectedPostalcode.Gemeente;
                PostalcodeList.Add(singlePostalcode);
            }

            return Json(PostalcodeList);
        }
        [HttpPost]
        public string GetCountryIsoCode(int countryid)
        {
            var pservice = ServiceFactory.GetCountryService();
            var presponse = pservice.GetCountryById(countryid);
            CountryBO iPostcode = new CountryBO();
            if ((presponse.Success))
                iPostcode = presponse.Values.FirstOrDefault();
            return iPostcode.ISOCode;
        }
        [HttpPost]
        public JsonResult GetAvailableUnitsByProjectId(int id)
        {
            var pservice = ServiceFactory.GetUnitService();
            var presponse = pservice.GetAvailableUnitsByProjectId(id);
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
