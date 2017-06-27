using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;

namespace XC.Project.DataImport.Scripts.FieldScript
{
    public class ConvertNullValueToNullString
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing ConvertYesToValue started ##################");

            if (args.SourceValue == null || string.IsNullOrEmpty((string)args.SourceValue))
            {
                args.Result = "NULL";
            }

            DataImportLogger.Log.Info("#################Field Processing ConvertYesToValue ended ##################");
        }
    }
}