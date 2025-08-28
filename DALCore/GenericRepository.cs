using System;
using System.Collections.Generic;
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

        // ===== Reads =====
        public IQueryable<T> GetNormal() => _dbSet; // tracked

        public IQueryable<T> GetNormal(Expression<Func<T, bool>> filter)
            => filter != null ? _dbSet.Where(filter) : _dbSet;

        public IQueryable<T> GetNoTracking() => _dbSet.AsNoTracking();

        public IQueryable<T> GetNoTracking(Expression<Func<T, bool>> filter)
            => (filter != null ? _dbSet.Where(filter) : _dbSet).AsNoTracking();

        public T GetById(int id) => _dbSet.Find(id);

        public IQueryable<T> Query() => _dbSet.AsQueryable();

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
            => _dbSet.Where(predicate);

        // ===== Writes =====
        /// <summary>
        /// Nieuw object, automatisch in Added-state (zoals jij al had).
        /// </summary>
        public T GetNew()
        {
            var retval = Activator.CreateInstance<T>();
            _dbSet.Add(retval);
            return retval;
        }

        public void Add(T entity) => _dbSet.Add(entity);

        public void Attach(T entity) => _dbSet.Attach(entity);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Remove(T entity) => _dbSet.Remove(entity);

        // ===== Oude delete-methodes behouden =====
        public void DeleteObject(T entity) => _dbSet.Remove(entity);

        public void DeleteObject(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
                _dbSet.Remove(entity);
        }
    }
}
