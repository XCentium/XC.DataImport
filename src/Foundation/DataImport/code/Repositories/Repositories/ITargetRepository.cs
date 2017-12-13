using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace XC.Foundation.DataImport.Models.DataSources
{
    public interface ITargetRepository
    {
        //Database Database { get; }
        IDataSourceModel Target { get; }
        Database Database { get; }

        Item ImportItem(ID itemId, Dictionary<ID, object> values, int index, Action<string, string> statusMethod, string statusFilepath);
    }
}
