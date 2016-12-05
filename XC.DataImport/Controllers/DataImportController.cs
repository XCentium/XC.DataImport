using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sitecore.Services.Core;
using Sitecore.Services.Infrastructure.Web.Http;
using System.Web.Http;
using Newtonsoft.Json;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Web;
using XC.DataImport.Repositories.Repositories;
using XC.DataImport.Repositories.Diagnostics;
using XC.DataImport.Repositories.Migration;
using XC.DataImport.Repositories.Models;
using XC.DataImport.Repositories.Configurations;
using XC.DataImport.Repositories.History;
using System.Data.SqlClient;
using System.Configuration;

namespace XC.DataImport.Controllers
{
    [ServicesController("speak.dataimport/databases")]
    public class DataImportController : ServicesApiController
    {
        [HttpGet]
        public string PingMds()
        {
            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["mds"].ConnectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT TOP 1 [ID] FROM [MDS].[mdm].[vw_product_class]";
                        cmd.CommandType = System.Data.CommandType.Text;
                        var result = cmd.ExecuteReader();
                        return "success";
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.StackTrace;
            }
        }

        /// <summary>
        /// Alls this instance.
        /// </summary>
        /// <returns></returns>
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
       [HttpGet, HttpPost]
        public object All()
        {
            var databases = new List<DatabaseEntity>();
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
        /// Templateses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        [HttpGet, HttpPost]
        public object Templates(string item)
        {
            var templates = new List<TemplateEntity>();
            var db = Factory.GetDatabase(item);
            if(db == null)
                return new
                {
                    data = new List<string>(),
                    messages = ""
                };

            var dbTemplates = db.Templates.GetTemplates(Sitecore.Context.Language).Where(t => !t.InnerItem.Name.Contains("__")).OrderBy(t => t.Name);
            if (dbTemplates.Any())
            {
                templates.Add(new TemplateEntity { Name = "", Id = "", Database = "", Path = "" });
                templates.AddRange(
                    dbTemplates.Select(
                        t =>
                            new TemplateEntity { Name = t.Name + " (" + t.InnerItem.Paths.Path + ")", Id = t.ID.ToString(), Database = t.Database.Name, Path = t.InnerItem.Paths.Path }));
            }
            return new
            {
                data = templates,
                messages = ""
            };
        }

        /// <summary>
        /// Fieldses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="database">The database.</param>
        /// <returns></returns>
        [HttpGet, HttpPost]
        public object Fields(string item = "", string database = "")
        {
            var fields = new List<TemplateFieldEntity>();
            if (string.IsNullOrEmpty(item) || string.IsNullOrEmpty(database))
            {
                return new
                {
                    data = fields,
                    messages = ""
                };
            }
            var db = Factory.GetDatabase(database);
            if (db == null)
                return new
                {
                    data = new List<string>(),
                    messages = ""
                };

            var template = db.Templates.GetTemplates(Sitecore.Context.Language)
                .FirstOrDefault(t => t.ID == new ID(item));

            if(template == null)
                return new
                {
                    data = new List<string>(),
                    messages = ""
                };

