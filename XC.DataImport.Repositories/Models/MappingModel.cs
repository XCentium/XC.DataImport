using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sitecore.Data;
using Sitecore.Configuration;

namespace XC.DataImport.Repositories.Models
{
    public class MappingModel : BaseMappingModel, IMappingModel
    {
       
        public bool MigrateAllFields { get; set; }
        public bool MigrateDescendants { get; set; }
        public SourceTargetPair Templates { get; set; }

        public FieldMapping[] FieldMapping { get; set; }


    }
    


}
