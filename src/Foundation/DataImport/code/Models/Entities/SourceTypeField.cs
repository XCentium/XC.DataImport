using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport.Models.Entities
{
    public class SourceTypeField
    {
        public string Name { get; set; }
        public string InputType { get; set; }
        public string OptionsSource { get; set; }
        public string TriggerFields { get; set; }
    }
}