﻿using HtmlAgilityPack;
using Sitecore;
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
using System.Web.Mvc;
using System.Web.Security;
using System.Xml;
using XC.Foundation.DataImport;
using XC.Foundation.DataImport.Utilities;

namespace XC.Project.DataImport.Controllers
{
    public class ImportController : Controller
    {
        private const string MediaReferenceTemplateId = "{170EDED0-DB36-4FC8-98F8-EFF1D6CC65F5}";

        public ActionResult CleanUpMediaReferenceItems(string rootId)
        {
            if (string.IsNullOrEmpty(rootId))
            {
                return Content("rootId is not valid");
            }
            var database = Factory.GetDatabase("master");
            if (database == null)
            {
                return Content("master database was not found");
            }
            var rootItem = database.GetItem(rootId);
            if (rootItem == null)
            {
                return Content("root item was not found");
            }

            try
            {
                var mediaQuery = string.Format("fast:/{0}//*[@@templateid='{1}']", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath), MediaReferenceTemplateId);
                var mediaItems = database.SelectItems(mediaQuery);

                if (mediaItems != null && mediaItems.Any())
                {
                    using (new SecurityDisabler())
                    {
                        foreach (var item in mediaItems)
                        {
                            var contentReference = (ReferenceField)item.Fields["Content Reference"];
                            if (contentReference != null && contentReference.TargetItem != null && contentReference.TargetItem.Paths.IsContentItem)
                            {
                                Response.Write("<div>Item Deleted: " + item.Paths.FullPath + " | " + contentReference.TargetItem.Paths.FullPath +"</div>");
                                Response.Flush();
                                item.Delete();
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Response.Write("<div>ERROR:  " + ex.StackTrace + "</div>");
                Response.Flush();
            }
            return Content("Finished");
        }

        public ActionResult MoveMediaOutOfFolder(string rootId)
        {
            if (string.IsNullOrEmpty(rootId))
            {
                return Content("rootId is not valid");
            }
            var database = Factory.GetDatabase("master");
            if (database == null)
            {
                return Content("master database was not found");
            }
            var rootItem = database.GetItem(rootId);
            if (rootItem == null)
            {
                return Content("root item was not found");
            }

            try
            {
                var mediaQuery = string.Format("fast:/{0}//*[@@templateid!='{1}']", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath), Sitecore.TemplateIDs.MediaFolder);
                var mediaItems = database.SelectItems(mediaQuery);

                if (mediaItems != null && mediaItems.Any())
                {
                    using (new SecurityDisabler())
                    {
                        foreach (var item in mediaItems)
                        {
                            if (item.Parent != null && item.Name == item.Parent.Name && item.Parent.TemplateID == Sitecore.TemplateIDs.MediaFolder)
                            {
                                var oldFolder = item.Parent;
                                item.MoveTo(item.Parent.Parent);
                                oldFolder.Delete();
                                Response.Write("<div>Item Moved: " + item.Paths.FullPath + "</div>");
                                Response.Flush();
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Response.Write("<div>ERROR:  " + ex.StackTrace + "</div>");
                Response.Flush();
            }
            return Content("Finished");
        }

        public ActionResult MoveMediaIntoML(string rootId)
        {
            if (string.IsNullOrEmpty(rootId))
            {
                return Content("rootId is not valid");
            }
            var database = Factory.GetDatabase("master");
            if (database == null)
            {
                return Content("master database was not found");
            }
            var rootItem = database.GetItem(rootId);
            if (rootItem == null)
            {
                return Content("root item was not found");
            }

            try
            {
                var mediaQuery = string.Format("fast:/{0}//*", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath));
                var mediaItems = database.SelectItems(mediaQuery);

                if (mediaItems != null && mediaItems.Any())
                {
                    var templateItem = database.GetItem(MediaReferenceTemplateId);
                    if (templateItem != null)
                    {
                        using (new SecurityDisabler())
                        {
                            foreach (var item in mediaItems.OrderBy(i=>i.Paths.FullPath))
                            {
                                MoveMediaAssetIntoMediaLibrary(item);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Response.Write("<div>ERROR:  " + ex.StackTrace + "</div>");
                Response.Flush();
            }
            return Content("Finished");
        }

        private void MoveMediaAssetIntoMediaLibrary(Item item)
        {
            if (item.Paths.IsContentItem && (item.IsDerived(Sitecore.TemplateIDs.UnversionedImage) || item.IsDerived(Sitecore.TemplateIDs.UnversionedFile)))
            {
                var contentItemPath = item.Paths.FullPath;
                var mediaLibraryPath = item.Paths.FullPath.Replace(Sitecore.Constants.ContentPath, Sitecore.Constants.MediaLibraryPath);
                var mediaLibraryParentItem = item.Database.GetItem(mediaLibraryPath);

                var mediaReferenceItem = CreateMediaReferenceItem(item, item.Parent);                
                CreateChildMediaReferences(item, mediaReferenceItem);                

                if (mediaLibraryParentItem == null)
                {
                    mediaLibraryParentItem = CreateMediaPath(item.Database, mediaLibraryPath);
                }
                if (mediaLibraryParentItem != null)
                {
                    item.MoveTo(mediaLibraryParentItem);
                    Response.Write("<div>Item Moved: " + item.Paths.FullPath + "</div>");
                    Response.Flush();
                }
            }
        }

        private void CreateChildMediaReferences(Item item, Item mediaReferenceItem)
        {
            if (item.HasChildren)
            {
                foreach (Item child in item.Children)
                {
                    var referenceItem = CreateMediaReferenceItem(child, mediaReferenceItem);
                    CreateChildMediaReferences(child, referenceItem);
                }
            }
        }

        //Work in progress - Not Finished
        public ActionResult ReplaceArticleLinks(string rootId)
        {
            if (string.IsNullOrEmpty(rootId))
            {
                return Content("rootId is not valid");
            }
            var database = Factory.GetDatabase("master");
            if (database == null)
            {
                return Content("master database was not found");
            }
            var rootItem = database.GetItem(rootId);
            if (rootItem == null)
            {
                return Content("root item was not found");
            }

            var mediaFolder = database.GetItem("{8263BCDC-D9CB-4EC3-804E-5EB4F3802A6B}");
            var mediaDescendants = mediaFolder.Axes.GetDescendants();

            var homeNode = database.GetItem("{A0A2FCC7-7AE8-4D54-8031-14B8D27D3E15}");
            var homeDescendants = homeNode.Axes.GetDescendants();

            var query = string.Format("fast:{0}//*",
                              FastQueryUtility.EscapeDashes(rootItem.Paths.Path));
            var results = database.SelectItems(query);
            if (results == null)
            {
                return Content("no valid descendants of the root Item were found");
            }

            foreach (var item in results)
            {

                var bodyText = item["Body"];
                if (!string.IsNullOrEmpty(bodyText))
                {
                    var htmldoc = new HtmlDocument();
                    htmldoc.LoadHtml(bodyText);
                    if (htmldoc.DocumentNode.SelectNodes("//a[@href]") != null)
                    {
                        foreach (HtmlNode link in htmldoc.DocumentNode.SelectNodes("//a[@href]"))
                        {
                            HtmlAttribute att = link.Attributes["href"];
                            if (att == null)
                                continue;
                            var linkId = Regex.Match(att.Value, @"\d+").Value;
                            var match = homeDescendants.FirstOrDefault(i => i[Templates.ImportedItem.Fields.OriginObjectId] == linkId);
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
                            var match = mediaDescendants.FirstOrDefault(i => i[Templates.ImportedItem.Fields.OriginObjectId] == linkId);
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

                    using (new SecurityDisabler())
                    {
                        try
                        {
                            using (new EditContext(item, false, true))
                            {
                                item[Templates.ImportedItem.Fields.OriginBodyTextId] = result;
                            }
                        }
                        catch (Exception e)
                        {
                            Sitecore.Diagnostics.Log.Error("Error on Item Edit: " + e.Message, e);
                        }
                    }
                }

            }


            //Begin Single item test code for images
            //var database = Factory.GetDatabase("master");
            //var mediaFolder = database.GetItem("{DB6F3E90-E556-F261-F0F0-8D41692B6C9E}");
            //var descendants = mediaFolder.Axes.GetDescendants();

            //var rootItem = database.GetItem(rootId);

            //var bodyText = rootItem["Body"];
            //var htmldoc = new HtmlDocument();
            //htmldoc.LoadHtml(bodyText);
            //foreach (HtmlNode link in htmldoc.DocumentNode.SelectNodes("//img[@src]"))
            //{
            //  HtmlAttribute att = link.Attributes["src"];
            //  if (att == null)
            //    continue;


            //  var linkId = Regex.Match(att.Value, @"\d+").Value;
            //  var match = descendants.FirstOrDefault(i => i["Object Id"] == linkId);
            //  if (match == null)
            //    continue;
            //  if (!match.Paths.IsMediaItem)
            //    continue;
            //  var formattedId = match.ID.ToString().Replace("-", "").Replace("{", "").Replace("}", "");
            //  var sitecoreString = String.Format("-/media/{0}.ashx", formattedId);
            //  link.SetAttributeValue("src", sitecoreString);

            //}

            //End Single Item test Code



            //string result = null;
            //using (StringWriter writer = new StringWriter())
            //{
            //  htmldoc.Save(writer);
            //  result = writer.ToString();
            //}

            //using (new SecurityDisabler())
            //{
            //  using (new EditContext(rootItem, false, true))
            //  {
            //    rootItem["Body"] = result;
            //  }
            //}


            return Content("Finished");
        }

        public ActionResult MissingObjectIdReport(string path)
        {
            Response.Buffer = true;
            if (string.IsNullOrEmpty(path))
                return Content("missing path");

            var result = new StringBuilder();
            result.AppendLine("<table>");
            result.AppendLine("<tr>");
            result.AppendLine("<th>#</th>");
            result.AppendLine("<th>Link Value</th>");
            result.AppendLine("<th>Object ID</th>");
            result.AppendLine("<th>Link Anchor Value</th>");
            result.AppendLine("</tr>");

            if (System.IO.File.Exists(path))
            {
                var htmldoc = new HtmlDocument();
                htmldoc.Load(path);
                var count = 1;

                if (htmldoc.DocumentNode.SelectNodes("//a[@href]") != null)
                {
                    foreach (HtmlNode link in htmldoc.DocumentNode.SelectNodes("//a[@href]"))
                    {
                        HtmlAttribute att = link.Attributes["href"];
                        if (att == null)
                            continue;

                        var match = Regex.Match(att.Value, @"link\-ref.*'(\d+)'");
                        if (match.Success)
                        {
                            var linkId = Regex.Match(att.Value, @"\d+").Value;
                            var linkAnchor = Regex.Match(att.Value, @"#\d+").Value;

                            result.AppendLine("<tr>");
                            result.AppendFormat("<td>{0}</td>", count);
                            result.AppendFormat("<td>{0}</td>", att.Value);
                            result.AppendFormat("<td>{0}</td>", linkId);
                            result.AppendFormat("<td>{0}</td>", linkAnchor);
                            result.AppendLine("</tr>");
                            count++;
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

                        var match = Regex.Match(att.Value, @"link\-ref.*'(\d+)'");
                        if (match.Success)
                        {
                            var linkId = Regex.Match(att.Value, @"\d+").Value;
                            result.AppendLine("<tr>");
                            result.AppendFormat("<td>{0}</td>", count);
                            result.AppendFormat("<td>{0}</td>", att.Value);
                            result.AppendFormat("<td>{0}</td>", linkId);
                            result.AppendLine("<td></td>");
                            result.AppendLine("</tr>");
                            count++;
                        }
                    }
                }

                result.AppendLine("</table>");
                return Content(result.ToString());
            }
            else
            {
                return Content("file not found");
            }
        }

        // GET: Import
        public ActionResult ImportRoles()
        {
            Response.Buffer = true;
            var filePath = @"C:\Websites\Car-Import\Data\XC.DataImport\usergroups.xml";
            if (System.IO.File.Exists(filePath))
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                var roles = xmlDoc.DocumentElement.ChildNodes;
                if (roles != null)
                {
                    foreach(XmlNode node in roles)
                    {
                        var name = node.FirstChild;
                        if(name != null)
                        {
                            AddRole(name.InnerText);
                        }
                    }
                }
            }
            else
            {
                return Content("file not found");
            }
            return Content("success");
        }

        public void AddRole(string roleName)
        {
            try
            {
                string domainRole = string.Format("car\\{0}", roleName);
                if (!Sitecore.Security.Accounts.Role.Exists(domainRole))
                {
                    Roles.CreateRole(domainRole);
                    Response.Write("<div>Added " + domainRole + "</div>");
                    Response.Flush();
                }
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Error in Client.Project.Security.RoleMaintenance (AddRole): Message: {0}; Source:{1}", ex.Message, ex.Source), this);
            }
        }

        /// <summary>
        /// Creates the media path.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="mediaLibraryPath">The media library path.</param>
        private Item CreateMediaPath(Database database, string mediaLibraryPath)
        {
            if (string.IsNullOrEmpty(mediaLibraryPath))
                return null;
            try
            {
                using (new SecurityDisabler())
                {
                    var parentPath = mediaLibraryPath.Replace(StringUtil.GetLastPart(mediaLibraryPath, '/', ""), "");
                    var parentItem = database.GetItem(parentPath);
                    if (parentItem != null)
                        return parentItem;

                    var templateItem = database.GetItem(Sitecore.TemplateIDs.MediaFolder);
                    return database.CreateItemPath(parentPath, templateItem);
                }
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Post Processing MoveMediaAndReplaceWithReference: Error creating path {0}. {1}", ex.StackTrace, ex.Source), this);
            }
            return null;
        }


        /// <summary>
        /// Creates the media reference item.
        /// </summary>
        /// <param name="item">The item.</param>
        private Item CreateMediaReferenceItem(Item item, Item parentItem)
        {
            if (item == null)
                return null;
            try
            {
                using (new SecurityDisabler())
                {
                    var templateItem = item.Database.GetItem(MediaReferenceTemplateId);
                    if (templateItem == null)
                        return null;

                    var mediaReferenceItem = parentItem.Add(item.Name, new TemplateItem(templateItem));
                    if (mediaReferenceItem == null)
                        return null;

                    using (new EditContext(mediaReferenceItem))
                    {
                        mediaReferenceItem["Content Reference"] = item.ID.ToString();
                    }
                    return mediaReferenceItem;
                }
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Post Processing MoveMediaAndReplaceWithReference: Error creating media reference item {0}. {1}", item.Paths.FullPath, ex.StackTrace), this);
            }
            return null;
        }

    }
}