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
using Sitecore.Globalization;

namespace XC.Foundation.DataImport.Repositories.Repositories
{
    public class SitecoreRepository : BaseTargetRepository, ITargetRepository
    {
        public SitecoreRepository(ImportMappingModel mapping) : base(mapping)
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
        public Item ImportItem(ImportDataItem dataItem, int index, Action<string, string> statusMethod, string statusFilepath)
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
                    if (_mapping.MergeWithExistingItems)
                    {
                        return UpdateExistingItem(dataItem, index, statusMethod, statusFilepath);
                    }
                    else
                    {
                        return CreateNewItem(dataItem, ParentItem, index, statusMethod, statusFilepath);
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
        private Item UpdateExistingItem(ImportDataItem dataItem, int index, Action<string, string> statusMethod, string statusFilepath)
        {
            var fieldId = _mapping.MergeColumnFieldMatch.Source;
            var matchingColumnValue = dataItem.Fields.ContainsKey(fieldId) ? dataItem.Fields[_mapping.MergeColumnFieldMatch.Source] as string : null;

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

                    UpdateFields(dataItem.Fields, existingItem, statusMethod, statusFilepath);

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
        private Item CreateNewItem(ImportDataItem dataItem, Item parentItem, int index, Action<string, string> statusMethod, string statusFilepath)
        {
            //using (new DatabaseCacheDisabler())
            using (new SecurityDisabler())
            {
                try
                {
                    var targetItem = Database.GetItem(dataItem.ItemId);
                    if (targetItem == null)
                    {
                        var itemName = dataItem.Name;
                        var itemNameMapping = _mapping.FieldMappings.FirstOrDefault();

                        if (itemNameMapping != null)
                        {
                            var fieldId = itemNameMapping.TargetFields;
                            itemName = dataItem.Fields.ContainsKey(fieldId) ? dataItem.Fields[fieldId].ToString() : "";
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
                            targetItem = ItemManager.CreateItem(ItemUtil.ProposeValidItemName(itemName), parentItem, templateId, dataItem.ItemId);
                            if (targetItem == null) return null;

                            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created </strong></span>", targetItem.Paths.Path), statusFilepath);

                            UpdateFields(dataItem.Fields, targetItem, statusMethod, statusFilepath);
                        }
                        else
                        {
                            statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> Templates.Target or itemName are not defined. ({1})</span>", parentItem.Paths.Path, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                        }
                    }
                    else
                    {
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] Updating Fields on {0} </strong></span>", targetItem.Paths.Path), statusFilepath);

                        ChangeTemplateIfNeeded(targetItem, statusMethod, statusFilepath);
                        UpdateFields(dataItem.Fields, targetItem, statusMethod, statusFilepath);
                    }

                    DataImportLogger.Log.Info("XC.DataImport - item processed: Target path - " + targetItem.Paths.FullPath);
                    HistoryLogging.ItemMigrated(targetItem, DateTime.Now, HistoryLogging.GetMappingFileName(_mapping.Id.ToString()));

                    ProcessLanguageVersions(dataItem, targetItem, statusMethod, statusFilepath);
                    ProcessVersions(dataItem, targetItem, statusMethod, statusFilepath);
                    ProcessChildren(dataItem, targetItem, statusMethod, statusFilepath);

                    return targetItem;
                }
                catch (Exception ex)
                {
                    statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> {1} ({2})</span>", parentItem.Paths.Path, ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                    DataImportLogger.Log.Error(ex.Message, ex);
                }
            }
            return null;
        }

        /// <summary>
        /// Processes the children.
        /// </summary>
        /// <param name="dataItem">The item.</param>
        /// <param name="targetItem">The target item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void ProcessChildren(ImportDataItem dataItem, Item targetItem, Action<string, string> statusMethod, string statusFilepath)
        {
            if (dataItem.Children != null && dataItem.Children.Any())
            {
                foreach (var child in dataItem.Children)
                {
                    try
                    {
                        var childItem = Database.GetItem(child.ItemId);
                        if (childItem == null && !string.IsNullOrWhiteSpace(child.TemplateId))
                        {
                            var templateId = new TemplateID(ID.Parse(child.TemplateId));
                            childItem = ItemManager.CreateItem(child.Name, targetItem, templateId, child.ItemId);
                            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created</strong></span>", targetItem.Paths.Path), statusFilepath);
                        }
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] Updating fields on {0}</strong></span>", targetItem.Paths.Path), statusFilepath);
                        UpdateFields(child.Fields, childItem, statusMethod, statusFilepath);

                        ProcessLanguageVersions(child, childItem, statusMethod, statusFilepath);
                        ProcessVersions(child, childItem, statusMethod, statusFilepath);
                        ProcessChildren(child, childItem, statusMethod, statusFilepath);
                    }
                    catch (Exception ex)
                    {
                        statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] ProcessChildren {0}</strong> {1} ({2})</span>", targetItem.Paths.Path, ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                        DataImportLogger.Log.Error(ex.Message, ex);
                    }
                }
            }
        }

        /// <summary>
        /// Processes the versions.
        /// </summary>
        /// <param name="dataItem">The data item.</param>
        /// <param name="targetItem">The target item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void ProcessVersions(ImportDataItem dataItem, Item targetItem, Action<string, string> statusMethod, string statusFilepath)
        {
            if (dataItem == null || dataItem.Versions == null || !dataItem.Versions.Any())
            {
                statusMethod(string.Format(" <span style=\"color:red\"><strong>[INFO] No Versions to process. ({0})</strong></span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                return;
            }
            foreach (var version in dataItem.Versions)
            {
                var versionItem = targetItem.Versions[version.Version];
                if (versionItem == null)
                {
                    versionItem = targetItem.Versions.AddVersion();
                }
                UpdateFields(version.Fields, versionItem, statusMethod, statusFilepath);
            }
        }

        /// <summary>
        /// Processes the language versions.
        /// </summary>
        /// <param name="dataItem">The item.</param>
        /// <param name="targetItem">The target item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void ProcessLanguageVersions(ImportDataItem dataItem, Item targetItem, Action<string, string> statusMethod, string statusFilepath)
        {
            if (dataItem == null || dataItem.LanguageVersions == null || !dataItem.LanguageVersions.Any())
            {
                statusMethod(string.Format(" <span style=\"color:red\"><strong>[INFO] No Language Versions to process. ({0})</strong></span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                return;
            }
            foreach (var version in dataItem.LanguageVersions)
            {
                var versionItem = Database.GetItem(dataItem.ItemId, version.Language);
                if (versionItem == null)
                {
                    using (new LanguageSwitcher(version.Language))
                    {
                        versionItem = targetItem.Versions.AddVersion();
                        statusMethod(string.Format(" <span style=\"color:blue\"><strong>[INFO] Add new language version. ({0})</strong></span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                    }
                }
                statusMethod(string.Format(" <span style=\"color:blue\"><strong>[INFO] Updating fields on language version. ({0})</strong></span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);

                UpdateFields(version.Fields, versionItem, statusMethod, statusFilepath);
            }
        }

    }
}