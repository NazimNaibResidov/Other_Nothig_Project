using System.Collections.Generic;
using System.Data;

namespace CMS.Core
{
    public class Cache<T>
    {
        private Dictionary<string, object> parameters;

        public List<T> Data
        {
            get;
            set;
        }

        public Dictionary<string, object> Parameters
        {
            get
            {
                return parameters;
            }
            set
            {
                parameters = value;
                ParamString = GetParamString(value);
            }
        }

        public string Query
        {
            get;
            set;
        }

        public string ParamString
        {
            get;
            set;
        }

        public string Key => GetKey(Query, ParamString);

        public static string GetParamString(Dictionary<string, object> prms)
        {
            string text = "";
            if (prms != null)
            {
                foreach (KeyValuePair<string, object> prm in prms)
                {
                    if (prm.Value is DataTable)
                    {
                        DataTable dataTable = (DataTable)prm.Value;
                        foreach (DataRow row in dataTable.Rows)
                        {
                            foreach (DataColumn column in dataTable.Columns)
                            {
                                text += string.Format("{2}.{0}={1}", column.ColumnName, row[column.ColumnName], prm.Key);
                            }
                        }
                    }
                    else
                    {
                        text += $"{prm.Key}={prm.Value},";
                    }
                }
                return text;
            }
            return text;
        }

        public static string GetKey(string Query, string ParamString)
        {
            return $"{Query};{ParamString}";
        }
    }
}
