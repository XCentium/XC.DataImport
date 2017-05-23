using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.Foundation.DataImport.Models
{
    public class NonSitecoreCommand
    {
        public string Command { get; set; }
        public bool IsStoredProc { get; set; }
    }
}
