﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using XC.DataImport.Repositories.Repositories;
using XC.Foundation.DataImport.Models;
using XC.DataImport.Repositories.History;
using Sitecore.Jobs;
using Sitecore;
using Sitecore.Data;
using Sitecore.SecurityModel;
using Sitecore.Data.Events;
using Sitecore.ContentSearch.Maintenance;
using XC.Foundation.DataImport.Diagnostics;

namespace XC.DataImport.Repositories.Migration
{
    public class NonSitecoreImportManagerGrouped : XC.DataImport.Repositories.Migration.INonSitecoreImportManager
    {
        public INonSitecoreDatabaseRepository SourceRepository { get; set; }
        public INonSitecoreDatabaseRepository TargetRepository { get; set; }
        public IMapping Mapping { get; set; }
        private static readonly object SyncRoot = new object();
        private static IDictionary<string, Tuple<int,string>> ProcessStatus { get; set; }
        private int ItemsToImportCount { get; set; }
        public Item CurrentItem { get; set; }
        public Tuple<string, string, string> Filter { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool? IncrementalUpdate { get; set; }
        public bool BatchUpdate { get; set; }
        public Dictionary<ID, Dictionary<ID, string>> UpdatedItems;

        public NonSitecoreImportManagerGrouped()
        {
            if (ProcessStatus == null)
            {
                ProcessStatus = new Dictionary<string, Tuple<int, string>>();
            }
        }

        /// <summary>
        /// Gets the job item count.
        /// </summary>
        /// <returns></returns>
        public int GetJobItemCount(Action<string, string> statusMethod, string statusFilename)
        {
            if (SourceRepository == null || TargetRepository == null || Mapping.Paths == null || string.IsNullOrEmpty(Mapping.Paths.Target)) return 0;
            return SourceRepository.GetSourceItemsForImportCount(statusMethod, statusFilename);
        }

        /// <summary>
        /// Starts the job.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        public void StartJob(string id, Action<string, string> statusMethod, string statusFileName)
        {
            JobOptions options = new JobOptions(id, "NonSitecoreImportManager", Context.Site.Name, this, "Run", new object[] { id, statusMethod, statusFileName })
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
        /// <returns></returns>
        public void GatherUpdateData(string id, Action<string, string> statusMethod, string statusFilepath)
        {
            try
            {
                if (SourceRepository == null || TargetRepository == null || Mapping.Paths == null || string.IsNullOrEmpty(Mapping.Paths.Target))
                {
                    statusMethod(" <span style=\"color:red\">[FAILURE] Mapping.Paths.Target is null</span>", statusFilepath);
                    return;
                }

                var startDate = DateTime.Now;
                HistoryLogging.ImportInitialized("nonsitecore-" + Mapping.Name, startDate);

                var itemsToImport = SourceRepository.GetSourceItemsForImportCount(statusMethod, statusFilepath, Filter);
                if (itemsToImport == 0)
                {
                    statusMethod(string.Format(" <span style=\"color:blue\">[INFO] Nothing to import ({0})</span>", Mapping.Name), statusFilepath);
                    return;
                }

                var parentItem = TargetRepository.Database.GetItem(Mapping.Paths.Target);
                if (parentItem == null)
                {
                    statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] parentItem is null ({0})</span>", Mapping.Name), statusFilepath);
                    return;
                }

                var itemsProcessed = new StringBuilder();

                DataImportLogger.Log.Info("XC.DataImport - Mapping: " + Mapping.Name + " Total import count: " + itemsToImport);
                statusMethod(string.Format(" <span style=\"color:blue\">[INFO] Total import count: {0} ({1})</span>", itemsToImport, Mapping.Name), statusFilepath);

                using (new SecurityDisabler())
                {
                    IndexCustodian.PauseIndexing();

                    //using (new BulkUpdateContext())
                    //{
                        using (new DatabaseCacheDisabler())
                        using (new EventDisabler())
                        {
                            //TargetRepository.RetrieveItemsToProcess(this.Code, this.Filter);
                            var dataSet = SourceRepository.GetDataSet(statusMethod, statusFilepath, Filter);

                            if (dataSet != null)
                            {
                                //TargetRepository.ClearMultilistFieldValues(statusMethod, statusFilepath, dataSet, this.Code, this.Filter);

                                lock (dataSet.Rows)
                                {
                                    for (var i = 0; i < dataSet.Rows.Count; i++)
                                    {
                                        if (TargetRepository.DetailedLogging)
                                        {
                                            statusMethod(string.Format(" <h4 style=\"color:blue\">[INFO] {0}</h4>", i + 1), statusFilepath);
                                        }
                                        var migratedItem = TargetRepository.MigrateItem(dataSet.Rows[i], parentItem, startDate, i, statusMethod, statusFilepath);
                                    }
                                    statusMethod(string.Format(" <h4 style=\"color:blue\">[INFO] Cached {0} items</h4>", this.UpdatedItems != null ? this.UpdatedItems.Count : 0), statusFilepath);

                                }
                            }
                            //ClearCache();
                            statusMethod(" <h4 style=\"color:blue\">[INFO] Imported is done</h4>", statusFilepath);

                        }
                    //}

                    IndexCustodian.ResumeIndexing();
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} ({1}) ({2})</span>", ex.Message, ex.StackTrace, Mapping.Name), statusFilepath);
                throw;
            }
            statusMethod(string.Format(" <span style=\"color:blue\">[INFO] {0} DONE", id), statusFilepath);
            return;
        }

        internal List<Item> GetItemsToSync(Action<string, string> statusMethod, string statusFilepath)
        {
            try
            {
                if (SourceRepository == null || TargetRepository == null || Mapping.Paths == null || string.IsNullOrEmpty(Mapping.Paths.Target))
                {
                    statusMethod(" <span style=\"color:red\">[FAILURE] Mapping.Paths.Target is null</span>", statusFilepath);
                    return null;
                }

                var startDate = DateTime.Now;
                HistoryLogging.ImportInitialized("nonsitecore-" + Mapping.Name, startDate);

                var itemsToImport = SourceRepository.GetSourceItemsForImportCount(statusMethod, statusFilepath, Filter);
                if (itemsToImport == 0)
                {
                    statusMethod(string.Format(" <span style=\"color:blue\">[INFO] Nothing to import ({0})</span>", Mapping.Name), statusFilepath);
                    return null;
                }

                var parentItem = TargetRepository.Database.GetItem(Mapping.Paths.Target);
                if (parentItem == null)
                {
                    statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] parentItem is null ({0})</span>", Mapping.Name), statusFilepath);
                    return null;
                }

                var itemsProcessed = new StringBuilder();

                DataImportLogger.Log.Info("XC.DataImport - Mapping: " + Mapping.Name + " Total import count: " + itemsToImport);
                statusMethod(string.Format(" <span style=\"color:blue\">[INFO] Total import count: {0} ({1})</span>", itemsToImport, Mapping.Name), statusFilepath);

                using (new SecurityDisabler())
                {
                    using (new DatabaseCacheDisabler())
                    using (new EventDisabler())
                    {
                        var items = TargetRepository.RetrieveItemsToProcess(Filter);
                        if (items != null)
                        {
                            return items;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} ({1}) ({2})</span>", ex.Message, ex.StackTrace, Mapping.Name), statusFilepath);
                throw;
            }
            return null;
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
        /// Runs from ce.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilename">The status filename.</param>
        public void RunFromCE(string id, Action<string, string> statusMethod, string statusFilename)
        {
            GatherUpdateData(id, statusMethod, statusFilename);
        }

        public void Run(string id, Action<string, string> statusMethod, string statusFilename)
        {
            GatherUpdateData(id, statusMethod, statusFilename);
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

        private void ClearCache()
        {
            TargetRepository.Database.Caches.DataCache.Clear();
            TargetRepository.Database.Caches.ItemCache.Clear();
            TargetRepository.Database.Caches.ItemPathsCache.Clear();
            TargetRepository.Database.Caches.StandardValuesCache.Clear();

            Sitecore.Caching.CacheManager.ClearAllCaches();
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
       
    }
}
