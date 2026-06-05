using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DALCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget
{
    public class BouwIndexService
    {
        private readonly UnitOfWorkCore _uow;

        public BouwIndexService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public async Task<decimal> GetActieveIndexAsync(string indexType)
        {
            var idx = await _uow.BouwIndex.GetNoTracking()
                .Where(x => x.IndexType == indexType && x.IsActief)
                .FirstOrDefaultAsync();
            if (idx != null) return idx.IndexWaarde;
            return indexType == "S" ? 119.1000m : 123.5000m;
        }

        public async Task<List<BouwIndex>> GetAlleIndexenAsync(string indexType)
        {
            return await _uow.BouwIndex.GetNoTracking()
                .Where(x => x.IndexType == indexType)
                .OrderByDescending(x => x.Jaar)
                .ThenByDescending(x => x.Maand)
                .ToListAsync();
        }

        public async Task SetActiefAsync(string indexType, int id)
        {
            var lijst = await _uow.BouwIndex.GetNormal()
                .Where(x => x.IndexType == indexType)
                .ToListAsync();
            foreach (var item in lijst)
                item.IsActief = (item.Id == id);
            await _uow.SaveChangesAsync();
        }

        // Formule: (I_huidig/I_start) × 0.40 + (S_huidig/S_start) × 0.40 + 0.20
        public decimal BerekenGewogenFactor(
            decimal sStart, decimal sHuidig,
            decimal iStart, decimal iHuidig)
        {
            decimal sFactor = sStart > 0 ? sHuidig / sStart : 1m;
            decimal iFactor = iStart > 0 ? iHuidig / iStart : 1m;
            return iFactor * 0.40m + sFactor * 0.40m + 0.20m;
        }

        public decimal GeindexeerdePrijs(
            decimal basisPrijs,
            decimal sStart, decimal sHuidig,
            decimal iStart, decimal iHuidig)
        {
            return basisPrijs * BerekenGewogenFactor(sStart, sHuidig, iStart, iHuidig);
        }
    }
}
