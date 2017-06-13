using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Pipelines.PostProcessing;

namespace XC.Foundation.DataImport.Pipelines
{
    public interface IProcessingPipelineProcessor
    {
        void Process(ProcessingPipelineArgs args);
    }
}