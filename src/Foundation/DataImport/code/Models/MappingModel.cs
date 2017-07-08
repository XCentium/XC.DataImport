using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sitecore.Data;
using Sitecore.Configuration;

namespace XC.Foundation.DataImport.Models
{
    public class ScMappingModel : BaseMappingModel, IMappingModel
    {
        public bool MigrateAllFields { get; set; }
        public bool MigrateDescendants { get; set; }
        public bool MigrateAllVersions { get; set; }
        public SourceTargetPair Templates { get; set; }
        public ScFieldMapping[] FieldMapping { get; set; }
    }
}
