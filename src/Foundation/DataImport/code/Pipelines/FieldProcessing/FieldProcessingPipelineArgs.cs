using Sitecore.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;
using XC.Foundation.DataImport.Models;
using System.IO;
using Newtonsoft.Json;

namespace XC.Foundation.DataImport.Pipelines.FieldProcessing
{
    public class FieldProcessingPipelineArgs : PipelineArgs
    {
        private object sourceValue;
        private IEnumerable<string> scripts;

        public FieldProcessingPipelineArgs(object sourceValue, IEnumerable<string> scripts)
        {
            this.sourceValue = sourceValue;
            this.scripts = scripts;
        }

        public object SourceValue
        {
            get { return sourceValue; }
        }

        public IEnumerable<string> ProcessingScripts
        {
            get {
                return scripts;
            }
        }
    }
}