using Sitecore.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;
using XC.Foundation.DataImport.Models;
using System.IO;
using Newtonsoft.Json;

namespace XC.Foundation.DataImport.Pipelines.PostProcessing
{
    public class ProcessingPipelineArgs : PipelineArgs
    {
        private IEnumerable<Item> migratedItems;
        private IEnumerable<string> postProcessingScripts;

        public ProcessingPipelineArgs(IEnumerable<Item> migratedItems, IEnumerable<string> postProcessingScripts)
        {
            this.migratedItems = migratedItems;
            this.postProcessingScripts = postProcessingScripts;
        }

        public IEnumerable<Item> MigratedItems
        {
            get { return migratedItems; }
        }

        public IEnumerable<string> PostProcessingScripts
        {
            get {
                return postProcessingScripts;
            }
        }
    }
}