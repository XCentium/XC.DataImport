using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.DataImport.Repositories.Models
{
    public class NonSitecoreSourceTargetPair
    {
        public NonSitecoreCommand Source { get; set; }
        public string Target { get; set; }
    }

}
