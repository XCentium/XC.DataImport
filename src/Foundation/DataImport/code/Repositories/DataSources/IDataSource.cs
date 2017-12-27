using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XC.Foundation.DataImport.Models;

namespace XC.Foundation.DataImport.Repositories.DataSources
{
    public interface IDataSource
    {
        object GetSource(Action<string, string> statusMethod, string statusFilepath);  
    }
}
