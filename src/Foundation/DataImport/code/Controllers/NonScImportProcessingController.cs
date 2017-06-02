using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Newtonsoft.Json;
using XC.DataImport.Repositories.Repositories;
using XC.DataImport.Repositories.Migration;
using XC.Foundation.DataImport.Models;
using XC.DataImport.Repositories.History;
using Sitecore.Data;
using XC.Foundation.DataImport.Diagnostics;

namespace XC.Foundation.DataImport.Controllers
{
    public class NonScImportProcessingController : BaseImportController
    {
        delegate string ProcessTask(string id);

        /// <summary>
        /// Imports the specified mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <param name="taskId"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult StartImport(string mapping, string taskId)
        {
            var statusFileName = HistoryLogging.GetStatusFilePath(mapping);
            WriteStatus("<h1>Import Started</h1>", statusFileName);
            WriteStatus(mapping, statusFileName);

            if (mapping != null)
            {
                try
                {
                    var mappingContent = System.IO.File.ReadAllText(mapping);
                    var mappingObject = (NonSitecoreMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(NonSitecoreMappingModel));
                    if (mappingObject != null)
                    {
                        var updateQueue = new Dictionary<ID, IDictionary<ID, string>>();

                        var _importManager =
                            new NonSitecoreImportManager()
                            {
                                SourceRepository = new NonSitecoreDatabaseRepository(mappingObject.Databases.Source, mappingObject, true, null, false),
                                TargetRepository = new NonSitecoreDatabaseRepository(mappingObject.Databases.Target, mappingObject, true, null, false),
                                Mapping = mappingObject
                            };

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
            return Redirect(HistoryLogging.GetRelativePath(statusFileName));
        }
    }
}
