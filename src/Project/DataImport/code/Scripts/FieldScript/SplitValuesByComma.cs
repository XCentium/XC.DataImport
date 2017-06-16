using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines;
using XC.Foundation.DataImport.Pipelines.PostProcessing;
using XC.Foundation.DataImport.Utilities;
using static Sitecore.Configuration.Settings;

namespace XC.Project.DataImport.Scripts.FieldScript
{
    public class SplitValuesByComma : IProcessingPipelineProcessor
    {
        public void Process(ProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing started ##################");

            if (args.MigratedItems == null)
                DataImportLogger.Log.Info("Post Processing: no migrated items");

            using (new SecurityDisabler())
            {
                //foreach (var item in args.MigratedItems)
                //{
                //    var parentId = item[Templates.ImportedItem.Fields.OriginParentObjectId];
                //    if (!string.IsNullOrEmpty(parentId))
                //    {
                //        var parent = GetParentItem(parentId);
                //        if (parent != null)
                //        {
                //            item.MoveTo(parent);
                //            DataImportLogger.Log.Info(string.Format("Post Processing: Moving item {0} to {1}", item.Name, item.Paths.FullPath));
                //        }
                //    }
                //}
            }

            DataImportLogger.Log.Info("#################Field Processing ended ##################");
        }
    }
}