using CMS.Core;
using CMS.Enums;
using CMS.Helpers;
using System.Collections.Generic;

namespace CMS.Interface
{
    public interface IORM<T>
    {
        Result<List<T>> Select();

        Result Insert(T entity);

        Result Update(T entity);

        Result Delete(T entity);

        T Select(object Id);

        Result<List<T>> Select(string where, Dictionary<string, object> p, SelectType st);
    }
}
