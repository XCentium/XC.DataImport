using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data.Items;
using XC.DataImport.Repositories.Repositories;
using XC.Foundation.DataImport.Models;
using XC.DataImport.Repositories.History;
using Sitecore.Data;
using Sitecore.SecurityModel;
using Sitecore.Data.Events;
using Sitecore.Jobs;
using Sitecore;
using Sitecore.ContentSearch.Maintenance;
using XC.Foundation.DataImport.Diagnostics;

namespace XC.DataImport.Repositories.Migration
{
    public class SitecoreImportManager : XC.DataImport.Repositories.Migration.ISitecoreImportManager
    {
        public ISitecoreDatabaseRepository SourceRepository { get; set; }
        public ISitecoreDatabaseRepository TargetRepository { get; set; }
        public IMappingModel Mapping { get; set; }
        private static readonly object SyncRoot = new object();
        private static IDictionary<string, Tuple<int,string>> ProcessStatus { get; set; }
        private IEnumerable<Item> ItemsToImport { get; set; }

        public SitecoreImportManager()
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
        public int GetJobItemCount()
        {
            if (SourceRepository == null || TargetRepository == null || Mapping.Paths == null || string.IsNullOrEmpty(Mapping.Paths.Target)) return 0;
            ItemsToImport = SourceRepository.GetSourceItemsForImport();
            return ItemsToImport == null ? 0 : ItemsToImport.Count();
        }

        /// <summary>
        /// Runs the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public void Run(string id, Action<string, string> statusMethod, string statusFilename)
        {
            try
            {
                statusMethod(string.Format(" <span style=\"color:blue\">[INFO] Mapping: {0}</span>", Mapping.Name), statusFilename);

                if (SourceRepository == null || TargetRepository == null || Mapping.Paths == null || string.IsNullOrEmpty(Mapping.Paths.Target))
                {
                    statusMethod(" <span style=\"color:red\">[FAILURE] Mapping.Paths.Target is null</span>", statusFilename);
                    return;
                }

                var startDate = DateTime.Now;
                HistoryLogging.ImportInitialized(Mapping.Name, startDate);
                using (new SecurityDisabler())
                {
                    IndexCustodian.PauseIndexing();

                    using (new BulkUpdateContext())
                    //using (new ProxyDisabler())
                    //using (new DatabaseCacheDisabler())
                    //using (new SyncOperationContext())
                    using (new EventDisabler())
                    {
                        var itemsToImport = SourceRepository.GetSourceItemsForImport();
                        if (itemsToImport == null)
                        {
                            statusMethod(" <span style=\"color:blue\">[INFO] Nothing to import</span>", statusFilename);
                            return;
                        }
                        DataImportLogger.Log.Info("XC.DataImport - Total import count: " + itemsToImport.Count() + ": Mapping Name:" + Mapping.Name);
                        statusMethod(string.Format(" <span style=\"color:blue\">[INFO] Total import count: {0} </span>", itemsToImport.Count()), statusFilename);

                        var parentItem = TargetRepository.Database.GetItem(Mapping.Paths.Target);
                        if (parentItem == null)
                        {
                            statusMethod(" <span style=\"color:red\">[FAILURE] parentItem is null</span>", statusFilename);
                            return;
                        }

                        for (var i = 0; i < itemsToImport.Count(); i++)
                        {
                            statusMethod(string.Format(" <span style=\"color:blue\">[INFO] {0}: {1}</span>", i, itemsToImport.ElementAt(i).ID), statusFilename);
                            var item = SourceRepository.Database.GetItem(itemsToImport.ElementAt(i).ID);
                            TargetRepository.MigrateItem(item, parentItem, startDate, statusMethod, statusFilename);
                        }
                        //if (BucketManager.IsBucket(parentItem) || BucketManager.IsBucketFolder(parentItem))
                        //{
                        //    BucketManager.Sync(parentItem);
                        //}

                        ClearCache();
                    }

                    IndexCustodian.ResumeIndexing();
                }
            }
            catch (Exception ex)
            {
                statusMethod(string.Format("<span style=\"color:red\">{0}</span>", ex.Message), statusFilename);
                DataImportLogger.Log.Error(ex.Message, ex);
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
        /// Adds the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        public void Add(string id, Action<string,  string> statusMethod, string statusFilename)
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
            JobOptions options = new JobOptions(id, "SitecoreImportManager", Context.Site.Name, this, "Run", new object[] { id, statusMethod, statusFileName })
            {
                Priority = System.Threading.ThreadPriority.Lowest,
                ExecuteInManagedThreadPool = true,
                EnableSecurity = false,
            };
            JobManager.Start(options);
        }

        /// <summary>
        /// Runs from ce.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilename">The status filename.</param>
        public void RunFromCE(string id, Action<string, string> statusMethod, string statusFilename)
        {
            Run(id, statusMethod, statusFilename);
        }
    }
}
