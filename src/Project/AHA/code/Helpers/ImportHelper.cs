﻿using HtmlAgilityPack;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using XC.Foundation.DataImport;
using XC.Foundation.DataImport.Disablers;
using XC.Foundation.DataImport.Exceptions;
using XC.Foundation.DataImport.Repositories.Repositories;
using XC.Foundation.DataImport.Utilities;

namespace Aha.Project.DataImport.Helpers
{
    internal static class ImportHelper
    {
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

        internal static string ImportMediaAndReplaceReferences(string sourceValue, Database database, HttpResponseBase response)
        {
            var result = sourceValue;

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(sourceValue);
            var imagesUpdated = ProcessHtmlFieldForRemoteImageReferences(htmldoc, database, response);

            var updated = imagesUpdated;

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

        private static bool ProcessHtmlFieldForRemoteImageReferences(HtmlDocument htmldoc, Database database, HttpResponseBase response)
        {
            if (htmldoc.DocumentNode.SelectNodes("//img[@src]") == null)
            {
                return false;
            }

            var updated = false;

            foreach (var link in htmldoc.DocumentNode.SelectNodes("//img[@src]"))
            {
                var att = link.Attributes["src"];
                if (att == null || string.IsNullOrEmpty(att.Value))
                {
                    continue;
                }

                var alt = link.Attributes["alt"]?.Value;

                var image = PullinImage(database, att.Value, alt);
                if (string.IsNullOrEmpty(image))
                {
                    response.Write($"<div>Unable to download image src {att.Value} </div>");
                    response.Flush();
                }

                link.SetAttributeValue("src", image);
                att = link.Attributes["src"];

                if (response != null)
                {

                    response.Write($"<div>Updating image to {att.Value} </div");
                    response.Flush();
                }
                updated = true;
            }
            return updated;
        }

        private static string PullinImage(Database database, string srcValue, string altValue)
        {
            try
            {
                var mediaLibraryFolder = Settings.GetSetting("Aha.DataImport.MediaLibraryFolder");
                if (string.IsNullOrEmpty(mediaLibraryFolder))
                {
                    return string.Empty;
                }

                Stream stream;
                using (var client = new WebClient())
                {
                    var imageData = client.DownloadData(srcValue);
                    stream = new MemoryStream(imageData);
                }
                if (stream == null)
                {
                    return string.Empty;
                }

                var filename = new Uri(srcValue).Segments.Last();
                var itemName = ItemUtil.ProposeValidItemName(Path.GetFileNameWithoutExtension(filename));
                var mediaPath = string.Concat(mediaLibraryFolder, "/", itemName);

                var mediaOptions = new MediaCreatorOptions
                {
                    FileBased = false,
                    IncludeExtensionInItemName = false,
                    OverwriteExisting = true,
                    Destination = mediaPath,
                    Database = database
                };

                var creator = new MediaCreator();
                var mediaItem = creator.CreateFromStream(stream, filename, mediaOptions);

                if (mediaItem != null)
                {
                    if (!string.IsNullOrEmpty(altValue))
                    {
                        using (new SecurityDisabler())
                        {
                            using (new EditContext(mediaItem))
                            {
                                mediaItem["Alt"] = altValue;
                            }
                        }
                    }
                    return string.Format("-/media/{0}.ashx", mediaItem.ID.ToShortID());
                }
            }
            catch (Exception ex)
            {
                throw new FieldProcessingException(ex.Message, ex);
            }
            return string.Empty;
        }

        internal static object RemoveLegacyMarkup(string sourceValue, Database database, HttpResponseBase response)
        {
            if (string.IsNullOrEmpty(sourceValue))
            {
                return sourceValue;
            }
            return Regex.Replace(sourceValue, @"\[\!\-\-\$(.*?)\-\-\]", string.Empty, RegexOptions.IgnoreCase);
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

                if (att.Value.Contains("wcmUrl"))
                {
                    var linkId = Regex.Match(att.Value, @"\'ucm_(.*)\'", RegexOptions.IgnoreCase)?.Value?.Replace("'", "");

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
                    if (match.IsDerived(Sitecore.TemplateIDs.UnversionedImage) || match.IsDerived(Sitecore.TemplateIDs.UnversionedFile))
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
                        link.SetAttributeValue("href", sitecoreString);
                        att = link.Attributes["href"];

                        if (response != null)
                        {
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

        private static bool ProcessHtmlFieldForImageReferences(HtmlDocument htmldoc, Database database, HttpResponseBase response)
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

                if (att.Value.Contains("custGetDocRenditionPath"))
                {
                    var linkId = Regex.Match(att.Value, @"\'ucm_(.*)\'", RegexOptions.IgnoreCase)?.Value?.Replace("'", "");
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
                    var attAlt = link.Attributes["alt"];
                    if (attAlt != null)
                    {
                        attAlt.Value = match["Alt"];
                    }

                    var formattedId = match.ID.ToShortID().ToString();
                    var sitecoreString = $"-/media/{formattedId}.ashx";

                    if (response != null)
                    {
                        response.Write($"<div>Field Processing from Media Item {sitecoreString} </div");
                        response.Flush();
                    }
                    updated = true;

                    if (!string.IsNullOrEmpty(sitecoreString))
                    {
                        link.SetAttributeValue("src", sitecoreString);
                        att = link.Attributes["src"];

                        if (response != null)
                        {
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

        private static bool ProcessHtmlFieldForImageRenderedReferences(HtmlDocument htmldoc, Database database, HttpResponseBase response)
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


                var linkId = Regex.Match(att.Value, @"ucm_(\d{6})", RegexOptions.IgnoreCase)?.Value?.Replace("'", "");
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
                var attAlt = link.Attributes["alt"];
                if (attAlt != null)
                {
                    attAlt.Value = match["Alt"];
                }

                var formattedId = match.ID.ToShortID().ToString();
                var sitecoreString = $"-/media/{formattedId}.ashx";

                if (response != null)
                {
                    response.Write($"<div>Field Processing from Media Item {sitecoreString} </div");
                    response.Flush();
                }
                updated = true;

                if (!string.IsNullOrEmpty(sitecoreString))
                {
                    link.SetAttributeValue("src", sitecoreString);
                    att = link.Attributes["src"];

                    if (response != null)
                    {
                        response.Write($"<div>Updating image to {att.Value} </div");
                        response.Flush();
                    }
                    updated = true;
                }
            }
            return updated;
        }

        private static bool ProcessHtmlFieldForRenderedLinks(HtmlDocument htmldoc, Database database, HttpResponseBase response)
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


                var linkId = Regex.Match(att.Value, @"ucm_(\d{6})", RegexOptions.IgnoreCase)?.Value?.Replace("'", "");

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
                if (match.IsDerived(Sitecore.TemplateIDs.UnversionedImage) || match.IsDerived(Sitecore.TemplateIDs.UnversionedFile))
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
                    link.SetAttributeValue("href", sitecoreString);
                    att = link.Attributes["href"];

                    if (response != null)
                    {
                        response.Write($"<div>Updating link to {att.Value} </div");
                        response.Flush();
                    }
                    updated = true;
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
            var linksUpdated = ProcessHtmlFieldForLinks(htmldoc, database, response);
            var imagesUpdated = ProcessHtmlFieldForImageReferences(htmldoc, database, response);

            var updated = linksUpdated
                 || imagesUpdated;

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
        public static string ProcessRenderedHtmlFieldValue(string sourceValue, Database database, HttpResponseBase response)
        {
            var result = sourceValue;

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(sourceValue);
            var linksUpdated = ProcessHtmlFieldForRenderedLinks(htmldoc, database, response);
            var imagesUpdated = ProcessHtmlFieldForImageRenderedReferences(htmldoc, database, response);

            var updated = linksUpdated
                 || imagesUpdated;

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