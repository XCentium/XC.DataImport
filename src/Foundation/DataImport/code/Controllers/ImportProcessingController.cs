using System;
using System.Web.Mvc;
using Newtonsoft.Json;
using XC.DataImport.Repositories.Repositories;
using XC.DataImport.Repositories.Migration;
using XC.Foundation.DataImport.Models;
using XC.DataImport.Repositories.History;
using XC.Foundation.DataImport.Diagnostics;

namespace XC.Foundation.DataImport.Controllers
{
    public class ImportProcessingController : BaseImportController
    {
        SitecoreImportManager _importManager = new SitecoreImportManager();

        /// <summary>
        /// Imports the specified mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <param name="taskId"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult StartImport(string mapping, string taskId)
        {
            Response.Buffer = false;
            var statusFileName = HistoryLogging.GetStatusFilePath(mapping);
            WriteStatus("<h1>Import Started</h1>", statusFileName);
            WriteStatus(mapping, statusFileName);
            
            if (mapping == null)
                return PartialView("ResponseWrite", "Mapping is null");

            try
            {
                var mappingContent = System.IO.File.ReadAllText(mapping);
                var mappingObject = (MappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(MappingModel));
                if (mappingObject == null)
                {
                    WriteStatus("MappingObject is null", statusFileName);
                }

                _importManager =
                    new SitecoreImportManager
                    {
                        SourceRepository = new SitecoreDatabaseRepository(mappingObject.Databases.Source, mappingObject),
                        TargetRepository = new SitecoreDatabaseRepository(mappingObject.Databases.Target, mappingObject),
                        Mapping = mappingObject
                    };

                _importManager.StartJob(mappingObject.Name, WriteStatus, statusFileName);
                Sitecore.Caching.CacheManager.ClearAllCaches();
            }
            catch (Exception ex)
            {
                WriteStatus(string.Format("<span style=\"color:red\">{0}</span>", ex.Message), statusFileName);
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return Redirect(HistoryLogging.GetRelativePath(statusFileName));
        }

        /// <summary>
        /// Gets the current progress.
        /// </summary>
        /// <param name="taskId"></param>
        public JsonResult GetStatus(string taskId)
        {
            ControllerContext.HttpContext.Response.AddHeader("cache-control", "no-cache");
            var messages = "";
            if (_importManager == null) 
                messages = "error occured";
            var currentProgress = _importManager.GetStatus(taskId);
            return Json(new
            {
                percentage = currentProgress.Item1, 
                items = currentProgress.Item2,
                messages
            }, JsonRequestBehavior.AllowGet);
        }

    }
}
