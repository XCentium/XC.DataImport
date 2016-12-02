using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.DataImport.Repositories.Models
{
    public class NonSitecoreCommand
    {
        public string Command { get; set; }
        public bool IsStoredProc { get; set; }
    }
}
