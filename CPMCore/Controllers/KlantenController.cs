using Microsoft.AspNetCore.Mvc;
using BOCore;
using CPMCore.Service;
using CPMCore.Models.Klanten;
using System.Text.RegularExpressions;
using System.Drawing;
using System;

namespace CPMCore.Controllers
{
    public class KlantenController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
        // KLANTEN
        // KLANT TOEVOEGEN
        [HttpGet]
        public ActionResult AddClientAccount(int id)
        {
            AddClientAccountModel model = new AddClientAccountModel();
            var service = ServiceFactory.GetProjectService();
            model.ProjectName = service.GetProjectNameById(id);
            model.ProjectId = id;
            model.ClientAccount.OwnerPercentage = 100;
            model.ClientAccount.OwnerType.Id = 1;
            FillInAddSelectLists(ref model);
            return View(model);
        }
        [HttpPost]
        public PartialViewResult AddCoOwner(string Name, string Forename, string Salutation, string Street, string Housenumber, string Busnumber, int Zipcode, string Phone, string Cellphone, string Email, int OwnerType, string OwnerPercentage, string VatNumber, string CompanyName, string InvoiceAddress, string InvoiceStreet, string InvoiceHousenumber, string InvoiceBusnumber, string InvoiceZipcode)
        {
            ClientContactBO nCoOwner = new ClientContactBO();
            // ophalen postcode
            // Dim pservice = ServiceFactory.GetPostalcodeService()
            // Dim presponse = pservice.GetPostalcodeById(Zipcode)
            // If (presponse.Success) Then nCoOwner.Postalcode = presponse.Values.FirstOrDefault
            nCoOwner.Name = Name;
            nCoOwner.Firstname = Forename;
            nCoOwner.Salutation = Enum.Parse<Salutation>(Salutation);
            nCoOwner.Street = Street;
            nCoOwner.Housenumber = Housenumber;
            nCoOwner.Busnumber = Busnumber;
            nCoOwner.Postalcode.PostcodeId = Zipcode;
            nCoOwner.VATnumber = VatNumber;
            nCoOwner.CompanyName = CompanyName;
            if (InvoiceAddress == "True")
            {
                nCoOwner.InvoiceStreet = InvoiceStreet;
                nCoOwner.InvoiceHousenumber = InvoiceHousenumber;
                nCoOwner.InvoiceBusnumber = InvoiceBusnumber;
                if(InvoiceZipcode is not null)
                {
                    nCoOwner.InvoicePostalcode.PostcodeId = int.Parse(InvoiceZipcode);
                }
               
            }  
            if (Phone != null)
                nCoOwner.Phone = Regex.Replace(Phone, "[^0-9]", "");
            if (Cellphone != null)
                nCoOwner.Cellphone = Regex.Replace(Cellphone, "[^0-9]", "");
            nCoOwner.Email = Email;
            var sservice = ServiceFactory.GetClientService();
            var sresponse = sservice.GetClientOwnerTypeById(OwnerType);
            nCoOwner.CoOwnerType = sresponse.Value;
            try
            {
                nCoOwner.CoOwnerPercentage = decimal.Parse(OwnerPercentage);
            }
            catch (Exception ex)
            {
                try
                {
                    OwnerPercentage = OwnerPercentage.Replace(".", ",");
                    nCoOwner.CoOwnerPercentage = decimal.Parse(OwnerPercentage);
                }
                catch (Exception ex2)
                {
                }
            }
            nCoOwner.CoOwnerPercentage = decimal.Parse(OwnerPercentage);
            ViewData["mode"] = "add";
            return PartialView("_CoOwnerRow", nCoOwner);
        }

        private void FillInAddSelectLists(ref AddClientAccountModel model)
        {
            var cservice = ServiceFactory.GetCountryService();
            var cresponse = cservice.GetVisibleCountriesForSelect();
            if ((cresponse.Success))
                model.Countries = cresponse.Values;
            var defCountry = model.Countries.Where(m => m.Group == "19").FirstOrDefault();
            if (model.SelectedCountry == 0)
            {
                if ((defCountry != null))
                    model.SelectedCountry = defCountry.ID;
            }
            var oservice = ServiceFactory.GetClientService();
            var oresponse = oservice.GetOwnerTypesForSelect();
            if ((oresponse.Success))
                model.OwnerTypes = oresponse.Values;
            var uservice = ServiceFactory.GetUnitService();
            var uresponse = uservice.GetAvailableUnitsByProjectId(model.ProjectId);
            if ((uresponse.Success))
                model.AvailableUnits = uresponse.Values;
        }
        public PartialViewResult BlankContactRow()
        {
            return PartialView("_ContactRow", new ClientContactBO());
        }



        public ActionResult PartialDeleteClientModal(int id)
        {
            var viewModel = new IdNameBO();
            if (id != 0)
            {
                var dservice = ServiceFactory.GetClientService();
                viewModel.Display = dservice.GetClientAccountNameById(id);
                viewModel.ID = id;
            }
            return PartialView("_DeleteClientModal", viewModel);
        }
        [HttpGet]
        public ActionResult DeleteClient(int id)
        {
            string stri = Request.Headers["Referer"].ToString();
            List<int> Idlist = new List<int>();
            Idlist.Add(id);
            if (id != 0)
            {
                var uservice = ServiceFactory.GetUnitService();
                var response = uservice.DeleteUnitFromClientAccountByAccountId(Idlist);
                var dservice = ServiceFactory.GetClientService();
                if (response.Success == true)
                {
                    response = dservice.Delete(Idlist);
                    if (response.Success == true)
                    {
                        AddMessage("", "De klant is verwijderd", "Geslaagd!");
                        return Redirect(stri);
                    }
                    else
                    {
                        AddMessage("error", "De klant niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                        return Redirect(stri);
                    }
                }
                else
                {
                    AddMessage("error", "De klant niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                    return Redirect(stri);
                }
            }
            else
                return Redirect(stri);
        }
        public void AddMessage(string messagetype, string message, string messagetitle)
        {
            TempData["Message"] = message;
            TempData["MessageType"] = messagetype;
            TempData["MessageTitle"] = messagetitle;
        }

    }
}
