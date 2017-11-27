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

namespace XC.Foundation.DataImport.Repositories.Repositories
{
    public class SitecoreRepository
    {
        private ImportMappingModel _mapping;

        public SitecoreRepository(ImportMappingModel mapping)
        {
            _mapping = mapping;
        }
        public Database Database
        {
            get
            {
                return Factory.GetDatabase(_mapping.Target.DatabaseName);
            }
        }

        internal Item ImportItem(ID itemId, Dictionary<ID, object> values, Item parentItem, int index, Action<string, string> statusMethod, string statusFilepath)
        {
            if (_mapping == null || _mapping.Target == null || parentItem == null)
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
                        return CreateNewItem(itemId, values, parentItem, index, statusMethod, statusFilepath);
                    }
                }
            }
            catch (Exception ex)
            {
                statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> {1} ({2})</span>", parentItem.Paths.Path, ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return null;
        }

        private Item UpdateExistingItem(ID itemId, Dictionary<ID, object> values, int index, Action<string, string> statusMethod, string statusFilepath)
        {
            var fieldId = ConvertToId(_mapping.MergeColumnFieldMatch.Source);
            var matchingColumnValue = values.ContainsKey(fieldId) ? values[ConvertToId(_mapping.MergeColumnFieldMatch.Source)] as string : null; 

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
                    HistoryLogging.ItemMigrated(values, existingItem, DateTime.Now, HistoryLogging.GetMappingFileName(_mapping.Id.ToString()));
                }
            }
            return existingItem;
        }

        private Item CreateNewItem(ID itemId, Dictionary<ID, object> values, Item parentItem, int index, Action<string, string> statusMethod, string statusFilepath)
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
                        var fieldId = ID.Parse(itemNameMapping.TargetFields);
                        if (itemNameMapping != null)
                        {
                            itemName = values.ContainsKey(fieldId) ? values[fieldId].ToString() : "";
                            var processedValue = (string)RunFieldProcessingScripts(itemName, itemNameMapping.ProcessingScripts);
                            if (!string.IsNullOrEmpty(processedValue))
                            {
                                itemName = processedValue;
                            }
                        }

                        if (!string.IsNullOrEmpty(itemName) && !string.IsNullOrEmpty(_mapping.Target.TemplateId))
                        {
                            var template = ItemUri.Parse(_mapping.Target.TemplateId);
                            var templateId = template.ItemID;
                            var newItem = ItemManager.CreateItem(ItemUtil.ProposeValidItemName(itemName), parentItem, templateId, itemId);
                            if (newItem == null) return null;

                            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created </strong></span>", newItem.Paths.Path), statusFilepath);

                            UpdateFields(values, newItem, statusMethod, statusFilepath);
                            existingItem = newItem;
                        }
                        else
                        {
                            statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> Tamplates.Target or itemName are not defined. ({1})</span>", parentItem.Paths.Path, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                        }
                    }
                    else
                    {
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] Updating Fields on {0} </strong></span>", existingItem.Paths.Path), statusFilepath);

                        ChangeTemplateIfNeeded(existingItem, statusMethod, statusFilepath);
                        UpdateFields(values, existingItem, statusMethod, statusFilepath);
                    }

                    DataImportLogger.Log.Info("XC.DataImport - item processed: Target path - " + existingItem.Paths.FullPath);
                    HistoryLogging.ItemMigrated(values, existingItem, DateTime.Now, HistoryLogging.GetMappingFileName(_mapping.Id.ToString()));

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

        private void ChangeTemplateIfNeeded(Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            var template = ItemUri.Parse(_mapping.Target.TemplateId);
            var templateItem = Database.GetItem(template);
            var oldTemplateName = item.TemplateName;

            if (item.TemplateID != template.ItemID)
            {
                item.ChangeTemplate(templateItem);
                statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] Changed Template on {0} from {1} to {2} </strong></span>", item.Paths.Path, oldTemplateName, item.TemplateName), statusFilepath);
            }
        }

        private void UpdateFields(Dictionary<ID, object> values, Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            foreach (var field in _mapping.FieldMappings)
            {
                try
                {
                    if (!string.IsNullOrEmpty(field.TargetFields) && item.Fields[field.TargetFields] != null && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                    {
                        var targetField = item.Fields[field.TargetFields];
                        if ((item.Fields[field.TargetFields].TypeKey.Contains("multilist") || item.Fields[field.TargetFields].TypeKey.Contains("treelist")) && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                        {
                            ProcessMultivalueField(values, item, statusMethod, statusFilepath, field, targetField);
                        }
                        else if ((item.Fields[field.TargetFields].TypeKey.Contains("droplink") || item.Fields[field.TargetFields].TypeKey.Contains("reference") || item.Fields[field.TargetFields].TypeKey.Contains("droptree")) && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                        {
                            ProcessReferenceField(values, item, statusMethod, statusFilepath, field, targetField);
                        }
                        else if (item.Fields[field.TargetFields].TypeKey.Contains("droplist") && !string.IsNullOrEmpty(field.ReferenceItemsTemplate) && !string.IsNullOrWhiteSpace(field.ReferenceItemsField) && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                        {
                            ProcessNameReferenceField(values, item, statusMethod, statusFilepath, field, targetField);
                        }
                        else if ((item.Fields[field.TargetFields].TypeKey.Contains("tristate") || item.Fields[field.TargetFields].TypeKey.Contains("checkbox")) && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                        {
                            ProcessTristateCheckboxField(values, item, statusMethod, statusFilepath, field, targetField);
                        }
                        else if (item.Fields[field.TargetFields].TypeKey.Contains("general link") && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                        {
                            ProcessLinkField(values, item, statusMethod, statusFilepath, field, targetField);
                        }
                        else if (item.Fields[field.TargetFields].TypeKey.Contains("attachment"))
                        {
                            ProcessAttachmentField(values, item, field, statusMethod, statusFilepath);
                        }
                        else if (item.Fields[field.TargetFields].TypeKey.Contains("image"))
                        {
                            ProcessImageField(values, item, field, targetField, statusMethod, statusFilepath);
                        }
                        else if (item.Fields[field.TargetFields].TypeKey.Contains("date"))
                        {
                            ProcessDateField(values, item, field, statusMethod, statusFilepath, targetField);
                        }
                        else if (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite)
                        {
                            ProcessGenericField(values, item, statusMethod, statusFilepath, field, targetField);
                        }
                    }
                }
                catch (Exception ex)
                {
                    statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0} ({1})</strong></span>", ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                    DataImportLogger.Log.Error(ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// Processes the attachment field.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void ProcessAttachmentField(Dictionary<ID, object> values, Item item, ScFieldMapping field, Action<string, string> statusMethod, string statusFilepath)
        {
            if (item.Fields[field.TargetFields].TypeKey.Contains("attachment") && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
            {
                var fieldId = ConvertToId(field.TargetFields);
                var matchingColumnValue = values.ContainsKey(fieldId) ? values[fieldId] : null;

                var processedValue = RunFieldProcessingScripts(matchingColumnValue, field.ProcessingScripts);
                if (processedValue == null)
                {
                    return;
                }

                byte[] fileContent = (byte[])processedValue;
                if (fileContent != null)
                {
                    AttachMediaStream(item, fileContent, statusMethod, statusFilepath);
                }
            }
        }

        /// <summary>
        /// Attaches the media stream.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="existingItem">The existing item.</param>
        private void AttachMediaStream(Item item, byte[] mediaStream, Action<string, string> statusMethod, string statusFilename)
        {
            var options = CreateMediaCreatorOptions(item);
            var creator = new MediaCreator();
            var sourceMediaItem = new MediaItem(item);
            var sourceMediaStream = new MemoryStream(mediaStream);
            var fileName = sourceMediaItem.Name + "." + sourceMediaItem.Extension;

            if (sourceMediaStream != null)
            {
                try
                {
                    Media media = MediaManager.GetMedia(sourceMediaItem);
                    var scMediaStream = new MediaStream(sourceMediaStream, media.Extension, item);
                    media.SetStream(scMediaStream);

                    if (item.Fields["Height"] != null && string.IsNullOrEmpty(item["Height"]) && item.Fields["Width"] != null && string.IsNullOrEmpty(item["Width"]))
                    {
                        try
                        {
                            using (new EditContext(item))
                            {
                                var image = System.Drawing.Image.FromStream(sourceMediaStream);
                                item["Height"] = image.Height.ToString();
                                item["Width"] = image.Height.ToString();
                            }
                        }
                        catch { }
                    }
                    statusMethod(string.Format("<span style=\"color:green\"><strong>[SUCCESS][{0}] Updating Attached Media: {1} </strong></span>", item.ID, item.Paths.FullPath), statusFilename);
                }
                catch (Exception ex)
                {
                    statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0} ({1})</strong</span>", ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilename);
                    DataImportLogger.Log.Error(ex.Message, ex);
                }
            }
            else
            {
                statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE][{0}] Stream is null</strong></span>", item.ID), statusFilename);
            }
        }

        /// <summary>
        /// Processes the generic field.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <param name="field">The field.</param>
        /// <param name="targetField">The target field.</param>
        private void ProcessGenericField(Dictionary<ID, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            using (new EditContext(item, false, true))
            {
                var fieldId = ConvertToId(field.TargetFields);
                var finalValue = values.ContainsKey(fieldId) ? values[fieldId].ToString() : string.Empty;

                var processedValue = RunFieldProcessingScripts(finalValue, field.ProcessingScripts);

                if (processedValue is string)
                {
                    item.Fields[field.TargetFields].Value = (string)processedValue;
                }
                else
                {
                    item.Fields[field.TargetFields].Value = finalValue;
                }

            }
            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
            DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID));
        }

        private static ID ConvertToId(string field)
        {
           return ID.Parse(field);
        }

        /// <summary>
        /// Processes the link field.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <param name="field">The field.</param>
        /// <param name="targetField">The target field.</param>
        private void ProcessLinkField(Dictionary<ID, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            var fieldId = ConvertToId(field.TargetFields);
            var matchingColumnValue = values.ContainsKey(fieldId) ? (string)values[fieldId] : null;

            var processedValue = (string)RunFieldProcessingScripts(matchingColumnValue, field.ProcessingScripts);
            if (!string.IsNullOrEmpty(processedValue))
            {
                var linkField = (LinkField)item.Fields[field.TargetFields];
                if (!string.IsNullOrEmpty(processedValue))
                {
                    if (!processedValue.Contains("://"))
                    {
                        processedValue = "http://" + processedValue;
                    }
                    if (linkField.Url != matchingColumnValue)
                    {
                        using (new EditContext(item, false, true))
                        {
                            linkField.Url = processedValue;
                        }
                        statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID));
                    }
                }
            }
        }

        /// <summary>
        /// Processes the tristate checkbox field.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <param name="field">The field.</param>
        /// <param name="targetField">The target field.</param>
        private void ProcessTristateCheckboxField(Dictionary<ID, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            var fieldId = ConvertToId(field.TargetFields);
            var matchingColumnValue = values.ContainsKey(fieldId) ? (string)values[fieldId] : null;

            var processedValue = RunFieldProcessingScripts(matchingColumnValue, field.ProcessingScripts);
            if (item.Fields[field.TargetFields].TypeKey.Contains("tristate"))
            {
                var fieldValue = "";
                if (processedValue is bool)
                {
                    if ((bool)processedValue)
                    {
                        fieldValue = "1";
                    }
                    else if (!(bool)processedValue)
                    {
                        fieldValue = "0";
                    }
                }
                var tristateField = (ValueLookupField)item.Fields[field.TargetFields];
                if (fieldValue != tristateField.Value)
                {
                    using (new EditContext(item, false, true))
                    {
                        tristateField.Value = fieldValue;
                    }
                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
                    DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID));
                }
            }
            else if (item.Fields[field.TargetFields].TypeKey.Contains("checkbox"))
            {
                var fieldValue = "";
                if (processedValue is bool)
                {
                    fieldValue = (bool)processedValue ? "1" : "0";
                }
                else if (processedValue is string)
                {
                    if ((string)processedValue == "0" || (string)processedValue == "1")
                    {
                        fieldValue = (string)processedValue;
                    }
                    else
                    {
                        var boolValue = false;
                        fieldValue = item.Fields[field.TargetFields].ContainsStandardValue ? item.Fields[field.TargetFields].Value : "0";
                        if (bool.TryParse((string)processedValue, out boolValue))
                        {
                            fieldValue = boolValue ? "1" : "0";
                        }
                    }
                }
                if (item.Fields[field.TargetFields].Value != fieldValue)
                {
                    var checkboxField = (CheckboxField)item.Fields[field.TargetFields];
                    using (new EditContext(item, false, true))
                    {
                        checkboxField.Value = fieldValue;
                    }

                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
                    DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID));
                }
            }
        }

        /// <summary>
        /// Processes the reference field.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <param name="field">The field.</param>
        /// <param name="targetField">The target field.</param>
        private void ProcessReferenceField(Dictionary<ID, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            var fieldId = ConvertToId(field.TargetFields);
            var matchingColumnValue = values.ContainsKey(fieldId) ? (string)values[fieldId] : null;

            var processedValue = RunFieldProcessingScripts(matchingColumnValue, field.ProcessingScripts);
            if (!string.IsNullOrEmpty((string)processedValue))
            {
                var referenceField = (ReferenceField)item.Fields[field.TargetFields];
                var startPath = "sitecore";
                var source = GetFieldSource(referenceField.InnerField.Source);
                if (!string.IsNullOrWhiteSpace(source))
                {
                    var fieldSource = item.Database.GetItem(source);
                    if (fieldSource != null)
                    {
                        startPath = fieldSource.Paths.FullPath;
                    }
                }
                Item matchingItem = null;
                var referenceFieldName = ResolveFieldName(field.ReferenceItemsField);
                if (!string.IsNullOrEmpty(field.ReferenceItemsTemplate))
                {
                    var template = ItemUri.Parse(field.ReferenceItemsTemplate);
                    matchingItem = Database.SelectSingleItem(string.Format("fast:/{3}//*[@{0}='{1}' and @@templateid='{2}']", FastQueryUtility.EscapeDashes(referenceFieldName), processedValue, template.ItemID, FastQueryUtility.EscapeDashes(startPath)));
                    if (matchingItem == null)
                    {
                        matchingItem = Database.SelectSingleItem(string.Format("fast:/{2}//*[@{0}='%{1}%']", FastQueryUtility.EscapeDashes(referenceFieldName), processedValue, FastQueryUtility.EscapeDashes(startPath)));
                    }
                    statusMethod(string.Format(" --- <span style=\"color:blue\">[INFO][{2}] Looking for field \"{0}\" match \"{1}\" under \"{3}\" </span>", referenceFieldName, processedValue, item.ID, startPath), statusFilepath);
                    DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:blue\">[INFO][{2}]  Looking for field \"{0}\" match \"{1}\" under \"{3}\"  </span>", referenceFieldName, processedValue, item.ID, startPath));
                }

                if (matchingItem != null && referenceField != null)
                {
                    if (referenceField.Value != matchingItem.ID.ToString())
                    {
                        using (new EditContext(item, false, true))
                        {
                            referenceField.Value = matchingItem.ID.ToString();
                        }
                    }
                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
                    DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID));
                }
                else
                {
                    statusMethod(string.Format(" --- <span style=\"color:red\">[FAILURE] Field \"{0}\" NOT Updated, because matching reference item was not found. ({1})</span>", targetField.DisplayName, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                    DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:red\">[FAILURE] Field \"{0}\" NOT Updated, because matching reference item was not found. ({1})</span>", targetField.DisplayName, _mapping != null ? _mapping.Name : "Unknown mapping"));
                }
            }
        }

        /// <summary>
        /// Processes the reference field.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <param name="field">The field.</param>
        /// <param name="targetField">The target field.</param>
        private void ProcessNameReferenceField(Dictionary<ID, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            var fieldId = ConvertToId(field.TargetFields);
            var matchingColumnValue = values.ContainsKey(fieldId) ? (string)values[fieldId] : null;

            var processedValue = RunFieldProcessingScripts(matchingColumnValue, field.ProcessingScripts);
            if (string.IsNullOrEmpty((string)processedValue))
            {
                return;
            }

            var itemField = (Field)item.Fields[field.TargetFields];
            var startPath = itemField.Source;
            if (string.IsNullOrEmpty(startPath))
            {
                var message = $" --- <span style=\"color:red\">[FAILURE] Field \"{targetField.DisplayName}\" NOT Updated, because droplist source path was not specified. ({(_mapping != null ? _mapping.Name : "Unknown mapping")})</span>";
                statusMethod(message, statusFilepath);
                DataImportLogger.Log.Info(message);
                return;
            }

            Item matchingItem = null;
            var referenceFieldName = ResolveFieldName(field.ReferenceItemsField);
            if (!string.IsNullOrEmpty(field.ReferenceItemsTemplate))
            {
                var template = ItemUri.Parse(field.ReferenceItemsTemplate);
                matchingItem = Database.SelectSingleItem(string.Format("fast:/{3}//*[@{0}='{1}' and @@templateid='{2}']", FastQueryUtility.EscapeDashes(referenceFieldName), processedValue, template.ItemID, FastQueryUtility.EscapeDashes(startPath))) ??
                               Database.SelectSingleItem(string.Format("fast:/{2}//*[@{0}='%{1}%']", FastQueryUtility.EscapeDashes(referenceFieldName), processedValue, FastQueryUtility.EscapeDashes(startPath)));

                var message = $" --- <span style=\"color:blue\">[INFO][{item.ID}] Looking for field \"{referenceFieldName}\" match \"{processedValue}\" under \"{startPath}\" </span>";
                statusMethod(message, statusFilepath);
                DataImportLogger.Log.Info(message);
            }

            if (matchingItem != null && itemField != null)
            {
                if (itemField.Value != matchingItem.ID.ToString())
                {
                    using (new EditContext(item, false, true))
                    {
                        itemField.Value = matchingItem.Name;
                    }
                }

                var message = $" --- <span style=\"color:green\">[SUCCESS][{item.ID}] Field \"{targetField.DisplayName}\" Updated to \"{item.Fields[field.TargetFields].Value}\" </span>";
                statusMethod(message, statusFilepath);
                DataImportLogger.Log.Info(message);
            }
            else
            {
                var message = $" --- <span style=\"color:red\">[FAILURE] Field \"{targetField.DisplayName}\" NOT Updated, because matching reference item was not found. ({(_mapping != null ? _mapping.Name : "Unknown mapping")})</span>";
                statusMethod(message, statusFilepath);
                DataImportLogger.Log.Info(message);
            }
        }

        /// <summary>
        /// Processes the multivalue field.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <param name="field">The field.</param>
        /// <param name="targetField">The target field.</param>
        private void ProcessMultivalueField(Dictionary<ID, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            var fieldId = ConvertToId(field.TargetFields);
            var matchingColumnValue = values.ContainsKey(fieldId) ? (string)values[fieldId] : null;

            var processedValue = RunFieldProcessingScripts(matchingColumnValue, field.ProcessingScripts);
            if (processedValue is IEnumerable<string>)
            {
                foreach (var val in (IEnumerable<string>)processedValue)
                {
                    UpdateMultilistField(item, statusMethod, statusFilepath, field, targetField, val);
                }
            }
            else
            {
                UpdateMultilistField(item, statusMethod, statusFilepath, field, targetField, processedValue.ToString());
            }
        }

        /// <summary>
        /// Updates the multilist field.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <param name="field">The field.</param>
        /// <param name="targetField">The target field.</param>
        /// <param name="matchingColumnValue">The matching column value.</param>
        private void UpdateMultilistField(Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField, string matchingColumnValue)
        {
            var multilistField = (MultilistField)item.Fields[field.TargetFields];
            var startPath = "sitecore";
            var source = GetFieldSource(multilistField.InnerField.Source);
            if (!string.IsNullOrWhiteSpace(source))
            {
                var fieldSource = item.Database.GetItem(source);
                if (fieldSource != null)
                {
                    startPath = fieldSource.Paths.FullPath;
                }
            }
            Item matchingItem = null;
            var referenceFieldName = ResolveFieldName(field.ReferenceItemsField);
            if (!string.IsNullOrEmpty(field.ReferenceItemsTemplate))
            {
                var template = ItemUri.Parse(field.ReferenceItemsTemplate);
                matchingItem = Database.SelectSingleItem(string.Format("fast:/{3}//*[@{0}=\"{1}\" and @@templateid='{2}']", FastQueryUtility.EscapeDashes(referenceFieldName), matchingColumnValue, template.ItemID, FastQueryUtility.EscapeDashes(startPath)));
                if (matchingItem == null)
                {
                    matchingItem = Database.SelectSingleItem(string.Format("fast:/{2}//*[@{0}='%{1}%']", FastQueryUtility.EscapeDashes(referenceFieldName), matchingColumnValue, FastQueryUtility.EscapeDashes(startPath)));
                }
                statusMethod(string.Format(" --- <span style=\"color:blue\">[INFO][{2}] Looking for field \"{0}\" match \"{1}\" under \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath), statusFilepath);
                DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:blue\">[INFO][{2}]  Looking for field \"{0}\" match \"{1}\" under \"{3}\"  </span>", referenceFieldName, matchingColumnValue, item.ID, startPath));
            }
            else
            {
                if (!string.IsNullOrEmpty(referenceFieldName))
                {
                    matchingItem = Database.SelectSingleItem(string.Format("fast:/{2}//*[@{0}=\"{1}\"]", FastQueryUtility.EscapeDashes(referenceFieldName), matchingColumnValue, FastQueryUtility.EscapeDashes(startPath)));
                    if (matchingItem == null)
                    {
                        matchingItem = Database.SelectSingleItem(string.Format("fast:/{2}//*[@{0}='%{1}%']", FastQueryUtility.EscapeDashes(referenceFieldName), matchingColumnValue, FastQueryUtility.EscapeDashes(startPath)));
                    }
                    statusMethod(string.Format(" --- <span style=\"color:blue\">[INFO][{2}] Looking for field \"{0}\" match \"{1}\"  under \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath), statusFilepath);
                    DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:blue\">[INFO][{2}]  Looking for field \"{0}\" match \"{1}\"  under \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath));
                }
                else
                {
                    statusMethod(string.Format(" --- <span style=\"color:red\">[FAILURE][{2}] referenceFieldName is empty: \"{0}\"  \"{1}\"   \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath), statusFilepath);
                    DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:red\">[FAILURE][{2}] referenceFieldName is empty:  \"{0}\"  \"{1}\"   \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath));
                }
            }
            if (matchingItem != null && multilistField != null)
            {
                if (!multilistField.Contains(matchingItem.ID.ToString()))
                {
                    using (new EditContext(item, false, true))
                    {
                        multilistField.Add(matchingItem.ID.ToString());
                    }
                }
                statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
                DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID));
            }
            else
            {
                statusMethod(string.Format(" --- <span style=\"color:red\">[FAILURE] Field \"{0}\" NOT Updated, because matching reference item was not found. ({1})</span>", targetField.DisplayName, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:red\">[FAILURE] Field \"{0}\" NOT Updated, because matching reference item was not found. ({1})</span>", targetField.DisplayName, _mapping != null ? _mapping.Name : "Unknown mapping"));
            }
        }

        /// <summary>
        /// Processes the image field.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="item">The item.</param>
        /// <param name="field">The field.</param>
        /// <param name="targetField">The target field.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void ProcessImageField(Dictionary<ID, object> values, Item item, ScFieldMapping field, Field targetField, Action<string, string> statusMethod, string statusFilepath)
        {
            var fieldId = ConvertToId(field.TargetFields);
            var matchingColumnValue = values.ContainsKey(fieldId) ? values[fieldId] : null;

            var processedValue = (string)RunFieldProcessingScripts(matchingColumnValue, field.ProcessingScripts);
            if (!string.IsNullOrEmpty(processedValue))
            {
                Item mediaItem = null;
                var imageField = (ImageField)item.Fields[field.TargetFields];
                if (!string.IsNullOrEmpty(processedValue))
                {
                    var startPath = "sitecore";
                    var source = GetFieldSource(imageField.InnerField.Source);
                    if (!string.IsNullOrWhiteSpace(source))
                    {
                        var fieldSource = item.Database.GetItem(source);
                        if (fieldSource != null)
                        {
                            startPath = fieldSource.Paths.FullPath;
                        }
                    }
                    var imageFieldName = ResolveFieldName(field.ReferenceItemsField);
                    if (!string.IsNullOrEmpty(field.ReferenceItemsTemplate))
                    {
                        var template = ItemUri.Parse(field.ReferenceItemsTemplate);
                        mediaItem = Database.SelectSingleItem(string.Format("fast:/{3}//*[@{0}='{1}' and @@templateid='{2}']", FastQueryUtility.EscapeDashes(imageFieldName), processedValue, template.ItemID, FastQueryUtility.EscapeDashes(startPath)));
                        if (mediaItem == null)
                        {
                            mediaItem = Database.SelectSingleItem(string.Format("fast:/{2}//*[@{0}='%{1}%']", FastQueryUtility.EscapeDashes(imageFieldName), processedValue, FastQueryUtility.EscapeDashes(startPath)));
                        }
                        statusMethod(string.Format(" --- <span style=\"color:blue\">[INFO][{2}] Looking for field \"{0}\" match \"{1}\" under \"{3}\" </span>", imageFieldName, processedValue, item.ID, startPath), statusFilepath);
                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:blue\">[INFO][{2}]  Looking for field \"{0}\" match \"{1}\" under \"{3}\"  </span>", imageFieldName, processedValue, item.ID, startPath));
                    }

                    if (mediaItem != null && imageField != null)
                    {
                        if (imageField.Value != mediaItem.ID.ToString())
                        {
                            using (new EditContext(item, false, true))
                            {
                                imageField.MediaID = mediaItem.ID;
                            }
                        }
                        statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID));
                    }
                    else
                    {
                        statusMethod(string.Format(" --- <span style=\"color:red\">[FAILURE] Field \"{0}\" NOT Updated, because matching reference item was not found. ({1})</span>", targetField.DisplayName, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:red\">[FAILURE] Field \"{0}\" NOT Updated, because matching reference item was not found. ({1})</span>", targetField.DisplayName, _mapping != null ? _mapping.Name : "Unknown mapping"));
                    }
                }
            }
        }

        /// <summary>
        /// Processes the date field.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="item">The item.</param>
        /// <param name="field">The field.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <param name="targetField">The target field.</param>
        private void ProcessDateField(Dictionary<ID, object> values, Item item, ScFieldMapping field, Action<string, string> statusMethod, string statusFilepath, Field targetField)
        {
            using (new EditContext(item, false, true))
            {
                var fieldId = ConvertToId(field.TargetFields);
                var finalValue = values.ContainsKey(fieldId) ? (string)values[fieldId] : null;

                if (string.IsNullOrEmpty(finalValue))
                    return;

                DateTime columnValue;
                DateTime.TryParse(finalValue, out columnValue);

                var processedValue = RunFieldProcessingScripts(columnValue, field.ProcessingScripts);

                if (processedValue is DateTime)
                {
                    var dateField = (DateField)item.Fields[field.TargetFields];
                    dateField.Value = DateUtil.ToIsoDate((DateTime)processedValue);
                }

            }
            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
            DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID));
        }

        /// <summary>
        /// Gets the field source.
        /// </summary>
        /// <param name="sourceRawValue">The source raw value.</param>
        /// <returns></returns>
        private string GetFieldSource(string sourceRawValue)
        {
            if (string.IsNullOrWhiteSpace(sourceRawValue) || ID.IsID(sourceRawValue))
                return sourceRawValue;

            var queryString = HttpUtility.ParseQueryString(sourceRawValue);
            return queryString["StartSearchLocation"];
        }

        /// <summary>
        /// Runs the field processing scripts.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="fieldMapping">The field mapping.</param>
        private object RunFieldProcessingScripts(object value, IEnumerable<string> processingScript)
        {
            var pipelineArgs = new FieldProcessingPipelineArgs(value, processingScript, Database);
            CorePipeline.Run("xc.dataimport.fieldprocessing", pipelineArgs);
            return pipelineArgs.SourceValue;
        }

        /// <summary>
        /// Resolves the name of the field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        private string ResolveFieldName(string fieldName)
        {
            if (fieldName.StartsWith("_"))
            {
                return ReplaceFirst(fieldName, "_", "@").ToLower();
            }
            return fieldName;
        }

        /// <summary>
        /// Replaces the first.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="search">The search.</param>
        /// <param name="replace">The replace.</param>
        /// <returns></returns>
        private string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
        /// <summary>
        /// Creates the media creator options.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parentItem">The parent item.</param>
        /// <returns></returns>
        public static MediaCreatorOptions CreateMediaCreatorOptions(Item item)
        {
            var options = new MediaCreatorOptions
            {
                FileBased = false,
                IncludeExtensionInItemName = false,
                OverwriteExisting = true,
                Destination = item.Paths.FullPath,
                Database = item.Database
            };
            return options;
        }

        /// <summary>
        /// Finds the item.
        /// </summary>
        /// <param name="matchingColumnValue">The matching column value.</param>
        /// <param name="targetFieldItem">The target field item.</param>
        /// <returns></returns>
        private Item FindItem(string matchingColumnValue, Item targetFieldItem)
        {
            if (string.IsNullOrEmpty(matchingColumnValue))
                return null;

            if (!string.IsNullOrEmpty(_mapping.Target.TemplateId))
            {
                var template = ItemUri.Parse(_mapping.Target.TemplateId);
                return Database.SelectSingleItem(string.Format("fast://sitecore/content//*[@{0}='{1}' and @@templateid='{2}']", FastQueryUtility.EscapeDashes(targetFieldItem.Name), matchingColumnValue, template.ItemID));
            }
            else
            {
                return Database.SelectSingleItem(string.Format("fast://sitecore/content//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(targetFieldItem.Name), matchingColumnValue));
            }
        }

    }
}