using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;
using DALCore;

namespace ServiceCore.Translators
{
    public class InvoiceTranslator
    {
        internal static ErrorCode TranslateEntityToBO(Invoices _entity, InvoiceBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            bo.Filename = _entity.Filename;
            bo.Invoicedate = _entity.Date;
            bo.Id = _entity.Id;
            bo.ClientId = _entity.ClientId;
            bo.ClientType = (ClientType)_entity.ClientType;
            bo.PublicId = _entity.PublicId;
            if (_entity.ExpirationDate is not null)
                bo.ExpirationDate = _entity.ExpirationDate;
            bo.VatNumber = _entity.VatNumber;
            bo.ClientName = _entity.ClientName;
            bo.Adress = _entity.Adress;
            // Gemeente en postcode van het project
            if ((_entity.PostalCode != null))
            {
                bo.PostalCode.Postcode = _entity.PostalCode.Postcode;
                bo.PostalCode.Gemeente = _entity.PostalCode.Gemeente;
                bo.PostalCode.PostcodeId = _entity.PostalCode.PostcodeId;
                if (_entity.PostalCode.Country != null)
                {
                    bo.PostalCode.Country.Name = _entity.PostalCode.Country.LandNaam;
                    bo.PostalCode.Country.CountryId = _entity.PostalCode.Country.Id;
                    bo.PostalCode.Country.ISOCode = _entity.PostalCode.Country.LandIsocode;
                }
                if (_entity.PostalCode.Provincie != null)
                {
                    bo.PostalCode.Provincie.Name = _entity.PostalCode.Provincie.ProvincieName;
                    bo.PostalCode.Provincie.ProvincieId = _entity.PostalCode.Provincie.ProvincieId;
                }
            }
            bo.BankAccount = _entity.BankAccount;
            bo.ExtraInfo = _entity.ExtraInfo;
            bo.Text = _entity.Text;
            string[] stringlist = _entity.DetailText.Split(new string[] { @"\n" }, StringSplitOptions.None);
            foreach (var line in stringlist)
            {
                if (line != stringlist.Last())
                    bo.DetailText += line + "<br />";
                else
                    bo.DetailText += line;
            }
            foreach (var x in _entity.InvoicesDetails)
            {
                InvoiceRowBO bou = new InvoiceRowBO();
                var err = InvoiceDetailTranslator.TranslateEntityToBO(x, bou);
                bo.Rows.Add(bou);
            }
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(Invoices _entity, InvoiceBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            _entity.Filename = bo.Filename;
            if (bo.Invoicedate is not null) _entity.Date = (DateOnly)bo.Invoicedate;
            _entity.ClientId = bo.ClientId;
            _entity.ClientType = (int)bo.ClientType;
            _entity.PublicId = bo.PublicId;
            _entity.ExpirationDate = bo.ExpirationDate;
            _entity.VatNumber = bo.VatNumber;
            _entity.ClientName = bo.ClientName;
            _entity.Adress = bo.Adress;
            if (bo.PostalCode is not null)
                _entity.PostalCodeId = bo.PostalCode.PostcodeId;
            _entity.BankAccount = bo.BankAccount;
            _entity.ExtraInfo = bo.ExtraInfo;
            _entity.Text = bo.Text;
            _entity.DetailText = bo.DetailText;

            var err = HandleRows(_entity, bo.Rows);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }

        private static ErrorCode HandleRows(Invoices _entity, List<InvoiceRowBO> rows)
        {
            if ((rows.Count == 0))
                return ErrorCode.Success;
            foreach (var x in rows)
            {
                if ((x.Id == 0))
                {
                    // insert
                    InvoicesDetails row = new InvoicesDetails();
                    var err = InvoiceDetailTranslator.TranslateBOToEntity(row, x);
                    _entity.InvoicesDetails.Add(row);
                }
                else
                {
                    // update
                    var row = _entity.InvoicesDetails.FirstOrDefault(f => f.Id == x.Id);
                    if ((row != null))
                    {
                        var err = InvoiceDetailTranslator.TranslateBOToEntity(row, x);
                    }
                }
            }
            // delete
            List<InvoicesDetails> delList = new List<InvoicesDetails>();
            foreach (var x in _entity.InvoicesDetails)
            {
                if ((!rows.Any(f => f.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.InvoicesDetails.Remove(x);
            return ErrorCode.Success;
        }
    }
}
