using Aha.Project.DataImport.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;

namespace Aha.Project.DataImport.Scripts.Fields
{
    public class UpdateRenderedReferences
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing UpdateRenderedReferences started ##################");

            if (args.SourceValue == null)
                DataImportLogger.Log.Info("UpdateRenderedReferences Field Processing: no SourceValue");

            if (!string.IsNullOrEmpty((string)args.SourceValue))
            {
                args.SourceValue = ImportHelper.ProcessRenderedHtmlFieldValue((string)args.SourceValue, args.Database, null);
            }

            DataImportLogger.Log.Info("#################Field Processing UpdateRenderedReferences ended ##################");
            Sitecore.Diagnostics.Log.Info("#################Field Processing UpdateRenderedReferences ended ##################", this);
        }
    }
}