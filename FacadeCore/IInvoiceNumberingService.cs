using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IInvoiceNumberingService
    {
        /// <summary>
        /// Kent een nummer toe aan een factuur en verhoogt de sequence.
        /// </summary>
        /// <param name="invoiceId">Id van de factuur</param>
        /// <param name="seriesId">De reeks waarin de factuur moet vallen</param>
        /// <param name="invoiceDate">Datum van de factuur (voor fiscaal jaar)</param>
        /// <param name="fiscalYear">bv. 2025</param>
        /// <returns>Tuple met nieuw publicId en currentNumber</returns>
        Task<(string publicId, int currentNumber)> IssueAsync(
            int invoiceId, int seriesId, DateTime invoiceDate, int fiscalYear,
            CancellationToken ct = default);


    }
}
