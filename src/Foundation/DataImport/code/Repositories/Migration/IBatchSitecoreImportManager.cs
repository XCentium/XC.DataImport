using Sitecore.Data.Items;
using System;
namespace XC.DataImport.Repositories.Migration
{
    public interface IBatchSitecoreImportManager : ISitecoreImportManagerBase
    {
        Tuple<int, string> GetStatus(string id);
        XC.Foundation.DataImport.Models.IBatchMappingModel Mapping { get; set; }
    }
}
