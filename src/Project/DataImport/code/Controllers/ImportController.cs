using HtmlAgilityPack;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml;
using XC.Foundation.DataImport;
using XC.Foundation.DataImport.Disablers;
using XC.Foundation.DataImport.Utilities;
using XC.Project.DataImport.Helpers;
using static XC.Foundation.DataImport.Templates;

namespace XC.Project.DataImport.Controllers
{
    public class ImportController : Controller
    {
        private const string _prefix = "XC.DataImport_";

        public ActionResult VerifyImport()
        {
            Response.Buffer = true;
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["car"].ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    Response.Write("missing car connection string");
                    Response.Flush();
                }

                var masterDb = Factory.GetDatabase("master");

                using (new ItemFilteringDisabler())
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    var commandText = "SELECT * FROM SitecoreData UNION ALL SELECT * FROM SitecoreData2 UNION ALL SELECT * FROM SitecoreData3 ORDER BY ObjectId ";
                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        connection.Open();
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandTimeout = 0;

                        var dataSet = new DataSet();
                        using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                        {
                            dataAdapter.SelectCommand = command;
                            dataAdapter.Fill(dataSet);
                            if (dataSet.Tables != null && dataSet.Tables.Count > 0)
                            {
                                Response.Write("<h1>Missing in Sitecore Objects</h1><table>");
                                Response.Write("<tr>");
                                Response.Write("<th>Object ID</th><th>Parent Object ID</th><th align=\"left\">Path</th><th>Object Type</th><th>t_Object Type</th><th>Blob Data</th><th>Sitecore ID</th>");
                                Response.Write("<tr>");
                                Response.Flush();

                                foreach (DataRow row in dataSet.Tables[0].Rows)
                                {
                                    var forReport = false;
                                    var stringBldr = new StringBuilder();
                                    var objectId = row["ObjectID"] != DBNull.Value ? row["ObjectID"].ToString() : null;
                                    var sitecoreID = StringToID(objectId);
                                    var sitecoreItem = masterDb.GetItem(sitecoreID);
                                    if (sitecoreItem == null)
                                    {
                                        stringBldr.AppendFormat("<td>{0}</td>", objectId);
                                        forReport = true;
                                    }
                                    else
                                    {
                                        stringBldr.Append("<td>-</td>");
                                    }

                                    var parentObjectId = row["ParentObjectID"] != DBNull.Value ? row["ParentObjectID"].ToString() : null;
                                    if (parentObjectId != null)
                                    {
                                        var sitecoreParentID = StringToID(parentObjectId);
                                        var sitecoreParentItem = masterDb.GetItem(sitecoreParentID);
                                        if (sitecoreParentItem == null)
                                        {
                                            stringBldr.AppendFormat("<td>{0}</td>", parentObjectId);
                                        }
                                        else
                                        {
                                            stringBldr.Append("<td>-</td>");
                                        }
                                    }
                                    else
                                    {
                                        stringBldr.Append("<td>-</td>");
                                    }

                                    if (forReport)
                                    {
                                        Response.Write("<tr>");

                                        Response.Write(stringBldr.ToString());

                                        var objectPath = row["Path"] != DBNull.Value ? row["Path"].ToString() : null;
                                        Response.Write(string.Format("<td>{0}</td>", objectPath));
                                        Response.Flush();

                                        var objectType = row["ObjectType_Description"] != DBNull.Value ? row["ObjectType_Description"].ToString() : null;
                                        Response.Write(string.Format("<td>{0}</td>", objectType));
                                        Response.Flush();

                                        var tobjectType = row["t_ObjectType_Description"] != DBNull.Value ? row["t_ObjectType_Description"].ToString() : null;
                                        Response.Write(string.Format("<td>{0}</td>", tobjectType));
                                        Response.Flush();

                                        var blobData = row["BlobData"] != DBNull.Value ? row["BlobData"].ToString() : null;
                                        Response.Write(string.Format("<td>{0}</td>", blobData == null ? "NULL" : ""));
                                        Response.Flush();

                                        Response.Write(string.Format("<td>{0}</td>", sitecoreID));
                                        Response.Flush();

                                        Response.Write("</tr>");
                                    }
                                }

                                Response.Write("</table>");
                                Response.Flush();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.StackTrace);
            }
            return Content("success");
        }

