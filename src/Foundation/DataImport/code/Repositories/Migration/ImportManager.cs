using Newtonsoft.Json;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Jobs;
using Sitecore.Pipelines;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using XC.DataImport.Repositories.History;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Models.Entities;
using XC.Foundation.DataImport.Models.Mappings;
using XC.Foundation.DataImport.Pipelines.PostProcessing;
using XC.Foundation.DataImport.Pipelines.SourceProcessing;
using XC.Foundation.DataImport.Repositories.DataSources;

namespace XC.Foundation.DataImport.Repositories.Migration
{
    public class ImportManager
    {
        private ImportMappingModel Mapping { get; set; }
        public List<ImportDataItem> Items2Import;
        public ITargetRepository TargetRepository;

        public ImportManager(ImportMappingModel model)
        {
            Mapping = model;
            TargetRepository = CreateTargetRepository(model);
        }

        /// <summary>
        /// Creates the target repository.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        private ITargetRepository CreateTargetRepository(ImportMappingModel model)
        {
            try
            {
                var targetDatasourceType = model?.TargetType?.DataSourceType;
                if (!string.IsNullOrEmpty(targetDatasourceType))
                {
                    return (ITargetRepository)Activator.CreateInstance(Type.GetType(targetDatasourceType), model);
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);                
            }
            return null;
        }

