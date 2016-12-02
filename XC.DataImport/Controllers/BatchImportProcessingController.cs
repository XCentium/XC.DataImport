using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using XC.DataImport.Repositories.Repositories;
using XC.DataImport.Repositories.Diagnostics;
using XC.DataImport.Repositories.Migration;
using XC.DataImport.Repositories.Models;
using XC.DataImport.Repositories.History;

namespace XC.DataImport.Controllers
{
    public class BatchImportProcessingController : ImportController
    {
        delegate string ProcessTask(string id);
        BatchSitecoreImportManager _importManager = new BatchSitecoreImportManager();

        /// <summary>
        /// Starts the batch import.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <param name="taskId">The task identifier.</param>
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
                var mappingObject = (BatchMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(BatchMappingModel));
                if (mappingObject == null)
                {
                    WriteStatus("MappingObject is null", statusFileName);
                }

                _importManager =
                    new BatchSitecoreImportManager
                    {
                        Mapping = mappingObject
                    };

                _importManager.StartJob(mappingObject.Name, WriteStatus, statusFileName);
                Sitecore.Caching.CacheManager.ClearAllCaches();
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return Redirect(HistoryLogging.GetRelativePath(statusFileName));
        }
        /// <summary>
        /// Ends the long running process.
        /// </summary>
        /// <param name="result">The result.</param>
        public void EndLongRunningProcess(IAsyncResult result)
        {
            var processTask = (ProcessTask)result.AsyncState;
            var id = processTask.EndInvoke(result);
            if (_importManager != null)
            {
                _importManager.Remove(id);
            }
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
