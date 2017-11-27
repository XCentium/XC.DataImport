using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Attributes;
using XC.Foundation.DataImport.Repositories.DataSources;

namespace XC.Foundation.DataImport.Models.DataSources
{
    [DataSourceName(Name = "WebDataSource", DataSourceType = typeof(WebDataSource))]
    public class WebDataSourceModel : DataSourceModel
    {
        public string Url { get; set; }
    }
}