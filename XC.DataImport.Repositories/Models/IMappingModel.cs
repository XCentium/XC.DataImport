namespace XC.DataImport.Repositories.Models
{
    public interface IMappingModel
    {
        string Name { get; set; }
        bool MigrateAllFields { get; set; }
        SourceTargetPair Databases { get; set; }
        SourceTargetPair Templates { get; set; }
        SourceTargetPair Paths { get; set; }
        FieldMapping[] FieldMapping { get; set; }
    }
}