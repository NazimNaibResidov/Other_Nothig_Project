using CMS.Enums;

namespace CMS.Core
{
    public class ExceptionDetail
    {
        public DBExTypes ExType
        {
            get;
            set;
        }

        public string ColumnName
        {
            get;
            set;
        }

        public string TableName
        {
            get;
            set;
        }
    }
}
