using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using XC.DataImport.Repositories.Databases;
using XC.DataImport.Repositories.Diagnostics;
using XC.DataImport.Repositories.Migration;
using XC.DataImport.Repositories.Models;

namespace XC.DataImport.Controllers
{
    public class ImportProcessingController : Controller
    {
        delegate string ProcessTask(string id);
        ImportManager _importManager = new ImportManager();

        /// <summary>
        /// Imports the specified mapping.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <param name="taskId"></param>
        /// <returns></returns>
        [HttpGet]
        public void StartImport(string mapping, string taskId)
        {
            if (mapping == null)
                return;

            try
            {
                var mappingContent = System.IO.File.ReadAllText(mapping);
                var mappingObject = (MappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(MappingModel));
                if (mappingObject == null) return;

                _importManager =
                    new ImportManager
                    {
                        SourceRepository = new SitecoreDatabaseRepository(mappingObject.Databases.Source, mappingObject),
                        TargetRepository = new SitecoreDatabaseRepository(mappingObject.Databases.Target, mappingObject),
                        Mapping = mappingObject
                    };

                _importManager.Add(taskId);
                var processTask = new ProcessTask(_importManager.Run);
                processTask.BeginInvoke(taskId, EndLongRunningProcess, processTask);

            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
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
