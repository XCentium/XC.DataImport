using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.DataImport.Repositories.Models
{
    public interface IMapping
    {
        string Name { get; set; }
        SourceTargetPair Paths { get; set; }
        SourceTargetPair Databases { get; set; }

    }
}
