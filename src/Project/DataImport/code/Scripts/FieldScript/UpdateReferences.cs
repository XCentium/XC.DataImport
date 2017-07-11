using HtmlAgilityPack;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;
using XC.Foundation.DataImport.Pipelines.PostProcessing;
using XC.Foundation.DataImport.Utilities;
using static Sitecore.Configuration.Settings;
using XC.Project.DataImport.Helpers;

namespace XC.Project.DataImport.Scripts.FieldScript
{
    public class UpdateReferences
    {
        private readonly ID StringSettingTemplateId = ID.Parse("{5EECF4A9-2D1F-44D1-AE33-0B7EE1230055}");
        private readonly ID MediaReferenceTemplateId = ID.Parse("{170EDED0-DB36-4FC8-98F8-EFF1D6CC65F5}");

        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing UpdateReferences started ##################");

            if (args.SourceValue == null)
                DataImportLogger.Log.Info("UpdateReferences Field Processing: no SourceValue");

            if (!string.IsNullOrEmpty((string)args.SourceValue))
            {
                args.Result = ImportHelper.ProcessHtmlFieldValue((string)args.SourceValue, args.Database, null, false);
            }

            DataImportLogger.Log.Info("#################Field Processing UpdateReferences ended ##################");
            Sitecore.Diagnostics.Log.Info("#################Field Processing UpdateReferences ended ##################", this);

        }
    }
}