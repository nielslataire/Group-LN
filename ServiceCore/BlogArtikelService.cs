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
    public class BlogArtikelService : IBlogArtikelService
    {
        private readonly UnitOfWorkCore _uow;

        public BlogArtikelService(UnitOfWorkCore uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public GetResponse<BlogArtikelBO> GetArtikelen(bool alleenGepubliceerd = false)
        {
            var response = new GetResponse<BlogArtikelBO>();

            var query = _uow.BlogArtikelen
                .GetNoTracking()
                .Include(a => a.Blokken)
                .AsQueryable();

            if (alleenGepubliceerd)
                query = query.Where(a => a.IsGepubliceerd);

            var entities = query
                .OrderByDescending(a => a.Datum)
                .ToList();

            foreach (var e in entities)
                response.AddValue(MapToBO(e, inclBlokken: false));

            return response;
        }

        public GetResponse<BlogArtikelBO> GetArtikelById(int id)
        {
            var response = new GetResponse<BlogArtikelBO>();

            var entity = _uow.BlogArtikelen
                .GetNoTracking()
                .Include(a => a.Blokken)
                .Include(a => a.FaqItems)
                .SingleOrDefault(a => a.Id == id);

            if (entity == null)
            {
                response.AddError("Artikel niet gevonden.");
                return response;
            }

            response.AddValue(MapToBO(entity, inclBlokken: true));
            return response;
        }

        public GetResponse<BlogArtikelBO> GetArtikelBySlug(string slug)
        {
            var response = new GetResponse<BlogArtikelBO>();

            if (string.IsNullOrWhiteSpace(slug))
            {
                response.AddError("Slug mag niet leeg zijn.");
                return response;
            }

            var entity = _uow.BlogArtikelen
                .GetNoTracking()
                .Include(a => a.Blokken)
                .SingleOrDefault(a => a.Slug == slug && a.IsGepubliceerd);

            if (entity == null)
            {
                response.AddError("Artikel niet gevonden.");
                return response;
            }

            response.AddValue(MapToBO(entity, inclBlokken: true));
            return response;
        }

        public Response InsertUpdate(BlogArtikelBO bo)
        {
            var response = new Response();

            if (string.IsNullOrWhiteSpace(bo?.Titel))
            {
                response.AddError("Titel is verplicht.");
                return response;
            }

            BlogArtikel entity;

            if (bo.ID == 0)
            {
                entity = _uow.BlogArtikelen.GetNew();
                entity.AangemaaktOp = DateTime.Now;
            }
            else
            {
                entity = _uow.BlogArtikelen.GetById(bo.ID);
                if (entity == null)
                {
                    response.AddError("Artikel niet gevonden.");
                    return response;
                }
            }

            entity.Titel             = bo.Titel;
            entity.Slug              = string.IsNullOrWhiteSpace(bo.Slug) ? GenereerSlug(bo.Titel) : bo.Slug;
            entity.PreviewTekst      = bo.PreviewTekst;
            entity.DetailTitel       = bo.DetailTitel;
            entity.DetailTitelTekst  = bo.DetailTitelTekst;
            entity.FotoBestand       = bo.FotoBestand;
            entity.Datum             = bo.Datum == default ? DateTime.Now : bo.Datum;
            entity.IsGepubliceerd    = bo.IsGepubliceerd;
            entity.SortOrder         = bo.SortOrder;
            entity.MetaTitel         = bo.MetaTitel;
            entity.MetaOmschrijving  = bo.MetaOmschrijving;
            entity.MetaKeywords      = bo.MetaKeywords;
            entity.GeoRegio          = bo.GeoRegio;
            entity.GeoPlaatsnaam     = bo.GeoPlaatsnaam;
            entity.GeoPositie        = bo.GeoPositie;
            entity.GewijzigdOp       = DateTime.Now;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Artikel opgeslagen.", "Artikel niet opgeslagen.");

            if (response.Success)
                response.InsertedId = entity.Id;

            return response;
        }

        public Response InsertUpdateBlok(BlogArtikelBlokBO bo)
        {
            var response = new Response();

            if (bo?.ArtikelId <= 0)
            {
                response.AddError("ArtikelId is verplicht.");
                return response;
            }

            BlogArtikelBlok entity;

            if (bo.ID == 0)
                entity = _uow.BlogArtikelBlokken.GetNew();
            else
            {
                entity = _uow.BlogArtikelBlokken.GetById(bo.ID);
                if (entity == null)
                {
                    response.AddError("Blok niet gevonden.");
                    return response;
                }
            }

            entity.ArtikelId  = bo.ArtikelId;
            entity.SortOrder  = bo.SortOrder;
            entity.BlokType   = string.IsNullOrWhiteSpace(bo.BlokType) ? "tekst" : bo.BlokType;
            entity.Titel      = bo.Titel;
            entity.RijkeTekst = bo.RijkeTekst;
            entity.FotoBestand = bo.FotoBestand;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Blok opgeslagen.", "Blok niet opgeslagen.");

            if (response.Success)
                response.InsertedId = entity.Id;

            return response;
        }

        public Response InsertUpdateFaq(BlogArtikelFaqBO bo)
        {
            var response = new Response();

            if (bo?.ArtikelId <= 0)
            {
                response.AddError("ArtikelId is verplicht.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(bo.Vraag))
            {
                response.AddError("Vraag is verplicht.");
                return response;
            }

            BlogArtikelFaq entity;

            if (bo.ID == 0)
                entity = _uow.BlogArtikelFaqItems.GetNew();
            else
            {
                entity = _uow.BlogArtikelFaqItems.GetById(bo.ID);
                if (entity == null)
                {
                    response.AddError("FAQ-item niet gevonden.");
                    return response;
                }
            }

            entity.ArtikelId = bo.ArtikelId;
            entity.SortOrder = bo.SortOrder;
            entity.Vraag     = bo.Vraag;
            entity.Antwoord  = bo.Antwoord;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "FAQ opgeslagen.", "FAQ niet opgeslagen.");

            if (response.Success)
                response.InsertedId = entity.Id;

            return response;
        }

        public Response UpdateFaqVolgorde(int artikelId, List<int> sortedIds)
        {
            var response = new Response();

            for (int i = 0; i < sortedIds.Count; i++)
            {
                var entity = _uow.BlogArtikelFaqItems.GetById(sortedIds[i]);
                if (entity == null || entity.ArtikelId != artikelId) continue;
                entity.SortOrder = i * 10;
            }

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Volgorde opgeslagen.", "Volgorde niet opgeslagen.");
            return response;
        }

        public Response UpdateBlokkenVolgorde(int artikelId, List<int> sortedIds)
        {
            var response = new Response();

            for (int i = 0; i < sortedIds.Count; i++)
            {
                var entity = _uow.BlogArtikelBlokken.GetById(sortedIds[i]);
                if (entity == null || entity.ArtikelId != artikelId) continue;
                entity.SortOrder = i * 10;
            }

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Volgorde opgeslagen.", "Volgorde niet opgeslagen.");
            return response;
        }

        public Response DeleteArtikel(int id)
        {
            var response = new Response();

            _uow.BlogArtikelen.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Artikel verwijderd.", "Artikel niet verwijderd.");

            return response;
        }

        public Response DeleteBlok(int id)
        {
            var response = new Response();

            _uow.BlogArtikelBlokken.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Blok verwijderd.", "Blok niet verwijderd.");

            return response;
        }

        public Response DeleteFaq(int id)
        {
            var response = new Response();

            _uow.BlogArtikelFaqItems.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "FAQ-item verwijderd.", "FAQ-item niet verwijderd.");

            return response;
        }

        // ── private helpers ──────────────────────────────────────────────

        private static BlogArtikelBO MapToBO(BlogArtikel e, bool inclBlokken)
        {
            var bo = new BlogArtikelBO
            {
                ID               = e.Id,
                Titel            = e.Titel,
                Slug             = e.Slug,
                PreviewTekst     = e.PreviewTekst,
                DetailTitel      = e.DetailTitel,
                DetailTitelTekst = e.DetailTitelTekst,
                FotoBestand      = e.FotoBestand,
                Datum            = e.Datum,
                IsGepubliceerd   = e.IsGepubliceerd,
                SortOrder        = e.SortOrder,
                AangemaaktOp     = e.AangemaaktOp,
                GewijzigdOp      = e.GewijzigdOp,
                MetaTitel        = e.MetaTitel,
                MetaOmschrijving = e.MetaOmschrijving,
                MetaKeywords     = e.MetaKeywords,
                GeoRegio         = e.GeoRegio,
                GeoPlaatsnaam    = e.GeoPlaatsnaam,
                GeoPositie       = e.GeoPositie
            };

            if (inclBlokken && e.Blokken != null)
            {
                foreach (var blok in e.Blokken.OrderBy(b => b.SortOrder))
                {
                    bo.Blokken.Add(new BlogArtikelBlokBO
                    {
                        ID          = blok.Id,
                        ArtikelId   = blok.ArtikelId,
                        SortOrder   = blok.SortOrder,
                        BlokType    = blok.BlokType ?? "tekst",
                        Titel       = blok.Titel,
                        RijkeTekst  = blok.RijkeTekst,
                        FotoBestand = blok.FotoBestand
                    });
                }
            }

            if (inclBlokken && e.FaqItems != null)
            {
                foreach (var faq in e.FaqItems.OrderBy(f => f.SortOrder))
                {
                    bo.FaqItems.Add(new BlogArtikelFaqBO
                    {
                        ID        = faq.Id,
                        ArtikelId = faq.ArtikelId,
                        SortOrder = faq.SortOrder,
                        Vraag     = faq.Vraag,
                        Antwoord  = faq.Antwoord
                    });
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
