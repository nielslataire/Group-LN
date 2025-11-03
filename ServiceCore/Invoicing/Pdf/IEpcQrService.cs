using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceCore.Invoicing.Pdf
{
    public interface IEpcQrService
    {
        byte[] CreatePng(string creditorName, string iban, string bic, decimal amount, string remittance);

        byte[] CreatePngFromPayload(string payload);
    }
}
