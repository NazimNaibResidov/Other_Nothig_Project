using CMS.Attrubite;
using CMS.Core;
using CMS.Enums;
using CMS.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CMS.Helpers
{

    public static class Tools
    {
        private static CacheCollection<object> _generalCacheContext;

        private static Dictionary<string, SqlConnection> _connections;

        public static CacheCollection<object> GeneralCacheContext
        {
            get
            {
                HttpContext current = HttpContext.Current;
                if (current != null)
                {
                    string name = "_GeneralDataCache_";
                    current.Application[name] = (current.Application[name] ?? new CacheCollection<object>());
                    return (CacheCollection<object>)current.Application[name];
                }
                _generalCacheContext = (_generalCacheContext ?? new CacheCollection<object>());
                return _generalCacheContext;
            }
        }

        public static SqlConnection Connection
        {
            get
            {
                if (!Connections.ContainsKey("DataBase"))
                {
                    Connections["DataBase"] = new SqlConnection(ConfigurationManager.ConnectionStrings["DataBase"].ConnectionString);
                }
                return Connections["DataBase"];
            }
            set
            {
                Connections["DataBase"] = value;
            }
        }

        public static Dictionary<string, SqlConnection> Connections
        {
            get
            {
                _connections = (_connections ?? new Dictionary<string, SqlConnection>());
                return _connections;
            }
            set
            {
                _connections = value;
            }
        }

        public static Table GetTable<T>()
        {
            return (Table)Attribute.GetCustomAttribute(typeof(T), typeof(Table));
        }

        public static string CreateSelect<T>(string extension = "", string columnExtension = "")
        {
            Table table = (Table)Attribute.GetCustomAttribute(typeof(T), typeof(Table));
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("SELECT ");
            stringBuilder.Append(columnExtension);
            stringBuilder.Append(" * FROM ");
            stringBuilder.Append($"{table.SchemaName}.{table.TableName}");
            stringBuilder.Append(" ");
            stringBuilder.Append(extension);
            return stringBuilder.ToString();
        }

        static Tools()
        {
            Connections["DataBase"] = new SqlConnection(ConfigurationManager.ConnectionStrings["DataBase"].ConnectionString);
        }

        public static Result<DataTable> Select(string query, Dictionary<string, object> parameters = null, string connectionName = "DataBase", CommandType commandType = CommandType.Text)
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(query, Connection);
                sqlDataAdapter.SelectCommand.CommandType = commandType;
                while (Connection.State != 0)
                {
                }
                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        sqlDataAdapter.SelectCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }
                }
                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                return new Result<DataTable>
                {
                    Data = dataTable,
                    Message = "Kayıt Listeleme Başarılı",
                    State = ResultState.Success
                };
            }
            catch (Exception ex)
            {
                return new Result<DataTable>
                {
                    Data = null,
                    Message = "Kayıt Listeleme sırasında hata oluştu. " + ex.Message,
                    State = ResultState.Exception
                };
            }
        }

        public static Result<List<T>> Select<T>(string query, Dictionary<string, object> parameters = null, string connectionName = "DataBase", CommandType commandType = CommandType.Text)
        {
            Result<DataTable> result = Select(query, parameters, connectionName, commandType);
            return new Result<List<T>>
            {
                Data = result.Data.ToList<T>(),
                Message = result.Message,
                State = result.State,
                ExDetail = result.ExDetail
            };
        }

        public static Result ExecuteNonQuery(string query, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text, string connectionName = "DataBase")
        {
            SqlCommand sqlCommand = new SqlCommand(query, Connections[connectionName]);
            sqlCommand.CommandType = commandType;
            try
            {
                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        sqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }
                }
                if (sqlCommand.Connection.State != ConnectionState.Open)
                {
                    sqlCommand.Connection.Open();
                }
                int num = sqlCommand.ExecuteNonQuery();
                if (num > 0)
                {
                    return new Result
                    {
                        Data = num,
                        State = ResultState.Success
                    };
                }
                return new Result
                {
                    Data = 0,
                    State = ResultState.Exception
                };
            }
            catch (Exception ex)
            {
                return new Result
                {
                    Data = 0,
                    State = ResultState.Exception,
                    Message = ex.Message
                };
            }
            finally
            {
                if (sqlCommand.Connection.State != 0)
                {
                    sqlCommand.Connection.Close();
                }
            }
        }

        public static Result ExecuteScalar(string query, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text, string connectionName = "DataBase")
        {
            SqlCommand sqlCommand = new SqlCommand(query, Connections[connectionName]);
            sqlCommand.CommandType = commandType;
            try
            {
                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        sqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }
                }
                if (sqlCommand.Connection.State != ConnectionState.Open)
                {
                    sqlCommand.Connection.Open();
                }
                object obj = sqlCommand.ExecuteScalar();
                if (query.Contains("Scope_Identity()"))
                {
                    return new Result
                    {
                        Data = obj,
                        State = ResultState.Success
                    };
                }
                if (!(obj is DBNull) && obj != null)
                {
                    return new Result
                    {
                        Data = obj,
                        State = ResultState.Success
                    };
                }
                return new Result
                {
                    Data = 0,
                    State = ResultState.Exception
                };
            }
            catch (Exception ex)
            {
                return new Result
                {
                    Data = 0,
                    State = ResultState.Exception,
                    Message = ex.Message
                };
            }
            finally
            {
                if (sqlCommand.Connection.State != 0)
                {
                    sqlCommand.Connection.Close();
                }
            }
        }

        public static Result<List<T>> View<T>()
        {
            Result<DataTable> result = Select(CreateSelect<T>(), null, GetTable<T>().ConnectionName);
            return new Result<List<T>>
            {
                Data = ((result.State == ResultState.Success) ? result.Data.ToList<T>() : null),
                Message = result.Message,
                State = result.State
            };
        }

        public static Result<List<T>> View<T>(object id)
        {
            Table table = GetTable<T>();
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("@" + table.PrimaryKey, id);
            Result<DataTable> result = Select(string.Format("WHERE {0}=@{0}", table.PrimaryKey), dictionary, table.ConnectionName);
            return new Result<List<T>>
            {
                Data = ((result.State == ResultState.Success) ? result.Data.ToList<T>() : null),
                Message = result.Message,
                State = result.State
            };
        }

        public static Result<List<T>> View<T>(string query, Dictionary<string, object> p)
        {
            Table table = GetTable<T>();
            Result<DataTable> result = Select(CreateSelect<T>(query), p, table.ConnectionName);
            return new Result<List<T>>
            {
                Data = ((result.State == ResultState.Success) ? result.Data.ToList<T>() : null),
                Message = result.Message,
                State = result.State
            };
        }
    }
}
