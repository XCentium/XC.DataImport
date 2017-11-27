using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport.Attributes
{
    public class DataSourceNameAttribute : Attribute
    {
        public string Name { get; set; }
        public Type DataSourceType { get; set; }
    }
}