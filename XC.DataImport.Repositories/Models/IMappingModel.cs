namespace XC.DataImport.Repositories.Models
{
    public interface IMappingModel : IMapping
    {
        SourceTargetPair Templates { get; set; }
        FieldMapping[] FieldMapping { get; set; }
    }
}