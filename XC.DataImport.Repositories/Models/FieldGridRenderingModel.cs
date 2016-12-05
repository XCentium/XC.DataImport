using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Speak.Components.Models.Containers.Forms.FormPanels;

namespace XC.DataImport.Repositories.Models
{
    public class FieldGridRenderingModel : FormRenderingModel
    {
        public override void Initialize(Sitecore.Mvc.Presentation.Rendering rendering)
        {
            base.Initialize(rendering);
            Requires.Clear();
            Requires.Script("client", "Applications/XCDataImport/FieldGrid/FieldGridView.js");
        }
        public string Text
        {
            get
            {
                return GetString("Text", "");
            }
        }
    }
}
