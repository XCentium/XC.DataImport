using HtmlAgilityPack;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    }
}