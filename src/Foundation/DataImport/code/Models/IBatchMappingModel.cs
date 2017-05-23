using System;
namespace XC.Foundation.DataImport.Models
{
    public interface IBatchMappingModel : IMapping
    {
        string Description { get; set; }
        string[] Files { get; set; }
    }
}
