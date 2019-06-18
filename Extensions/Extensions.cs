using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Extensions
{
    public static class Extensions
    {
        private static string[] ignoredPropertyTypes = new string[1]
        {
        "String"
        };

        public static List<T> ToList<T>(this DataTable dt)
        {
            if (dt == null)
            {
                return null;
            }
            PropertyInfo[] properties = typeof(T).GetProperties();
            Dictionary<string, PropertyInfo> dictionary = new Dictionary<string, PropertyInfo>();
            PropertyInfo[] array = properties;
            foreach (PropertyInfo propertyInfo in array)
            {
                if (dt.Columns[propertyInfo.Name] != null && !dictionary.Keys.Contains(propertyInfo.Name))
                {
                    dictionary.Add(propertyInfo.Name, propertyInfo);
                }
            }
            List<T> list = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T val = Activator.CreateInstance<T>();
                foreach (KeyValuePair<string, PropertyInfo> item in dictionary)
                {
                    if (row[item.Key] is DBNull)
                    {
                        item.Value.SetValue(val, null);
                    }
                    else
                    {
                        item.Value.SetValue(val, row[item.Key]);
                    }
                }
                list.Add(val);
            }
            return list;
        }

        public static T MapTo<T>(this object source, T result = null) where T : class
        {
            if (source == null)
            {
                return null;
            }
            return (T)MapTo(typeof(T), source.GetType(), source, result);
        }

        private static object MapTo(Type TargetType, Type SourceType, object source, object result = null)
        {
            if (source == null)
            {
                return null;
            }
            if (TargetType.Name.Contains("List") || TargetType.Name.Contains("Collection"))
            {
                return null;
            }
            PropertyInfo[] properties = TargetType.GetProperties();
            PropertyInfo[] properties2 = SourceType.GetProperties();
            result = (result ?? Activator.CreateInstance(TargetType));
            PropertyInfo[] array = properties;
            foreach (PropertyInfo t in array)
            {
                PropertyInfo propertyInfo = properties2.FirstOrDefault((PropertyInfo x) => x.Name == t.Name);
                if (propertyInfo != null && t.GetSetMethod() != null)
                {
                    Type propertyType = propertyInfo.PropertyType;
                    if (!ignoredPropertyTypes.Contains(propertyType.Name) && (propertyType.IsClass || propertyType.IsInterface))
                    {
                        t.SetValue(result, MapTo(t.PropertyType, propertyInfo.PropertyType, propertyInfo.GetValue(source)));
                    }
                    else
                    {
                        t.SetValue(result, propertyInfo.GetValue(source));
                    }
                }
            }
            return result;
        }

        public static object GetField(this DataRow row, string field)
        {
            if (!(row[field] is DBNull))
            {
                return row[field];
            }
            return null;
        }

        private static string ReplaceURL(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                url = "link";
            }
            return url.ToLower().Replace(" ", "-").Replace("ü", "u")
                .Replace("ö", "o")
                .Replace("ı", "i")
                .Replace('ç', 'c')
                .Replace('ş', 's')
                .Replace('ğ', 'g')
                .Replace('/', '-')
                .Replace('.', '-')
                .Replace("%", "")
                .Replace("$", "")
                .Replace("&", "")
                .Replace('>', '-')
                .Replace('<', '-')
                .Replace('*', '-')
                .Replace(':', '-')
                .Replace("-----", "-")
                .Replace("----", "-")
                .Replace("---", "-")
                .Replace("--", "-");
        }

        public static string ToLinkText(this string url)
        {
            return ReplaceURL(url);
        }

        public static string ListToString(this IEnumerable<string> list)
        {
            if (list == null)
            {
                return "";
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string item in list)
            {
                stringBuilder.Append(item);
                stringBuilder.Append("-");
            }
            if (stringBuilder.Length > 0)
            {
                StringBuilder stringBuilder2 = stringBuilder;
                stringBuilder2.Remove(stringBuilder2.Length - 1, 1);
            }
            return stringBuilder.ToString();
        }

        public static Dictionary<string, object> CreateParameters(this object t, string name)
        {
            return new Dictionary<string, object>
        {
            {
                name,
                t
            }
        };
        }
    }
}
