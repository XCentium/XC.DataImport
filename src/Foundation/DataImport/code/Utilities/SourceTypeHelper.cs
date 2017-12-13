using System;
using System.Linq;
using System.Reflection;
using XC.Foundation.DataImport.Configurations;
using XC.Foundation.DataImport.Models.Entities;

namespace XC.Foundation.DataImport.Utilities
{
    public static class SourceTypeHelper
    {


        public static SourceType GetDatasourceSourceType(string datasourceName)
        {
            var types = DataImportContainer.SourceProviders.GetDatasourceTypes();
            if(types != null)
            {
                return types.FirstOrDefault(t => t.Name == datasourceName);
            }            
            return null;
        }

        internal static SourceType GetDatasourceTargetType(string datasourceName)
        {
            var types = DataImportContainer.SourceProviders.GetTargetTypes();
            if (types != null)
            {
                return types.FirstOrDefault(t => t.Name == datasourceName);
            }
            return null;
        }
    }
}