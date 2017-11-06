using HtmlAgilityPack;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore.Data.Managers;
using XC.Foundation.DataImport;
using XC.Foundation.DataImport.Disablers;
using XC.Foundation.DataImport.Utilities;

namespace XC.Project.DataImport.Helpers
{
    internal static class ImportHelper
    {
        internal const string MediaReferenceTemplateId = "{170EDED0-DB36-4FC8-98F8-EFF1D6CC65F5}";
        internal static ID StringSettingTemplateId = ID.Parse("{5EECF4A9-2D1F-44D1-AE33-0B7EE1230055}");

        /// <summary>
        /// Updates the item references.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="updateItem">Updates the field value on item if set to true.</param>
        public static void UpdateItemReferences(Item item, string fieldName, HttpResponseBase response = null, bool updateItem = true)
        {
            using (new EditContext(item))
            {
                if (response != null)
                {
                    response.Write($"<div><b>Evaluating {fieldName} field for Item :{item.Paths.FullPath}</b></div>");
                    response.Flush();
                }

                var result = ProcessHtmlFieldValue(item[fieldName], item.Database, response);

                if (updateItem && !string.IsNullOrEmpty(result))
                {
                    item[fieldName] = result;

                    if (response != null)
                    {
                        response.Write($"<div>Updated {fieldName} field for Item :{item.Paths.FullPath}</div>");
                        response.Flush();
                    }
                }
            }
        }

        private static bool ProcessHtmlFieldForLinks(HtmlDocument htmldoc, Database database, HttpResponseBase response)
        {
            if (htmldoc.DocumentNode.SelectNodes("//a[@href]") == null)
            {
                return false;
            }

            var updated = false;

            foreach (var link in htmldoc.DocumentNode.SelectNodes("//a[@href]"))
            {
                var att = link.Attributes["href"];
                if (att == null)
                {
                    continue;
                }

                if (att.Value.Contains("link-ref"))
                {
                    var linkId = Regex.Match(att.Value, @"\d+").Value;
                    var linkAnchor = Regex.Match(att.Value, @"#.*").Value;
                    var match = FindItem(database, linkId);
                    if (match == null)
                    {
                        if (response != null)
                        {
                            response.Write($"<div>Unable to match link referenced ID: src {att.Value} </div>");
                            response.Flush();
                        }
                        continue;
                    }

                    if (!match.Paths.IsContentItem && !match.Paths.IsMediaItem)
                    {
                        if (response != null)
                        {
                            response.Write($"<div>Unable to match valid referenced ID: src {att.Value} <br/> match {match.Paths.FullPath} </div>");
                            response.Flush();
                        }
                        continue;
                    }

                    var sitecoreString = "";
                    if (match.IsDerived(StringSettingTemplateId))
                    {
                        var urlValue = match["Value"];
                        if (!string.IsNullOrEmpty(urlValue))
                        {
                            sitecoreString = urlValue;

                            if (response != null)
                            {
                                response.Write($"<div>Field Processing from StringSettings to {sitecoreString} </div>");
                                response.Flush();
                            }

                            if (urlValue.Contains("www.car.org"))
                            {
                                var uri = new Uri(urlValue);
                                if (!string.IsNullOrEmpty(uri.Query))
                                {
                                    //query string present, use external link
                                    continue;
                                }

                                var mappedPath = uri.LocalPath;
                                var mappedMatch = FindItemByPath(database, mappedPath);
                                if (mappedMatch == null)
                                {
                                    //mappedMatch not found, use external link
                                    continue;
                                }

                                var formattedId = mappedMatch.ID.ToShortID().ToString();
                                sitecoreString = $"~/link.aspx?_id={formattedId}&amp;_z=z";

                                if (response != null)
                                {
                                    response.Write($"<div>Field Processing from StringSettings mapped to Content Item {sitecoreString} </div>");
                                    response.Flush();
                                }
                            }

                            updated = true;
                        }
                    }
                    else if (match.IsDerived(Sitecore.TemplateIDs.UnversionedImage) || match.IsDerived(Sitecore.TemplateIDs.UnversionedFile))
                    {
                        var formattedId = match.ID.ToShortID().ToString();
                        sitecoreString = $"-/media/{formattedId}.ashx";

                        if (response != null)
                        {
                            response.Write($"<div>Field Processing from Media Item {sitecoreString} </div");
                            response.Flush();
                        }
                        updated = true;
                    }
                    else
                    {
                        var formattedId = match.ID.ToShortID().ToString();
                        sitecoreString = $"~/link.aspx?_id={formattedId}&amp;_z=z";

                        if (response != null)
                        {
                            response.Write($"<div>Field Processing from Content Item {sitecoreString} </div");
                            response.Flush();
                        }
                        updated = true;
                    }

                    if (!string.IsNullOrEmpty(sitecoreString))
                    {
                        link.SetAttributeValue("href", sitecoreString + linkAnchor);

                        if (response != null)
                        {
                            att = link.Attributes["href"];
                            response.Write($"<div>Updating link to {att.Value} </div");
                            response.Flush();
                        }
                        updated = true;
                    }
                }
                else
                {
                    if (response != null)
                    {
                        response.Write($"<div>Not Updated: href {att.Value} </div>");
                        response.Flush();
                    }
                }
            }

            return updated;
        }

