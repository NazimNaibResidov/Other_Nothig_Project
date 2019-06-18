using CMS.Core;
using CMS.Enums;
using CMS.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Delegate
{
    public delegate T GetEntityHandler<T>(object id);
    public delegate K ConvertHandler<K, L>(L entity);
    public delegate Result<List<T>> SelectHandler<T>(string where, Dictionary<string, object> prm, SelectType selectType);
    public delegate List<T> SelectListHandler<T>(int id);
    public delegate Result<List<T>> SelectRelatedBusinessEntity<T>(long primaryId, int typeID, int operation);
    public class Helpers
    {
    }
}
