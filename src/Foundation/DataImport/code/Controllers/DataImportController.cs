using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Web;
using XC.Foundation.DataImport.Models;
using XC.DataImport.Repositories.History;
using System.Configuration;
using Sitecore.Services.Core;
using Sitecore.Services.Infrastructure.Web.Http;
using System.Web.Mvc;
using Sitecore;
using XC.Foundation.DataImport.Configurations;
using XC.Foundation.DataImport.Diagnostics;
using Sitecore.Data.Managers;
using XC.Foundation.DataImport.Repositories.FileSystem;
using Sitecore.Data.DataSources;
using Sitecore.Links;
using Sitecore.Sites;

namespace XC.Foundation.DataImport.Controllers
{
    [ServicesController("speak.dataimport/data")]
    public class DataImportController : ServicesApiController
    {
        private IFileSystemRepository _fileSystemRepository;

        public DataImportController(): base()
        {
            _fileSystemRepository = new FileSystemRepository();
        }

        public DataImportController(IFileSystemRepository fileSystemRepository): base()
        {
            _fileSystemRepository = fileSystemRepository;
        }

        [System.Web.Http.AcceptVerbs("GET")]
        [HttpGet]
        public object DefaultAction()
        {
            return new
            {
                messages = "This is default action of DataImportController"
            };
        }
        /// <summary>
        /// Alls this instance.
        /// </summary>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object ConnectionStrings()
        {
            var databases = new List<DatabaseEntity>();
            foreach (ConnectionStringSettings connection in ConfigurationManager.ConnectionStrings)
            {
                databases.Add(new DatabaseEntity { Name = connection.Name, ConnectionString = connection.ConnectionString, Id = connection.Name });
            }

            return new
            {
                data = databases,
                messages = ""
            };
        }

        /// <summary>
        /// Alls this instance.
        /// </summary>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object Databases()
        {
            var databases = new List<DatabaseEntity>() { new DatabaseEntity { Name = "Select Target Database" } };
            var dbs = Factory.GetDatabases().Where(d => !string.IsNullOrEmpty(d.ConnectionStringName)).ToList();
            if (dbs.Any())
            {
                databases.AddRange(
                    dbs.Select(
                        d =>
                            new DatabaseEntity {Name = d.Name, ConnectionString = d.ConnectionStringName, Id = d.Name}));
            }

            return new
            {
                data = databases,
                messages = ""
            };
        }
        /// <summary>
        /// Getting Templates.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object Templates(string item = "")
        {
            var templates = new List<TemplateEntity>();

            if(string.IsNullOrWhiteSpace(item))
                return new
                {
                    data = templates,
                    messages = "Database name is empty"
                };

            var db = Factory.GetDatabase(item);
            if(db == null)
                return new
                {
                    data = templates,
                    messages = "Database with the name provided doesn't exist"
                };

            var dbTemplates = db.Templates.GetTemplates(Context.Language).Where(t => !t.InnerItem.Name.Contains("__")).OrderBy(t => t.Name);
            if (dbTemplates.Any())
            {
                templates.Add(new TemplateEntity { Name = "Select Template", Id = "", Database = "", Path = "" });
                templates.AddRange(
                    dbTemplates.Select(
                        t =>
                            new TemplateEntity { Name = t.Name + " (" + t.InnerItem.Paths.Path + ")", Id = t.InnerItem.Uri.ToString(), Database = t.Database.Name, Path = t.InnerItem.Paths.Path }));
            }
            return new
            {
                data = templates,
                messages = ""
            };
        }

        /// <summary>
        /// Getting Fields the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="database">The database.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object Fields(string item = "")
        {
            var fields = new List<TemplateFieldEntity>();
            if (string.IsNullOrEmpty(item))
            {
                return new
                {
                    data = fields,
                    messages = ""
                };
            }
            var itemUri = ItemUri.Parse(item);
            if (itemUri == null)
                return new
                {
                    data = new List<string>(),
                    messages = ""
                };

            var db = Factory.GetDatabase(itemUri.DatabaseName);
            if (db == null)
                return new
                {
                    data = new List<string>(),
                    messages = ""
                };

            var template = TemplateManager.GetTemplate(itemUri.ItemID, db);

            if(template == null)
                return new
                {
                    data = new List<string>(),
                    messages = ""
                };

            if (template.GetFields(true).Any())
            {
                fields.Add(new TemplateFieldEntity {Id = "", Name = "Select Field"});
                fields.AddRange(
                    template.GetFields(true).OrderBy(f=>f.Name).Select(
                        t =>
                            new TemplateFieldEntity { Name = t.Name + " (" + t.Type + ")", Id = t.ID.ToString() }));
            }

            return new
            {
                data = fields,
                messages = ""
            };
        }

