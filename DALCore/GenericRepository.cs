using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using DALCore.Models;

namespace DALCore
{
    public class GenericRepository<T> where T : class
    {
        private readonly cpmRunningContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(cpmRunningContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public IQueryable<T> GetNormal()
        {
            return _dbSet;
        }

        public IQueryable<T> GetNoTracking()
        {
            return _dbSet.AsNoTracking();
        }

        public IQueryable<T> GetNormal(Expression<Func<T, bool>> filter)
        {
            return filter != null ? _dbSet.Where(filter) : _dbSet;
        }

        public T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public void DeleteObject(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void DeleteObject(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
                _dbSet.Remove(entity);
        }

        public T GetNew()
        {
            var retval = Activator.CreateInstance<T>();
            _dbSet.Add(retval);
            return retval;
        }

        public IQueryable<T> Query() => _dbSet.AsQueryable();
        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate) => _dbSet.Where(predicate);
    }
}
