using CMS.Attrubite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Data
{
    [Table(TableType =Enums.TableType.PrimaryTable,TableName ="Products",SearchFields =new string [] { "ProductName" })]
   public class product
    {
        public int  ProductID { get; set; }
        public string ProductName { get; set; }
    }
}
