using Sitecore.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.Foundation.DataImport.Models
{
    public class NonSitecoreMappingModel : BaseMappingModel, INonSitecoreMappingModel
    {
        public NonSitecoreSourceTargetPair Templates { get; set; }
        public SourceTargetPair MergeColumnFieldMatch { get; set; }
        public bool MergeWithExistingItems { get; set; }

        public NonScFieldMapping[] FieldMapping { get; set; }

        public bool IncrementalUpdate { get; set; }
        public string IncrementalUpdateSourceColumn { get; set; }


        public void ReplaceLongIdWithPath()
        {
            if (Databases == null && string.IsNullOrEmpty(Databases.Target))
                return;

            var targetDatabase = Factory.GetDatabase(Databases.Target);
            if (targetDatabase != null)
            {
                var item = targetDatabase.GetItem(Paths.Source);
                if (item != null)
                {
                    Paths.Source = item.Paths.FullPath;
                }
            }

        }
    }
}