        public ActionResult ArrangeItemsUnderParents(string rootId)
        {
            Response.Buffer = true;
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
                var query = string.Format("fast:/{0}//*", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath));
                var items = database.SelectItems(query);

                if (items != null && items.Any())
                {
                    using (new SecurityDisabler())
                    {
                        foreach (var item in items)
                        {
                            var parentId = item[Templates.ImportedItem.Fields.OriginParentObject_FieldName];
                            if (!string.IsNullOrEmpty(parentId) && parentId != item.Parent[Templates.ImportedItem.Fields.OriginObject_FieldName])
                            {
                                var parentItem = ImportHelper.FindItem(item.Database, parentId);
                                if (parentItem != null)
                                {
                                    item.MoveTo(parentItem);
                                    Response.Write("<div>Item Moved: " + item.Paths.FullPath + " | " + parentItem.Paths.FullPath + "</div>");
                                    Response.Flush();
                                }
                                else
                                {
                                    Response.Write("<div>Item Missing Parent : " + item.Paths.FullPath + " | " + parentId + "</div>");
                                    Response.Flush();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.StackTrace);
            }
            return Content("success");
        }

        public ActionResult RemoveDuplicateMediaReferences(string rootId)
        {
            Response.Buffer = true;
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
                var mediaQuery = string.Format("fast:/{0}//*[@@templateid='{1}']", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath), ImportHelper.MediaReferenceTemplateId);
                var mediaItems = database.SelectItems(mediaQuery);

                if (mediaItems != null && mediaItems.Any())
                {
                    using (new SecurityDisabler())
                    {
                        foreach (var itemGroup in mediaItems.GroupBy(i => i.Name))
                        {
                            if (itemGroup.Count() > 1)
                            {
                                var identicalReference = true;
                                var fieldValue = "";
                                foreach (var item in itemGroup)
                                {
                                    if (fieldValue != item["Content Reference"])
                                        identicalReference = false;

                                    fieldValue = item["Content Reference"];
                                }
                                if (!identicalReference)
                                {
                                    var notImported = itemGroup.Where(i => string.IsNullOrWhiteSpace(i[ImportedItem.Fields.OriginObject_FieldName]));
                                    for (var idx = 1; idx < notImported.Count(); idx++)
                                    {
                                        var item = notImported.ElementAt(idx);
                                        if (item != null)
                                        {
                                            Response.Write("<div>Duplicate Item Deleted: " + item.Paths.FullPath + "</div>");
                                            Response.Flush();
                                            item.Delete();
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var item in itemGroup)
                                    {
                                        Response.Write("<div>Duplicate Items with different references: " + item.Paths.FullPath + " -- " + item["Content Reference"] + "</div>");
                                        Response.Flush();
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.StackTrace);
            }
            return Content("success");
        }

        public ActionResult BrokenReferences(string rootId)
        {
            Response.Buffer = true;
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
                var query = string.Format("fast:/{0}//*", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath));
                var items = database.SelectItems(query);

                if (items != null && items.Any())
                {
                    var fields = new List<string> { "Body", "Short Description" };
                    using (new SecurityDisabler())
                    {
                        Response.Write("<table>");
                        Response.Write("<tr><th>Object Id</th><th>Sitecore Item ID</th><th>Sitecore Item Path</th><th>Sitecore Item Field</th><th>Reference Type</th></tr>");
                        Response.Flush();

                        foreach (var item in items.Where(i => !i.IsDerived(ID.Parse(ImportHelper.MediaReferenceTemplateId))))
                        {
                            Response.Write("<tr>");
                            Response.Flush();

                            foreach (var field in fields)
                            {
                                var htmldoc = new HtmlDocument();
                                htmldoc.LoadHtml(item[field]);

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

                                            var matchItem = ImportHelper.FindItem(item.Database, linkId);
                                            if (matchItem == null)
                                            {
                                                Response.Write(string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>", linkId, item.ID, item.Paths.FullPath, field, "HREF"));
                                                Response.Flush();
                                                continue;
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
                                            var matchItem = ImportHelper.FindItem(item.Database, linkId);
                                            if (ShortID.IsShortID(linkId))
                                                continue;
                                            if (matchItem == null)
                                            {
                                                Response.Write(string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>", linkId, item.ID, item.Paths.FullPath, field, "IMG"));
                                                Response.Flush();
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }

                            Response.Write("</tr>");
                            Response.Flush();
                        }

                        foreach (var item in items.Where(i => i.IsDerived(ID.Parse(ImportHelper.MediaReferenceTemplateId))))
                        {
                            if(string.IsNullOrEmpty(item["Content Reference"]))
                            {
                                Response.Write(string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>", item["Origin Content Reference Object Id"], item.ID, item.Paths.FullPath, "Content Reference", ""));
                                Response.Flush();
                            }
                        }

                        Response.Write("</table>");
                        Response.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.StackTrace);
            }
            return Content("success");
        }

        public ActionResult Generate301RedirectsForIIS(string rootId, bool onlyStatic = false)
        {
            Response.Buffer = true;
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
                var query = string.Format("fast:/{0}//*", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath));
                var items = database.SelectItems(query);

                if (items != null && items.Any())
                {
                    var fileName = "RewriteMaps.config";
                    var redirectFileContent = new StringBuilder();
                    redirectFileContent.AppendLine("<rewriteMaps>");
                    redirectFileContent.AppendLine("<rewriteMap name = \"AlterianRedirects\">");

                    using (new SecurityDisabler())
                    {
                        var linkOptions = LinkManager.GetDefaultUrlOptions();
                        linkOptions.AlwaysIncludeServerUrl = false;
                        linkOptions.LowercaseUrls = true;
                        linkOptions.ShortenUrls = true;
                        linkOptions.SiteResolving = true;
                        linkOptions.Site = SiteContextFactory.GetSiteContext("car");

                        foreach (var item in items)
                        {
                            var originPath = item[Templates.ImportedItem.Fields.OriginPath_FieldName];
                            if (!string.IsNullOrEmpty(originPath))
                            {
                                var newLink = "";
                                if (item.Paths.IsMediaItem)
                                {
                                    newLink = MediaManager.GetMediaUrl(item);
                                }
                                else
                                {
                                    if (item.Paths.FullPath.StartsWith(string.Concat(linkOptions.Site.RootPath, linkOptions.Site.StartItem)))
                                    {
                                        newLink = LinkManager.GetItemUrl(item, linkOptions);
                                    }
                                }
                                if (!string.IsNullOrEmpty(newLink) && newLink.ToLowerInvariant() != originPath.TrimEnd('/').ToLowerInvariant())
                                {
                                    if (!onlyStatic)
                                    {
                                        redirectFileContent.AppendFormat("\t<add key=\"{0}\" value=\"{1}\" />\n", item[Templates.ImportedItem.Fields.OriginPath_FieldName], newLink);
                                        Response.Write(string.Format("<div>{0} | {1} | {2}</div>", newLink.ToLowerInvariant(), originPath.ToLowerInvariant(), newLink.ToLowerInvariant() != originPath.ToLowerInvariant()));
                                        Response.Flush();
                                    }
                                    else if (originPath.Contains("."))
                                    {
                                        redirectFileContent.AppendFormat("\t<add key=\"{0}\" value=\"{1}\" />\n", item[Templates.ImportedItem.Fields.OriginPath_FieldName], newLink);
                                        Response.Write(string.Format("<div>{0} | {1} | {2}</div>", newLink.ToLowerInvariant(), originPath.ToLowerInvariant(), newLink.ToLowerInvariant() != originPath.ToLowerInvariant()));
                                        Response.Flush();
                                    }
                                }
                            }
                        }
                    }
                    redirectFileContent.AppendLine("</rewriteMap>");
                    redirectFileContent.AppendLine("</rewriteMaps>");

                    var mappingFolderName = EnsureMappingFolder("Redirects");
                    if (!string.IsNullOrEmpty(mappingFolderName))
                    {
                        var currentImportFilePath = Path.Combine(mappingFolderName, fileName);
                        System.IO.File.WriteAllText(currentImportFilePath, redirectFileContent.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.StackTrace);
            }
            return Content("success");
        }

        public ActionResult UpdateReferences(string rootId)
        {
            Response.Buffer = true;
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
                var query = string.Format("fast:/{0}//*", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath));
                var items = database.SelectItems(query);

                if (items != null && items.Any())
                {
                    using (new SecurityDisabler())
                    {
                        foreach (var item in items.Where(i => !i.IsDerived(ID.Parse(ImportHelper.MediaReferenceTemplateId)) && i.IsDerived(Templates.ImportedItem.ID)))
                        {
                            Response.Write(string.Format("<h4>UpdateReferences. Item Path {0} </h4>", item.Paths.FullPath));
                            Response.Flush();

                            ImportHelper.UpdateItemReferences(item, Templates.HasPageContent.Fields.Body_FieldName, Response);
                            ImportHelper.UpdateItemReferences(item, Templates.HasPageContent.Fields.Summary_FieldName, Response);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.StackTrace);
            }
            return Content("success");
        }


        private static string EnsureMappingFolder(string mappingName)
        {
            var mappingPath = Path.Combine(Settings.DataFolder, mappingName);
            if (!Directory.Exists(mappingPath))
            {
                Directory.CreateDirectory(mappingPath);
            }
            return mappingPath;
        }

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
                var mediaQuery = string.Format("fast:/{0}//*[@@templateid='{1}']", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath), ImportHelper.MediaReferenceTemplateId);
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
                                Response.Write("<div>Item Deleted: " + item.Paths.FullPath + " | " + contentReference.TargetItem.Paths.FullPath + "</div>");
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
                    var templateItem = database.GetItem(ImportHelper.MediaReferenceTemplateId);
                    if (templateItem != null)
                    {
                        using (new SecurityDisabler())
                        {
                            foreach (var item in mediaItems.OrderBy(i => i.Paths.FullPath))
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
                    foreach (XmlNode node in roles)
                    {
                        var name = node.FirstChild;
                        if (name != null)
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
                    var templateItem = item.Database.GetItem(ImportHelper.MediaReferenceTemplateId);
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
        public ID StringToID(string value)
        {
            Assert.ArgumentNotNull((object)value, "value");
            return new ID(new Guid(MD5.Create().ComputeHash(Encoding.Default.GetBytes(_prefix + value))));
        }

        /// <summary>
        /// Replaces items that were imported with the wrong id with items imported with the correct id and reassigns the children.
        /// </summary>
        /// <param name="rootId">id of the location containing items with correct id</param>
        /// <param name="targetRootId">id of the location contain items with incorrect id</param>
        /// <returns></returns>
        public ActionResult ReplaceItems(string rootId, string targetRootId)
        {
            Response.Buffer = true;
            if (string.IsNullOrEmpty(rootId))
            {
                return Content("rootId is not valid");
            }
            if (string.IsNullOrEmpty(targetRootId))
            {
                return Content("targetRootId is not valid");
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
            var targetRootItem = database.GetItem(targetRootId);
            if (targetRootItem == null)
            {
                return Content("target root item was not found");
            }
            try
            {
                var items = rootItem.Children;

                if (items != null && items.Any())
                {
                    using (new SecurityDisabler())
                    {
                        foreach (var item in items.Where(i => i.IsDerived(Templates.ImportedItem.ID)))
                        {
                            Response.Write($"<h4>UpdateReferencesIssue. Item Path {item.Paths.FullPath} </h4>");
                            Response.Flush();

                            var targetItemPath = $"{targetRootItem.Paths.FullPath}/{item[Templates.ImportedItem.Fields.OriginPath_FieldName]}";
                            var targetItem = database.GetItem(targetItemPath);
                            if (targetItem == null)
                            {
                                Response.Write($"<div>Unable to find item at: {targetItemPath} </div>");
                                Response.Flush();
                                continue;
                            }

                            Response.Write($"<div>Moving chidren of <i>{targetItem.Paths.FullPath}</i> to <i>{item.Paths.FullPath}</i> </div>");
                            Response.Flush();

                            foreach (Item itemChild in targetItem.Children)
                            {
                                itemChild.MoveTo(item);
                            }

                            Response.Write($"<div>Replacing <span>{targetItem.Paths.FullPath}</span> with <span>{item.Paths.FullPath}</span> </div>");
                            Response.Flush();

                            item.MoveTo(targetItem.Parent);
                            targetItem.Delete();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.StackTrace);
            }
            return Content("success");
        }

        /// <summary>
        /// Iterates thru media items and converts items that contain media items into Media Folder items and removes non-media items
        /// </summary>
        /// <param name="rootId">Id of the item to start with</param>
        /// <returns></returns>
        public ActionResult HygineMediaItems(string rootId)
        {
            var ArticleTempalteID = new ID("{94A8C8E9-690B-4E65-98E7-F95800222767}");
            var SectionTempalteID = new ID("{8EE208F9-A6A6-41E2-88A0-C188737A178C}");
            var CommonFolderTemplateID = new ID("{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}");
            var MediaFolderTemplateID = new ID("{FE5DD826-48C6-436D-B87A-7C4210C7413B}");
            
            Response.Buffer = true;
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
                var query = string.Format("fast:/{0}//*", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath));
                var items = database.SelectItems(query);

                if (items != null && items.Any())
                {
                    using (new SecurityDisabler())
                    {
                        foreach (var item in items.Where(i => i.IsDerived(Templates.ImportedItem.ID)))
                        {
                            Response.Write($"<h4>Hygine media item: {item.Paths.FullPath} </h4>");
                            Response.Flush();

                            if (item.IsDerived(ID.Parse(ImportHelper.MediaReferenceTemplateId)) ||
                                item.IsDerived(ArticleTempalteID))
                            {
                                Response.Write($"<div>Deleteing {item.TemplateName} item </div>");
                                Response.Flush();

                                item.Delete();
                            }
                            else if ((item.HasChildren && !item.IsDerived(MediaFolderTemplateID)) || 
                                item.IsDerived(CommonFolderTemplateID) ||
                                item.IsDerived(SectionTempalteID))
                            {
                                Response.Write($"<div>Converting {item.TemplateName} to Media Folder item </div>");
                                Response.Flush();

                                var mediaFolderTemplate = database.GetTemplate(MediaFolderTemplateID);
                                item.ChangeTemplate(mediaFolderTemplate);
                            }
                            else
                            {
                                Response.Write($"<div>No action </div>");
                                Response.Flush();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.StackTrace);
            }
            return Content("success");
        }

        public ActionResult RevertTemplatesToBeforeImport(string rootId)
        {
            Response.Buffer = true;
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
                var query = string.Format("fast:/{0}//*", FastQueryUtility.EscapeDashes(rootItem.Paths.FullPath));
                var items = database.SelectItems(query);

                if (items != null && items.Any())
                {
                    using (new SecurityDisabler())
                    {
                        foreach (var item in items.Where(i => i.IsDerived(Templates.ImportedItem.ID)))
                        {
                            Response.Write($"<h4>Evaluating item: {item.Paths.FullPath} </h4>");
                            Response.Flush();

                            var templates = item.Template.BaseTemplates;
                            var preImportTemplate = templates.FirstOrDefault(t => t.Name == item.TemplateName);
                            if (preImportTemplate != null)
                            {
                                Response.Write($"<div>Changing {item.Template.FullName} to {preImportTemplate.FullName}</div>");
                                Response.Flush();

                                item.ChangeTemplate(preImportTemplate);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.StackTrace);
            }
            return Content("success");
        }
    }
}