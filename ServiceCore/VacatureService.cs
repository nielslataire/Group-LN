using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ServiceCore
{
    public class VacatureService : IVacatureService
    {
        private readonly UnitOfWorkCore _uow;

        public VacatureService(UnitOfWorkCore uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public GetResponse<VacatureBO> GetVacatures(bool alleenGepubliceerd = false)
        {
            var response = new GetResponse<VacatureBO>();

            var query = _uow.Vacaturen
                .GetNoTracking()
                .AsQueryable();

            if (alleenGepubliceerd)
                query = query.Where(v => v.IsGepubliceerd);

            var entities = query
                .OrderBy(v => v.SortOrder)
                .ThenBy(v => v.Id)
                .ToList();

            foreach (var e in entities)
                response.AddValue(MapToBO(e));

            return response;
        }

        public GetResponse<VacatureBO> GetVacatureById(int id)
        {
            var response = new GetResponse<VacatureBO>();

            var entity = _uow.Vacaturen
                .GetNoTracking()
                .SingleOrDefault(v => v.Id == id);

            if (entity == null)
            {
                response.AddError("Vacature niet gevonden.");
                return response;
            }

            response.AddValue(MapToBO(entity));
            return response;
        }

        public GetResponse<VacatureBO> GetVacatureBySlug(string slug)
        {
            var response = new GetResponse<VacatureBO>();

            if (string.IsNullOrWhiteSpace(slug))
            {
                response.AddError("Slug mag niet leeg zijn.");
                return response;
            }

            var entity = _uow.Vacaturen
                .GetNoTracking()
                .SingleOrDefault(v => v.Slug == slug && v.IsGepubliceerd);

            if (entity == null)
            {
                response.AddError("Vacature niet gevonden.");
                return response;
            }

            response.AddValue(MapToBO(entity));
            return response;
        }

        public Response InsertUpdate(VacatureBO bo)
        {
            var response = new Response();

            if (string.IsNullOrWhiteSpace(bo?.Titel))
            {
                response.AddError("Titel is verplicht.");
                return response;
            }

            var slug = string.IsNullOrWhiteSpace(bo.Slug) ? GenereerSlug(bo.Titel) : GenereerSlug(bo.Slug);

            if (string.IsNullOrWhiteSpace(slug))
            {
                response.AddError("Kon geen geldige slug bepalen op basis van de titel.");
                return response;
            }

            // Slug-uniciteit expliciet afdwingen — anders crasht de unique index in de
            // databank pas bij SaveChangesAsync met een onafgehandelde fout.
            var bestaatAl = _uow.Vacaturen.GetNoTracking()
                .Any(v => v.Slug == slug && v.Id != bo.ID);
            if (bestaatAl)
            {
                response.AddError("Er bestaat al een vacature met deze slug. Kies een andere titel of slug.");
                return response;
            }

            Vacature entity;

            if (bo.ID == 0)
            {
                entity = _uow.Vacaturen.GetNew();
                entity.AangemaaktOp = DateTime.Now;
            }
            else
            {
                entity = _uow.Vacaturen.GetById(bo.ID);
                if (entity == null)
                {
                    response.AddError("Vacature niet gevonden.");
                    return response;
                }
            }

            entity.Titel             = bo.Titel;
            entity.Slug              = slug;
            entity.Categorie         = bo.Categorie;
            entity.Locatie           = bo.Locatie;
            entity.Dienstverband     = bo.Dienstverband;
            entity.KorteBeschrijving = bo.KorteBeschrijving;
            entity.Beschrijving      = bo.Beschrijving;
            entity.IsGepubliceerd    = bo.IsGepubliceerd;
            entity.SortOrder         = bo.SortOrder;
            entity.GewijzigdOp       = DateTime.Now;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Vacature opgeslagen.", "Vacature niet opgeslagen.");

            if (response.Success)
                response.InsertedId = entity.Id;

            return response;
        }

        public Response DeleteVacature(int id)
        {
            var response = new Response();

            _uow.Vacaturen.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Vacature verwijderd.", "Vacature niet verwijderd.");

            return response;
        }

        // ── private helpers ──────────────────────────────────────────────

        private static VacatureBO MapToBO(Vacature e)
        {
            return new VacatureBO
            {
                ID                = e.Id,
                Titel             = e.Titel,
                Slug              = e.Slug,
                Categorie         = e.Categorie,
                Locatie           = e.Locatie,
                Dienstverband     = e.Dienstverband,
                KorteBeschrijving = e.KorteBeschrijving,
                Beschrijving      = e.Beschrijving,
                IsGepubliceerd    = e.IsGepubliceerd,
                SortOrder         = e.SortOrder,
                AangemaaktOp      = e.AangemaaktOp,
                GewijzigdOp       = e.GewijzigdOp
            };
        }

        private static string GenereerSlug(string titel)
        {
            if (string.IsNullOrWhiteSpace(titel)) return string.Empty;

            var slug = titel.ToLowerInvariant().Trim();
            slug = Regex.Replace(slug, @"[àáâãäå]", "a");
            slug = Regex.Replace(slug, @"[èéêë]", "e");
            slug = Regex.Replace(slug, @"[ìíîï]", "i");
            slug = Regex.Replace(slug, @"[òóôõö]", "o");
            slug = Regex.Replace(slug, @"[ùúûü]", "u");
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-").Trim('-');

            return slug;
        }
    }
}
