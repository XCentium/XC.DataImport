using Sitecore.SecurityModel;
using Sitecore.Services.Core;
using Sitecore.Data;
using System.Web.Mvc;
using Sitecore.Services.Infrastructure.Web.Http;
using XC.Foundation.DataImport.Repositories.FileSystem;
using XC.Foundation.DataImport.Models;
using System.Collections.Generic;
using System.Configuration;
using Sitecore.Configuration;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using Sitecore.Data.Managers;
using System;
using XC.Foundation.DataImport.Diagnostics;
using System.IO;
using Sitecore;
using XC.Foundation.DataImport.Configurations;
using Sitecore.Web;
using XC.DataImport.Repositories.History;
using Sitecore.Data.DataSources;
using Sitecore.Sites;
using Sitecore.Links;
using Newtonsoft.Json;
using XC.Foundation.DataImport.Models.Mappings;
using XC.Foundation.DataImport.Utilities;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Models.Entities;
using XC.Foundation.DataImport.Repositories.DataSources;
using XC.Foundation.DataImport.Models.Tree;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using Sitecore.Resources;

namespace XC.Foundation.DataImport.Controllers
{
    [ServicesController("dataimport/mappings")]
    [Authorize]
    public class ImportConfigurationController : ServicesApiController
    {
        private IFileSystemRepository _fileSystemRepository;

        public ImportConfigurationController() : base()
        {
            _fileSystemRepository = new FileSystemRepository();
        }

        public ImportConfigurationController(IFileSystemRepository fileSystemRepository) : base()
        {
            _fileSystemRepository = fileSystemRepository;
        }

        [HttpGet]
        public System.Web.Http.IHttpActionResult DefaultAction()
        {
            return Ok("This is default action of DataImportController");
        }

        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet]
        public System.Web.Http.IHttpActionResult CreateTestMapping()
        {
            var messages = new List<string>();

            try
            {
                var id = Guid.NewGuid();
                var fileName = "myTestMapping_" + id.ToString() + ".json";
                var filePath = Path.Combine(_fileSystemRepository.EnsureFolder(DataImportConfigurations.MappingFolder), fileName);
                var processedMappings = new List<ScFieldMapping>();

                var mappingObject = new ImportMappingModel()
                {
                    Name = "Test Mapping",
                    Id = id,
                    SourceType = new SourceType() { Name = DataImportContainer.SourceProviders.GetDatasourceName(typeof(FileDataSourceModel)), ModelType = typeof(FileDataSourceModel).FullName, DataSourceType = typeof(FileDataSource).FullName },
                    Source = new FileDataSourceModel { FilePath = @"C:\Users\Julia.Gavrilova\Downloads\xcentium-samplefiles\XCentium-TestFiles-17-nov-10_11.57.39_295_01-17314115739-1.xml" },
                    SourceProcessingScripts = new List<string>() { "Aha.Project.DataImport.Scripts.Source.XmlReferenceList, Aha.Project.DataImport" },
                    PostImportScripts = new List<string>(),
                    FieldMappings = processedMappings.ToArray(),
                    Target = new TargetSitecoreDataSourceModel
                    {
                        DatabaseName = "master",
                        ItemPath = "/sitecore/content/Data Import/Articles",
                        TemplateId = "{AD386352-ACAF-42CF-9DFC-0EB3700BAAA8}",
                    }
                };

                mappingObject.ConvertPathsToLongIds();

                File.WriteAllText(filePath, JsonConvert.SerializeObject(mappingObject, Formatting.Indented));

                messages.Add(Messages.MappingHasBeenSaved + " at " + filePath);

                return Ok(new
                {
                    data = mappingObject,
                    messages = messages
                });
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(ex.Message);
            }

            return Ok(new
            {
                data = new List<string>(),
                messages = messages
            });
        }

        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetConnectionStrings()
        {
            var databases = new List<DatabaseEntity>();
            foreach (ConnectionStringSettings connection in ConfigurationManager.ConnectionStrings)
            {
                databases.Add(new DatabaseEntity { Name = connection.Name, Value = connection.ConnectionString, Id = connection.Name });
            }

            return Ok(new
            {
                data = databases,
                messages = ""
            });
        }

        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetDatabases()
        {
            var databases = new List<DatabaseEntity>() { new DatabaseEntity { Name = "Select Database" } };
            var dbs = Factory.GetDatabases().Where(d => !string.IsNullOrEmpty(d.ConnectionStringName)).ToList();
            if (dbs.Any())
            {
                databases.AddRange(
                    dbs.Select(
                        d =>
                            new DatabaseEntity { Name = d.Name, Value = d.ConnectionStringName, Id = d.Name }));
            }

            return Ok(new
            {
                data = databases,
                messages = ""
            });
        }

        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetTemplates(string id = "")
        {
            var templates = new List<TemplateEntity>();

            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Database name is empty");

            var db = Factory.GetDatabase(id);
            if (db == null)
                return BadRequest("Database with the name provided doesn't exist");

            var dbTemplates = db.Templates.GetTemplates(Sitecore.Context.Language).Where(t => !t.InnerItem.Name.Contains("__")).OrderBy(t => t.Name);
            if (dbTemplates.Any())
            {
                templates.Add(new TemplateEntity { Name = "Select Template", Value = "", Database = "", Path = "" });
                templates.AddRange(
                    dbTemplates.Select(
                        t =>
                            new TemplateEntity { Name = t.Name + " (" + t.InnerItem.Paths.Path + ")", Value = t.InnerItem.Uri.ToString(), Database = t.Database.Name, Path = t.InnerItem.Paths.Path }));
            }
            return Ok(new
            {
                data = templates,
                messages = ""
            });
        }

        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetFields(string id = "", string fieldId = "")
        {
            var fields = new List<TemplateFieldEntity>();
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(fieldId))
            {
                return Ok(new
                {
                    data = fields,
                    messages = ""
                });
            }
            var itemUri = ItemUri.Parse(fieldId);
            if (itemUri == null)
                return Ok(new
                {
                    data = new List<string>(),
                    messages = ""
                });

