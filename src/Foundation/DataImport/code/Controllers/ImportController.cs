using Newtonsoft.Json;
using Sitecore.Data;
using Sitecore.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using XC.DataImport.Repositories.History;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models.Mappings;
using XC.Foundation.DataImport.Repositories.FileSystem;
using XC.Foundation.DataImport.Repositories.Migration;
using XC.Foundation.DataImport.Utilities;

namespace XC.Foundation.DataImport.Controllers
{
    [ServicesController("dataimport/run")]
    [Authorize]
    public class ImportController : BaseImportController
    {
        private IFileSystemRepository _fileSystemRepository;
        private IMappingRepository _mappingRepository;

        public ImportController() : base()
        {
            _fileSystemRepository = new FileSystemRepository();
            _mappingRepository = new MappingRepository();
        }

        public ImportController(IFileSystemRepository fileSystemRepository) : base()
        {
            _fileSystemRepository = fileSystemRepository;
            _mappingRepository = new MappingRepository();
        }

        public ImportController(IFileSystemRepository fileSystemRepository, IMappingRepository mappingRepository) : base()
        {
            _fileSystemRepository = fileSystemRepository;
            _mappingRepository = mappingRepository;
        }
        /// <summary>
        /// Imports the specified mapping.
        /// </summary>
        /// <param name="id">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult StartImport(string id)
        {
            var statusFileName = HistoryLogging.RefreshStatusFilePath(id);
            WriteStatus("<h1>Import Started</h1>", statusFileName);
            WriteStatus(id, statusFileName);

            if (id != null)
            {
                try
                {
                    ImportMappingModel mappingObject = _mappingRepository.RetrieveMappingModel<ImportMappingModel>(id) as ImportMappingModel;
                    if (mappingObject != null)
                    {
                        var updateQueue = new Dictionary<ID, IDictionary<ID, string>>();

                        var _importManager = new ImportManager(mappingObject);

                        _importManager.StartJob(mappingObject.Name, WriteStatus, statusFileName);
                        Sitecore.Caching.CacheManager.ClearAllCaches();
                    }
                }
                catch (Exception ex)
                {
                    WriteStatus(string.Format("<span style=\"color:red\">[FAILURE] {0}</span>", ex.Message), statusFileName);
                    WriteStatus("<span style=\"color:red\">[DONE]</span>", statusFileName);
                    DataImportLogger.Log.Error(ex.Message, ex);
                }
            }
            return Ok(string.Format("< h1>Import Started</h1>", statusFileName));            
        }

        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetImportResults(string id)
        {
            var statusFileName = HistoryLogging.GetStatusFilePath(id);
            if (System.IO.File.Exists(statusFileName))
            {
                try
                {
                    return Ok(System.IO.File.ReadAllText(statusFileName));
                }
                catch
                {
                    return Ok(string.Empty);
                }
            }
            return Ok(string.Empty);
        }

        /// <summary>
        /// Imports the specified mapping.
        /// </summary>
        /// <param name="id">The mapping.</param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult StartBatchImport(string id)
        {
            var statusFileName = HistoryLogging.RefreshStatusFilePath(id);
            WriteStatus("<h1>Import Started</h1>", statusFileName);
            WriteStatus(id, statusFileName);

            if (id != null)
            {
                try
                {
                    var mappingObject = _mappingRepository.RetrieveMappingModel<ImportBatchMappingModel>(id) as ImportBatchMappingModel;
                    if (mappingObject != null)
                    {
                        var updateQueue = new Dictionary<ID, IDictionary<ID, string>>();
                        var _importManager = new BatchImportManager(mappingObject);
                        _importManager.StartJob(mappingObject.Name, WriteStatus, statusFileName);
                        Sitecore.Caching.CacheManager.ClearAllCaches();
                    }
                }
                catch (Exception ex)
                {
                    WriteStatus(string.Format("<span style=\"color:red\">[FAILURE] {0}</span>", ex.Message), statusFileName);
                    WriteStatus("<span style=\"color:red\">[DONE]</span>", statusFileName);
                    DataImportLogger.Log.Error(ex.Message, ex);
                }
            }
            return Ok(string.Format("< h1>Import Started</h1>", statusFileName));
        }
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        [HttpGet, HttpPost]
        public System.Web.Http.IHttpActionResult GetBatchImportResults(string id)
        {
            var statusFileName = HistoryLogging.GetStatusFilePath(id);
            if (System.IO.File.Exists(statusFileName))
            {
                try
                {
                    return Ok(System.IO.File.ReadAllText(statusFileName));
                }
                catch
                {
                    return Ok(string.Empty);
                }
            }
            return Ok(string.Empty);
        }
    }
}