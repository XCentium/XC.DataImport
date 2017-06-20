using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;
using XC.Foundation.DataImport.Pipelines.PostProcessing;
using XC.Foundation.DataImport.Utilities;
using static Sitecore.Configuration.Settings;

namespace XC.Project.DataImport.Scripts.FieldScript
{
    public class ConvertSecuritySettings
    {
        private const string accessPattern = "ar|car\\{0}|pe|+item:read|pd|+item:read|";
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing ConvertSecuritySettings started ##################");

            if (args.SourceValue == null)
                DataImportLogger.Log.Info("ConvertSecuritySettings Field Processing: no SourceValue");

            if (!string.IsNullOrEmpty((string)args.SourceValue))
            {
                var convertedRoles = new StringBuilder();
                string[] sourceSecurityRoles = args.SourceValue is string ? ((string)args.SourceValue).Replace("\"", "").Split(',') : null;
                if(sourceSecurityRoles != null)
                {
                    foreach(var role in sourceSecurityRoles)
                    {
                        convertedRoles.AppendFormat(accessPattern, role);
                    }
                }
                args.Result = convertedRoles.ToString();
            }

            DataImportLogger.Log.Info("#################Field Processing ConvertSecuritySettings ended ##################");
        }
    }
}