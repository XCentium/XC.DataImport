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

        public IEnumerable<ScriptReference> PostProcessingScripts
        {
            get {
                if(postProcessingScripts == null || !postProcessingScripts.Any())
                    return null;

                var scripts = new List<ScriptReference>();
                foreach(var path in postProcessingScripts)
                {
                    if (File.Exists(path))
                    {
                        var scriptContent = File.ReadAllText(path);
                        var scriptObject = (ScriptReference)JsonConvert.DeserializeObject(scriptContent, typeof(ScriptReference));
                        if (scriptObject != null)
                            scripts.Add(scriptObject);
                    }
                }
                return scripts;
            }
        }
    }
}