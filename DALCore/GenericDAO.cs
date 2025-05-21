using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace DALCore
{
    public class GenericDAO<T> : IDisposable where T : class
    {
        private DbSet<T> _dbSet;
        private cpmRunningContext _Context;
        public cpmRunningContext Context
        {
            get
            {
                return _Context;
            }
            set
            {
                if ((value != null))
                {
                    _Context = value;
                    _dbSet = _Context.Set<T>();
                }
            }
        }

        public void Dispose()
        {
            if ((_Context != null))
            {
                _Context.Dispose();
                _Context = null;
            }
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
            IQueryable<T> query = _dbSet;
            if (filter != null)
                query = query.Where(filter);
            return query;
        }

        public T GetById(int id)
        {
            return _dbSet.Find(id);
        }
        public void DeleteObject(T t)
        {
            _dbSet.Remove(t);
        }
        public void DeleteObject(int id)
        {
            var entity = _dbSet.Find(id);

            if ((entity != null))
                _dbSet.Remove(entity);
            return;
        }


        public T GetNew()
        {
            var retval = Activator.CreateInstance<T>(); // maakt een gewone instance aan
            //var retval = _dbSet.CreateProxy();
            _dbSet.Add(retval);
            return retval;
        }
    }

}
