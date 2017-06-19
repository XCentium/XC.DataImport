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
    public class PrependUserWithDomainName
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing PrependUserWithDomainName started ##################");

            if (args.SourceValue == null)
                DataImportLogger.Log.Info("PrependUserWithDomainName Field Processing: no SourceValue");

            if (!string.IsNullOrEmpty((string)args.SourceValue))
            {
                args.Result = "car\\" + ((string)args.SourceValue).Replace(" ", "").ToLowerInvariant();
            }
            else
            {
                args.Result = "sitecore\\dataimport";
            }

            DataImportLogger.Log.Info("#################Field Processing PrependUserWithDomainName ended ##################");
        }
    }
}