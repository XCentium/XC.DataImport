namespace XC.Foundation.DataImport.Models
{
    public interface IMappingModel : IMapping
    {
        SourceTargetPair Templates { get; set; }
        FieldMapping[] FieldMapping { get; set; }
    }
}