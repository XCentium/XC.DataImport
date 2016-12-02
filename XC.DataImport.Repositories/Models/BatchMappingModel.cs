using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.DataImport.Repositories.Models
{
    public class BatchMappingModel : BaseMappingModel, IBatchMappingModel
    {
        public string[] Files { get; set; }
        public string Description { get; set; }
    }
}
