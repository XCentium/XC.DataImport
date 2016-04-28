using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.DataImport.Repositories.Models
{
    public class MappingInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string RunLabel { get; set; }
        public string EditLabel { get; set; }
        public string RunLink { get; set; }
        public string EditLink { get; set; }
    }
}
