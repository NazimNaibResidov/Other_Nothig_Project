using CMS.Attrubite;
using CMS.Enums;
using CMS.Extensions;
using CMS.Helpers;
using CMS.Interface;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CMS.Core
{
    public class ORMBase<T, OT> : IORM<T> where T : class where OT : class
    {
        private static int? pageSize;

        private static OT _current;

        private static CacheCollection<T> _cacheCollection;

        public static int PageSize
        {
            get
            {
                if (!pageSize.HasValue)
                {
                    pageSize = Convert.ToInt32(ConfigurationManager.AppSettings["PageSize"]);
                }
                if (!pageSize.HasValue)
                {
                    return 20;
                }
                return pageSize.Value;
            }
            set
            {
                pageSize = value;
            }
        }

        public static OT Current
        {
            get
            {
                HttpContext current = HttpContext.Current;
                string name = $"_{Table.SchemaName}.{Table.TableName}";
                if (current != null)
                {
                    current.Session[name] = (current.Session[name] ?? Activator.CreateInstance<OT>());
                    return current.Session[name] as OT;
                }
                _current = (_current ?? Activator.CreateInstance<OT>());
                return _current;
            }
        }

        public CacheCollection<T> CacheContext
        {
            get
            {
                HttpContext current = HttpContext.Current;
                if (current != null)
                {
                    string name = $"{Table.SchemaName}.{Table.TableName}_Cache";
                    current.Application[name] = (current.Application[name] ?? new CacheCollection<T>());
                    return (CacheCollection<T>)current.Application[name];
                }
                _cacheCollection = (_cacheCollection ?? new CacheCollection<T>());
                return _cacheCollection;
            }
        }

        protected static Table Table => (Table)Attribute.GetCustomAttribute(typeof(T), typeof(Table), inherit: true);

        private Type Type => typeof(T);

        private Table GetTable<K>()
        {
            return ((Table)Attribute.GetCustomAttribute(typeof(K), typeof(Table))) ?? Table;
        }

        protected object GetPrimaryValue(T entity)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            int num = 0;
            if (num < properties.Length)
            {
                PropertyInfo propertyInfo = properties[num];
                if (propertyInfo.Name.ToLower() == Table.PrimaryKey.ToLower())
                {
                    return propertyInfo.GetValue(entity);
                }
                return null;
            }
            return null;
        }

        private string GetColumnsString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            PropertyInfo[] properties = Type.GetProperties();
            foreach (PropertyInfo propertyInfo in properties)
            {
                stringBuilder.Append($"{propertyInfo.Name}, ");
            }
            StringBuilder stringBuilder2 = stringBuilder;
            stringBuilder2.Remove(stringBuilder2.Length - 1, 1);
            return stringBuilder.ToString();
        }

        private string CreateSelect<T>(string extension = "", string columnExtension = "")
        {
            Table table = (Table)Attribute.GetCustomAttribute(typeof(T), typeof(Table));
            table = (table ?? Table);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("SELECT ");
            stringBuilder.Append(columnExtension);
            stringBuilder.Append(" * FROM ");
            stringBuilder.Append($"{table.SchemaName}.{table.TableName}");
            stringBuilder.Append(" ");
            if (Table.IsActiveValid)
            {
                if (string.IsNullOrEmpty(extension.Trim()))
                {
                    stringBuilder.Append("WHERE ISActive=1");
                }
                else if (extension.ToLower().Contains("where"))
                {
                    int num = extension.ToLower().IndexOf("where");
                    extension = extension.Insert(num + 5, " (");
                    int startIndex = extension.Length;
                    if (extension.ToLower().Contains("order by"))
                    {
                        startIndex = extension.ToLower().IndexOf("order by");
                    }
                    else if (extension.ToLower().Contains("group by"))
                    {
                        startIndex = extension.ToLower().IndexOf("group by");
                    }
                    extension = extension.Insert(startIndex, ")  AND IsActive=1 ");
                    stringBuilder.Append(extension);
                }
                else
                {
                    stringBuilder.Append("WHERE IsActive=1 ");
                    stringBuilder.Append(extension);
                }
            }
            else
            {
                stringBuilder.Append(extension);
            }
            return stringBuilder.ToString();
        }

        public virtual Result Insert(T entity)
        {
            if (Table.TableType == TableType.BusinessEntityTable)
            {
                Dictionary<string, object> parameters = Table.BETypeID.CreateParameters("@type");
                Result result = Tools.ExecuteScalar("Insert BusinessEntities values(@type,1);select Scope_Identity();", parameters);
                if (result.State != 0)
                {
                    return result;
                }
                PropertyInfo propertyInfo = Type.GetProperties().FirstOrDefault((PropertyInfo x) => x.Name.ToLower() == Table.PrimaryKey.ToLower());
                if (!(propertyInfo != null))
                {
                    return new Result
                    {
                        State = ResultState.Exception,
                        Message = "Business Entity üretilirken bir sorun oluştu. Oluşturulan BusinessEntity Nesneye eklenemiyor."
                    };
                }
                propertyInfo.SetValue(entity, Convert.ToInt64(result.Data));
            }
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("INSERT INTO ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName}(");
            PropertyInfo propertyInfo2 = null;
            PropertyInfo[] properties = Type.GetProperties();
            foreach (PropertyInfo propertyInfo3 in properties)
            {
                if (Table.TableType == TableType.PrimaryTable && propertyInfo3.Name.ToLower() == Table.IdentityColumn.ToLower())
                {
                    propertyInfo2 = propertyInfo3;
                }
                if (propertyInfo3.GetValue(entity) != null && propertyInfo3.Name.ToLower() != Table.IdentityColumn.ToLower())
                {
                    stringBuilder.Append($"{propertyInfo3.Name},");
                }
            }
            StringBuilder stringBuilder2 = stringBuilder;
            stringBuilder2.Remove(stringBuilder2.Length - 1, 1);
            stringBuilder.Append(") values (");
            properties = Type.GetProperties();
            foreach (PropertyInfo propertyInfo4 in properties)
            {
                object value = propertyInfo4.GetValue(entity);
                if (value != null && propertyInfo4.Name.ToLower() != Table.IdentityColumn.ToLower())
                {
                    stringBuilder.Append($"@{propertyInfo4.Name},");
                    dictionary.Add($"@{propertyInfo4.Name}", value);
                }
            }
            StringBuilder stringBuilder3 = stringBuilder;
            stringBuilder3.Remove(stringBuilder3.Length - 1, 1);
            stringBuilder.Append("); ");
            Result result2;
            if (Table.TableType == TableType.PrimaryTable)
            {
                stringBuilder.Append("Select Scope_Identity();");
                result2 = Tools.ExecuteScalar(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
                if (propertyInfo2 != null)
                {
                    propertyInfo2.SetValue(entity, Convert.ToInt32(result2.Data));
                }
            }
            else
            {
                result2 = Tools.ExecuteNonQuery(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
            }
            if (result2.State == ResultState.Success)
            {
                CacheContext.Refresh();
            }
            return result2;
        }

        public virtual Result Insert(IEnumerable<T> list)
        {
            List<Result> list2 = new List<Result>();
            foreach (T item2 in list)
            {
                Result item = Insert(item2);
                list2.Add(item);
            }
            Result result = list2.FirstOrDefault((Result x) => x.State != ResultState.Success);
            if (result != null)
            {
                return result;
            }
            return list2.FirstOrDefault();
        }

        public virtual Result Update(IEnumerable<T> list)
        {
            List<Result> list2 = new List<Result>();
            foreach (T item2 in list)
            {
                Result item = Update(item2);
                list2.Add(item);
            }
            Result result = list2.FirstOrDefault((Result x) => x.State != ResultState.Success);
            if (result != null)
            {
                return result;
            }
            return list2.FirstOrDefault();
        }

        public virtual Result Update(T entity)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("UPDATE ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName}");
            stringBuilder.Append(" SET ");
            List<PropertyInfo> list = new List<PropertyInfo>();
            PropertyInfo[] properties = Type.GetProperties();
            foreach (PropertyInfo propertyInfo in properties)
            {
                object value = propertyInfo.GetValue(entity);
                if (propertyInfo.Name.ToLower() == Table.PrimaryKey.ToLower() || propertyInfo.Name.ToLower() == Table.IdentityColumn.ToLower())
                {
                    dictionary.Add($"@{propertyInfo.Name}", value);
                    continue;
                }
                if (value != null)
                {
                    dictionary.Add($"@{propertyInfo.Name}", value);
                    stringBuilder.Append(string.Format("{0}=@{0},", propertyInfo.Name));
                }
                else
                {
                    dictionary.Add($"@{propertyInfo.Name}", DBNull.Value);
                    stringBuilder.Append(string.Format("{0}=@{0},", propertyInfo.Name));
                }
                if (Table.CompositeKeys != null && Table.CompositeKeys.Contains(propertyInfo.Name))
                {
                    list.Add(propertyInfo);
                }
            }
            StringBuilder stringBuilder2 = stringBuilder;
            stringBuilder2.Remove(stringBuilder2.Length - 1, 1);
            switch (Table.TableType)
            {
                case TableType.PrimaryTable:
                case TableType.BusinessEntityTable:
                    stringBuilder.Append(string.Format(" WHERE {0}=@{0}", Table.PrimaryKey, GetPrimaryValue(entity)));
                    break;
                case TableType.CompositTable:
                    {
                        stringBuilder.Append(" WHERE ");
                        foreach (PropertyInfo item in list)
                        {
                            item.GetValue(entity);
                            stringBuilder.Append(string.Format("{0}=@{0}", item.Name));
                            stringBuilder.Append(" AND ");
                        }
                        StringBuilder stringBuilder3 = stringBuilder;
                        stringBuilder3.Remove(stringBuilder3.Length - 4, 4);
                        break;
                    }
                default:
                    return new Result
                    {
                        State = ResultState.Exception,
                        Message = "Tablo tipi belirtilmeli"
                    };
            }
            Result result = Tools.ExecuteNonQuery(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
            if (result.State == ResultState.Success)
            {
                CacheContext.Refresh();
            }
            return result;
        }

        public virtual Result UpdatePart(string key, object value, object id)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("UPDATE ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName}");
            stringBuilder.Append(" SET ");
            stringBuilder.Append(key);
            stringBuilder.Append("=@");
            stringBuilder.Append(key);
            stringBuilder.Append(string.Format(" WHERE {0}=@{0}", Table.PrimaryKey, id));
            dictionary.Add("@" + key, value);
            dictionary.Add("@" + Table.PrimaryKey, id);
            Result result = Tools.ExecuteNonQuery(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
            if (result.State == ResultState.Success)
            {
                CacheContext.Refresh();
            }
            return result;
        }

        public virtual Result UpdatePart(string key, object value, params Tuple<string, object>[] ids)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("UPDATE ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName}");
            stringBuilder.Append(" SET ");
            stringBuilder.Append(key);
            stringBuilder.Append("=@");
            stringBuilder.Append(key);
            stringBuilder.Append(" WHERE ");
            foreach (Tuple<string, object> tuple in ids)
            {
                stringBuilder.Append(string.Format(" {0}=@{0} AND", tuple.Item1));
                dictionary.Add("@" + tuple.Item1, tuple.Item2);
            }
            StringBuilder stringBuilder2 = stringBuilder;
            stringBuilder2.Remove(stringBuilder2.Length - 4, 4);
            dictionary.Add("@" + key, value);
            Result result = Tools.ExecuteNonQuery(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
            if (result.State == ResultState.Success)
            {
                CacheContext.Refresh();
            }
            return result;
        }

        public virtual Result UpdatePart(Dictionary<string, object> updateFields, object id)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("UPDATE ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName}");
            stringBuilder.Append(" SET ");
            foreach (KeyValuePair<string, object> updateField in updateFields)
            {
                stringBuilder.Append(updateField.Key);
                stringBuilder.Append("=@");
                stringBuilder.Append(updateField.Key);
                stringBuilder.Append(",");
                dictionary.Add("@" + updateField.Key, updateField.Value);
            }
            StringBuilder stringBuilder2 = stringBuilder;
            stringBuilder2.Remove(stringBuilder2.Length - 1, 1);
            stringBuilder.Append(" WHERE ");
            stringBuilder.Append(Table.PrimaryKey);
            stringBuilder.Append("=@");
            stringBuilder.Append(Table.PrimaryKey);
            dictionary.Add("@" + Table.PrimaryKey, id);
            Result result = Tools.ExecuteNonQuery(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
            if (result.State == ResultState.Success)
            {
                CacheContext.Refresh();
            }
            return result;
        }

        public virtual Result UpdatePart(Dictionary<string, object> updateFields, params Tuple<string, object>[] ids)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("UPDATE ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName}");
            stringBuilder.Append(" SET ");
            foreach (KeyValuePair<string, object> updateField in updateFields)
            {
                stringBuilder.Append(updateField.Key);
                stringBuilder.Append("=@");
                stringBuilder.Append(updateField.Key);
                stringBuilder.Append(",");
                dictionary.Add("@" + updateField.Key, updateField.Value);
            }
            StringBuilder stringBuilder2 = stringBuilder;
            stringBuilder2.Remove(stringBuilder2.Length - 1, 1);
            stringBuilder.Append(" WHERE ");
            foreach (Tuple<string, object> tuple in ids)
            {
                stringBuilder.Append(string.Format(" {0}=@{0} AND", tuple.Item1));
                dictionary.Add("@" + tuple.Item1, tuple.Item2);
            }
            StringBuilder stringBuilder3 = stringBuilder;
            stringBuilder3.Remove(stringBuilder3.Length - 4, 4);
            Result result = Tools.ExecuteNonQuery(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
            if (result.State == ResultState.Success)
            {
                CacheContext.Refresh();
            }
            return result;
        }

        public virtual Result Delete(T entity)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("DELETE FROM ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName} WHERE ");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            switch (Table.TableType)
            {
                case TableType.PrimaryTable:
                case TableType.BusinessEntityTable:
                    stringBuilder.Append(string.Format("{0}=@{0}", Table.PrimaryKey));
                    dictionary.Add($"@{Table.PrimaryKey}", GetPrimaryValue(entity));
                    break;
                case TableType.CompositTable:
                    {
                        string[] compositeKeys = Table.CompositeKeys;
                        foreach (string item in compositeKeys)
                        {
                            PropertyInfo propertyInfo = entity.GetType().GetProperties().FirstOrDefault((PropertyInfo x) => x.Name.ToLower() == item.ToLower());
                            if (propertyInfo != null)
                            {
                                stringBuilder.Append(string.Format("{0}=@{0} AND ", item));
                                dictionary.Add($"@{item}", propertyInfo.GetValue(entity));
                            }
                        }
                        StringBuilder stringBuilder2 = stringBuilder;
                        stringBuilder2.Remove(stringBuilder2.Length - 4, 4);
                        break;
                    }
                default:
                    return new Result
                    {
                        Message = "Tablo Tipi Bulunamadığından silme işlemi iptal edildi",
                        State = ResultState.Exception
                    };
            }
            Result result = Tools.ExecuteNonQuery(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
            if (result.State == ResultState.Success)
            {
                CacheContext.Refresh();
            }
            return result;
        }

        public virtual Result DeleteWithID(object ID)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("DELETE FROM ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName} WHERE ");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            stringBuilder.Append(string.Format("{0}=@{0}", Table.PrimaryKey));
            dictionary.Add($"@{Table.PrimaryKey}", ID);
            Result result = Tools.ExecuteNonQuery(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
            if (result.State == ResultState.Success)
            {
                CacheContext.Refresh();
            }
            return result;
        }

        public virtual Result DeleteWithID(params object[] IDs)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("DELETE FROM ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName} WHERE ");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            for (int i = 0; i < Table.CompositeKeys.Length; i++)
            {
                string arg = Table.CompositeKeys[i];
                stringBuilder.Append(string.Format("{0}=@{0} AND ", arg));
                dictionary.Add($"@{arg}", IDs[i]);
            }
            StringBuilder stringBuilder2 = stringBuilder;
            stringBuilder2.Remove(stringBuilder2.Length - 4, 4);
            Result result = Tools.ExecuteNonQuery(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
            if (result.State == ResultState.Success)
            {
                CacheContext.Refresh();
            }
            return result;
        }

        public virtual Result DeleteWithField(string fieldName, object fieldValue)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("DELETE FROM ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName} WHERE ");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            stringBuilder.Append(string.Format("{0}=@{0}", fieldName));
            dictionary.Add(string.Format("@{0}", Table.CompositeKeys), fieldValue);
            Result result = Tools.ExecuteNonQuery(stringBuilder.ToString(), dictionary, CommandType.Text, Table.ConnectionName);
            if (result.State == ResultState.Success)
            {
                CacheContext.Refresh();
            }
            return result;
        }

        private Result<List<T>> ExecuteSelect(string query, Dictionary<string, object> parameters, string connectionName, SelectType type)
        {
            List<T> items = CacheContext.GetItems(query, parameters, type);
            if (items != null)
            {
                return new Result<List<T>>
                {
                    Data = items,
                    State = ResultState.Success
                };
            }
            CommandType commandType = CommandType.Text;
            switch (type)
            {
                case SelectType.StoredProcedure:
                    commandType = CommandType.StoredProcedure;
                    break;
                case SelectType.Text:
                    commandType = CommandType.Text;
                    break;
                case SelectType.TableDirect:
                    commandType = CommandType.TableDirect;
                    break;
                case SelectType.Where:
                    commandType = CommandType.Text;
                    break;
            }
            Result<List<T>> result = Tools.Select<T>(query, parameters, connectionName, commandType);
            if (result.State == ResultState.Success)
            {
                CacheContext.InsertItems(result.Data, query, parameters, type);
            }
            return result;
        }

        public virtual Result<List<T>> Select()
        {
            string query = CreateSelect<T>();
            Result<List<T>> result = ExecuteSelect(query, null, Table.ConnectionName, SelectType.Text);
            return new Result<List<T>>
            {
                Data = result.Data,
                Message = result.Message,
                State = result.State
            };
        }

        public virtual T Select(object value)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("@" + Table.PrimaryKey, value);
            Result<List<T>> result = ExecuteSelect(CreateSelect<T>(string.Format("WHERE {0}=@{0}", Table.PrimaryKey)), dictionary, Table.ConnectionName, SelectType.Where);
            if (result.Data == null)
            {
                return null;
            }
            return result.Data.FirstOrDefault();
        }

        public virtual Result<List<T>> Select(string query, Dictionary<string, object> p, SelectType type)
        {
            string query2 = "";
            switch (type)
            {
                case SelectType.StoredProcedure:
                    query2 = query;
                    break;
                case SelectType.Text:
                    query2 = query;
                    break;
                case SelectType.TableDirect:
                    query2 = query;
                    break;
                case SelectType.Where:
                    query2 = CreateSelect<T>(query);
                    break;
            }
            return ExecuteSelect(query2, p, Table.ConnectionName, type);
        }

        public virtual PagedResult<List<T>> Select(string commandText = "", Dictionary<string, object> parameters = null, SelectType type = SelectType.Text, int page = 1, int count = 999999)
        {
            string query;
            switch (type)
            {
                case SelectType.StoredProcedure:
                    query = commandText;
                    break;
                case SelectType.Text:
                    query = commandText;
                    break;
                case SelectType.TableDirect:
                    query = commandText;
                    break;
                case SelectType.Where:
                    query = CreateSelect<T>(commandText);
                    break;
                default:
                    query = commandText;
                    break;
            }
            Result<List<T>> result = ExecuteSelect(query, parameters, Table.ConnectionName, type);
            string text = commandText.ToLower().Replace("ı", "I");
            if (text.Contains("order by"))
            {
                text = ((text.IndexOf("order by") != 0) ? text.Substring(0, text.IndexOf("order by")).Replace('ı', 'i') : "");
            }
            int totalRowCount = RowCount(text, parameters);
            if (result.Data != null)
            {
                return new PagedResult<List<T>>
                {
                    Data = result.Data.ToList().Skip(count * (page - 1)).Take(count)
                        .ToList(),
                    Message = result.Message,
                    State = result.State,
                    TotalRowCount = totalRowCount,
                    PageSize = count,
                    ActivePage = page
                };
            }
            return new PagedResult<List<T>>
            {
                Message = result.Message,
                State = result.State
            };
        }

        public virtual PagedResult<List<T>> Select(string commandText, int page, int count, Dictionary<string, object> parameters = null, SelectType type = SelectType.Text)
        {
            return Select(commandText, parameters, type, page, count);
        }

        public virtual PagedResult<List<T>> Select(long primaryId, int typeId, int operationID = 0)
        {
            return Select(primaryId, typeId, 1, 999999, operationID);
        }

        public virtual PagedResult<List<T>> Select(long primaryId, int typeId, int page, int count, int operationID = 0)
        {
            Dictionary<string, object> dictionary = primaryId.CreateParameters("@pid");
            dictionary.Add("@type", typeId);
            dictionary.Add("@operation", operationID);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("WHERE BEID in (Select SecondaryBEID from BusinessEntityRelations Where PrimaryBEID=@pid and TypeID=@type AND OperationID=@operation )");
            return Select(stringBuilder.ToString(), page, count, dictionary, SelectType.Where);
        }

        public virtual int RowCount(string commandText = "", Dictionary<string, object> parameters = null)
        {
            Result result = Tools.ExecuteScalar($"SELECT Count(*) FROM {Table.SchemaName}.{Table.TableName} {commandText}", parameters);
            if (result.State == ResultState.Success)
            {
                return Convert.ToInt32(result.Data);
            }
            return 0;
        }

        public int GetRowCount(string commandText, Dictionary<string, object> parameters)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Select Count(*) from ");
            stringBuilder.Append($"{Table.SchemaName}.{Table.TableName}");
            stringBuilder.Append(" ");
            stringBuilder.Append(commandText);
            return Convert.ToInt32(Tools.ExecuteScalar(stringBuilder.ToString(), parameters));
        }

        public virtual Result<List<T>> Search(object text)
        {
            if (text is string)
            {
                text = ((string)text).Replace(' ', '%').Replace(',', '%');
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("WHERE ");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            string[] searchFields = Table.SearchFields;
            foreach (string text2 in searchFields)
            {
                stringBuilder.Append(string.Format("{0} like '%'+@{0}+'%'", text2));
                dictionary.Add("@" + text2, text);
                stringBuilder.Append(" OR ");
            }
            if (stringBuilder.Length > 6)
            {
                StringBuilder stringBuilder2 = stringBuilder;
                stringBuilder2.Remove(stringBuilder2.Length - 3, 3);
            }
            else
            {
                stringBuilder.Remove(0, stringBuilder.Length);
            }
            return Select(stringBuilder.ToString(), dictionary, SelectType.Where);
        }

        public virtual PagedResult<List<T>> Search(object text, int count, int page)
        {
            if (text is string)
            {
                text = ((string)text).Replace(' ', '%').Replace(',', '%');
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("WHERE ");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            string[] searchFields = Table.SearchFields;
            foreach (string text2 in searchFields)
            {
                stringBuilder.Append(string.Format("{0} like '%'+@{0}+'%'", text2));
                dictionary.Add("@" + text2, text);
                stringBuilder.Append(" OR ");
            }
            if (stringBuilder.Length > 6)
            {
                StringBuilder stringBuilder2 = stringBuilder;
                stringBuilder2.Remove(stringBuilder2.Length - 3, 3);
            }
            else
            {
                stringBuilder.Remove(0, stringBuilder.Length);
            }
            return Select(stringBuilder.ToString(), page, count, dictionary, SelectType.Where);
        }

        public virtual Result<List<K>> View<K>()
        {
            return Tools.Select<K>(CreateSelect<K>());
        }
    }

}
