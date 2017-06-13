﻿using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Models;
using XC.Foundation.DataImport.Pipelines.PostProcessing;

namespace XC.Foundation.DataImport.Pipelines
{
    public class RunPostProcessingScripts : IProcessingPipelineProcessor
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

        public Pipeline GetPipeline(IEnumerable<ScriptReference> scripts)
        {
            var pipelineName = "RunPostProcessingScripts";
            ArrayList processors = GetProcessors(pipelineName, scripts);
            return new Pipeline(pipelineName, processors, Pipeline.PipelineType.Dynamic);
        }

        private ArrayList GetProcessors(string pipelineName, IEnumerable<ScriptReference> scripts)
        {
            Assert.ArgumentNotNullOrEmpty(pipelineName, "pipelineName");
            var processors = new ArrayList();
            foreach (ScriptReference script in scripts)
            {
                Processor processor = parseProcessor(script);
                if (processor != null)
                    processors.Add(processor);
            }
            return processors;
        }

        private Processor parseProcessor(ScriptReference script)
        {
            return new Processor(script.Name, script.Type, "Process");
        }
    }
}