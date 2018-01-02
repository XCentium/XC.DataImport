using Sitecore.Data;
using Sitecore.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport.Models.Entities
{
    public class ImportDataItem
    {
        public ID ItemId { get; set; }
        public string Name { get; set; }
        public string TemplateId { get; set; }
        public Dictionary<string,object> Fields { get; set; }
        public int Version { get; set; }
        public Language Language { get; set; }
        public List<ImportDataItem> Children { get; set; }
        public List<ImportDataItemVersion> LanguageVersions { get; set; }
        public List<ImportDataItemVersion> Versions { get; set; }

        public ImportDataItem()
        {
            Children = new List<ImportDataItem>();
            Fields = new Dictionary<string, object>();
            LanguageVersions = new List<ImportDataItemVersion>();
            Versions = new List<ImportDataItemVersion>();
        }
    }

    public class ImportDataItemVersion
    {
        public Dictionary<string, object> Fields { get; set; }
        public Sitecore.Data.Version Version { get; set; }
        public Language Language { get; set; }
    }
}