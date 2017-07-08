using HtmlAgilityPack;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
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
        private readonly ID MediaReferenceTemplateId = ID.Parse("{170EDED0-DB36-4FC8-98F8-EFF1D6CC65F5}");

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
                        if (att.Value.Contains("link-ref"))
                        {
                            var linkId = Regex.Match(att.Value, @"\d+").Value;
                            var linkAnchor = Regex.Match(att.Value, @"#.*").Value;
                            var match = FindItem(args.Database, linkId);
                            if (match == null)
                                continue;
                            if (!match.Paths.IsContentItem)
                                continue;
                            var sitecoreString = "";

                            if (match.TemplateID == StringSettingTemplateId)
                            {
                                var urlValue = match["Value"];
                                if (!string.IsNullOrEmpty(match["Value"]))
                                {
                                    if (urlValue.Contains("www.car.org"))
                                    {
                                        urlValue = new Uri(urlValue).PathAndQuery;
                                    }
                                    sitecoreString = urlValue;
                                    Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences Updating link from StringSettings to {0} ##################", sitecoreString), this);
                                }
                            }
                            else if (match.TemplateID == MediaReferenceTemplateId)
                            {
                                var fld = (ReferenceField)match.Fields["Content Reference"];
                                if (fld != null && fld.TargetItem != null && (fld.TargetItem.IsDerived(Sitecore.TemplateIDs.UnversionedImage) || fld.TargetItem.IsDerived(Sitecore.TemplateIDs.UnversionedFile)))
                                {
                                    sitecoreString = string.Format("-/media/{0}.ashx", fld.TargetItem.ID.ToShortID().ToString());
                                    Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences Updating link from MediaReferenceTemplateId to {0} ##################", sitecoreString), this);
                                }
                            }
                            else
                            {
                                if (match.IsDerived(Sitecore.TemplateIDs.UnversionedImage) || match.IsDerived(Sitecore.TemplateIDs.UnversionedFile))
                                {
                                    var formattedId = match.ID.ToShortID().ToString();
                                    sitecoreString = string.Format("-/media/{0}.ashx", formattedId);
                                    Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences Updating link to media item {0} ##################", sitecoreString), this);
                                }
                                else
                                {
                                    var formattedId = match.ID.ToShortID().ToString();
                                    sitecoreString = string.Format("~/link.aspx?_id={0}&amp;_z=z", formattedId);
                                    Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences Updating link from default to {0} ##################", sitecoreString), this);
                                }
                            }
                            if (!string.IsNullOrEmpty(sitecoreString))
                            {
                                link.SetAttributeValue("href", sitecoreString + linkAnchor);
                                Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences Updating link to {0} ##################", sitecoreString), this);
                            }
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

                        if (att.Value.Contains("link-ref"))
                        {
                            var linkId = Regex.Match(att.Value, @"\d+").Value;
                            var match = FindItem(args.Database, linkId);
                            if (match == null)
                                continue;

                            var sitecoreString = "";
                            if (match.TemplateID == MediaReferenceTemplateId)
                            {
                                var fld = (ReferenceField)match.Fields["Content Reference"];
                                if (fld != null && fld.TargetItem != null)
                                {
                                    sitecoreString = string.Format("-/media/{0}.ashx", fld.TargetItem.ID.ToShortID().ToString());
                                    Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences Updating media src from MediaReferenceTemplateId to {0} ##################", sitecoreString), this);
                                }
                            }
                            else
                            {
                                var formattedId = match.ID.ToShortID().ToString();
                                sitecoreString = string.Format("-/media/{0}.ashx", formattedId);
                                Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences Updating media src from default to {0} ##################", sitecoreString), this);
                            }

                            if (!string.IsNullOrEmpty(sitecoreString))
                            {
                                link.SetAttributeValue("src", sitecoreString);
                                Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences Updating media src to {0} ##################", sitecoreString), this);
                            }
                        }
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
            if (string.IsNullOrEmpty(linkId))
                return null;

            Sitecore.Diagnostics.Log.Info(string.Format("#################Field Processing UpdateReferences FindItem {0} ##################", linkId), this);
            return database.SelectSingleItem(string.Format("fast://sitecore//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(Foundation.DataImport.Templates.ImportedItem.Fields.OriginObjectId), linkId));
        }
    }
}