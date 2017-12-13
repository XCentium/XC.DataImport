using Sitecore.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;
using XC.Foundation.DataImport.Models;
using System.IO;
using Newtonsoft.Json;
using Sitecore.Data;
using XC.Foundation.DataImport.Models.Mappings;

namespace XC.Foundation.DataImport.Pipelines.FieldProcessing
{
    public class FieldProcessingPipelineArgs : PipelineArgs
    {
        private object sourceValue;
        private IEnumerable<string> scripts;
        public Database Database { get; internal set; }
        public ImportMappingModel Mapping { get; }

        public FieldProcessingPipelineArgs(object sourceValue, IEnumerable<string> scripts, Database database, ImportMappingModel mapping)
        {
            this.sourceValue = sourceValue;
            this.scripts = scripts;
            Database = database;
            Mapping = mapping;
        }

        public object SourceValue
        {
            get { return sourceValue; }
            set { sourceValue = value;  }
        }

        public IEnumerable<string> ProcessingScripts
        {
            get {
                return scripts;
            }
        }
    }
}