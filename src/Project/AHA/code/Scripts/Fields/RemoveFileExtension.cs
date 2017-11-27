using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;

namespace Aha.Project.DataImport.Scripts.Fields
{
    public class RemoveFileExtension
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing RemoveFileExtension started ##################");

            if (args.SourceValue == null)
                DataImportLogger.Log.Info("RemoveFileExtension Field Processing: no SourceValue");

            args.SourceValue = args.SourceValue is string ? Path.GetFileNameWithoutExtension((string)args.SourceValue) : args.SourceValue;

            DataImportLogger.Log.Info("#################Field Processing RemoveFileExtension ended ##################");
        }
    }
}