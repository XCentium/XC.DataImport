using Sitecore.Configuration;
using System.IO;
using System.Web;

namespace XC.Foundation.DataImport.Configurations
{
    public static class DataImportConfigurations
    {
        /// <summary>
        /// Gets the get folder path.
        /// </summary>
        /// <value>
        /// The get folder path.
        /// </value>
        internal static string GetFolderPath
        {
            get
            {
                var path = Settings.GetSetting("XC.Foundation.DataImport.DataFolder", Settings.DataFolder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }
        /// <summary>
        /// Gets the non sitecore mapping folder.
        /// </summary>
        /// <value>
        /// The non sitecore mapping folder.
        /// </value>
        public static string NonSitecoreMappingFolder
        {
            get
            {
                var path = Path.Combine(GetFolderPath, "non-sc-mappings");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        /// <summary>
        /// Gets the non sitecore batch mapping folder.
        /// </summary>
        /// <value>
        /// The non sitecore batch mapping folder.
        /// </value>
        public static string NonSitecoreBatchMappingFolder
        {
            get
            {
                var path = Path.Combine(GetFolderPath, "non-sc-batchmappings");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }
        /// <summary>
        /// Gets the mapping folder.
        /// </summary>
        /// <value>
        /// The mapping folder.
        /// </value>
        public static string MappingFolder
        {
            get
            {
                var path = Path.Combine(GetFolderPath, "mappings");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        /// <summary>
        /// Gets the post processing scripts folder.
        /// </summary>
        /// <value>
        /// The post processing scripts folder.
        /// </value>
        public static string PostProcessingScriptsFolder
        {
            get
            {
                var path = Path.Combine(GetFolderPath, "post-processing-scripts");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }
        /// <summary>
        /// Gets the history folder.
        /// </summary>
        /// <value>
        /// The history folder.
        /// </value>
        internal static string HistoryFolder
        {
            get
            {
                var path = Path.Combine(GetFolderPath, "history");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        /// <summary>
        /// Gets the batch mappings folder.
        /// </summary>
        /// <value>
        /// The batch mappings folder.
        /// </value>
        public static string BatchMappingsFolder
        {
            get
            {
                var path = Path.Combine(GetFolderPath, "batchmappings");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        /// <summary>
        /// Gets the batch non sc mappings folder.
        /// </summary>
        /// <value>
        /// The batch non sc mappings folder.
        /// </value>
        public static string BatchNonScMappingsFolder
        {
            get
            {
                var path = Path.Combine(GetFolderPath, "batchnonscmappings");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        /// <summary>
        /// Gets the status folder.
        /// </summary>
        /// <value>
        /// The status folder.
        /// </value>
        public static string StatusFolder
        {
            get
            {
                var folderName = "temp\\importstatus";
                var siteRoot = HttpContext.Current.Server.MapPath("~");
                var path = Path.Combine(siteRoot, folderName);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }
    }
}
