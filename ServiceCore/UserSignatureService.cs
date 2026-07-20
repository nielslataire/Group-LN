using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using System;
using System.Linq;

namespace ServiceCore
{
    public class UserSignatureService : IUserSignatureService
    {
        private readonly UnitOfWorkCore _uow;

        public UserSignatureService(UnitOfWorkCore uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public GetResponse<UserEmailSignatureBO> GetByUserId(int userId)
        {
            var response = new GetResponse<UserEmailSignatureBO>();

            var entity = _uow.UserEmailSignatures.GetNoTracking().SingleOrDefault(s => s.UserId == userId);
            if (entity == null)
            {
                response.AddValue(new UserEmailSignatureBO { UserId = userId, SignatureHtml = string.Empty });
                return response;
            }

            response.AddValue(new UserEmailSignatureBO
            {
                UserId = entity.UserId,
                SignatureHtml = entity.SignatureHtml,
                SignatureFormat = string.IsNullOrWhiteSpace(entity.SignatureFormat) ? "Visual" : entity.SignatureFormat,
                GewijzigdOp = entity.GewijzigdOp
            });
            return response;
        }

        public Response Save(int userId, string signatureHtml, string signatureFormat)
        {
            var response = new Response();

            var entity = _uow.UserEmailSignatures.GetNoTracking().SingleOrDefault(s => s.UserId == userId);
            if (entity == null)
            {
                entity = _uow.UserEmailSignatures.GetNew();
                entity.UserId = userId;
            }
            else
            {
                entity = _uow.UserEmailSignatures.GetById(entity.Id);
            }

            entity.SignatureHtml = signatureHtml;
            entity.SignatureFormat = string.IsNullOrWhiteSpace(signatureFormat) ? "Visual" : signatureFormat;
            entity.GewijzigdOp = DateTime.Now;

            var result = _uow.SaveChangesAsync().GetAwaiter().GetResult();
            response.AddSaveChangesResult(result, "Handtekening opgeslagen.", "Handtekening niet opgeslagen.");

            return response;
        }
    }
}
