using HtmlAgilityPack;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;
using XC.Foundation.DataImport.Pipelines.PostProcessing;
using XC.Foundation.DataImport.Utilities;
using static Sitecore.Configuration.Settings;

namespace XC.Project.DataImport.Scripts.FieldScript
{
    public class UpdateReferences
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing UpdateReferences started ##################");

            if (args.SourceValue == null)
                DataImportLogger.Log.Info("UpdateReferences Field Processing: no SourceValue");

            if (!string.IsNullOrEmpty((string)args.SourceValue))
            {
                var htmldoc = new HtmlDocument();
                htmldoc.LoadHtml((string)args.SourceValue);

                if (htmldoc.DocumentNode.SelectNodes("//a[@href]") != null)
                {
                    foreach (HtmlNode link in htmldoc.DocumentNode.SelectNodes("//a[@href]"))
                    {
                        HtmlAttribute att = link.Attributes["href"];
                        if (att == null)
                            continue;
                        var linkId = Regex.Match(att.Value, @"\d+").Value;
                        var match = FindItem(args.Database, linkId);
                        if (match == null)
                            continue;
                        var formattedId = match.ID.ToString().Replace("-", "").Replace("{", "").Replace("}", "");
                        var sitecoreString = String.Format("~/link.aspx?_id={0}&amp;_z=z", formattedId);
                        link.SetAttributeValue("href", sitecoreString);
                    }
                }
                if (htmldoc.DocumentNode.SelectNodes("//img[@src]") != null)
                {
                    foreach (HtmlNode link in htmldoc.DocumentNode.SelectNodes("//img[@src]"))
                    {
                        HtmlAttribute att = link.Attributes["src"];
                        if (att == null)
                            continue;

                        var linkId = Regex.Match(att.Value, @"\d+").Value;
                        var match = FindItem(args.Database, linkId);
                        if (match == null)
                            continue;
                        if (!match.Paths.IsMediaItem)
                            continue;
                        //var formattedId = match.ID.ToString().Replace("-", "").Replace("{", "").Replace("}", "");
                        var formattedId = match.ID.ToShortID().ToString();
                        var sitecoreString = String.Format("-/media/{0}.ashx", formattedId);
                        link.SetAttributeValue("src", sitecoreString);
                    }
                }

                string result = null;
                using (StringWriter writer = new StringWriter())
                {
                    htmldoc.Save(writer);
                    result = writer.ToString();
                }

                args.Result = result;
            }

            DataImportLogger.Log.Info("#################Field Processing UpdateReferences ended ##################");
        }

        private Item FindItem(Database database, string linkId)
        {
            return database.SelectSingleItem(string.Format("fast://sitecore//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(Foundation.DataImport.Templates.ImportedItem.Fields.OriginObjectId), linkId));
        }
    }
}