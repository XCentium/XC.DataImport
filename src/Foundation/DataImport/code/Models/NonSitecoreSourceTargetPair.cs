using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.Foundation.DataImport.Models
{
    public class NonSitecoreSourceTargetPair
    {
        public NonSitecoreCommand Source { get; set; }
        public string Target { get; set; }
    }

}
