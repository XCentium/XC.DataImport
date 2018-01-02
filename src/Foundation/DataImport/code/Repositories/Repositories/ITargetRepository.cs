using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data;
using Sitecore.Data.Items;
using XC.Foundation.DataImport.Models.Entities;

namespace XC.Foundation.DataImport.Models.DataSources
{
    public interface ITargetRepository
    {
        IDataSourceModel Target { get; }
        Item ImportItem(ImportDataItem dataItem, int index, Action<string, string> statusMethod, string statusFilepath);
        Database Database { get; }
    }
}
