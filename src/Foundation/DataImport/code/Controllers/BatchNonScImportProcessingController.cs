using System;
using System.Web.Mvc;
using Newtonsoft.Json;
using XC.DataImport.Repositories.Migration;
using XC.Foundation.DataImport.Models;
using XC.DataImport.Repositories.History;
using XC.Foundation.DataImport.Diagnostics;

namespace XC.Foundation.DataImport.Controllers
{
    public class BatchNonScImportProcessingController : BaseImportController
    {
        delegate string ProcessTask(string id);
        BatchNonSitecoreImportManager _importManager = new BatchNonSitecoreImportManager();

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
            WriteStatus(Messages.ImportStarted, statusFileName);
            WriteStatus(mapping, statusFileName);

            if (mapping == null)
                return PartialView("ResponseWrite", Messages.MappingIsNull);

            try
            {
                var mappingContent = System.IO.File.ReadAllText(mapping);
                var mappingObject = (BatchMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(BatchMappingModel));
                if (mappingObject == null)
                {
                    WriteStatus(Messages.MappingObjectIsNull, statusFileName);
                }

                _importManager =
                    new BatchNonSitecoreImportManager
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
