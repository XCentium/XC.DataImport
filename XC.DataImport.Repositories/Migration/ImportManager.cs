using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using XC.DataImport.Repositories.Databases;
using XC.DataImport.Repositories.Models;
using XC.DataImport.Repositories.Diagnostics;

namespace XC.DataImport.Repositories.Migration
{
    public class ImportManager
    {
        public ISitecoreDatabaseRepository SourceRepository { get; set; }
        public ISitecoreDatabaseRepository TargetRepository { get; set; }
        public IMappingModel Mapping { get; set; }
        private static readonly object SyncRoot = new object();
        private static IDictionary<string, Tuple<int,string>> ProcessStatus { get; set; }
        private IEnumerable<Item> ItemsToImport { get; set; }

        public ImportManager()
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
        public string Run(string id)
        {
            try
            {
                if (SourceRepository == null || TargetRepository == null || Mapping.Paths == null || string.IsNullOrEmpty(Mapping.Paths.Target)) return string.Empty;

                var itemsToImport = SourceRepository.GetSourceItemsForImport();
                if (itemsToImport == null) return string.Empty;

                var parentItem = TargetRepository.Database.GetItem(Mapping.Paths.Target);
                if (parentItem == null) return string.Empty;

                var increment = (decimal)itemsToImport.Count()/100;

                var itemsProcessed = new StringBuilder();

                DataImportLogger.Log.Info("XC.DataImport - Total import count: " + itemsToImport.Count());

                for (var i = 0; i< itemsToImport.Count(); i++)
                {
                    TargetRepository.MigrateItem(itemsToImport.ElementAt(i), parentItem);
                    itemsProcessed.AppendFormat("<div>{0}</div>",itemsToImport.ElementAt(i).Paths.Path);
                    lock (SyncRoot)
                    {
                        ProcessStatus[id] = new Tuple<int, string>((int)((i + 1) * increment * 100), itemsProcessed.ToString()); 
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return id;
        }

        /// <summary>
        /// Adds the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        public void Add(string id)
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
    }
}
