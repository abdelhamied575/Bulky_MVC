using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll(string? includeProperties = null);
        T Get(Expression<Func<T, bool>> filter, string? includeProperties = null);
        void Add(T entity);

        // I remove The Update Because May Be I Want To Update Some Property Some Not
        //void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}
