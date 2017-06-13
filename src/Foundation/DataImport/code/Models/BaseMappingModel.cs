using Sitecore.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.Foundation.DataImport.Models
{
    public class BaseMappingModel : IMapping
    {
        public BaseMappingModel()
        {
            FullPaths = new SourceTargetPair();
        }
        public string Name { get; set; }
        public SourceTargetPair Databases { get; set; }
        public SourceTargetPair Paths { get; set; }

        public SourceTargetPair FullPaths { get; set; }

        public IEnumerable<string> PostImportScripts { get; set; }

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
                        FullPaths.Source = item.Paths.LongID.TrimStart('/');
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
                        FullPaths.Target = item.Paths.LongID.TrimStart('/');
                    }
                }
            }
        }
    }
}
