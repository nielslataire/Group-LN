using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
                response.AddValue(MapToBO(e, inclDetails: false));

            return response;
        }

        public GetResponse<VacatureBO> GetVacatureById(int id)
        {
            var response = new GetResponse<VacatureBO>();

            var entity = _uow.Vacaturen
                .GetNoTracking()
                .Include(v => v.TaakItems)
                .Include(v => v.VereisteItems)
                .Include(v => v.VoordeelItems)
                .Include(v => v.SollicitatieStapItems)
                .SingleOrDefault(v => v.Id == id);

            if (entity == null)
            {
                response.AddError("Vacature niet gevonden.");
                return response;
            }

            response.AddValue(MapToBO(entity, inclDetails: true));
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
                .Include(v => v.TaakItems)
                .Include(v => v.VereisteItems)
                .Include(v => v.VoordeelItems)
                .Include(v => v.SollicitatieStapItems)
                .SingleOrDefault(v => v.Slug == slug && v.IsGepubliceerd);

            if (entity == null)
            {
                response.AddError("Vacature niet gevonden.");
                return response;
            }

            response.AddValue(MapToBO(entity, inclDetails: true));
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
            entity.Opleiding         = bo.Opleiding;
            entity.Start             = bo.Start;
            entity.KorteBeschrijving   = bo.KorteBeschrijving;
            entity.Beschrijving        = bo.Beschrijving;
            entity.VideoBestand        = bo.VideoBestand;
            entity.VideoPosterBestand  = bo.VideoPosterBestand;
            entity.IsGepubliceerd      = bo.IsGepubliceerd;
            entity.SortOrder           = bo.SortOrder;
            entity.GewijzigdOp         = DateTime.Now;

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

        // ── TAKENPAKKET ─────────────────────────────────────────────────────

        public Response InsertUpdateTaak(VacatureTaakBO bo)
        {
            var response = new Response();

            if (bo?.VacatureId <= 0)
            {
                response.AddError("VacatureId is verplicht.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(bo.Tekst))
            {
                response.AddError("Tekst is verplicht.");
                return response;
            }

            VacatureTaak entity;

            if (bo.ID == 0)
                entity = _uow.VacatureTaken.GetNew();
            else
            {
                entity = _uow.VacatureTaken.GetById(bo.ID);
                if (entity == null)
                {
                    response.AddError("Taak niet gevonden.");
                    return response;
                }
            }

            entity.VacatureId = bo.VacatureId;
            entity.SortOrder  = bo.SortOrder;
            entity.Tekst      = bo.Tekst;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Taak opgeslagen.", "Taak niet opgeslagen.");

            if (response.Success)
                response.InsertedId = entity.Id;

            return response;
        }

        public Response UpdateTakenVolgorde(int vacatureId, List<int> sortedIds)
        {
            var response = new Response();

            for (int i = 0; i < sortedIds.Count; i++)
            {
                var entity = _uow.VacatureTaken.GetById(sortedIds[i]);
                if (entity == null || entity.VacatureId != vacatureId) continue;
                entity.SortOrder = i * 10;
            }

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Volgorde opgeslagen.", "Volgorde niet opgeslagen.");
            return response;
        }

        public Response DeleteTaak(int id)
        {
            var response = new Response();

            _uow.VacatureTaken.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Taak verwijderd.", "Taak niet verwijderd.");

            return response;
        }

        // ── WIE ZOEKEN WE (must-have / mooi meegenomen) ─────────────────────

        public Response InsertUpdateVereiste(VacatureVereisteBO bo)
        {
            var response = new Response();

            if (bo?.VacatureId <= 0)
            {
                response.AddError("VacatureId is verplicht.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(bo.Tekst))
            {
                response.AddError("Tekst is verplicht.");
                return response;
            }

            VacatureVereiste entity;

            if (bo.ID == 0)
                entity = _uow.VacatureVereisten.GetNew();
            else
            {
                entity = _uow.VacatureVereisten.GetById(bo.ID);
                if (entity == null)
                {
                    response.AddError("Vereiste niet gevonden.");
                    return response;
                }
            }

            entity.VacatureId = bo.VacatureId;
            entity.SortOrder  = bo.SortOrder;
            entity.Categorie  = string.IsNullOrWhiteSpace(bo.Categorie) ? "MustHave" : bo.Categorie;
            entity.Tekst      = bo.Tekst;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Vereiste opgeslagen.", "Vereiste niet opgeslagen.");

            if (response.Success)
                response.InsertedId = entity.Id;

            return response;
        }

        public Response UpdateVereistenVolgorde(int vacatureId, List<int> sortedIds)
        {
            var response = new Response();

            for (int i = 0; i < sortedIds.Count; i++)
            {
                var entity = _uow.VacatureVereisten.GetById(sortedIds[i]);
                if (entity == null || entity.VacatureId != vacatureId) continue;
                entity.SortOrder = i * 10;
            }

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Volgorde opgeslagen.", "Volgorde niet opgeslagen.");
            return response;
        }

        public Response DeleteVereiste(int id)
        {
            var response = new Response();

            _uow.VacatureVereisten.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Vereiste verwijderd.", "Vereiste niet verwijderd.");

            return response;
        }

        // ── WAT BIEDEN WE ────────────────────────────────────────────────────

        public Response InsertUpdateVoordeel(VacatureVoordeelBO bo)
        {
            var response = new Response();

            if (bo?.VacatureId <= 0)
            {
                response.AddError("VacatureId is verplicht.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(bo.Tekst))
            {
                response.AddError("Tekst is verplicht.");
                return response;
            }

            VacatureVoordeel entity;

            if (bo.ID == 0)
                entity = _uow.VacatureVoordelen.GetNew();
            else
            {
                entity = _uow.VacatureVoordelen.GetById(bo.ID);
                if (entity == null)
                {
                    response.AddError("Voordeel niet gevonden.");
                    return response;
                }
            }

            entity.VacatureId = bo.VacatureId;
            entity.SortOrder  = bo.SortOrder;
            entity.Tekst      = bo.Tekst;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Voordeel opgeslagen.", "Voordeel niet opgeslagen.");

            if (response.Success)
                response.InsertedId = entity.Id;

            return response;
        }

        public Response UpdateVoordelenVolgorde(int vacatureId, List<int> sortedIds)
        {
            var response = new Response();

            for (int i = 0; i < sortedIds.Count; i++)
            {
                var entity = _uow.VacatureVoordelen.GetById(sortedIds[i]);
                if (entity == null || entity.VacatureId != vacatureId) continue;
                entity.SortOrder = i * 10;
            }

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Volgorde opgeslagen.", "Volgorde niet opgeslagen.");
            return response;
        }

        public Response DeleteVoordeel(int id)
        {
            var response = new Response();

            _uow.VacatureVoordelen.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Voordeel verwijderd.", "Voordeel niet verwijderd.");

            return response;
        }

        // ── STAPPENLIJST SOLLICITATIE ────────────────────────────────────────

        public Response InsertUpdateSollicitatieStap(VacatureSollicitatieStapBO bo)
        {
            var response = new Response();

            if (bo?.VacatureId <= 0)
            {
                response.AddError("VacatureId is verplicht.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(bo.Titel))
            {
                response.AddError("Titel is verplicht.");
                return response;
            }

            VacatureSollicitatieStap entity;

            if (bo.ID == 0)
                entity = _uow.VacatureSollicitatieStappen.GetNew();
            else
            {
                entity = _uow.VacatureSollicitatieStappen.GetById(bo.ID);
                if (entity == null)
                {
                    response.AddError("Stap niet gevonden.");
                    return response;
                }
            }

            entity.VacatureId = bo.VacatureId;
            entity.SortOrder  = bo.SortOrder;
            entity.Titel      = bo.Titel;
            entity.Tekst      = bo.Tekst;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Stap opgeslagen.", "Stap niet opgeslagen.");

            if (response.Success)
                response.InsertedId = entity.Id;

            return response;
        }

        public Response UpdateSollicitatieStappenVolgorde(int vacatureId, List<int> sortedIds)
        {
            var response = new Response();

            for (int i = 0; i < sortedIds.Count; i++)
            {
                var entity = _uow.VacatureSollicitatieStappen.GetById(sortedIds[i]);
                if (entity == null || entity.VacatureId != vacatureId) continue;
                entity.SortOrder = i * 10;
            }

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Volgorde opgeslagen.", "Volgorde niet opgeslagen.");
            return response;
        }

        public Response DeleteSollicitatieStap(int id)
        {
            var response = new Response();

            _uow.VacatureSollicitatieStappen.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Stap verwijderd.", "Stap niet verwijderd.");

            return response;
        }

        // ── private helpers ──────────────────────────────────────────────

        private static VacatureBO MapToBO(Vacature e, bool inclDetails)
        {
            var bo = new VacatureBO
            {
                ID                = e.Id,
                Titel             = e.Titel,
                Slug              = e.Slug,
                Categorie         = e.Categorie,
                Locatie           = e.Locatie,
                Dienstverband     = e.Dienstverband,
                Opleiding         = e.Opleiding,
                Start             = e.Start,
                KorteBeschrijving  = e.KorteBeschrijving,
                Beschrijving       = e.Beschrijving,
                VideoBestand       = e.VideoBestand,
                VideoPosterBestand = e.VideoPosterBestand,
                IsGepubliceerd     = e.IsGepubliceerd,
                SortOrder          = e.SortOrder,
                AangemaaktOp      = e.AangemaaktOp,
                GewijzigdOp       = e.GewijzigdOp
            };

            if (inclDetails)
            {
                if (e.TaakItems != null)
                {
                    foreach (var t in e.TaakItems.OrderBy(t => t.SortOrder))
                    {
                        bo.TaakItems.Add(new VacatureTaakBO
                        {
                            ID = t.Id,
                            VacatureId = t.VacatureId,
                            SortOrder = t.SortOrder,
                            Tekst = t.Tekst
                        });
                    }
                }

                if (e.VereisteItems != null)
                {
                    foreach (var v in e.VereisteItems.OrderBy(v => v.SortOrder))
                    {
                        bo.VereisteItems.Add(new VacatureVereisteBO
                        {
                            ID = v.Id,
                            VacatureId = v.VacatureId,
                            SortOrder = v.SortOrder,
                            Categorie = v.Categorie ?? "MustHave",
                            Tekst = v.Tekst
                        });
                    }
                }

                if (e.VoordeelItems != null)
                {
                    foreach (var v in e.VoordeelItems.OrderBy(v => v.SortOrder))
                    {
                        bo.VoordeelItems.Add(new VacatureVoordeelBO
                        {
                            ID = v.Id,
                            VacatureId = v.VacatureId,
                            SortOrder = v.SortOrder,
                            Tekst = v.Tekst
                        });
                    }
                }

                if (e.SollicitatieStapItems != null)
                {
                    foreach (var s in e.SollicitatieStapItems.OrderBy(s => s.SortOrder))
                    {
                        bo.SollicitatieStapItems.Add(new VacatureSollicitatieStapBO
                        {
                            ID = s.Id,
                            VacatureId = s.VacatureId,
                            SortOrder = s.SortOrder,
                            Titel = s.Titel,
                            Tekst = s.Tekst
                        });
                    }
                }
            }

            return bo;
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
