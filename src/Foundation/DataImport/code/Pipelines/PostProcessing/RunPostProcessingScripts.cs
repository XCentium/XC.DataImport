using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Models;

namespace XC.Foundation.DataImport.Pipelines.PostProcessing
{
    public class RunPostProcessingScripts 
    {
        /// <summary>The processor list cache.</summary>
        private readonly Hashtable processorListCache = new Hashtable();
        /// <summary>The _use caching.</summary>
        private bool useCaching = true;

        public void Process(ProcessingPipelineArgs args)
        {
            if(args.PostProcessingScripts != null)
            {
                Pipeline pipeline = GetPipeline(args.PostProcessingScripts);
                if (pipeline == null)
                    return;
                pipeline.Start(args, true);
            }
        }

        public Pipeline GetPipeline(IEnumerable<string> scripts)
        {
            var pipelineName = "RunPostProcessingScripts";
            ArrayList processors = GetProcessors(pipelineName, scripts);
            return new Pipeline(pipelineName, processors, Pipeline.PipelineType.Dynamic);
        }

        private ArrayList GetProcessors(string pipelineName, IEnumerable<string> scripts)
        {
            Assert.ArgumentNotNullOrEmpty(pipelineName, "pipelineName");
            var processors = new ArrayList();
            foreach (var script in scripts)
            {
                Processor processor = parseProcessor(script);
                if (processor != null)
                    processors.Add(processor);
            }
            return processors;
        }

        private Processor parseProcessor(string script)
        {
            var scriptName = StringUtil.GetPostfix(script, '.');
            return new Processor(scriptName, script, "Process");
        }
    }
}