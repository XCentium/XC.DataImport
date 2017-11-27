using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data;
using XC.Foundation.DataImport.Models;

namespace XC.Foundation.DataImport.Repositories.DataSources
{
    public class SitecoreDataSource : IDataSource
    {
        public Dictionary<ID, Dictionary<ID, object>> GetSourceItemsForImport(IEnumerable<string> sourceProcessingScripts, ScFieldMapping[] fieldMappings, Action<string, string> statusMethod, string statusFilepath)
        {
            throw new NotImplementedException();
        }
    }
}