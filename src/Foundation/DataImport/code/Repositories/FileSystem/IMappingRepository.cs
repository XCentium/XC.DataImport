using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XC.Foundation.DataImport.Models.Mappings;

namespace XC.Foundation.DataImport.Repositories.FileSystem
{
    public interface IMappingRepository
    {
        IMappingModel RetrieveMappingModel<T>(string id) where T : IMappingModel;
    }
}
