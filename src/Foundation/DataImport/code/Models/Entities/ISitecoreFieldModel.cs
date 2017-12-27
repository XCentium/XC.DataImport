using System.Collections.Generic;

namespace XC.Foundation.DataImport.Models.Entities
{
    public interface ISitecoreFieldModel
    {
        string ItemId { get; set; }
        string Name { get; set; }
        Dictionary<string, object> Properties { get; set; }
        Dictionary<string, object> Fields { get; set; }
    }
}