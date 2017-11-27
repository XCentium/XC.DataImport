using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Attributes;
using XC.Foundation.DataImport.Repositories.DataSources;

namespace XC.Foundation.DataImport.Models.DataSources
{
    [DataSourceName(Name = "FolderDataSource", DataSourceType =typeof(FolderDataSource))]
    public class FolderDataSourceModel : DataSourceModel
    {
        public string FolderPath { get; set; }
        public string FilePattern { get; set; }
        public string FileType { get; set; }
    }
}