using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.Foundation.DataImport.Models.Mappings
{
    public interface IMappingModel
    {
        string Name { get; set; }
        Guid Id { get; set; }
    }
}