            var db = Factory.GetDatabase(itemUri.DatabaseName);
            if (db == null)
                return Ok(new
                {
                    data = new List<string>(),
                    messages = ""
                });

            var template = TemplateManager.GetTemplate(itemUri.ItemID, db);

            if (template == null)
                return Ok(new
                {
                    data = new List<string>(),
                    messages = ""
                });

            if (template.GetFields(true).Any())
            {
                fields.Add(new TemplateFieldEntity { Id = "", Name = "Select Field" });
                fields.AddRange(
                    template.GetFields(true).OrderBy(f => f.Name).Select(
                        t =>
                            new TemplateFieldEntity { Name = t.Name + " (" + t.Type + ")", Id = t.ID.ToString() }));
            }
            return Ok(new
            {
                data = fields,
                messages = ""
            });
        }
        [HttpGet, HttpPost]
        public object GetMapping(string id = "")
        {
            var messages = new List<string>();
            if (id == null)
            {
                messages.Add(Messages.MappingIsNull);
                return new
                {
                    data = new ImportMappingModel(),
                    messages = messages
                };
            }

            try
            {
                var filePath = _fileSystemRepository.FindMappingById(id);
                if (string.IsNullOrEmpty(filePath))
                {
                    messages.Add(Messages.MappingIsNull);
                    return new
                    {
                        data = new ImportMappingModel(),
                        messages = messages
                    };
                }
                var mappingContent = File.ReadAllText(filePath);
                var mappingObject = (ImportMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(ImportMappingModel));

                return new
                {
                    data = mappingObject,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(ex.Message);
            }

            return new
            {
                data = new ImportMappingModel(),
                messages = messages
            };
        }


        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetMappings()
        {
            var messages = new List<string>();
            try
            {
                var files =
                    Directory.GetFiles(DataImportConfigurations.MappingFolder, "*.json")
                        .Select(
                            f =>
                                PopulateMappingModel(f));

                return Ok(new
                {
                    data = files,
                    messages = messages
                });
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(ex.Message);
            }

            return Ok(new
            {
                data = new List<string>(),
                messages = messages
            });
        }

        [HttpPost]
        public System.Web.Http.IHttpActionResult EditMapping(ImportMappingModel mappingObject)
        {

            var messages = new List<string>();
            if (mappingObject == null)
            {
                messages.Add(Messages.MappingIsNull);
                return Ok(new
                {
                    data = new ImportMappingModel(),
                    messages = messages
                });
            }

            try
            {
                //var mappingObject = (ImportMappingModel)JsonConvert.DeserializeObject(mapping, typeof(ImportMappingModel));

                if (mappingObject != null)
                {
                    var fileName = (!string.IsNullOrEmpty(mappingObject.Name) ? mappingObject.Name : "unknown") + "_" + mappingObject.Id.ToString() + ".json";
                    if (mappingObject.Id == Guid.Empty)
                    {
                        mappingObject.Id = Guid.NewGuid();
                        fileName = (!string.IsNullOrEmpty(mappingObject.Name) ? mappingObject.Name : "unknown") + "_" + mappingObject.Id.ToString() + ".json";
                    }
                    else
                    {
                        fileName = _fileSystemRepository.FindMappingById(mappingObject.Id.ToString());
                    }

                    if (mappingObject.SourceType != null && !string.IsNullOrEmpty(mappingObject.SourceType.Name) && string.IsNullOrEmpty(mappingObject.SourceType.ModelType))
                    {
                        mappingObject.SourceType = SourceTypeHelper.GetDatasourceSourceType(mappingObject.SourceType.Name);
                    }

                    if (mappingObject.TargetType != null && !string.IsNullOrEmpty(mappingObject.TargetType.Name) && string.IsNullOrEmpty(mappingObject.TargetType.ModelType))
                    {
                        mappingObject.TargetType = SourceTypeHelper.GetDatasourceTargetType(mappingObject.TargetType.Name);
                    }

                    var filePath = Path.Combine(_fileSystemRepository.EnsureFolder(DataImportConfigurations.MappingFolder), fileName);
                    mappingObject.ConvertPathsToLongIds();
                    if (mappingObject.FieldMappings != null)
                    {
                        var processedMappings = new List<ScFieldMapping>();
                        var count = 0;
                        foreach (var mp in mappingObject.FieldMappings)
                        {
                            if (!string.IsNullOrEmpty(mp.SourceFields))
                            {
                                mp.Id = count;
                                processedMappings.Add(mp);
                                count++;
                            }
                        }
                        mappingObject.FieldMappings = processedMappings.ToArray();
                    }
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(mappingObject, Formatting.Indented));
                }
                messages.Add(Messages.MappingHasBeenSaved);

                return Ok(new
                {
                    data = mappingObject,
                    messages = messages
                });
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(ex.Message);
            }

            return Ok(new
            {
                data = new List<string>(),
                messages = messages
            });
        }

        [HttpGet]
        public System.Web.Http.IHttpActionResult GetPostProcessingScript(string id = "")
        {
            var messages = new List<string>();
            if (id == null)
            {
                messages.Add(Messages.MappingIsNull);
                return Ok(new
                {
                    data = new List<string>(),
                    messages = messages
                });
            }

            try
            {
                var mappingContent = File.ReadAllText(id);
                var mappingObject = (ScriptReference)JsonConvert.DeserializeObject(mappingContent, typeof(ScriptReference));

                return Ok(new
                {
                    data = mappingObject,
                    messages = messages
                });
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(ex.Message);
            }

            return Ok(new
            {
                data = new List<string>(),
                messages = messages
            });
        }

        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetFieldProcessingScript(string id = "")
        {
            var messages = new List<string>();
            if (id == null)
            {
                messages.Add(Messages.MappingIsNull);
                return Ok(new
                {
                    data = new List<string>(),
                    messages = messages
                });
            }

            try
            {
                var mappingContent = File.ReadAllText(id);
                var mappingObject = (ScriptReference)JsonConvert.DeserializeObject(mappingContent, typeof(ScriptReference));

                return Ok(new
                {
                    data = mappingObject,
                    messages = messages
                });
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(ex.Message);
            }

            return Ok(new
            {
                data = new List<string>(),
                messages = messages
            });
        }

        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetSourceTypes()
        {
            var messages = new List<string>();
            try
            {
                return Ok(new
                {
                    data = DataImportContainer.SourceProviders.GetDatasourceTypes(),
                    messages = messages
                });
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(ex.StackTrace);
            }

            return Ok(new
            {
                data = new List<string>(),
                messages = messages
            });
        }

        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetTargetTypes()
        {
            var messages = new List<string>();
            try
            {
                return Ok(new
                {
                    data = DataImportContainer.SourceProviders.GetTargetTypes(),
                    messages = messages
                });
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(ex.StackTrace);
            }

            return Ok(new
            {
                data = new List<string>(),
                messages = messages
            });
        }


        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetSitecoreTree(string id = "master", string itemId = "")
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Database name is empty");

            var db = Factory.GetDatabase(id);
            if (db == null)
                return BadRequest("Database with the name provided doesn't exist");

            var rootItem = db.GetRootItem();
            if (rootItem != null)
            {
                itemId = itemId.Trim();
                var node = PopulateTreeNode(rootItem);
                node.isExpanded = true;

                var items = rootItem.GetChildren();
                if (items.Any())
                {
                    node.children = new List<TreeNode>();
                    foreach (Item item in items)
                    {
                        var childNode = PopulateTreeNode(item);
                        if (!string.IsNullOrWhiteSpace(itemId) && itemId.Contains(item.Paths.LongID) && item.HasChildren)
                        {
                            childNode.children = PopulateTreeNodes(item, itemId);
                            childNode.isExpanded = true;
                        }
                        if (itemId == item.Paths.LongID)
                        {
                            childNode.isActive = true;
                        }
                        node.children.Add(childNode);
                    }
                }

                return Ok(new
                {
                    data = new List<TreeNode>() { node },
                    messages = ""
                });
            }
            return Ok(new
            {
                messages = ""
            });
        }

        private List<TreeNode> PopulateTreeNodes(Item item, string itemId)
        {
            var nodes = new List<TreeNode>();
            if (item != null && item.HasChildren)
            {
                foreach (Item childItem in item.Children)
                {
                    var childNode = PopulateTreeNode(childItem);
                    if (!string.IsNullOrEmpty(itemId) && itemId.Contains(childItem.Paths.LongID))
                    {
                        if (itemId == childItem.Paths.LongID)
                        {
                            childNode.isActive = true;
                            childNode.isFocused = true;
                        }
                        if (childItem.HasChildren && itemId != childItem.Paths.LongID)
                        {
                            childNode.children = PopulateTreeNodes(childItem, itemId);
                            childNode.isExpanded = true;
                        }
                    }
                    nodes.Add(childNode);
                }
            }
            return nodes;
        }

        public System.Web.Http.IHttpActionResult GetSitecoreTreeChildNodes(string itemId, string id = "master")
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Database name is empty");

            var db = Factory.GetDatabase(id);
            if (db == null)
                return BadRequest("Database with the name provided doesn't exist");

            var rootItem = string.IsNullOrEmpty(itemId) ? db.GetRootItem() : db.GetItem(itemId);
            if (rootItem != null)
            {
                if (!string.IsNullOrEmpty(itemId))
                {
                    return Ok(rootItem.HasChildren ? rootItem.GetChildren().Select(i => PopulateTreeNode(i)).ToList() : new List<TreeNode>());
                }
            }
            return Ok(new
            {
                messages = ""
            });
        }

        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult DeleteMapping(string id)
        {
            var messages = new List<string>();
            if (string.IsNullOrEmpty(id))
            {
                messages.Add(Messages.MappingIsNull);
                return Ok(new
                {
                    data = new List<string>(),
                    messages = messages
                });
            }

            try
            {
                var fileForMappingId = FindFileForMappingId(id);
                if (File.Exists(fileForMappingId))
                {
                    File.Delete(fileForMappingId);
                    return Ok(new
                    {
                        data = new List<string>(),
                        messages = new List<string> { "Mapping has been deleted" }
                    });
                }
                else
                {
                    return Ok(new
                    {
                        data = new List<string>(),
                        messages = new List<string> { "Mapping doesn't exist" }
                    });
                }

            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(ex.Message);
            }

            return Ok(new
            {
                data = new List<string>(),
                messages = messages
            });
        }

        private static MappingInfo PopulateMappingModel(string filePath)
        {
            if (File.Exists(filePath))
            {
                var mappingContent = File.ReadAllText(filePath);
                var mappingObject = (ImportMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(ImportMappingModel));

                return new MappingInfo
                {
                    Name = mappingObject.Name,
                    FileName = Path.GetFileName(filePath),
                    Path = filePath,
                    NumberOfItemsProcessed = HistoryLogging.GetNumberOfItemsProcessed(filePath),
                    Id = mappingObject.Id,
                    LastRun = HistoryLogging.GetLatestRunDateString(filePath),
                };
            }
            return null;
        }

        private string FindFileForMappingId(string id)
        {
            var file = Directory.GetFiles(DataImportConfigurations.MappingFolder, "*" + id + ".json", SearchOption.TopDirectoryOnly);
            if (file != null)
            {
                return file.FirstOrDefault();
            }
            return string.Empty;
        }

        private TreeNode PopulateTreeNode(Item rootItem)
        {
            return new TreeNode
            {
                hasChildren = rootItem.HasChildren,
                id = rootItem.ID.ToString(),
                longId = rootItem.Paths.LongID,
                mediaUrl = Images.GetThemedImageSource(rootItem.Appearance.Icon),
                name = rootItem.DisplayName,
                path = rootItem.Paths.FullPath
            };
        }

        /// <summary>
        /// Gets the item URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private string GetItemUrl(IDataSourceItem item)
        {
            DataSourceItem dataSourceItem = item as DataSourceItem;
            if (dataSourceItem == null)
                return string.Empty;
            var site = SiteContextFactory.GetSiteContext("shell");
            return LinkManager.GetItemUrl(dataSourceItem.InnerItem, new UrlOptions { AlwaysIncludeServerUrl = false, Site = site });
        }
    }
}