        /// <summary>
        /// Gathers the source data.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        public void GatherSourceData(string id, Action<string, string> statusMethod, string statusFilepath)
        {
            DataImportLogger.Log.Info("XC.DataImport - Mapping: " + Mapping.Name + " gathering data ");
            statusMethod($" <span style=\"color:blue\">[INFO] gathering data: {Mapping.Name} </span>", statusFilepath);

            try
            {
                if (Mapping == null || Mapping.Source == null || Mapping.Target == null)
                {
                    statusMethod(" <span style=\"color:red\">[FAILURE] Mapping is null</span>", statusFilepath);
                    return;
                }

                var startDate = DateTime.Now;
                HistoryLogging.ImportInitialized(HistoryLogging.GetMappingFileName(id), startDate);

                var sourceDatasource = CreateDatasource(Mapping?.SourceType.DataSourceType, ConvertToDatasourceModel(Mapping.Source, Mapping.SourceType), statusMethod, statusFilepath);
                if (sourceDatasource == null)
                {
                    statusMethod(" <span style=\"color:red\">[FAILURE] Source DataSource creation failed</span>", statusFilepath);
                    return;
                }

                using (new SecurityDisabler())
                {
                    //using (new DatabaseCacheDisabler())
                    //using (new EventDisabler())
                    {
                        var source = sourceDatasource.GetSource(statusMethod, statusFilepath);
                        Items2Import = ProcessSource(source, Mapping.SourceProcessingScripts, Mapping.FieldMappings, statusMethod, statusFilepath);
                        if (Items2Import == null)
                        {
                            statusMethod(
                                $" <span style=\"color:blue\">[FAILURE] Couldn't produce items to import ({Mapping.Name})</span>", statusFilepath);
                            return;
                        }

                        DataImportLogger.Log.Info("XC.DataImport - Mapping: " + Mapping.Name + " Total import count: " + Items2Import.Count);
                        statusMethod(
                            $" <span style=\"color:blue\">[INFO] Total import count: {Items2Import.Count} ({Mapping.Name})</span>", statusFilepath);
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(
                    $" <span style=\"color:red\">[FAILURE] {ex.Message} ({ex.StackTrace}) ({Mapping.Name})</span>", statusFilepath);
                throw;
            }
            statusMethod($" <span style=\"color:blue\">[INFO] {id} Source data has been gathered", statusFilepath);
            return;
        }

        /// <summary>
        /// Converts to datasource model.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourceType">Type of the source.</param>
        /// <returns></returns>
        public static IDataSourceModel ConvertToDatasourceModel(dynamic source, SourceType sourceType)
        {
            return Convert.ChangeType(JsonConvert.DeserializeObject(source.ToString(), Type.GetType(sourceType.ModelType)),Type.GetType(sourceType.ModelType));
        }

        /// <summary>
        /// Starts the job.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFileName"></param>
        public void StartJob(string id, Action<string, string> statusMethod, string statusFileName)
        {
            JobOptions options = new JobOptions(id, "ImportManager", Sitecore.Context.Site.Name, this, "Run", new object[] { id, statusMethod, statusFileName })
            {
                Priority = System.Threading.ThreadPriority.Lowest,
                ExecuteInManagedThreadPool = true,
                EnableSecurity = false,
                WriteToLog = true,
                AtomicExecution = true,
            };
            JobManager.Start(options);
        }

        /// <summary>
        /// Runs the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilename">The status filename.</param>
        public void Run(string id, Action<string, string> statusMethod, string statusFilename)
        {
            GatherSourceData(id, statusMethod, statusFilename);
            ImportSourceItems(id,statusMethod, statusFilename);
        }

        /// <summary>
        /// Imports the source items.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void ImportSourceItems(string id, Action<string, string> statusMethod, string statusFilepath)
        {
            statusMethod($" <span style=\"color:blue\">[INFO] importing data: {Mapping.Name} </span>", statusFilepath);
            DataImportLogger.Log.Info("XC.DataImport - Mapping: " + Mapping.Name + " importing data ");

            try
            {
                if (Mapping == null || Mapping.Source == null || Mapping.Target == null)
                {
                    statusMethod(" <span style=\"color:red\">[FAILURE] Mapping is null</span>", statusFilepath);
                    return;
                }

                using (new SecurityDisabler())
                {
                    using (new DatabaseCacheDisabler())
                    using (new EventDisabler())
                    {
                        if (Items2Import != null)
                        {
                            var migratedItems = new List<Item>();
                            for (var i = 0; i < Items2Import.Count; i++)
                            {
                                statusMethod($" <h4 style=\"color:blue\">[INFO] {i + 1}</h4>", statusFilepath);
                                var migratedItem = TargetRepository.ImportItem(Items2Import.ElementAt(i), i, statusMethod, statusFilepath);
                                migratedItems.Add(migratedItem);
                            }

                            if (Mapping.PostImportScripts != null && Mapping.PostImportScripts.Any())
                            {
                                statusMethod(" <h4 style=\"color:blue\">[INFO] Post Processing scripts starting</h4>", statusFilepath);
                                RunPostProcessingScripts(migratedItems, Mapping.PostImportScripts);
                                statusMethod(" <h4 style=\"color:blue\">[INFO] Post Processing scripts are done</h4>", statusFilepath);
                            }
                        }                        

                        //ClearCache();
                        statusMethod(" <h4 style=\"color:blue\">[INFO] Import is done</h4>", statusFilepath);
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(
                    $" <span style=\"color:red\">[FAILURE] {ex.Message} ({ex.StackTrace}) ({Mapping.Name})</span>", statusFilepath);
                throw;
            }
            statusMethod($" <span style=\"color:blue\">[INFO] {id} DONE", statusFilepath);
            return;
        }

        /// <summary>
        /// Runs the post processing scripts.
        /// </summary>
        /// <param name="migratedItems">The migrated items.</param>
        /// <param name="postProcessingScripts">The post processing scripts.</param>
        private void RunPostProcessingScripts(IEnumerable<Item> migratedItems, IEnumerable<string> postProcessingScripts)
        {
            var pipelineArgs = new ProcessingPipelineArgs(migratedItems, postProcessingScripts);
            CorePipeline.Run("xc.dataimport.postprocessing", pipelineArgs);
        }

        /// <summary>
        /// Creates the datasource.
        /// </summary>
        /// <param name="dataSourceType">Type of the data source.</param>
        /// <param name="model">The model.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        private IDataSource CreateDatasource(string dataSourceType, IDataSourceModel model, Action<string, string> statusMethod, string statusFilepath)
        {
            try
            {
                return (IDataSource)Activator.CreateInstance(Type.GetType(dataSourceType), model);
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(
                    $" <span style=\"color:red\">[FAILURE] {ex.Message} ({ex.StackTrace}) ({Mapping.Name})</span>", statusFilepath);
            }
            return null;
        }
        /// <summary>
        /// Processes the source.
        /// </summary>
        /// <param name="fileContent">Content of the file.</param>
        /// <param name="sourceProcessingScripts">The source processing scripts.</param>
        /// <param name="fieldMappings">The field mappings.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        public List<ImportDataItem> ProcessSource(object fileContent, IEnumerable<string> sourceProcessingScripts, ScFieldMapping[] fieldMappings, Action<string, string> statusMethod, string statusFilepath)
        {
            return RunSourceProcessingScripts(fileContent, fieldMappings, sourceProcessingScripts);
        }

        /// <summary>
        /// Runs the source processing scripts.
        /// </summary>
        /// <param name="fileContent">Content of the file.</param>
        /// <param name="fieldMappings">The field mappings.</param>
        /// <param name="sourceProcessingScripts">The source processing scripts.</param>
        /// <returns></returns>
        public List<ImportDataItem> RunSourceProcessingScripts(object fileContent, ScFieldMapping[] fieldMappings, IEnumerable<string> sourceProcessingScripts)
        {
            var pipelineArgs = new SourceProcessingPipelineArgs(fileContent, fieldMappings, sourceProcessingScripts, Mapping.Name);
            CorePipeline.Run("xc.dataimport.sourceprocessing", pipelineArgs);
            return pipelineArgs.Items2Import;
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        private void ClearCache()
        {
            TargetRepository.Database.Caches.DataCache.Clear();
            TargetRepository.Database.Caches.ItemCache.Clear();
            TargetRepository.Database.Caches.ItemPathsCache.Clear();
            TargetRepository.Database.Caches.StandardValuesCache.Clear();
            Sitecore.Caching.CacheManager.ClearAllCaches();
        }
    }
}