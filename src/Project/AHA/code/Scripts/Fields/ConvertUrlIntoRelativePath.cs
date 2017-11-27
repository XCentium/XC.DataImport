using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;

namespace Aha.Project.DataImport.Scripts.Fields
{
    public class ConvertUrlIntoRelativePath
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing ConvertUrlIntoRelativePath started ##################");

            if (args.SourceValue == null)
                DataImportLogger.Log.Info("ConvertUrlIntoRelativePath Field Processing: no SourceValue");

            if (args.SourceValue is string && !string.IsNullOrEmpty((string)args.SourceValue))
            {
                var uri = new Uri((string)args.SourceValue);
                args.SourceValue = uri.PathAndQuery;
            }
            
            DataImportLogger.Log.Info("#################Field Processing ConvertUrlIntoRelativePath ended ##################");
        }
    }
}