        /// <summary>
        /// Saves the mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("POST")]
        [HttpPost]
        public object SaveMapping()
        {
            string mapping = System.Web.HttpContext.Current.Request.Form["mapping"];
            var messages = new List<MessageModel>();

            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                var mappingObject = (MappingModel)JsonConvert.DeserializeObject(mapping, typeof(MappingModel));

                if (mappingObject != null)
                {
                    var fileName = mappingObject.Name + ".json";
                    var filePath = Path.Combine(DataImportConfigurations.MappingFolder, fileName);
                    mappingObject.ConvertPathsToLongIds();
                    if (mappingObject.FieldMapping != null)
                    {
                        foreach (var mp in mappingObject.FieldMapping)
                        {
                            if (string.IsNullOrEmpty(mp.SourceFields))
                            {
                                mappingObject.FieldMapping.ToList().Remove(mp);
                            }
                        }
                    }
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(mappingObject, Formatting.Indented));
                }
                messages.Add(new MessageModel { Text = Messages.MappingHasBeenSaved, Type = MessageType.Notification.ToString() });

                return new
                {
                    data = mappingObject,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }

        /// <summary>
        /// Saves the mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("POST")]
        [HttpPost]
        public object SaveNonScMapping()
        {
            string mapping = System.Web.HttpContext.Current.Request.Form["mapping"];
            var messages = new List<MessageModel>();
            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                var mappingObject = (NonSitecoreMappingModel)JsonConvert.DeserializeObject(mapping, typeof(NonSitecoreMappingModel));

                if (mappingObject != null)
                {
                    var fileName = !string.IsNullOrEmpty(mappingObject.Name) ? mappingObject.Name + ".json" : "unknown" + ".json";
                    var filePath = Path.Combine(_fileSystemRepository.EnsureFolder(DataImportConfigurations.NonSitecoreMappingFolder), fileName);
                    mappingObject.ConvertPathsToLongIds();
                    if (mappingObject.FieldMapping != null)
                    {
                        foreach (var mp in mappingObject.FieldMapping)
                        {
                            if (string.IsNullOrEmpty(mp.SourceFields))
                            {
                                mappingObject.FieldMapping.ToList().Remove(mp);
                            }
                        }
                    }
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(mappingObject));
                }
                messages.Add(new MessageModel { Text = Messages.MappingHasBeenSaved, Type = MessageType.Notification.ToString() });

                return new
                {
                    data = mappingObject,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }


        /// <summary>
        /// Saves the mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("POST")]
        [HttpPost]
        public object SavePostProcessingScript()
        {
            string mapping = System.Web.HttpContext.Current.Request.Form["script"];
            var messages = new List<MessageModel>();
            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                var mappingObject = (ScriptReference)JsonConvert.DeserializeObject(mapping, typeof(ScriptReference));

                if (mappingObject != null)
                {
                    var fileName = !string.IsNullOrEmpty(mappingObject.Name) ? mappingObject.Name + ".json" : "unknown" + ".json";
                    var filePath = Path.Combine(_fileSystemRepository.EnsureFolder(DataImportConfigurations.PostProcessingScriptsFolder), fileName);
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(mappingObject));
                }
                messages.Add(new MessageModel { Text = Messages.ScriptHasBeenSaved, Type = MessageType.Notification.ToString() });

