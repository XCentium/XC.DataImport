using Sitecore.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Xml;
using XC.Foundation.DataImport.Models.Entities;

namespace XC.Foundation.DataImport.Configurations
{
    public static class DataImportContainer
    {
        private static readonly string DataImportRootPath = FormattableString.Invariant(FormattableStringFactory.Create("{0}/xc.dataimport/", (object)"xcDataImport"));

        public static class SourceProviders
        {
            internal static IEnumerable<SourceType> GetDatasourceTypes()
            {
                var types = new List<SourceType>();
                var sources = Factory.GetConfigNode("xc.dataimport/sourceProviders");
                if(sources != null && sources.HasChildNodes)
                {                    
                    foreach(XmlNode source in sources.ChildNodes)
                    {
                        types.Add(new SourceType { Name = source.Attributes["name"]?.Value, DataSourceType = source.Attributes["dataSourceType"]?.Value, ModelType = source.Attributes["modelType"]?.Value, Fields = GetInputFields(source.ChildNodes) });
                    }
                }
                return types;
            }

            public static string GetDatasourceName(Type type)
            {
                var sources = GetDatasourceTypes();
                if (sources != null)
                {
                    return sources.FirstOrDefault(s => s.ModelType == type.FullName)?.Name;
                }
                return string.Empty;
            }

            private static List<SourceTypeField> GetInputFields(XmlNodeList childNodes)
            {
                if(childNodes == null || childNodes.Count == 0)
                {
                    return new List<SourceTypeField>();
                }
                return childNodes.Cast<XmlNode>().Select(n => new SourceTypeField { InputType = n.Attributes["type"]?.Value, Name = n.Attributes["name"]?.Value, OptionsSource = n.Attributes["optionsSource"]?.Value, TriggerFields = n.Attributes["triggerFields"]?.Value }).ToList();
            }

            internal static IEnumerable<SourceType> GetTargetTypes()
            {
                var types = new List<SourceType>();
                var sources = Factory.GetConfigNode("xc.dataimport/targetProviders");
                if (sources != null && sources.HasChildNodes)
                {
                    foreach (XmlNode source in sources.ChildNodes)
                    {
                        types.Add(new SourceType { Name = source.Attributes["name"]?.Value, DataSourceType = source.Attributes["dataSourceType"]?.Value, ModelType = source.Attributes["modelType"]?.Value, Fields = GetInputFields(source.ChildNodes) });
                    }
                }
                return types;
            }
        }
    }
}