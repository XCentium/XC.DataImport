using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Models.DataSources;

namespace Aha.Project.DataImport.DataSources
{
    public class JotFormsDataSourceModel : IDataSourceModel
    {
        public string ApiKey { get; set; }
        public string FormIds { get; set; }
    }
}