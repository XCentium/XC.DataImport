using Sitecore.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using XC.DataImport.Repositories.History;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models.Mappings;
using XC.Foundation.DataImport.Repositories.FileSystem;

namespace XC.Foundation.DataImport.Repositories.Migration
{
    public class BatchImportManager
    {
        private ImportBatchMappingModel _mapping { get; set; }
        private IMappingRepository _mappingRepository;

        public BatchImportManager(ImportBatchMappingModel model, IMappingRepository mappingRepository)
        {
            _mapping = model;
            _mappingRepository = mappingRepository;
        }

        public BatchImportManager(ImportBatchMappingModel model)
        {
            _mapping = model;
            _mappingRepository = new MappingRepository();
        }

        public void Run(string id, Action<string, string> statusMethod, string statusFilepath)
        {
            try
            {
                if (_mapping == null || _mapping.Mappings == null)
                {
                    statusMethod(" <span style=\"color:red\">[FAILURE] Mapping is null</span>", statusFilepath);
                    return;
                }

                var startDate = DateTime.Now;
                HistoryLogging.ImportInitialized(HistoryLogging.GetMappingFileName(id), startDate);
                
                if (_mapping.RunInParallel)
                {
                    Parallel.ForEach(_mapping.Mappings, mapping =>
                    {
                        RunMappingEntry(statusMethod, statusFilepath, mapping);
                    });                    
                }
                else
                {
                    foreach(var mapping in _mapping.Mappings)
                    {
                        RunMappingEntry(statusMethod, statusFilepath, mapping);
                    }
                }                
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} ({1}) ({2})</span>", ex.Message, ex.StackTrace, _mapping.Name), statusFilepath);
                throw;
            }
            return;
        }

        private void RunMappingEntry(Action<string, string> statusMethod, string statusFilepath, Models.Entities.MappingReference mapping)
        {
            statusMethod(string.Format(" <span style=\"color:green\">[SUCCESS] Starting '{0}' mapping import ({1})</span>", mapping.Name, mapping.Id), statusFilepath);
            var mappingModel = _mappingRepository.RetrieveMappingModel<ImportMappingModel>(mapping.Id.ToString()) as ImportMappingModel;
            if (mappingModel == null)
            {
                DataImportLogger.Log.Error(string.Format("mapping model is null for {0}", mapping.Name));
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] mapping model is null for {0}</span>", _mapping.Name), statusFilepath);
            }
            var importManager = new ImportManager(mappingModel);
            importManager.Run(mapping.Name, statusMethod, statusFilepath);
            statusMethod(" <span style=\"color:green\">[SUCCESS] BATCH IMPORT IS DONE</span>", statusFilepath);
        }

        public void StartJob(string id, Action<string, string> statusMethod, string statusFileName)
        {
            JobOptions options = new JobOptions(id, "BatchImportManager", Sitecore.Context.Site.Name, this, "Run", new object[] { id, statusMethod, statusFileName })
            {
                Priority = System.Threading.ThreadPriority.Lowest,
                ExecuteInManagedThreadPool = true,
                EnableSecurity = false,
                WriteToLog = true,
                AtomicExecution = true,
            };
            JobManager.Start(options);
        }
    }
}