                return new
                {
                    data = mappingObject,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object ViewPostProcessingScript(string item = "")
        {
            var messages = new List<MessageModel>();
            if (item == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                var mappingContent = File.ReadAllText(item);
                var mappingObject = (ScriptReference)JsonConvert.DeserializeObject(mappingContent, typeof(ScriptReference));

                return new
                {
                    data = mappingObject,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }
        /// <summary>
        /// Saves the mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object SaveBatchMapping(string mapping)
        {
            var messages = new List<MessageModel>();
            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                var mappingObject = (BatchMappingModel)JsonConvert.DeserializeObject(mapping, typeof(BatchMappingModel));

                if (mappingObject != null)
                {
                    var fileName = mappingObject.Name + ".json";
                    var filePath = Path.Combine(DataImportConfigurations.BatchMappingsFolder, fileName);                    
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(mappingObject));
                }
                messages.Add(new MessageModel { Text = Messages.MappingHasBeenSaved, Type = MessageType.Notification.ToString() });

                return new
                {
                    data = mappingObject,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }

        /// <summary>
        /// Saves the non sc batch mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("POST")]
        [HttpPost]
        public object SaveNonScBatchMapping()
        {
            string mapping = System.Web.HttpContext.Current.Request.Form["mapping"];
            var messages = new List<MessageModel>();
            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                var mappingObject = (BatchMappingModel)JsonConvert.DeserializeObject(mapping, typeof(BatchMappingModel));

                if (mappingObject != null)
                {
                    var fileName = mappingObject.Name + ".json";
                    var filePath = Path.Combine(DataImportConfigurations.BatchNonScMappingsFolder, fileName);
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(mappingObject));
                }
                messages.Add(new MessageModel { Text = Messages.MappingHasBeenSaved, Type = MessageType.Notification.ToString() });

                return new
                {
                    data = mappingObject,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }

        /// <summary>
        /// Mappings the specified mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object ViewMapping(string mapping)
        {
            var messages = new List<MessageModel>();

            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                var mappingContent = File.ReadAllText(mapping);
                var mappingObject = (MappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(MappingModel));

                return new
                {
                    data = mappingObject,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }

        /// <summary>
        /// Mappings the specified mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object DeleteMapping(string mapping)
        {
            var messages = new List<MessageModel>();

            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                if (File.Exists(mapping))
                {
                    File.Delete(mapping);
                }
                return new
                {
                    data = new List<string>(),
                    messages = "Mapping has been deleted"
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }

        /// <summary>
        /// Mappings the specified mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object DeleteScript(string script)
        {
            var messages = new List<MessageModel>();

            if (script == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                if (File.Exists(script))
                {
                    File.Delete(script);
                }
                return new
                {
                    data = new List<string>(),
                    messages = "Script has been deleted"
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }

        /// <summary>
        /// Mappings the specified mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object ViewNonScMapping(string item = "")
        {
            var messages = new List<MessageModel>();
            if (item == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                var mappingContent = File.ReadAllText(item);
                var mappingObject = (NonSitecoreMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(NonSitecoreMappingModel));

                return new
                {
                    data = mappingObject,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object AllPostProcessingScripts()
        {
            var messages = new List<MessageModel>();
            try
            {
                var db = Factory.GetDatabase("core");
                var editItem = db.GetItem(MappingPages.Scripts.PostProcessingScriptCreate);
                var editMappingItem = ClientHost.Factory.GetDataSourceItem(editItem);

                var deleteItem = db.GetItem(MappingPages.Scripts.PostProcessingScriptCreate);
                var deleteMappingItem = ClientHost.Factory.GetDataSourceItem(deleteItem);

                var files =
                    Directory.GetFiles(DataImportConfigurations.PostProcessingScriptsFolder, "*.json")
                        .Select(
                            f =>
                                new ScriptInfo
                                {
                                    Name = Path.GetFileNameWithoutExtension(f),
                                    FileName = Path.GetFileName(f),
                                    Path = f,
                                    EditLabel = Labels.Edit,
                                    EditLink = WebUtil.AddQueryString(GetItemUrl(editMappingItem), "script", f),
                                    Type = GetScriptType(f),
                                    DeleteLabel = Labels.Delete,
                                    DeleteLink = WebUtil.AddQueryString(GetItemUrl(deleteMappingItem), "script", f),
                                    itemId = f
                                });

                return new
                {
                    data = files,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }

        

        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object PostProcessingScripts(int item = 0, string mapping = "")
        {
            var messages = new List<MessageModel>();
            if (string.IsNullOrEmpty(mapping))
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = GetScripts(1, 0),
                    messages = messages
                };
            }

            try
            {
                var mappingContent = File.ReadAllText(mapping);
                var mappingObject = (NonSitecoreMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(NonSitecoreMappingModel));

                var scripts = mappingObject.PostImportScripts.ToList();

                return new
                {
                    data = scripts,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = GetScripts(1, 0),
                messages = messages
            };
        }

        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object NonScFieldMappings(int item = 0, string mapping = "")
        {
            var messages = new List<MessageModel>();
            if (string.IsNullOrEmpty(mapping))
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = GetFieldMappings(1, 0),
                    messages = messages
                };
            }

            try
            {
                var mappingContent = File.ReadAllText(mapping);
                var mappingObject = (NonSitecoreMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(NonSitecoreMappingModel));

                var mappings = mappingObject.FieldMapping.ToList();

                IEnumerable<NonScFieldMapping> additionalMappings = null;
                if(item > mappingObject.FieldMapping.Count())
                {
                    additionalMappings = GetFieldMappings(item, mappingObject.FieldMapping.Count());
                    mappings.AddRange(additionalMappings);
                }
                return new
                {
                    data = mappings,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = GetFieldMappings(1, 0),
                messages = messages
            };
        }


        private IEnumerable<ScriptReference> GetScripts(int item, int start)
        {
            var emptyScriptList = new List<ScriptReference>();
            for (var i = start; i < item; i++)
            {
                emptyScriptList.Add(new ScriptReference() { Id = i });
            }
            return emptyScriptList;
        }

        /// <summary>
        /// Gets the field mappings.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private IEnumerable<NonScFieldMapping> GetFieldMappings(int item, int start)
        {
            var emptyMappingList = new List<NonScFieldMapping>();
            for (var i = start; i < item; i++)
            {
                emptyMappingList.Add(new NonScFieldMapping() { Id = i});
            }
            return emptyMappingList;
        }

        /// <summary>
        /// Mappings the specified mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object ViewBatchMapping(string mapping)
        {
            var messages = new List<MessageModel>();
            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = Messages.MappingIsNull, Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                var mappingContent = File.ReadAllText(mapping);
                var mappingObject = (BatchMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(BatchMappingModel));

                return new
                {
                    data = mappingObject,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }
        /// <summary>
        /// Mappingses this instance.
        /// </summary>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object Mappings()
        {
            var messages = new List<MessageModel>();
            try
            {
                var db = Factory.GetDatabase("core");
                var editItem = db.GetItem(MappingPages.Sitecore.ScMappingsEditTemplateMapping);
                var editMappingItem = ClientHost.Factory.GetDataSourceItem(editItem);

                var runItem = db.GetItem(MappingPages.Sitecore.ScMappingsRunTemplateImport);
                var runMappingItem = ClientHost.Factory.GetDataSourceItem(runItem);

                var files =
                    Directory.GetFiles(DataImportConfigurations.MappingFolder, "*.json")
                        .Select(
                            f =>
                                new MappingInfo
                                {
                                    Name = Path.GetFileNameWithoutExtension(f),
                                    FileName = Path.GetFileName(f),
                                    Path = f,
                                    EditLabel = Labels.Edit,
                                    RunLabel = Labels.Run,
                                    RunLink = WebUtil.AddQueryString(GetItemUrl(runMappingItem), "mapping", f),
                                    EditLink = WebUtil.AddQueryString(GetItemUrl(editMappingItem), "mapping", f),
                                    LastRun = HistoryLogging.GetLatestRunDateString(f),
                                    NumberOfItemsProcessed = HistoryLogging.GetNumberOfItemsProcessed(f),
                                    DeleteLabel = Labels.Delete,
                                    DeleteLink = WebUtil.AddQueryString(GetItemUrl(editMappingItem), "mapping", f)
                                });

                return new
                {
                    data = files,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }
        /// <summary>
        /// Mappingses this instance.
        /// </summary>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object NonScMappings()
        {
            var messages = new List<MessageModel>();
            try
            {
                var db = Factory.GetDatabase("core");
                var editItem = db.GetItem(MappingPages.NonSitecore.NonScMappingsCreateTemplateMapping);
                var editMappingItem = ClientHost.Factory.GetDataSourceItem(editItem);

                var runItem = db.GetItem(MappingPages.NonSitecore.NonScMappingsRunTemplateImport);
                var runMappingItem = ClientHost.Factory.GetDataSourceItem(runItem);

                var deleteItem = db.GetItem(MappingPages.NonSitecore.NonScMappingsDeleteTemplateImport);
                var deleteMappingItem = ClientHost.Factory.GetDataSourceItem(deleteItem);

                var files =
                    Directory.GetFiles(DataImportConfigurations.NonSitecoreMappingFolder, "*.json")
                        .Select(
                            f =>
                                new MappingInfo
                                {
                                    Name = Path.GetFileNameWithoutExtension(f),
                                    FileName = Path.GetFileName(f),
                                    Path = f,
                                    EditLabel = Labels.Edit,
                                    RunLabel = Labels.Run,
                                    RunLink = WebUtil.AddQueryString(GetItemUrl(runMappingItem), "mapping", f),
                                    EditLink = WebUtil.AddQueryString(GetItemUrl(editMappingItem), "mapping", f),
                                    LastRun = HistoryLogging.NonSitecoreGetLatestRunDateString(f),
                                    NumberOfItemsProcessed = HistoryLogging.GetNumberOfItemsProcessed(f),
                                    DeleteLabel = Labels.Delete,
                                    DeleteLink = WebUtil.AddQueryString(GetItemUrl(deleteMappingItem), "mapping", f)
                                });

                return new
                {
                    data = files,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }

        /// <summary>
        /// Mappingses this instance.
        /// </summary>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object BatchMappings()
        {
            var messages = new List<MessageModel>();
            try
            {
                var db = Factory.GetDatabase("core");
                var editItem = db.GetItem(MappingPages.SitecoreBatch.BatchMappingsCreateBatchMapping);
                var editMappingItem = ClientHost.Factory.GetDataSourceItem(editItem);

                var runItem = db.GetItem(MappingPages.SitecoreBatch.BatchMappingsRunBatchImport);
                var runMappingItem = ClientHost.Factory.GetDataSourceItem(runItem);

                var files =
                    Directory.GetFiles(DataImportConfigurations.BatchMappingsFolder, "*.json")
                        .Select(
                            f =>
                                new MappingInfo
                                {
                                    Name = Path.GetFileNameWithoutExtension(f),
                                    FileName = Path.GetFileName(f),
                                    Path = f,
                                    EditLabel = Labels.Edit,
                                    RunLabel = Labels.Run,
                                    RunLink = WebUtil.AddQueryString(GetItemUrl(runMappingItem), "mapping", f),
                                    EditLink = WebUtil.AddQueryString(GetItemUrl(editMappingItem), "mapping", f),
                                    LastRun = HistoryLogging.GetLatestRunDateString(f),
                                    NumberOfItemsProcessed = HistoryLogging.GetNumberOfItemsProcessed(f),
                                    DeleteLabel = Labels.Delete,
                                    DeleteLink = WebUtil.AddQueryString(GetItemUrl(editMappingItem), "mapping", f)
                                });

                return new
                {
                    data = files,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }


        /// <summary>
        /// Mappingses this instance.
        /// </summary>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public object BatchNonScMappings()
        {
            var messages = new List<MessageModel>();
            try
            {
                var db = Factory.GetDatabase("core");
                var editItem = db.GetItem(MappingPages.NonSitecoreBatch.BatchNonScMappingsCreateBatchMapping);
                var editMappingItem = ClientHost.Factory.GetDataSourceItem(editItem);

                var runItem = db.GetItem(MappingPages.NonSitecoreBatch.BatchNonScMappingsRunBatchImport);
                var runMappingItem = ClientHost.Factory.GetDataSourceItem(runItem);

                var files =
                    Directory.GetFiles(DataImportConfigurations.BatchNonScMappingsFolder, "*.json")
                        .Select(
                            f =>
                                new MappingInfo
                                {
                                    Name = Path.GetFileNameWithoutExtension(f),
                                    FileName = Path.GetFileName(f),
                                    Path = f,
                                    EditLabel = Labels.Edit,
                                    RunLabel = Labels.Run,
                                    RunLink = WebUtil.AddQueryString(GetItemUrl(runMappingItem), "mapping", f),
                                    EditLink = WebUtil.AddQueryString(GetItemUrl(editMappingItem), "mapping", f),
                                    LastRun = HistoryLogging.GetLatestRunDateString(f),
                                    NumberOfItemsProcessed = HistoryLogging.GetNumberOfItemsProcessed(f),
                                    DeleteLabel = Labels.Delete,
                                    DeleteLink = WebUtil.AddQueryString(GetItemUrl(editMappingItem), "mapping", f)
                                });

                return new
                {
                    data = files,
                    messages = messages
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                messages.Add(new MessageModel { Text = ex.Message, Type = MessageType.Error.ToString() });
            }

            return new
            {
                data = new List<string>(),
                messages = messages
            };
        }

        /// <summary>
        /// Gets the type of the script.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private string GetScriptType(string item)
        {
            if (string.IsNullOrEmpty(item))
                return string.Empty;
            try
            {
                var mappingContent = File.ReadAllText(item);
                var mappingObject = (ScriptReference)JsonConvert.DeserializeObject(mappingContent, typeof(ScriptReference));

                if (mappingObject != null)
                    return mappingObject.Type;
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return string.Empty;
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