            if (template.Fields.Any())
            {
                fields.Add(new TemplateFieldEntity {Id = "", Name = ""});
                fields.AddRange(
                    template.Fields.OrderBy(f=>f.Name).Select(
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
        [HttpPost]
        public object SaveMapping()
        {
            string mapping = System.Web.HttpContext.Current.Request.Form["mapping"];
            var messages = new List<MessageModel>();

            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = "Mapping is null", Type = MessageType.Error.ToString() });
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
                messages.Add(new MessageModel { Text = "Mapping has been saved", Type = MessageType.Notification.ToString() });

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
        [HttpPost]
        public object SaveNonScMapping()
        {
            string mapping = System.Web.HttpContext.Current.Request.Form["mapping"];
            var messages = new List<MessageModel>();
            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = "Mapping is null", Type = MessageType.Error.ToString() });
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
                    var fileName = mappingObject.Name + ".json";
                    var filePath = Path.Combine(DataImportConfigurations.NonSitecoreMappingFolder, fileName);
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
                messages.Add(new MessageModel { Text = "Mapping has been saved", Type = MessageType.Notification.ToString() });

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
        [HttpGet, HttpPost]
        public object SaveBatchMapping(string mapping)
        {
            var messages = new List<MessageModel>();
            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = "Mapping is null", Type = MessageType.Error.ToString() });
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
                messages.Add(new MessageModel { Text = "Mapping has been saved", Type = MessageType.Notification.ToString() });

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
        [HttpPost]
        public object SaveNonScBatchMapping()
        {
            string mapping = System.Web.HttpContext.Current.Request.Form["mapping"];
            var messages = new List<MessageModel>();
            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = "Mapping is null", Type = MessageType.Error.ToString() });
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
                messages.Add(new MessageModel { Text = "Mapping has been saved", Type = MessageType.Notification.ToString() });

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
        [HttpGet, HttpPost]
        public object ViewMapping(string mapping)
        {
            var messages = new List<MessageModel>();

            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = "Mapping is null", Type = MessageType.Error.ToString() });
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
        [HttpGet, HttpPost]
        public object ViewNonScMapping(string mapping)
        {
            var messages = new List<MessageModel>();
            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = "Mapping is null", Type = MessageType.Error.ToString() });
                return new
                {
                    data = new List<string>(),
                    messages = messages
                };
            }

            try
            {
                var mappingContent = File.ReadAllText(mapping);
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

        /// <summary>
        /// Mappings the specified mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [HttpGet, HttpPost]
        public object ViewBatchMapping(string mapping)
        {
            var messages = new List<MessageModel>();
            if (mapping == null)
            {
                messages.Add(new MessageModel { Text = "Mapping is null", Type = MessageType.Error.ToString() });
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
        [HttpGet, HttpPost]
        public object Mappings()
        {
            var messages = new List<MessageModel>();
            try
            {
                var db = Factory.GetDatabase("core");
                var editItem = db.GetItem("{F683E74B-35C1-4C16-9168-8DD1A989640A}");
                var editMappingItem = ClientHost.Factory.GetDataSourceItem(editItem);

                var runItem = db.GetItem("{9539E038-208E-4C03-8C9F-AFF72CF1DCD7}");
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
                                    EditLabel = "Edit",
                                    RunLabel = "Run",
                                    RunLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(runMappingItem), "mapping", f),
                                    EditLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(editMappingItem), "mapping", f),
                                    LastRun = HistoryLogging.GetLatestRunDateString(f),
                                    NumberOfItemsProcessed = HistoryLogging.GetNumberOfItemsProcessed(f),
                                    DeleteLabel = "Delete",
                                    DeleteLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(editMappingItem), "mapping", f)
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
        [HttpGet, HttpPost]
        public object NonScMappings()
        {
            var messages = new List<MessageModel>();
            try
            {
                var db = Factory.GetDatabase("core");
                var editItem = db.GetItem("{DF2723B0-CAB8-4086-8E8B-73AEFB079164}");
                var editMappingItem = ClientHost.Factory.GetDataSourceItem(editItem);

                var runItem = db.GetItem("{A6E59199-D1D6-4DDD-AAE3-14B6B228B136}");
                var runMappingItem = ClientHost.Factory.GetDataSourceItem(runItem);

                var files =
                    Directory.GetFiles(DataImportConfigurations.NonSitecoreMappingFolder, "*.json")
                        .Select(
                            f =>
                                new MappingInfo
                                {
                                    Name = Path.GetFileNameWithoutExtension(f),
                                    FileName = Path.GetFileName(f),
                                    Path = f,
                                    EditLabel = "Edit",
                                    RunLabel = "Run",
                                    RunLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(runMappingItem), "mapping", f),
                                    EditLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(editMappingItem), "mapping", f),
                                    LastRun = HistoryLogging.NonSitecoreGetLatestRunDateString(f),
                                    NumberOfItemsProcessed = HistoryLogging.GetNumberOfItemsProcessed(f),
                                    DeleteLabel = "Delete",
                                    DeleteLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(editMappingItem), "mapping", f)
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
        [HttpGet, HttpPost]
        public object BatchMappings()
        {
            var messages = new List<MessageModel>();
            try
            {
                var db = Factory.GetDatabase("core");
                var editItem = db.GetItem("{C7E0C875-E351-4359-932D-9806D377779A}");
                var editMappingItem = ClientHost.Factory.GetDataSourceItem(editItem);

                var runItem = db.GetItem("{E218E2FA-1911-47DB-8479-91851BAACEB6}");
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
                                    EditLabel = "Edit",
                                    RunLabel = "Run",
                                    RunLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(runMappingItem), "mapping", f),
                                    EditLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(editMappingItem), "mapping", f),
                                    LastRun = HistoryLogging.GetLatestRunDateString(f),
                                    NumberOfItemsProcessed = HistoryLogging.GetNumberOfItemsProcessed(f),
                                    DeleteLabel = "Delete",
                                    DeleteLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(editMappingItem), "mapping", f)
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
        [HttpGet, HttpPost]
        public object BatchNonScMappings()
        {
            var messages = new List<MessageModel>();
            try
            {
                var db = Factory.GetDatabase("core");
                var editItem = db.GetItem("{9F6CE075-50B0-4488-9F71-FE18463EB47F}");
                var editMappingItem = ClientHost.Factory.GetDataSourceItem(editItem);

                var runItem = db.GetItem("{58EE9324-03FC-4BAF-99B5-C83E94F8601A}");
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
                                    EditLabel = "Edit",
                                    RunLabel = "Run",
                                    RunLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(runMappingItem), "mapping", f),
                                    EditLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(editMappingItem), "mapping", f),
                                    LastRun = HistoryLogging.GetLatestRunDateString(f),
                                    NumberOfItemsProcessed = HistoryLogging.GetNumberOfItemsProcessed(f),
                                    DeleteLabel = "Delete",
                                    DeleteLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(editMappingItem), "mapping", f)
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

    }
}
