using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sitecore.Data;
using Sitecore.Configuration;

namespace XC.DataImport.Repositories.Models
{
    public class MappingModel : IMappingModel
    {
        public string Name { get; set; }
        public bool MigrateAllFields { get; set; }
        public SourceTargetPair Databases { get; set; }
        public SourceTargetPair Templates { get; set; }
        public SourceTargetPair Paths { get; set; }
        public SourceTargetPair FullPaths { get; set; }
        public FieldMapping[] FieldMapping { get; set; }


        public void ConvertPathsToLongIds()
        {
            if (Paths == null || Databases == null)
                return;

            if (!string.IsNullOrEmpty(Paths.Source) && !string.IsNullOrEmpty(Databases.Source))
            {
                var sourceDatabase = Factory.GetDatabase(Databases.Source);
                if (sourceDatabase != null)
                {
                    var item = sourceDatabase.GetItem(Paths.Source);
                    if (item != null)
                    {
                        Paths.Source = item.Paths.LongID;
                    }
                }
            }

            if (!string.IsNullOrEmpty(Paths.Target) && !string.IsNullOrEmpty(Databases.Target))
            {
                var targetDatabase = Factory.GetDatabase(Databases.Target);
                if (targetDatabase != null)
                {
                    var item = targetDatabase.GetItem(Paths.Target);
                    if (item != null)
                    {
                        Paths.Target = item.Paths.LongID;
                    }
                }
            }
        }
    }

    public class SourceTargetPair
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }

    public class FieldMapping
    {
        public bool Exclude { get; set; }
        public bool Overwrite { get; set; }
        public string SourceFields { get; set; }
        public string TargetFields { get; set; }
    }
}
