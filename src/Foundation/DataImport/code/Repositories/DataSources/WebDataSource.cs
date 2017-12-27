using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Models;

namespace XC.Foundation.DataImport.Repositories.DataSources
{
    public class WebDataSource : IDataSource
    {
        public object GetSource(Action<string, string> statusMethod, string statusFilepath)
        {
            throw new NotImplementedException();
        }
    }
}