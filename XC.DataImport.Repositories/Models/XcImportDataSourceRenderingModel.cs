using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Speak.Components.Models;

namespace XC.DataImport.Repositories.Models
{
    public class XcImportDataSourceRenderingModel : ComponentRenderingModel
    {
        public override void Initialize(Sitecore.Mvc.Presentation.Rendering rendering)
        {
            base.Initialize(rendering);
            Properties["HasNoData"] = true;
            Requires.Clear();
            Requires.Script("client", "Applications/XCDataImport/DatabaseDatasource/AsyncDatasource.js");
        }

        public string StartMethod
        {
            get
            {
                return GetString("StartMethod", "");
            }
        }

        public string StatusCheckMethod
        {
            get
            {
                return GetString("StatusCheckMethod", "");
            }
        }
    }
}
