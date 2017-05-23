using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XC.DataImport.Repositories.Repositories;
using XC.Foundation.DataImport.Models;
using XC.DataImport.Repositories.History;
using System.IO;
using Newtonsoft.Json;
using Sitecore;
using Sitecore.Jobs;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Configurations;

namespace XC.DataImport.Repositories.Migration
{
    public class BatchSitecoreImportManager : IBatchSitecoreImportManager
    {
        public bool FlushEnabled { get; set; }
        public IBatchMappingModel Mapping { get; set; }
        private static readonly object SyncRoot = new object();
        private static IDictionary<string, Tuple<int,string>> ProcessStatus { get; set; }
        private IEnumerable<string> MappingsToRun { get; set; }

        public BatchSitecoreImportManager()
        {
            if (ProcessStatus == null)
            {
                ProcessStatus = new Dictionary<string, Tuple<int, string>>();
            }
            FlushEnabled = true;
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
                if (Mapping.Files == null) return;

                var startDate = DateTime.Now;
                var mappingName = "batch-" + Mapping.Name;
                HistoryLogging.ImportInitialized(mappingName, startDate);

                var increment = (decimal)Mapping.Files.Count() / 100;

                var itemsProcessed = new StringBuilder();

                DataImportLogger.Log.Info("XC.DataImport - Total import file count: " + Mapping.Files.Count());
                statusMethod(string.Format("[INFO] Mapping file: {0} </span>", mappingName), statusFilename);
                DataImportLogger.Log.Info(string.Format("[INFO] Mapping file: {0} </span>", mappingName));

                for (var i = 0; i < Mapping.Files.Count(); i++)
                {
                    var mappingContent = File.ReadAllText(Path.Combine(DataImportConfigurations.MappingFolder, Mapping.Files[i]));
                    var mappingObject = (MappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(MappingModel));

                    DataImportLogger.Log.Info(string.Format("[INFO] Mapping: {0} </span>", mappingObject.Name));

                    var individualManager = new SitecoreImportManager
                    {
                        SourceRepository = new SitecoreDatabaseRepository(mappingObject.Databases.Source, mappingObject),
                        TargetRepository = new SitecoreDatabaseRepository(mappingObject.Databases.Target, mappingObject),
                        Mapping = mappingObject
                    };

                    individualManager.StartJob(id + Mapping.Files.ElementAt(i), statusMethod, statusFilename);
                    itemsProcessed.AppendFormat("<div>{0}</div>",Mapping.Files.ElementAt(i));
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
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
            JobOptions options = new JobOptions(id, "BatchSitecoreImportManager", Context.Site.Name, this, "Run", new object[] { id, statusMethod, statusFileName })
            {
                Priority = System.Threading.ThreadPriority.Lowest,
                ExecuteInManagedThreadPool = true,
                AfterLife = TimeSpan.FromHours(1),  // keep job data for one hour
                EnableSecurity = false,
            };
            JobManager.Start(options);
        }

        public void RunFromCE(string id, Action<string, string> statusMethod, string statusFilename)
        {
            throw new NotImplementedException();
        }
    }
}
