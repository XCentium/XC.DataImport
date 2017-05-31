using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Mvc.Presentation;
using Sitecore.Web.UI.Controls.Common.ListControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using Sitecore.StringExtensions;

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

        public override string RenderRow()
        {
            Item dataSource = this.GetDataSource();
            if (dataSource == null)
                return string.Empty;
            StringWriter stringWriter = new StringWriter();
            HtmlTextWriter output = new HtmlTextWriter((TextWriter)stringWriter);
            foreach (Item child in dataSource.Children)
            {
                string str1 = string.IsNullOrEmpty(child["ContentAlignment"]) ? string.Empty : " sc-text-align-" + ClientHost.Items.GetItem(child["ContentAlignment"]).Name.ToLower();
                output.AddAttribute(HtmlTextWriterAttribute.Class, "ventilate" + str1);
                output.AddAttribute("data-sc-important", "data-sc-important");
                string str2 = string.Empty;
                var innerHtml = string.Empty;
                if (!string.IsNullOrEmpty(child["DataField"]) && !string.IsNullOrEmpty(child["Tag"]))
                {
                    var tagType = string.Empty;
                    var options = string.Empty;
                    if (!string.IsNullOrEmpty(child["Tag Type"]))
                    {
                        tagType = " type=\"" + child["Tag Type"] + "\"";
                        switch (child["Tag Type"])
                        {
                            case "checkbox":
                                options += ", checked: " + child["DataField"];
                                break;
                            default:
                                options += ", value: " + child["DataField"];
                                break;
                        }
                    }
                    if(!string.IsNullOrEmpty(child["Options Property"]))
                    {
                        options += ", options: " + child["Options Property"] + ", optionsText: \"" + child["DisplayFieldName"] + "\", optionsValue: \"" + child["ValueFieldName"] + "\"";
                        options += ", value: " + child["DataField"];
                    }
                    innerHtml = "<" + child["Tag"] + tagType + " data-bind='attr: { id: formatValue(\"\",\"" + child["DataField"] + "_{{Id}}\") } " + options + " '></" + child["Tag"] + ">";
                }
                else if (string.IsNullOrEmpty(child["Formatter"]) && string.IsNullOrEmpty(child["HTMLTemplate"]))
                {
                    string str3 = "(typeof $data['" + child["DataField"] + "'] != 'undefined' && $data['" + child["DataField"] + "']() != null)";
                    string str4 = str3 + " ? $data['" + child["DataField"] + "'] : '" + child["EmptyText"] + "'";
                    str2 = string.Format("{0},{1},{2}", ("text: " + str4), ("attr: { title: " + str4 + " }"), ("css: { 'sc-nodata': !" + str3 + "}"));
                }
                else if (string.IsNullOrEmpty(child["DataField"]) && !string.IsNullOrEmpty(child["HTMLTemplate"]))
                {
                    if (!string.IsNullOrEmpty(child["Options Property"]))
                    {
                        str2 = "html: formatValue('', '" + child["HTMLTemplate"] + "'), options: " + child["Options Property"];
                    }
                    else
                    {
                        str2 = "html: formatValue('', '" + child["HTMLTemplate"] + "')";
                    }
                }

                else
                {
                    str2 = string.Format("{0},{1}", (object)("text: formatValue('" + child["DataField"] + "', '" + child["Formatter"] + "')"), (object)("attr: { title:  formatValue('" + child["DataField"] + "', '" + child["Formatter"] + "')}"));
                }
                SetWidthStyle(output, child);
                output.AddAttribute("data-bind", str2);
                output.RenderBeginTag(HtmlTextWriterTag.Td);
                output.Write(innerHtml);
                output.RenderEndTag();
            }
            return stringWriter.ToString();
        }

        private static void SetWidthStyle(HtmlTextWriter output, Item child)
        {
            System.Web.UI.WebControls.Unit unit = ClientHost.Factory.UnitParser.Parse(child["Width"]);
            if (unit.IsEmpty)
                return;
            output.AddAttribute("style", "width:{0};".FormatWith((object)unit));
        }

    }
}