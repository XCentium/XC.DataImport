using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;
using XC.Foundation.DataImport.Pipelines.PostProcessing;
using XC.Foundation.DataImport.Utilities;
using static Sitecore.Configuration.Settings;

namespace XC.Project.DataImport.Scripts.FieldScript
{
    public class SplitValuesByComma 
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing SplitValuesByComma started ##################");

            if (args.SourceValue == null)
                DataImportLogger.Log.Info("SplitValuesByComma Field Processing: no SourceValue");

            args.SourceValue = args.SourceValue is string ? ((string)args.SourceValue).Replace("\"","").Split(',').Select(i=>i.Trim()) : args.SourceValue;

            DataImportLogger.Log.Info("#################Field Processing SplitValuesByComma ended ##################");
        }
    }
}