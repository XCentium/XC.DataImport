using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using XC.Foundation.DataImport.Attributes;

namespace XC.Foundation.DataImport.Models.DataSources
{
    [DataSourceName(Name = "SqlDataSource", DataSourceType = typeof(SqlDataSource))]
    public class SqlDataSourceModel : DataSourceModel
    {
        public string ConnectionStringName { get; set; }
        public string SqlStatement { get; set; }

    }
}