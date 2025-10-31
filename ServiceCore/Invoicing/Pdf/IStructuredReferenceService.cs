using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceCore.Invoicing.Pdf;

public interface IStructuredReferenceService
{
    string CreateOgm(string base10Digits);
    string CreateRf(string raw);
}