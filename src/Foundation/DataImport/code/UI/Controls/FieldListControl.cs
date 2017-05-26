using Sitecore.Mvc.Presentation;
using Sitecore.Web.UI.Controls.Common.ListControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace XC.Foundation.DataImport.UI.Controls
{
    public class FieldListControl : ListControl
    {
        public FieldListControl(RenderingParametersResolver parametersResolver) : base(parametersResolver)
        {
        }
        public override string RenderMainLayout()
        {
            StringWriter stringWriter = new StringWriter();
            HtmlTextWriter htmlTextWriter = new HtmlTextWriter((TextWriter)stringWriter);
            htmlTextWriter.AddAttribute(HtmlTextWriterAttribute.Class, "sc-table sc-table-header table");
            htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Table);
            htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Thead);
            htmlTextWriter.RenderEndTag();
            htmlTextWriter.RenderEndTag();
            htmlTextWriter.AddAttribute(HtmlTextWriterAttribute.Class, "sc-fieldlistcontrol-body");
            htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Div);
            htmlTextWriter.AddAttribute(HtmlTextWriterAttribute.Class, "sc-table table");
            htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Table);
            htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Tbody);
            htmlTextWriter.RenderEndTag();
            htmlTextWriter.RenderEndTag();
            htmlTextWriter.RenderEndTag();
            htmlTextWriter.AddAttribute(HtmlTextWriterAttribute.Class, "sc-table-footer");
            htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Table);
            htmlTextWriter.RenderEndTag();
            return stringWriter.ToString();
        }
    }
}