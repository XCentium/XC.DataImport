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
        public static void UpdateItemReferences(Item item, string fieldName, HttpResponseBase response = null, bool updateItem = true)
        {
            using (new EditContext(item))
            {
                var result = ProcessHtmlFieldValue(item[fieldName], item.Database, response, updateItem);

                if (updateItem && !string.IsNullOrEmpty(result))
                {
                    item[fieldName] = result;

                    if (response != null)
                    {
                        response.Write($"<div>Item {fieldName} field updated ItemId :{item.Paths.FullPath}</div>");
                        response.Flush();
                    }
                }
            }
        }

        /// <summary>
        /// Processes the HTML field value.
        /// </summary>
        /// <param name="sourceValue">The source value.</param>
        /// <param name="database">The database.</param>
        /// <param name="response">The response.</param>
        /// <param name="updateItem">if set to <c>true</c> [update item].</param>
        /// <returns></returns>
        public static string ProcessHtmlFieldValue(string sourceValue, Database database, HttpResponseBase response, bool updateItem)
        {
            var result = sourceValue;

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(sourceValue);

            var updated = false;

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
                        var match = FindItem(database, linkId);
                        if (match == null)
                        {
                            continue;
                        }

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

                                if (response != null)
                                {
                                    response.Write(string.Format("<div>Field Processing UpdateReferences Updating link from StringSettings to {0} </div>", sitecoreString));
                                    response.Flush();
                                }

                                updated = true;
                            }
                        }
                        else if (match.TemplateID == ID.Parse(MediaReferenceTemplateId))
                        {
                            var fld = (ReferenceField)match.Fields["Content Reference"];
                            if (fld != null && fld.TargetItem != null && (fld.TargetItem.IsDerived(Sitecore.TemplateIDs.UnversionedImage) || fld.TargetItem.IsDerived(Sitecore.TemplateIDs.UnversionedFile)))
                            {
                                sitecoreString = string.Format("-/media/{0}.ashx", fld.TargetItem.ID.ToShortID().ToString());
                                if (response != null)
                                {
                                    response.Write(string.Format("<div>Field Processing UpdateReferences Updating link from MediaReferenceTemplateId to {0} </div", sitecoreString));
                                    response.Flush();
                                }
                                updated = true;
                            }
                        }
                        else
                        {
                            if (match.IsDerived(Sitecore.TemplateIDs.UnversionedImage) || match.IsDerived(Sitecore.TemplateIDs.UnversionedFile))
                            {
                                var formattedId = match.ID.ToShortID().ToString();
                                sitecoreString = string.Format("-/media/{0}.ashx", formattedId);

                                if (response != null)
                                {
                                    response.Write(string.Format("<div>UpdateReferences Updating link to media item {0} </div", sitecoreString));
                                    response.Flush();
                                }
                                updated = true;
                            }
                            else
                            {
                                var formattedId = match.ID.ToShortID().ToString();
                                sitecoreString = string.Format("~/link.aspx?_id={0}&amp;_z=z", formattedId);

                                if (response != null)
                                {
                                    response.Write(string.Format("<div>UpdateReferences Updating link from default to {0} </div", sitecoreString));
                                    response.Flush();
                                }
                                updated = true;
                            }
                        }
                        if (!string.IsNullOrEmpty(sitecoreString))
                        {
                            link.SetAttributeValue("href", sitecoreString + linkAnchor);

                            if (response != null)
                            {
                                response.Write(string.Format("<div>UpdateReferences Updating link to {0} </div", sitecoreString));
                                response.Flush();
                            }
                            updated = true;
                        }
                    }
                    else
                    {
                        if (response != null)
                        {
                            response.Write(string.Format("<div>Not Updated: href {0} </div>", att.Value));
                            response.Flush();
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
                        var match = FindItem(database, linkId);
                        if (match == null)
                        {
                            continue;
                        }

                        var sitecoreString = "";
                        if (match.TemplateID == ID.Parse(MediaReferenceTemplateId))
                        {
                            var fld = (ReferenceField)match.Fields["Content Reference"];
                            if (fld != null && fld.TargetItem != null)
                            {
                                sitecoreString = string.Format("-/media/{0}.ashx", fld.TargetItem.ID.ToShortID().ToString());

                                if (response != null)
                                {
                                    response.Write(string.Format("<div>UpdateReferences Updating media src from MediaReferenceTemplateId to {0} </div>", sitecoreString));
                                    response.Flush();
                                }
                                updated = true;
                            }
                        }
                        else
                        {
                            var formattedId = match.ID.ToShortID().ToString();
                            sitecoreString = string.Format("-/media/{0}.ashx", formattedId);

                            if (response != null)
                            {
                                response.Write(string.Format("<div>UpdateReferences Updating link media src from default to {0} </div>", sitecoreString));
                                response.Flush();
                            }
                            updated = true;
                        }

                        if (!string.IsNullOrEmpty(sitecoreString))
                        {
                            link.SetAttributeValue("src", sitecoreString);

                            if (response != null)
                            {
                                response.Write(string.Format("<div>UpdateReferences Updating media src to {0} </div>", sitecoreString));
                                response.Flush();
                            }
                            updated = true;
                        }
                    }
                    else
                    {
                        if (response != null)
                        {
                            response.Write(string.Format("<div>Not Updated: src {0} </div>", att.Value));
                            response.Flush();
                        }
                    }
                }
            }
            if (updated)
            {
                using (StringWriter writer = new StringWriter())
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
                return database.SelectSingleItem(string.Format("fast://sitecore//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(Templates.ImportedItem.Fields.OriginObjectId), objectId));
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
                return database.SelectSingleItem(string.Format("fast://sitecore//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(Templates.ImportedItem.Fields.OriginPath), path));
        }

    }
}