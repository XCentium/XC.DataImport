using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data.Items;
using XC.DataImport.Repositories.Repositories;
using XC.Foundation.DataImport.Models;
using XC.DataImport.Repositories.History;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Sitecore.Jobs;
using Sitecore;
using Sitecore.Data;
using Sitecore.SecurityModel;
using Sitecore.Configuration;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Configurations;

namespace XC.DataImport.Repositories.Migration
{
    public class BatchNonSitecoreImportManagerGrouped : XC.DataImport.Repositories.Migration.IBatchSitecoreImportManager
    {
        public bool FlushEnabled { get; set; }
        public IBatchMappingModel Mapping { get; set; }
        private static readonly object SyncRoot = new object();
        private static IDictionary<string, Tuple<int,string>> ProcessStatus { get; set; }
        private IEnumerable<string> MappingsToRun { get; set; }
        public bool? IncrementalUpdate { get; set; }
        public Item CurrentItem { get; set; }
        public DateTime? LastUpdated { get; set; }
        public Tuple<string,string> Filter { get; set; }

        public BatchNonSitecoreImportManagerGrouped()
        {
            if (ProcessStatus == null)
            {
                ProcessStatus = new Dictionary<string, Tuple<int, string>>();
            }
            FlushEnabled = true;
        }
        public void Run(string id, Action<string, string> statusMethod, string statusFilename)
        {
            try
            {
                if (Mapping.Files == null) return;

                var startDate = DateTime.Now;
                var mappingName = "batchnonsc-" + Mapping.Name;
                HistoryLogging.ImportInitialized(mappingName, startDate);

                DataImportLogger.Log.Info("XC.DataImport - Total import file count: " + Mapping.Files.Count());
                statusMethod(string.Format("[INFO] Total import file count: {0} </span>", Mapping.Files.Count()), statusFilename);

                var updatedItems = new Dictionary<ID, Dictionary<ID, string>>();

                Parallel.ForEach(Mapping.Files, file => {
                    updatedItems = ProcessMapping(id, statusMethod, statusFilename, file, ref updatedItems);
                });

                statusMethod(string.Format("[INFO] Update Data: {0} </span>", updatedItems.Count), statusFilename);
                DataImportLogger.Log.Info(string.Format("[INFO] Update Data: {0} </span>", updatedItems.Count));

                UpdateSitecoreItems(updatedItems, statusMethod, statusFilename);

                var job = JobManager.GetJob(id);
                if (job != null)
                {
                    job.Status.State = JobState.Finished;
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
        }

        /// <summary>
        /// Processes the mapping.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilename">The status filename.</param>
        /// <param name="file">The file.</param>
        /// <param name="updatedItems">The updated items.</param>
        /// <returns></returns>
        private Dictionary<ID, Dictionary<ID, string>> ProcessMapping(string id, Action<string, string> statusMethod, string statusFilename, string file, ref Dictionary<ID, Dictionary<ID, string>> updatedItems)
        {
            var filePath = Path.Combine(DataImportConfigurations.NonSitecoreMappingFolder, file);
            statusMethod(string.Format("[INFO] Mapping file: {0} </span>", filePath), statusFilename);
            DataImportLogger.Log.Info(string.Format("[INFO] Mapping file: {0} </span>", filePath));

            if (File.Exists(filePath))
            {
                var mappingContent = File.ReadAllText(filePath);
                var mappingObject = (NonSitecoreMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(NonSitecoreMappingModel));

                statusMethod(string.Format("[INFO] Mapping: {0} </span>", mappingObject.Name), statusFilename);
                DataImportLogger.Log.Info(string.Format("[INFO] Mapping: {0} </span>", mappingObject.Name));

                var individualManager = new NonSitecoreImportManagerGrouped
                {
                    SourceRepository = new NonSitecoreDatabaseRepository(mappingObject.Databases.Source, mappingObject, false, LastUpdated, this.IncrementalUpdate),
                    TargetRepository = new NonSitecoreDatabaseRepositoryGrouped(mappingObject.Databases.Target, mappingObject, ref updatedItems, false, LastUpdated, this.IncrementalUpdate),
                    Mapping = mappingObject,
                    CurrentItem = CurrentItem,
                    UpdatedItems = updatedItems
                };
                individualManager.GatherUpdateData(id + file, statusMethod, statusFilename);
            }
            return updatedItems;
        }
        /// <summary>
        /// Runs the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public void Run3(string id, Action<string, string> statusMethod, string statusFilename)
        {
            try
            {
                if (Mapping.Files == null) return;

                var updateQueue = new Dictionary<ID, IDictionary<ID, string>>();

                var startDate = DateTime.Now;
                var mappingName = "batchnonsc-" + Mapping.Name;
                HistoryLogging.ImportInitialized(mappingName, startDate);

                DataImportLogger.Log.Info("XC.DataImport - Total import file count: " + Mapping.Files.Count());
                statusMethod(string.Format("<span>[INFO] Total import file count: {0} </span>", Mapping.Files.Count()), statusFilename);

                if (CurrentItem == null)
                {
                    var itemsToProcess = GetItemsToSync(statusMethod, statusFilename, CurrentItem);
                    if (itemsToProcess != null)
                    {
                        var count = 1;
                        foreach (var item in itemsToProcess)
                        {
                            DataImportLogger.Log.Info("XC.DataImport - Importing Item: " + item.Paths.FullPath);
                            statusMethod(string.Format("<h2>{1}. [INFO] Importing Item: {0} </h2>", item.Paths.FullPath, count), statusFilename);

                            ProcessMappingFiles(id, statusMethod, statusFilename, item);
                            count++;
                        }
                    }
                }
                else
                {
                    ProcessMappingFiles(id, statusMethod, statusFilename, CurrentItem);
                }     
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
        }

        /// <summary>
        /// Processes the mapping files.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilename">The status filename.</param>
        /// <param name="currentItem">The current item.</param>
        private void ProcessMappingFiles(string id, Action<string, string> statusMethod, string statusFilename, Item currentItem)
        {
            for (var i = 0; i < Mapping.Files.Count(); i++)
            {
                var filePath = Path.Combine(DataImportConfigurations.NonSitecoreMappingFolder, Mapping.Files[i]);

                statusMethod(string.Format("[INFO] Item: {0} </span>", currentItem.Paths.FullPath), statusFilename);

                statusMethod(string.Format("[INFO] Mapping file: {0} </span>", filePath), statusFilename);
                DataImportLogger.Log.Info(string.Format("[INFO] Mapping file: {0} </span>", filePath));

                if (File.Exists(filePath))
                {
                    var mappingContent = File.ReadAllText(filePath);
                    var mappingObject = (NonSitecoreMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(NonSitecoreMappingModel));

                    statusMethod(string.Format("[INFO] Mapping: {0} </span>", mappingObject.Name), statusFilename);
                    DataImportLogger.Log.Info(string.Format("[INFO] Mapping: {0} </span>", mappingObject.Name));

                    var individualManager = new NonSitecoreImportManager()
                    {
                        SourceRepository = new NonSitecoreDatabaseRepository(mappingObject.Databases.Source, mappingObject, true, LastUpdated, this.IncrementalUpdate),
                        TargetRepository = new NonSitecoreDatabaseRepository(mappingObject.Databases.Target, mappingObject, true, LastUpdated, this.IncrementalUpdate),
                        Mapping = mappingObject,
                        CurrentItem = currentItem,
                        BatchUpdate = true
                    };
                    individualManager.TargetRepository.ClearMultilistFieldValues(statusMethod, statusFilename, currentItem);
                    individualManager.StartJob(id + Mapping.Files.ElementAt(i) + currentItem.ID, statusMethod, statusFilename);
                }
            }
        }

        /// <summary>
        /// Gets the items to synchronize.
        /// </summary>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilename">The status filename.</param>
        /// <param name="currentItem">The current item.</param>
        /// <returns></returns>
        private List<Item> GetItemsToSync(Action<string, string> statusMethod, string statusFilename, Item currentItem)
        {
            var items = new List<Item>();
            for (var i = 0; i < Mapping.Files.Count(); i++)
            {
                var filePath = Path.Combine(DataImportConfigurations.NonSitecoreMappingFolder, Mapping.Files[i]);
                //statusMethod(string.Format("[INFO] Mapping file: {0} </span>", filePath), statusFilename);
                //DataImportLogger.Log.Info(string.Format("[INFO] Mapping file: {0} </span>", filePath));

                if (File.Exists(filePath))
                {
                    var mappingContent = File.ReadAllText(filePath);
                    var mappingObject = (NonSitecoreMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(NonSitecoreMappingModel));

                    //statusMethod(string.Format("[INFO] Mapping: {0} </span>", mappingObject.Name), statusFilename);
                    //DataImportLogger.Log.Info(string.Format("[INFO] Mapping: {0} </span>", mappingObject.Name));

                    var individualManager = new NonSitecoreImportManager()
                    {
                        SourceRepository = new NonSitecoreDatabaseRepository(mappingObject.Databases.Source, mappingObject, true, LastUpdated, this.IncrementalUpdate),
                        TargetRepository = new NonSitecoreDatabaseRepository(mappingObject.Databases.Target, mappingObject, true, LastUpdated, this.IncrementalUpdate),
                        Mapping = mappingObject,
                        CurrentItem = currentItem,
                        BatchUpdate = true
                    };

                    var toProcess = individualManager.GetItemsToSync(statusMethod, statusFilename);
                    if (toProcess != null)
                    {
                        foreach (var item in toProcess)
                        {
                            if (!items.Select(it=>it.ID).Contains(item.ID))
                            {
                                items.Add(item);
                            }
                        }
                    }
                }
            }
            return items;
        }

        /// <summary>
        /// Runs from ce.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilename">The status filename.</param>
        /// <returns></returns>
        public void RunFromCE(string id, Action<string, string> statusMethod, string statusFilename)
        {
            try
            {
                if (Mapping.Files == null) return;

                var updateQueue = new Dictionary<ID, IDictionary<ID,string>>();

                var startDate = DateTime.Now;
                var mappingName = "batchnonsc-" + Mapping.Name;
                HistoryLogging.ImportInitialized(mappingName, startDate);

                DataImportLogger.Log.Info("XC.DataImport - Total import file count: " + Mapping.Files.Count());
                statusMethod(string.Format("[INFO] Total mappings to sync: {0}", Mapping.Files.Count()), statusFilename);

                var updatedItems = new Dictionary<ID, Dictionary<ID, string>>();

                for (var i = 0; i < Mapping.Files.Count(); i++)
                {
                    updatedItems = ProcessMapping(id, statusMethod, statusFilename, Mapping.Files[i], ref updatedItems);
                }

                var job = JobManager.GetJob(id);
                if (job != null)
                {
                    job.Status.State = JobState.Finished;
                }
                statusMethod(string.Format("[INFO] Update Data: {0} </span>", updatedItems.Count), statusFilename);
                DataImportLogger.Log.Info(string.Format("[INFO] Update Data: {0} </span>", updatedItems.Count));

                UpdateSitecoreItems(updatedItems, statusMethod, statusFilename);
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            statusMethod(string.Format(" <span style=\"color:blue\">[INFO] {0} DONE", id), statusFilename);
        }

        private static void UpdateSitecoreItems(Dictionary<ID, Dictionary<ID, string>> updatedItems, Action<string, string> statusMethod, string statusFilename)
        {
            if (updatedItems.Any())
            {
                var database = Factory.GetDatabase("master");
                if (database != null)
                {
                    //update items
                    statusMethod("[INFO] Updating Sitecore items with gathered data", statusFilename);
                    DataImportLogger.Log.Info("[INFO] Updating Sitecore items with gathered data");

                    foreach (var itemId in updatedItems)
                    {
                        using (new SecurityDisabler())
                        {
                            if (itemId.Key != ID.Null && itemId.Value != null && itemId.Value.Any())
                            {
                                var item = database.GetItem(itemId.Key);
                                using (new EditContext(item,false,true))
                                {
                                    foreach (var fld in itemId.Value)
                                    {
                                        item[fld.Key.ToString()] = fld.Value;

                                        statusMethod(string.Format("[INFO] Update Item: {0}, field: {1} </span>", itemId.Key, fld.Key), statusFilename);
                                        DataImportLogger.Log.Info(string.Format("[INFO] Update Item: {0}, field: {1} </span>", itemId.Key, fld.Key));
                                    }
                                }
                            }
                        }
                    }
                    ClearCache(database);
                }
            }
        }

        private static void ClearCache(Database database)
        {
            database.Caches.DataCache.Clear();
            database.Caches.ItemCache.Clear();
            database.Caches.ItemPathsCache.Clear();
            database.Caches.StandardValuesCache.Clear();

            Sitecore.Caching.CacheManager.ClearAllCaches();
        }
        /// <summary>
        /// Adds the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        public void Add(string id, Action<string, string> statusMethod, string statusFilename)
        {
            lock (SyncRoot)
            {
                if (!ProcessStatus.ContainsKey(id))
                {
                    ProcessStatus.Add(id, new Tuple<int, string>(0, ""));
                }
            }
        }

        /// <summary>
        /// Removes the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        public void Remove(string id)
        {
            lock (SyncRoot)
            {
                ProcessStatus.Remove(id);
            }
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <param name="id">The id.</param>
        public Tuple<int, string> GetStatus(string id)
        {
            lock (SyncRoot)
            {
                return ProcessStatus.Keys.Count(x => x == id) == 1 ? ProcessStatus[id] : new Tuple<int, string>(100,"");
            }
        }


        /// <summary>
        /// Starts the job.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFileName">Name of the status file.</param>
        public void StartJob(string id, Action<string, string> statusMethod, string statusFileName)
        {
            JobOptions options = new JobOptions(id, "BatchNonSitecoreImportManager", Context.Site.Name, this, "Run2", new object[] { id, statusMethod, statusFileName })
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
