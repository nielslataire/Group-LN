using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using System;
using System.Linq;

namespace ServiceCore
{
    public class VacatureSollicitatieService : IVacatureSollicitatieService
    {
        private readonly UnitOfWorkCore _uow;

        public VacatureSollicitatieService(UnitOfWorkCore uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public GetResponse<VacatureSollicitatieBO> GetSollicitaties(int? vacatureId = null)
        {
            var response = new GetResponse<VacatureSollicitatieBO>();

            var query = _uow.VacatureSollicitaties.GetNoTracking().AsQueryable();

            if (vacatureId.HasValue)
                query = query.Where(s => s.VacatureId == vacatureId.Value);

            // Projectie zonder CvBestand (VARBINARY(MAX)) — die is niet nodig in een lijstweergave
            // en zou nodeloos veel data ophalen voor elke rij.
            var entities = query
                .OrderByDescending(s => s.AangemaaktOp)
                .Select(s => new VacatureSollicitatie
                {
                    Id = s.Id,
                    VacatureId = s.VacatureId,
                    VacatureTitelSnapshot = s.VacatureTitelSnapshot,
                    Voornaam = s.Voornaam,
                    Achternaam = s.Achternaam,
                    Email = s.Email,
                    Telefoon = s.Telefoon,
                    Motivatie = s.Motivatie,
                    CvBestandsnaam = s.CvBestandsnaam,
                    CvBestandType = s.CvBestandType,
                    IsGelezen = s.IsGelezen,
                    AangemaaktOp = s.AangemaaktOp
                })
                .ToList();

            foreach (var e in entities)
                response.AddValue(MapToBO(e, inclCv: false));

            return response;
        }

        public GetResponse<VacatureSollicitatieBO> GetSollicitatieCv(int id)
        {
            var response = new GetResponse<VacatureSollicitatieBO>();

            var entity = _uow.VacatureSollicitaties.GetNoTracking().SingleOrDefault(s => s.Id == id);
            if (entity == null)
            {
                response.AddError("Sollicitatie niet gevonden.");
                return response;
            }

            response.AddValue(MapToBO(entity, inclCv: true));
            return response;
        }

        public Response MarkeerGelezen(int id, bool gelezen)
        {
            var response = new Response();

            var entity = _uow.VacatureSollicitaties.GetById(id);
            if (entity == null)
            {
                response.AddError("Sollicitatie niet gevonden.");
                return response;
            }

            entity.IsGelezen = gelezen;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Status bijgewerkt.", "Status niet bijgewerkt.");
            return response;
        }

        public Response DeleteSollicitatie(int id)
        {
            var response = new Response();

            _uow.VacatureSollicitaties.DeleteObject(id);

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Sollicitatie verwijderd.", "Sollicitatie niet verwijderd.");
            return response;
        }

        private static VacatureSollicitatieBO MapToBO(VacatureSollicitatie e, bool inclCv)
        {
            return new VacatureSollicitatieBO
            {
                ID = e.Id,
                VacatureId = e.VacatureId,
                VacatureTitelSnapshot = e.VacatureTitelSnapshot,
                Voornaam = e.Voornaam,
                Achternaam = e.Achternaam,
                Email = e.Email,
                Telefoon = e.Telefoon,
                Motivatie = e.Motivatie,
                CvBestandsnaam = e.CvBestandsnaam,
                CvBestandType = e.CvBestandType,
                CvBestand = inclCv ? e.CvBestand : null,
                IsGelezen = e.IsGelezen,
                AangemaaktOp = e.AangemaaktOp
            };
        }
    }
}
