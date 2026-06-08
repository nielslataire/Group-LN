using System;
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
            var query = _uow.BouwIndex.GetNoTracking()
                .Where(x => x.IndexType == indexType);

            // Eerst actief gemarkeerde rij proberen
            var actief = await query.Where(x => x.IsActief).FirstOrDefaultAsync();
            if (actief != null) return actief.IndexWaarde;

            // Fallback: meest recente op basis van jaar + maand
            var meestRecent = await query
                .OrderByDescending(x => x.Jaar)
                .ThenByDescending(x => x.Maand)
                .FirstOrDefaultAsync();
            if (meestRecent != null) return meestRecent.IndexWaarde;

            return indexType == "S" ? 119.1000m : 123.5000m;
        }

        // Geeft de indexwaarde terug die het dichtst bij de opgegeven datum ligt (meest recente <= datum, anders vroegste)
        public async Task<decimal?> GetIndexOpDatumAsync(string indexType, DateTime datum)
        {
            var query = _uow.BouwIndex.GetNoTracking()
                .Where(x => x.IndexType == indexType && x.Jaar != null && x.Maand != null);

            var opOfVoor = await query
                .Where(x => x.Jaar < datum.Year ||
                            (x.Jaar == datum.Year && x.Maand <= datum.Month))
                .OrderByDescending(x => x.Jaar)
                .ThenByDescending(x => x.Maand)
                .FirstOrDefaultAsync();
            if (opOfVoor != null) return opOfVoor.IndexWaarde;

            // Geen data vóór de datum: neem vroegst beschikbare
            var vroegste = await query
                .OrderBy(x => x.Jaar)
                .ThenBy(x => x.Maand)
                .FirstOrDefaultAsync();
            return vroegste?.IndexWaarde;
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
            decimal sBase   = sStart > 0 ? sStart : 100m;
            decimal iBase   = iStart > 0 ? iStart : 100m;
            decimal sFactor = sHuidig / sBase;
            decimal iFactor = iHuidig / iBase;
            return iFactor * 0.40m + sFactor * 0.40m + 0.20m;
        }

        public decimal GeindexeerdePrijs(
            decimal basisPrijs,
            decimal sStart, decimal sHuidig,
            decimal iStart, decimal iHuidig)
        {
            return basisPrijs * BerekenGewogenFactor(sStart, sHuidig, iStart, iHuidig);
        }

        public async Task<List<BouwIndex>> GetGefilterdAsync(string indexType, int? jaar = null, string? categorie = null)
        {
            var query = _uow.BouwIndex.GetNoTracking().Where(x => x.IndexType == indexType);
            if (jaar.HasValue) query = query.Where(x => x.Jaar == jaar);
            if (!string.IsNullOrEmpty(categorie)) query = query.Where(x => x.Categorie == categorie);
            return await query
                .OrderByDescending(x => x.Jaar)
                .ThenByDescending(x => x.Maand)
                .ToListAsync();
        }

        public async Task OpslaanAsync(BouwIndex index)
        {
            if (index.Id == 0)
            {
                _uow.BouwIndex.Add(index);
            }
            else
            {
                var bestaand = await _uow.BouwIndex.GetNormal().FirstOrDefaultAsync(x => x.Id == index.Id);
                if (bestaand != null)
                {
                    bestaand.IndexWaarde = index.IndexWaarde;
                    bestaand.Jaar        = index.Jaar;
                    bestaand.Maand       = index.Maand;
                    bestaand.IsActief    = index.IsActief;
                    bestaand.Opmerking   = index.Opmerking;
                    bestaand.Bron        = index.Bron;
                }
            }
            await _uow.SaveChangesAsync();
        }

        public async Task<bool> VerwijderenAsync(int id)
        {
            var item = await _uow.BouwIndex.GetNormal().FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return false;
            _uow.BouwIndex.DeleteObject(item);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
