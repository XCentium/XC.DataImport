using System;
namespace XC.DataImport.Repositories.Models
{
    public interface IBatchMappingModel : IMapping
    {
        string Description { get; set; }
        string[] Files { get; set; }
    }
}