        private static bool ProcesHtmlFieldForImageReferences(HtmlDocument htmldoc, Database database, HttpResponseBase response)
        {
            if (htmldoc.DocumentNode.SelectNodes("//img[@src]") == null)
            {
                return false;
            }

            var updated = false;

            foreach (var link in htmldoc.DocumentNode.SelectNodes("//img[@src]"))
            {
                var att = link.Attributes["src"];
                if (att == null)
                {
                    continue;
                }

                if (att.Value.Contains("link-ref"))
                {
                    var linkId = Regex.Match(att.Value, @"\d+").Value;
                    var match = FindItem(database, linkId);
                    if (match == null)
                    {
                        if (response != null)
                        {
                            response.Write($"<div>Unable to match image referenced ID: src {att.Value} </div>");
                            response.Flush();
                        }
                        continue;
                    }

                    var sitecoreString = "";
                    if (match.IsDerived(ID.Parse(MediaReferenceTemplateId)))
                    {
                        var fld = (ReferenceField)match.Fields["Content Reference"];
                        if (fld != null && fld.TargetItem != null)
                        {
                            sitecoreString = $"-/media/{fld.TargetItem.ID.ToShortID()}.ashx";

                            if (response != null)
                            {
                                response.Write($"<div>Field Processing from Media Reference Item to {sitecoreString} </div");
                                response.Flush();
                            }
                            updated = true;
                        }
                    }
                    else
                    {
                        var formattedId = match.ID.ToShortID().ToString();
                        sitecoreString = $"-/media/{formattedId}.ashx";

                        if (response != null)
                        {
                            response.Write($"<div>Field Processing from Media Item {sitecoreString} </div");
                            response.Flush();
                        }
                        updated = true;
                    }

                    if (!string.IsNullOrEmpty(sitecoreString))
                    {
                        link.SetAttributeValue("src", sitecoreString);

                        if (response != null)
                        {
                            att = link.Attributes["src"];
                            response.Write($"<div>Updating image to {att.Value} </div");
                            response.Flush();
                        }
                        updated = true;
                    }
                }
                else
                {
                    if (response != null)
                    {
                        response.Write($"<div>Not Updated: src {att.Value} </div>");
                        response.Flush();
                    }
                }
            }

            return updated;
        }

        /// <summary>
        /// Processes the HTML field value.
        /// </summary>
        /// <param name="sourceValue">The source value.</param>
        /// <param name="database">The database.</param>
        /// <param name="response">The response.</param>
        /// <returns></returns>
        public static string ProcessHtmlFieldValue(string sourceValue, Database database, HttpResponseBase response)
        {
            var result = sourceValue;

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(sourceValue);

            var updated =
                ProcessHtmlFieldForLinks(htmldoc, database, response) ||
                ProcesHtmlFieldForImageReferences(htmldoc, database, response);
            
            if (updated)
            {
                using (var writer = new StringWriter())
                {
                    htmldoc.Save(writer);
                    result = writer.ToString();                    
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the item.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        public static Item FindItem(Database database, string objectId)
        {
            using (new ItemFilteringDisabler())
                return database.SelectSingleItem(string.Format("fast://sitecore//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(Templates.ImportedItem.Fields.OriginObject_FieldName), objectId));
        }

        /// <summary>
        /// Finds the item by path.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static Item FindItemByPath(Database database, string path)
        {
            using (new ItemFilteringDisabler())
                return database.SelectSingleItem(string.Format("fast://sitecore//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(Templates.ImportedItem.Fields.OriginPath_FieldName), path));
        }

    }
}