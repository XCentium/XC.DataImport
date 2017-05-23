using Newtonsoft.Json;
using Sitecore.IO;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Configurations;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models;

namespace XC.DataImport.Repositories.History
{
    public static class HistoryLogging
    {

        /// <summary>
        /// Imports the initialized.
        /// </summary>
        /// <param name="Mapping">The mapping.</param>
        internal static void ImportInitialized(string mappingName, DateTime startDate)
        {
            try
            {
                var mappingFolderName = EnsureMappingFolder(mappingName);
                if (!string.IsNullOrEmpty(mappingFolderName))
                {
                    var fileName = startDate.ToString("yyyy-dd-M--HH-mm-ss") + ".xcimport";
                    var currentImportFilePath = Path.Combine(mappingFolderName, fileName);
                    File.WriteAllText(currentImportFilePath, mappingName);

                    if (File.Exists(currentImportFilePath))
                    {
                        using (var file = File.AppendText(currentImportFilePath))
                        {
                            file.WriteLine("Items:");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
        }
        /// <summary>
        /// Items the migrated.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="existingItem">The existing item.</param>
        /// <param name="importStartDate">The import start date.</param>
        /// <param name="p">The p.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static void ItemMigrated(DataRow row, Sitecore.Data.Items.Item existingItem, DateTime importStartDate, string mappingName)
        {
            try
            {
                var mappingFolderName = EnsureMappingFolder(mappingName);
                if (!string.IsNullOrEmpty(mappingFolderName))
                {
                    var fileName = importStartDate.ToString("yyyy-dd-M--HH-mm-ss") + ".xcimport";
                    var currentImportFilePath = Path.Combine(mappingFolderName, fileName);
                    if (File.Exists(currentImportFilePath))
                    {
                        using (var file = File.AppendText(currentImportFilePath))
                        {
                            file.WriteLine(string.Format("Target Item: {0};", existingItem.Paths.Path));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
        }
        /// <summary>
        /// Items the migrated.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="existingItem">The existing item.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static void ItemMigrated(Sitecore.Data.Items.Item item, Sitecore.Data.Items.Item existingItem, DateTime startDate, string mappingName)
        {
            try
            {
                var mappingFolderName = EnsureMappingFolder(mappingName);
                if (!string.IsNullOrEmpty(mappingFolderName))
                {
                    var fileName = startDate.ToString("yyyy-dd-M--HH-mm-ss") + ".xcimport";
                    var currentImportFilePath = Path.Combine(mappingFolderName, fileName);
                    if (File.Exists(currentImportFilePath))
                    {
                        using (var file = File.AppendText(currentImportFilePath))
                        {
                            file.WriteLine(string.Format("Source Item: {0}; Target Item: {1};", item.Paths.Path, existingItem.Paths.Path));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
        }

        /// <summary>
        /// Items the count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="mappingName">Name of the mapping.</param>
        internal static void ItemCount(int count, DateTime startDate, string mappingName)
        {
            try
            {
                var mappingFolderName = EnsureMappingFolder(mappingName);
                if (!string.IsNullOrEmpty(mappingFolderName))
                {
                    var fileName = startDate.ToString("yyyy-dd-M--HH-mm-ss") + ".xcimport";
                    var currentImportFilePath = Path.Combine(mappingFolderName, fileName);
                    if (File.Exists(currentImportFilePath))
                    {
                        using (var file = File.AppendText(currentImportFilePath))
                        {
                            file.WriteLine(string.Format("Item Count: {0};", count));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
        }
        /// <summary>
        /// Ensures the mapping folder.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static string EnsureMappingFolder(string mappingName)
        {
            var mappingPath = Path.Combine(DataImportConfigurations.HistoryFolder, mappingName);
            if (!Directory.Exists(mappingPath))
            {
                Directory.CreateDirectory(mappingPath);
            }
            return mappingPath;
        }


        /// <summary>
        /// Gets the latest run date string.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static string GetLatestRunDateString(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var mappingContent = File.ReadAllText(path);
                    var mappingObject = (MappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(MappingModel));

                    if (mappingObject != null)
                    {
                        var mappingPath = Path.Combine(DataImportConfigurations.HistoryFolder, mappingObject.Name);
                        var directory = new DirectoryInfo(mappingPath);
                        if (directory != null)
                        {
                            var latestFile = directory.GetFiles()
                                 .OrderByDescending(f => f.LastWriteTime)
                                 .First();
                            if (latestFile != null)
                            {
                                return latestFile.LastWriteTime.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return string.Empty;
        }

        /// <summary>
        /// Nons the sitecore get latest run date string.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static string NonSitecoreGetLatestRunDateString(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var mappingContent = File.ReadAllText(path);
                    var mappingObject = (NonSitecoreMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(NonSitecoreMappingModel));

                    if (mappingObject != null)
                    {
                        var mappingPath = Path.Combine(DataImportConfigurations.HistoryFolder, "nonsitecore-" + mappingObject.Name);
                        if (Directory.Exists(mappingPath))
                        {
                            var directory = new DirectoryInfo(mappingPath);
                            var latestFile = directory.GetFiles()
                                 .OrderByDescending(f => f.LastWriteTime)
                                 .First();
                            if (latestFile != null)
                            {
                                return latestFile.LastWriteTime.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return string.Empty;
        }

        public static string GetNumberOfItemsProcessed(string path)
        {
            return string.Empty;
        }

        /// <summary>
        /// Nons the sitecore get latest run date string.
        /// </summary>
        /// <param name="_mapping">The _mapping.</param>
        /// <returns></returns>
        internal static string NonSitecoreGetLatestRunDateString(INonSitecoreMappingModel _mapping)
        {
            if (_mapping == null)
            {
                return DateTime.MinValue.ToString();
            }
            try
            {
                var mappingHistoryPath = Path.Combine(DataImportConfigurations.HistoryFolder, "nonsitecore-" + _mapping.Name);
                if (Directory.Exists(mappingHistoryPath))
                {
                    var directory = new DirectoryInfo(mappingHistoryPath);
                    var latestFile = directory.GetFiles()
                         .OrderByDescending(f => f.LastWriteTime)
                         .First();
                    if (latestFile != null)
                    {
                        return latestFile.LastWriteTime.ToString();
                    }
                }
                else
                {
                    return DateTime.MinValue.ToString();
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the status file path.
        /// </summary>
        /// <param name="mapping">The mapping.</param>
        /// <returns></returns>
        public static string GetStatusFilePath(string mapping)
        {
            if (mapping == null)
            {
                return string.Empty;
            }
            try
            {
                var statusPath = Path.Combine(DataImportConfigurations.StatusFolder);
                if (!Directory.Exists(statusPath))
                {
                    Directory.CreateDirectory(statusPath);
                }
                var filePath = Path.Combine(statusPath, "nonsitecore-" + FileUtil.GetFileName(mapping) + ".html");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return filePath;
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the relative path.
        /// </summary>
        /// <param name="statusFileName">Name of the status file.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static string GetRelativePath(string statusFileName)
        {
            if (statusFileName == null || HttpContext.Current == null)
            {
                return string.Empty;
            }
            try
            {
                var siteRoot = HttpContext.Current.Server.MapPath("~");
                return statusFileName.Replace(siteRoot, "/").Replace(@"\", "/");
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return string.Empty;
        }
    }
}
