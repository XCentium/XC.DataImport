using Sitecore.Speak.Components.Models.Forms.FormPanels;

namespace XC.Foundation.DataImport.Models
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
