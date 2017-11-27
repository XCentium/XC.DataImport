using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Attributes;
using XC.Foundation.DataImport.Repositories.DataSources;

namespace XC.Foundation.DataImport.Models.DataSources
{
    [DataSourceName(Name = "SitecoreQueryDataSource", DataSourceType = typeof(SitecoreQueryDataSource))]
    public class SitecoreQueryDataSourceModel : DataSourceModel
    {
        public string DatabaseName { get; set; }
        public string Query { get; set; }
    }
}