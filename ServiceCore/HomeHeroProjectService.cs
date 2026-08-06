using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore
{
    public class HomeHeroProjectService : IHomeHeroProjectService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;

        public HomeHeroProjectService(UnitOfWorkCore uow)
        {
            _uow = uow;
            _db = (cpmRunningContext)_uow.Context;
        }

        public async Task<HomeHeroProjectBO> GetAsync(CancellationToken ct = default)
        {
            return await _db.HomeHeroProject.AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Select(x => new HomeHeroProjectBO
                {
                    Id = x.Id,
                    ProjectId = x.ProjectId,
                    Kicker = x.Kicker,
                    Titel = x.Titel,
                    Tekst = x.Tekst,
                    ProjectTitelOverride = x.ProjectTitelOverride,
                    GewijzigdOp = x.GewijzigdOp
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task SaveAsync(HomeHeroProjectBO bo, CancellationToken ct = default)
        {
            var e = await _db.HomeHeroProject.FirstOrDefaultAsync(ct);
            if (e == null)
            {
                e = new HomeHeroProject();
                _db.HomeHeroProject.Add(e);
            }

            e.ProjectId = bo.ProjectId;
            e.Kicker = bo.Kicker?.Trim();
            e.Titel = bo.Titel?.Trim();
            e.Tekst = bo.Tekst?.Trim();
            e.ProjectTitelOverride = bo.ProjectTitelOverride?.Trim();
            e.GewijzigdOp = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);
        }
    }
}
