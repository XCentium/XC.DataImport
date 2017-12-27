using Sitecore.Configuration;
using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Models.DataSources;
using Sitecore.Data.Items;
using XC.Foundation.DataImport.Repositories.DataSources;
using XC.Foundation.DataImport.Disablers;
using XC.Foundation.DataImport.Models.Mappings;
using XC.Foundation.DataImport.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.Data.Managers;
using XC.DataImport.Repositories.History;
using XC.Foundation.DataImport.Models;
using Sitecore.Data.Fields;
using Sitecore.Resources.Media;
using System.IO;
using Sitecore;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;
using Sitecore.Pipelines;
using XC.Foundation.DataImport.Utilities;
using Newtonsoft.Json;
using XC.Foundation.DataImport.Models.Entities;

namespace XC.Foundation.DataImport.Repositories.Repositories
{
    public class SitecoreRepository : BaseTargetRepository, ITargetRepository
    {        
        public SitecoreRepository(ImportMappingModel mapping):base(mapping)
        {
        }

        /// <summary>
        /// Imports the item.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="values">The values.</param>
        /// <param name="index">The index.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        public Item ImportItem(ID itemId, Dictionary<string, object> values, int index, Action<string, string> statusMethod, string statusFilepath)
        {
            if (_mapping == null || Target == null || ParentItem == null)
            {
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] Not Imported or Updated: either item, _mapping, parentItem is null or item is not a content item. ({0})</span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                return null;
            }
            try
            {
                using (new ItemFilteringDisabler())
                {
                    var sitecoreId = itemId;

                    if (_mapping.MergeWithExistingItems)
                    {
                        return UpdateExistingItem(itemId, values, index, statusMethod, statusFilepath);
                    }
                    else
                    {
                        return CreateNewItem(itemId, values, ParentItem, index, statusMethod, statusFilepath);
                    }
                }
            }
            catch (Exception ex)
            {
                statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> {1} ({2})</span>", ParentItem.Paths.Path, ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return null;
        }

        /// <summary>
        /// Updates the existing item.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="values">The values.</param>
        /// <param name="index">The index.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        private Item UpdateExistingItem(ID itemId, Dictionary<string, object> values, int index, Action<string, string> statusMethod, string statusFilepath)
        {
            var fieldId = _mapping.MergeColumnFieldMatch.Source;
            var matchingColumnValue = values.ContainsKey(fieldId) ? values[_mapping.MergeColumnFieldMatch.Source] as string : null; 

            if (string.IsNullOrEmpty(matchingColumnValue))
            {
                return null;
            }
            Item existingItem = null;
            using (new ItemFilteringDisabler())
            {
                var targetFieldItem = Database.GetItem(_mapping.MergeColumnFieldMatch.Target);
                if (targetFieldItem == null)
                {
                    return null;
                }
                existingItem = FindItem(matchingColumnValue, targetFieldItem);
                if (existingItem == null)
                {
                    statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} not found ({1})</strong></span>", matchingColumnValue, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                    return null;
                }

                if (existingItem != null)
                {
                    statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Updated </strong></span>", existingItem.Paths.Path), statusFilepath);

                    UpdateFields(values, existingItem, statusMethod, statusFilepath);

                    DataImportLogger.Log.Info("XC.DataImport - item processed: Target path - " + existingItem.Paths.FullPath);
                    HistoryLogging.ItemMigrated(existingItem, DateTime.Now, HistoryLogging.GetMappingFileName(_mapping.Id.ToString()));
                }
            }
            return existingItem;
        }

        /// <summary>
        /// Creates the new item.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="values">The values.</param>
        /// <param name="parentItem">The parent item.</param>
        /// <param name="index">The index.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        private Item CreateNewItem(ID itemId, Dictionary<string, object> values, Item parentItem, int index, Action<string, string> statusMethod, string statusFilepath)
        {
            using (new SecurityDisabler())
            {
                try
                {
                    var existingItem = Database.GetItem(itemId);
                    if (existingItem == null)
                    {
                        var itemName = _mapping.Name + " " + index;
                        var itemNameMapping = _mapping.FieldMappings.FirstOrDefault();
                        var fieldId = itemNameMapping.TargetFields;
                        if (itemNameMapping != null)
                        {
                            itemName = values.ContainsKey(fieldId) ? values[fieldId].ToString() : "";
                            var processedValue = (string)RunFieldProcessingScripts(itemName, itemNameMapping.ProcessingScripts);
                            if (!string.IsNullOrEmpty(processedValue))
                            {
                                itemName = processedValue;
                            }
                        }

                        if (!string.IsNullOrEmpty(itemName) && !string.IsNullOrEmpty(SitecoreTarget.TemplateId))
                        {
                            var template = ItemUri.Parse(SitecoreTarget.TemplateId);
                            var templateId = template.ItemID;
                            var newItem = ItemManager.CreateItem(ItemUtil.ProposeValidItemName(itemName), parentItem, templateId, itemId);
                            if (newItem == null) return null;

                            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created </strong></span>", newItem.Paths.Path), statusFilepath);

                            UpdateFields(values, newItem, statusMethod, statusFilepath);
                            existingItem = newItem;
                        }
                        else
                        {
                            statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> Templates.Target or itemName are not defined. ({1})</span>", parentItem.Paths.Path, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                        }
                    }
                    else
                    {
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] Updating Fields on {0} </strong></span>", existingItem.Paths.Path), statusFilepath);

                        ChangeTemplateIfNeeded(existingItem, statusMethod, statusFilepath);
                        UpdateFields(values, existingItem, statusMethod, statusFilepath);
                    }

                    DataImportLogger.Log.Info("XC.DataImport - item processed: Target path - " + existingItem.Paths.FullPath);
                    HistoryLogging.ItemMigrated(existingItem, DateTime.Now, HistoryLogging.GetMappingFileName(_mapping.Id.ToString()));

                    return existingItem;
                }
                catch (Exception ex)
                {
                    statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> {1} ({2})</span>", parentItem.Paths.Path, ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                    DataImportLogger.Log.Error(ex.Message, ex);
                }
            }
            return null;
        }
    }
}