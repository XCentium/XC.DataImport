using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using XC.DataImport.Repositories.Repositories;
using XC.DataImport.Repositories.Diagnostics;
using XC.DataImport.Repositories.Migration;
using XC.DataImport.Repositories.Models;
using Sitecore.Jobs;
using Sitecore;
using Sitecore.IO;
using XC.DataImport.Repositories.History;
using Sitecore.Data;

namespace XC.DataImport.Controllers
{
    public class NonScImportProcessingController : ImportController
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
                                SourceRepository = new NonSitecoreDatabaseRepository(mappingObject.Databases.Source, mappingObject, true, false),
                                TargetRepository = new NonSitecoreDatabaseRepository(mappingObject.Databases.Target, mappingObject, true, false),
                                Mapping = mappingObject
                            };

                        _importManager.StartJob(mappingObject.Name, WriteStatus, statusFileName);
                        Sitecore.Caching.CacheManager.ClearAllCaches();
                    }
                }
                catch (Exception ex)
                {
                    WriteStatus(string.Format("<span style=\"color:red\">[FAILURE] {0}</span>", ex.Message), statusFileName);
                    DataImportLogger.Log.Error(ex.Message, ex);
                }
            }
            return Redirect(HistoryLogging.GetRelativePath(statusFileName));
        }
    }
}
