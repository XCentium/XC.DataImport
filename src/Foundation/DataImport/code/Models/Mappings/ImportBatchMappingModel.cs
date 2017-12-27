using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Models.Entities;

namespace XC.Foundation.DataImport.Models.Mappings
{
    public class ImportBatchMappingModel : IMappingModel
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public List<MappingReference> Mappings { get; set; }
        public bool RunInParallel { get; set; }
    }
}