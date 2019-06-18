using CMS.Attrubite;
using CMS.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMS.Core
{
    public class CacheCollection<T>
    {
        private Dictionary<string, CacheCollection<object>> _caches = new Dictionary<string, CacheCollection<object>>();

        private List<Cache<T>> cache = new List<Cache<T>>();

        public Table Table => GetTable<T>();

        public static CacheCollection<T> GetCollection(string appName)
        {
            HttpContext current = HttpContext.Current;
            if (current != null)
            {
                current.Application[appName] = (current.Application[appName] ?? new CacheCollection<T>());
                return (CacheCollection<T>)current.Application[appName];
            }
            return null;
        }

        private Table GetTable<K>()
        {
            return ((Table)Attribute.GetCustomAttribute(typeof(K), typeof(Table))) ?? Table;
        }

        public List<T> GetItems(string query, Dictionary<string, object> prms, SelectType selectType)
        {
            if (Table.CacheState == CacheType.NoCache)
            {
                return null;
            }
            string paramString = Cache<T>.GetParamString(prms);
            string Key = Cache<T>.GetKey(query, paramString);
            Cache<T> cache = this.cache.FirstOrDefault((Cache<T> x) => x.Key == Key);
            if (cache == null)
            {
                return null;
            }
            return cache.Data;
        }

        public void InsertItems(List<T> Data, string query, Dictionary<string, object> prms, SelectType selectType)
        {
            if (Table.CacheState != CacheType.NoCache && Data != null)
            {
                string paramString = Cache<T>.GetParamString(prms);
                string Key = Cache<T>.GetKey(query, paramString);
                Cache<T> cache = this.cache.FirstOrDefault((Cache<T> x) => x.Key == Key);
                if (cache != null)
                {
                    this.cache.Remove(cache);
                }
                Cache<T> cache2 = new Cache<T>();
                cache2.Query = query;
                cache2.Parameters = prms;
                cache2.Data = Data;
                this.cache.Add(cache2);
            }
        }

        public void Clear()
        {
            cache.Clear();
        }

        public void Refresh()
        {
            cache.Clear();
        }
    }
}