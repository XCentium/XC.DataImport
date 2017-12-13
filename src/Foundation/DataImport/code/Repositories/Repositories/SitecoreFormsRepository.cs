using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data;
using Sitecore.Data.Items;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Models.Mappings;

namespace XC.Foundation.DataImport.Repositories.Repositories
{
    public class SitecoreFormsRepository : BaseTargetRepository, ITargetRepository
    {
        public SitecoreFormsRepository(ImportMappingModel mapping) : base(mapping)
        {
        }
        public Database Database => throw new NotImplementedException();

        public Item ImportItem(ID itemId, Dictionary<ID, object> values, int index, Action<string, string> statusMethod, string statusFilepath)
        {
            throw new NotImplementedException();
        }
    }
}