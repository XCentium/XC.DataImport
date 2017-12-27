using Sitecore.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Models.Entities;

namespace XC.Foundation.DataImport.Models.Mappings
{
    public class ImportMappingModel : IMappingModel
    {
        public ImportMappingModel()
        {
            SourceProcessingScripts = new List<string>();
            PostImportScripts = new List<string>();
        }
        public string Name { get; set; }
        public Guid Id { get; set; }
        public SourceType SourceType { get; set; }
        public SourceType TargetType { get; set; }
        public dynamic Source { get; set; }

        public List<string> SourceProcessingScripts { get; set; }
        public List<string> PostImportScripts { get; set; }

        public dynamic Target { get; set; }

        public ScFieldMapping[] FieldMappings { get; set; }
        public bool MergeWithExistingItems { get; internal set; }
        public SourceTargetPair MergeColumnFieldMatch { get; internal set; }

        public void ConvertPathsToLongIds()
        {
            //if (TargetPath == null || TargetDatabaseName == null)
            //    return;

            //if (!string.IsNullOrEmpty(TargetPath) && !string.IsNullOrEmpty(TargetDatabaseName))
            //{
            //    var targetDatabase = Factory.GetDatabase(TargetDatabaseName);
            //    if (targetDatabase != null)
            //    {
            //        var item = targetDatabase.GetItem(TargetPath);
            //        if (item != null)
            //        {
            //            TargetFullPath = item.Paths.LongID.TrimStart('/');
            //        }
            //    }
            //}
        }
    }
}