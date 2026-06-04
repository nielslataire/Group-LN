using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DALCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget
{
    public class ABEXService
    {
        private readonly UnitOfWorkCore _uow;

        public ABEXService(UnitOfWorkCore uow)
        {
            _uow = uow;
        }

        public async Task<decimal> GetActieveIndexAsync()
        {
            var actief = await _uow.ABEXIndex.GetNoTracking()
                .FirstOrDefaultAsync(a => a.IsActief);
            return actief?.IndexWaarde ?? 958m;
        }

        public async Task<List<ABEXIndex>> GetAlleIndexenAsync()
        {
            return await _uow.ABEXIndex.GetNoTracking()
                .OrderByDescending(a => a.Jaar)
                .ThenByDescending(a => a.Kwartaal)
                .ToListAsync();
        }

        public async Task SetActiefAsync(int id)
        {
            var all = await _uow.ABEXIndex.GetNormal().ToListAsync();
            foreach (var item in all)
                item.IsActief = item.Id == id;
            await _uow.SaveChangesAsync();
        }

        public decimal BerekenIndexFactor(decimal abexBasis, decimal abexHuidig)
        {
            if (abexBasis == 0m) return 1m;
            return abexHuidig / abexBasis;
        }

        public decimal GeindexeerdePrijs(decimal nacalcBasisprijs, decimal abexBasis, decimal abexHuidig)
        {
            return nacalcBasisprijs * BerekenIndexFactor(abexBasis, abexHuidig);
        }
    }
}
