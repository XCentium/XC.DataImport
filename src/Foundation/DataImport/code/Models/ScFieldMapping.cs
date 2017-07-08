using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.Foundation.DataImport.Models
{
    public class ScFieldMapping
    {
        public ScFieldMapping()
        {
            ProcessingScripts = new List<string>();
        }
        public bool Exclude { get; set; }
        public bool Overwrite { get; set; }
        public string SourceFields { get; set; }
        public string TargetFields { get; set; }
        public bool IsId { get; set; }
        public int Id { get; set; }
        public IEnumerable<string> ProcessingScripts { get; set; }

        public string ReferenceItemsTemplate { get; set; }
        public string ReferenceItemsField { get; set; }

    }
}
