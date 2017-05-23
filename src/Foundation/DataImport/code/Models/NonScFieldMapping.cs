using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.Foundation.DataImport.Models
{
    public class NonScFieldMapping : FieldMapping
    {
        public string ReferenceItemsTemplate { get; set; }
        public string ReferenceItemsField { get; set; }
    }
}
