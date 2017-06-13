﻿using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XC.Foundation.DataImport;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines;
using XC.Foundation.DataImport.Pipelines.PostProcessing;

namespace XC.Project.DataImport.Scripts.PostImport
{
    public class ArrangeItemsBasedOnObjectId : IProcessingPipelineProcessor
    {
        public void Process(ProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Post Processing started ##################");

            if (args.MigratedItems == null)
                DataImportLogger.Log.Info("Post Processing: no migrated items");

            using (new SecurityDisabler())
            {
                foreach (var item in args.MigratedItems)
                {
                    var parentId = item[Templates.ImportedItem.Fields.OriginParentObjectId];
                    if (!string.IsNullOrEmpty(parentId))
                    {
                        var parent = GetParentItem(parentId);
                        if (parent != null)
                        {
                            item.MoveTo(parent);
                            DataImportLogger.Log.Info(string.Format("Post Processing: Moving item {0} to {1}", item.Name, item.Paths.FullPath));
                        }
                    }
                }
            }

            DataImportLogger.Log.Info("#################Post Processing ended ##################");
        }

        private Item GetParentItem(string parentId)
        {
            var query = string.Format("fast:/sitecore//*['{0}'='{1}']", Templates.ImportedItem.Fields.OriginObjectId, parentId);
            var db = Factory.GetDatabase("master");
            if (db == null)
                return null;
            return db.SelectSingleItem(query);
        }
    }
}