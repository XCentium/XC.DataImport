using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines.PostProcessing;

namespace Aha.Project.DataImport.Scripts.PostImport
{
    public class UpdateReferencesInHtml
    {
        public object ImportHelper { get; private set; }

        public void Process(ProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Post Import Processing UpdateReferencesInHtml started ##################");

            using (new SecurityDisabler())
            {
                foreach (var item in args.MigratedItems)
                {

                }
            }

            DataImportLogger.Log.Info("#################Field Processing UpdateReferences ended ##################");
            Sitecore.Diagnostics.Log.Info("#################Field Processing UpdateReferences ended ##################", this);

        }
    }
}