using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.Foundation.DataImport.Models
{
    public class FieldMapping
    {
        public bool Exclude { get; set; }
        public bool Overwrite { get; set; }
        public string SourceFields { get; set; }
        public string TargetFields { get; set; }
        public bool IsId { get; set; }
    }
}
