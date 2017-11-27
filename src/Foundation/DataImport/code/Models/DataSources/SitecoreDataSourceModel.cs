using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Attributes;
using XC.Foundation.DataImport.Repositories.DataSources;

namespace XC.Foundation.DataImport.Models.DataSources
{
    [DataSourceName(Name ="SitecoreDataSource", DataSourceType =typeof(SitecoreDataSource))]
    public class SitecoreDataSourceModel : DataSourceModel
    {
        public string DatabaseName { get; set; }
        public string Path { get; set; }
        public string Template { get; set; }
        public string FullPath { get; set; }
    }
}