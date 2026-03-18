
using DALCore.Models.Partials.Contracts;

namespace DALCore.Models.Partials.Extensions;

public static class QueryScopeExtensions
{
    public static IQueryable<TEntity> ForUser<TEntity>(this IQueryable<TEntity> query, int userId)
        where TEntity : class, IUserScopedEntity
    {
        return query.Where(x => x.UserId == userId);
    }

    public static IQueryable<TEntity> ForCompany<TEntity>(this IQueryable<TEntity> query, int companyId)
        where TEntity : class, ICompanyScopedEntity
    {
        return query.Where(x => x.CompanyId == companyId);
    }

    public static IQueryable<TEntity> ForCompanies<TEntity>(this IQueryable<TEntity> query, IReadOnlyCollection<int> companyIds)
        where TEntity : class, ICompanyScopedEntity
    {
        if (companyIds == null || companyIds.Count == 0)
            return query.Where(_ => false);

        return query.Where(x => companyIds.Contains(x.CompanyId));
    }
}