using System;
namespace XC.Foundation.DataImport.Models
{
    public interface INonSitecoreMappingModel : IMapping
    {
        NonSitecoreSourceTargetPair Templates { get; set; }
        SourceTargetPair MergeColumnFieldMatch { get; set; }
        bool MergeWithExistingItems { get; set; }
        NonScFieldMapping[] FieldMapping { get; set; }
        bool IncrementalUpdate { get; set; }
        string IncrementalUpdateSourceColumn { get; set; }
    }
}
