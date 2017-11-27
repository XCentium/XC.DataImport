using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Http.Dispatcher;
using XC.Foundation.DataImport.Attributes;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Models.Entities;

namespace XC.Foundation.DataImport.Utilities
{
    public static class SourceTypeHelper
    {
        public static string GetDatasourceName(Type type)
        {
            var attr = type.GetCustomAttribute<DataSourceNameAttribute>();
            if(attr != null)
            {
                return attr.Name;
            }
            return string.Empty;
        }

        public static SourceType GetDatasourceSourceType(string datasourceName)
        {
            var types = GetDatasourceTypes();
            if(types != null)
            {
                return types.FirstOrDefault(t => t.Name == datasourceName);
            }            
            return null;
        }

        public static IEnumerable<SourceType> GetDatasourceTypes()
        {
            Type ti = typeof(IDataSourceModel);
            var types = new List<SourceType>();
            var assemblies = new DefaultAssembliesResolver().GetAssemblies();
            foreach (var asm in assemblies)
            {
                try {
                    foreach (Type t in asm.GetTypes())
                    {
                        if (ti.IsAssignableFrom(t) && ti != t && !t.IsAbstract)
                        {
                            var attr = t.GetCustomAttribute<DataSourceNameAttribute>();
                            if (attr != null)
                            {
                                types.Add(new SourceType { Name = attr.Name, ModelType = t.FullName, DataSourceType = attr.DataSourceType?.FullName });
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (Exception exSub in ex.LoaderExceptions)
                    {
                        sb.AppendLine(exSub.Message);
                        FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                        if (exFileNotFound != null)
                        {
                            if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                            {
                                sb.AppendLine("Fusion Log:");
                                sb.AppendLine(exFileNotFound.FusionLog);
                            }
                        }
                        sb.AppendLine();
                    }
                    string errorMessage = sb.ToString();
                    DataImportLogger.Log.Error(ex.Message, ex);
                }
            }
            return types;
        }
    }
}