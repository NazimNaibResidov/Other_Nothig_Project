using CMS.Enums;
using System;


namespace CMS.Attrubite
{
    public class Table : Attribute
    {
        public string SchemaName
        {
            get;
            set;
        }

        public string TableName
        {
            get;
            set;
        }

        public string PrimaryKey
        {
            get;
            set;
        }

        public string IdentityColumn
        {
            get;
            set;
        }

        public TableType TableType
        {
            get;
            set;
        }

        public string[] CompositeKeys
        {
            get;
            set;
        }

        public string[] SearchFields
        {
            get;
            set;
        }

        public string ConnectionName
        {
            get;
            set;
        }

        public CacheType CacheState
        {
            get;
            set;
        }

        public bool IsActiveValid
        {
            get;
            set;
        }

        public int BETypeID
        {
            get;
            set;
        }

        public Table()
        {
            SchemaName = "dbo";
            TableName = "sys.Tables";
            PrimaryKey = "Id";
            IdentityColumn = "Id";
            TableType = TableType.PrimaryTable;
            ConnectionName = "DataBase";
            SearchFields = new string[1]
            {
            "Name"
            };
            CacheState = CacheType.NoCache;
        }
    }
}
