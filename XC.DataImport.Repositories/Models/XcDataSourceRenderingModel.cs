using Sitecore.Speak.Components.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.DataImport.Repositories.Models
{
    public class XcDataSourceRenderingModel : ComponentRenderingModel
    {
        public override void Initialize(Sitecore.Mvc.Presentation.Rendering rendering)
        {
            base.Initialize(rendering);
            Properties["HasNoData"] = true;
            this.Requires.Clear();
            this.Requires.Script("client", "Applications/XCDataImport/DatabaseDatasource/DatabaseDatasource.js");
        }
    }
}
