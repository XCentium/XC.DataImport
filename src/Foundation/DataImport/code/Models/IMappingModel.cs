namespace XC.Foundation.DataImport.Models
{
    public interface IMappingModel : IMapping
    {
        SourceTargetPair Templates { get; set; }
        ScFieldMapping[] FieldMapping { get; set; }
        bool MigrateAllVersions { get; set; }
        bool MigrateDescendants { get; set; }
        bool MigrateAllFields { get; set; }
    }
}