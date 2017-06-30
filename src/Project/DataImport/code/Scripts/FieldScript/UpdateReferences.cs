﻿using HtmlAgilityPack;
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
        private readonly ID StringSettingTemplateId = ID.Parse("{5EECF4A9-2D1F-44D1-AE33-0B7EE1230055}");

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
                        var anchorValue = att.Value;

                        var linkId = Regex.Match(att.Value, @"\d+").Value;
                        var linkAnchor = Regex.Match(att.Value, @"#.*").Value;
                        var match = FindItem(args.Database, linkId);
                        if (match == null)
                            continue;

                        var sitecoreString = "";
                        if (match.TemplateID == StringSettingTemplateId)
                        {
                            sitecoreString = match["Value"];
                        }
                        else
                        {
                            var formattedId = match.ID.ToShortID().ToString();
                            sitecoreString = string.Format("~/link.aspx?_id={0}&amp;_z=z", formattedId);
                        }
                        if (!string.IsNullOrEmpty(sitecoreString))
                        {
                            link.SetAttributeValue("href", sitecoreString + linkAnchor);
                            Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences Updating link to {0} ##################", sitecoreString), this);
                        }
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
                        var formattedId = match.ID.ToShortID().ToString();
                        var sitecoreString = string.Format("-/media/{0}.ashx", formattedId);
                        link.SetAttributeValue("src", sitecoreString);
                        Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences Updating media src to {0} ##################", sitecoreString), this);
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
            Sitecore.Diagnostics.Log.Info("#################Field Processing UpdateReferences ended ##################", this);

        }

        private Item FindItem(Database database, string linkId)
        {
            Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences FindItem {0} ##################", linkId), this);
            return database.SelectSingleItem(string.Format("fast://sitecore//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(Foundation.DataImport.Templates.ImportedItem.Fields.OriginObjectId), linkId));
        }
    }
}