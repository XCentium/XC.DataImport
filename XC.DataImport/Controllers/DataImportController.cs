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
using XC.DataImport.Repositories.Databases;
using XC.DataImport.Repositories.Diagnostics;
using XC.DataImport.Repositories.Migration;
using XC.DataImport.Repositories.Models;

namespace XC.DataImport.Controllers
{
    [ServicesController("speak.dataimport/databases")]
    public class DataImportController : ServicesApiController
    {
        /// <summary>
        /// Alls this instance.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
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
        [HttpGet]
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

            var dbTemplates = db.Templates.GetTemplates(Sitecore.Context.Language).Where(t => !t.InnerItem.Name.Contains("__") && !t.InnerItem.Paths.Path.Contains("System")).OrderBy(t => t.Name);
            if (dbTemplates.Any())
            {
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
        [HttpGet]
        public object Fields(string item, string database)
        {
            var fields = new List<TemplateFieldEntity>();
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
        [HttpGet]
        public object SaveMapping(string mapping)
        {
            if (mapping == null)
            {
                return new
                {
                    data = new List<string>(),
                    messages = ""
                };
            }

            try
            {
                var mappingObject = (MappingModel)JsonConvert.DeserializeObject(mapping, typeof(MappingModel));

                if (mappingObject != null)
                {
                    var fileName = mappingObject.Name + ".json";
                    var filePath = Path.Combine(MappingFolder, fileName);
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

                return new
                {
                    data = mappingObject,
                    messages = "Saved"
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }

            return new
            {
                data = new List<string>(),
                messages = ""
            };
        }

        /// <summary>
        /// Mappings the specified mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        [HttpGet]
        public object ViewMapping(string mapping)
        {
            if (mapping == null)
            {
                return new
                {
                    data = new List<string>(),
                    messages = "mapping is null"
                };
            }

            try
            {
                var mappingContent = File.ReadAllText(mapping);
                var mappingObject = (MappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(MappingModel));

                return new
                {
                    data = mappingObject,
                    messages = ""
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }

            return new
            {
                data = new List<string>(),
                messages = "An error occured"
            };
        }

        /// <summary>
        /// Mappingses this instance.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public object Mappings()
        {
            try
            {
                var db = Factory.GetDatabase("core");
                var editItem = db.GetItem("{F683E74B-35C1-4C16-9168-8DD1A989640A}");
                var editMappingItem = ClientHost.Factory.GetDataSourceItem(editItem);

                var runItem = db.GetItem("{9539E038-208E-4C03-8C9F-AFF72CF1DCD7}");
                var runMappingItem = ClientHost.Factory.GetDataSourceItem(runItem);

                var files =
                    Directory.GetFiles(MappingFolder, "*.json")
                        .Select(
                            f =>
                                new MappingInfo
                                {
                                    Name = Path.GetFileNameWithoutExtension(f),
                                    Path = f,
                                    EditLabel = "Edit",
                                    RunLabel = "Run",
                                    RunLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(runMappingItem), "mapping", f),
                                    EditLink = WebUtil.AddQueryString(ClientHost.Links.GetItemUrl(editMappingItem), "mapping", f)
                                });

                return new
                {
                    data = files,
                    messages = ""
                };
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }

            return new
            {
                data = new List<string>(),
                messages = "An error occured"
            };
        }

        /// <summary>
        /// Gets the get folder path.
        /// </summary>
        /// <value>
        /// The get folder path.
        /// </value>
        private string GetFolderPath
        {
            get
            {
                var path = Path.Combine(Sitecore.Configuration.Settings.DataFolder, "XC.DataImport");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        private string MappingFolder
        {
            get
            {
                var path = Path.Combine(GetFolderPath, "mappings");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }
        private string HistoryFolder
        {
            get
            {
                var path = Path.Combine(GetFolderPath, "history");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

    }
}
