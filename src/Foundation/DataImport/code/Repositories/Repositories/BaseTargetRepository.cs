using Newtonsoft.Json;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Pipelines;
using Sitecore.Resources.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Models.Entities;
using XC.Foundation.DataImport.Models.Mappings;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;
using XC.Foundation.DataImport.Utilities;

namespace XC.Foundation.DataImport.Repositories.Repositories
{
    public class BaseTargetRepository
    {
        internal ImportMappingModel _mapping;

        public BaseTargetRepository(ImportMappingModel mapping)
        {
            _mapping = mapping;
        }

        public IDataSourceModel Target => ConvertToTargetRepository(_mapping.Target, _mapping.TargetType);

        internal IDataSourceModel ConvertToTargetRepository(dynamic target, SourceType targetType)
        {
            return JsonConvert.DeserializeObject(target.ToString(), Type.GetType(targetType.ModelType));
        }

        internal void ChangeTemplateIfNeeded(Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            var template = ItemUri.Parse(SitecoreTarget.TemplateId);
            var templateItem = Database.GetItem(template);
            var oldTemplateName = item.TemplateName;

            if (item.TemplateID != template.ItemID)
            {
                item.ChangeTemplate(templateItem);
                statusMethod(
                    $" <span style=\"color:green\"><strong>[SUCCESS] Changed Template on {item.Paths.Path} from {oldTemplateName} to {item.TemplateName} </strong></span>", statusFilepath);
            }
        }

        internal virtual void UpdateFields(Dictionary<string, object> values, Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            if (_mapping.FieldMappings.Any() && !_mapping.ExcludeFieldMappingFields)
            {
                UpdateFieldsBasedOnFieldMappings(values, item, statusMethod, statusFilepath);
            }
            else
            {
                UpdateFieldsBasedOnValues(values, item, statusMethod, statusFilepath);
            }
        }

        /// <summary>
        /// Updates the fields based on values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void UpdateFieldsBasedOnValues(Dictionary<string, object> values, Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            if (item == null)
            {
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] UpdateFieldsBasedOnValues item is null. ({0})</span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                return;
            }
            foreach (var fieldName in values.Keys)
            {
                try
                {
                    var field = item.Fields[fieldName];
                    if (field != null)
                    {
                        if (_mapping.FieldMappings != null && _mapping.FieldMappings.Any() && _mapping.ExcludeFieldMappingFields)
                        {
                            if ((string.IsNullOrWhiteSpace(field.Value) || SitecoreTarget.OverwriteFieldValues) && _mapping.FieldMappings.Any(f => f.SourceFields == fieldName))
                            {
                                ProcessGenericField((string)values[fieldName], item, statusMethod, statusFilepath, field);
                            }
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(field.Value) || SitecoreTarget.OverwriteFieldValues || IsAllowedSystemField(field.Name))
                            {
                                ProcessGenericField((string)values[fieldName], item, statusMethod, statusFilepath, field);
                            }
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
        /// Processes the generic field.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <param name="field">The field.</param>
        private void ProcessGenericField(string value, Item item, Action<string, string> statusMethod, string statusFilepath, Field field)
        {
            using (new EditContext(item, false, true))
            {
                field.Value = value;
            }
            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", field.DisplayName, field.Value, item.ID), statusFilepath);
            DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", field.DisplayName, field.Value, item.ID));
        }

        /// <summary>
        /// Updates the fields based on field mappings.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void UpdateFieldsBasedOnFieldMappings(Dictionary<string, object> values, Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            if (item == null)
            {
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] UpdateFieldsBasedOnFieldMappings item is null. ({0})</span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                return;
            }
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
        /// Gets the field source.
        /// </summary>
        /// <param name="sourceRawValue">The source raw value.</param>
        /// <returns></returns>
        internal string GetFieldSource(string sourceRawValue)
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
        internal object RunFieldProcessingScripts(object value, IEnumerable<string> processingScript)
        {
            var pipelineArgs = new FieldProcessingPipelineArgs(value, processingScript, Database, _mapping);
            CorePipeline.Run("xc.dataimport.fieldprocessing", pipelineArgs);
            return pipelineArgs.SourceValue;
        }

        public Database Database
        {
            get
            {
                return Factory.GetDatabase(SitecoreTarget.DatabaseName);
            }
        }

        internal Item ParentItem
        {
            get
            {
                return Database.GetItem(SitecoreTarget.ItemPath);
            }
        }

        internal TargetSitecoreDataSourceModel SitecoreTarget
        {
            get
            {
                return (TargetSitecoreDataSourceModel)Target;
            }
        }


        /// <summary>
        /// Processes the attachment field.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        internal void ProcessAttachmentField(Dictionary<string, object> values, Item item, ScFieldMapping field, Action<string, string> statusMethod, string statusFilepath)
        {
            if (item.Fields[field.TargetFields].TypeKey.Contains("attachment") && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
            {
                var fieldValue = GetFieldValue(values, field);

                var processedValue = RunFieldProcessingScripts(fieldValue, field.ProcessingScripts);
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
        internal void AttachMediaStream(Item item, byte[] mediaStream, Action<string, string> statusMethod, string statusFilename)
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
        internal virtual void ProcessGenericField(Dictionary<string, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            using (new EditContext(item, false, true))
            {
                var fieldValue = GetFieldValue(values, field);

                var processedValue = RunFieldProcessingScripts(fieldValue, field.ProcessingScripts);

                if (processedValue is string)
                {
                    item.Fields[field.TargetFields].Value = (string)processedValue;
                }
                else
                {
                    item.Fields[field.TargetFields].Value = fieldValue;
                }

            }
            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
            DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID));
        }

        internal virtual string GetFieldValue(Dictionary<string, object> values, ScFieldMapping field)
        {
            return values.ContainsKey(field.TargetFields) && values[field.TargetFields] != null ? values[field.TargetFields].ToString() : string.Empty;
        }

        internal static ID ConvertToId(string field)
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
        internal void ProcessLinkField(Dictionary<string, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            var fieldValue = GetFieldValue(values, field);

            var processedValue = (string)RunFieldProcessingScripts(fieldValue, field.ProcessingScripts);
            if (!string.IsNullOrEmpty(processedValue))
            {
                var linkField = (LinkField)item.Fields[field.TargetFields];
                if (!string.IsNullOrEmpty(processedValue))
                {
                    if (!processedValue.Contains("://"))
                    {
                        processedValue = "http://" + processedValue;
                    }
                    if (linkField.Url != fieldValue)
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
        internal void ProcessTristateCheckboxField(Dictionary<string, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            var fieldValue = GetFieldValue(values, field);

            var processedValue = RunFieldProcessingScripts(fieldValue, field.ProcessingScripts);
            if (item.Fields[field.TargetFields].TypeKey.Contains("tristate"))
            {
                var finalValue = "";
                if (processedValue is bool)
                {
                    if ((bool)processedValue)
                    {
                        finalValue = "1";
                    }
                    else if (!(bool)processedValue)
                    {
                        finalValue = "0";
                    }
                }
                var tristateField = (ValueLookupField)item.Fields[field.TargetFields];
                if (finalValue != tristateField.Value)
                {
                    using (new EditContext(item, false, true))
                    {
                        tristateField.Value = finalValue;
                    }
                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
                    DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID));
                }
            }
            else if (item.Fields[field.TargetFields].TypeKey.Contains("checkbox"))
            {
                var finalValue = "";
                if (processedValue is bool)
                {
                    finalValue = (bool)processedValue ? "1" : "0";
                }
                else if (processedValue is string)
                {
                    if ((string)processedValue == "0" || (string)processedValue == "1")
                    {
                        finalValue = (string)processedValue;
                    }
                    else
                    {
                        var boolValue = false;
                        finalValue = item.Fields[field.TargetFields].ContainsStandardValue ? item.Fields[field.TargetFields].Value : "0";
                        if (bool.TryParse((string)processedValue, out boolValue))
                        {
                            finalValue = boolValue ? "1" : "0";
                        }
                    }
                }
                if (item.Fields[field.TargetFields].Value != fieldValue)
                {
                    var checkboxField = (CheckboxField)item.Fields[field.TargetFields];
                    using (new EditContext(item, false, true))
                    {
                        checkboxField.Value = finalValue;
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
        internal void ProcessReferenceField(Dictionary<string, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            var fieldValue = GetFieldValue(values, field);

            var processedValue = RunFieldProcessingScripts(fieldValue, field.ProcessingScripts);
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
        internal void ProcessNameReferenceField(Dictionary<string, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            var fieldValue = GetFieldValue(values, field);

            var processedValue = RunFieldProcessingScripts(fieldValue, field.ProcessingScripts);
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
        internal void ProcessMultivalueField(Dictionary<string, object> values, Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField)
        {
            var fieldValue = GetFieldValue(values, field);

            var processedValue = RunFieldProcessingScripts(fieldValue, field.ProcessingScripts);
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
        internal void UpdateMultilistField(Item item, Action<string, string> statusMethod, string statusFilepath, ScFieldMapping field, Field targetField, string matchingColumnValue)
        {
            if (string.IsNullOrWhiteSpace(field.ReferenceItemsField))
            {
                return;
            }
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
                matchingItem = Database.SelectSingleItem(string.Format("fast:/{3}//*[@{0}=\"{1}\" and @@templateid='{2}']", FastQueryUtility.EscapeDashes(referenceFieldName), matchingColumnValue, template.ItemID, FastQueryUtility.EscapeDashes(startPath))) ??
                               Database.SelectSingleItem(string.Format("fast:/{2}//*[@{0}='%{1}%']", FastQueryUtility.EscapeDashes(referenceFieldName), matchingColumnValue, FastQueryUtility.EscapeDashes(startPath)));
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
        internal void ProcessImageField(Dictionary<string, object> values, Item item, ScFieldMapping field, Field targetField, Action<string, string> statusMethod, string statusFilepath)
        {
            var fieldValue = GetFieldValue(values, field);

            var processedValue = (string)RunFieldProcessingScripts(fieldValue, field.ProcessingScripts);
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
        internal void ProcessDateField(Dictionary<string, object> values, Item item, ScFieldMapping field, Action<string, string> statusMethod, string statusFilepath, Field targetField)
        {
            using (new EditContext(item, false, true))
            {
                var fieldValue = GetFieldValue(values, field);

                if (string.IsNullOrEmpty(fieldValue))
                    return;

                DateTime columnValue;
                DateTime.TryParse(fieldValue, out columnValue);

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
        /// Resolves the name of the field.
        /// </summary>
        /// <param name="fieldId"></param>
        /// <param name="templateId"></param>
        /// <returns></returns>
        internal string ResolveFieldName(string fieldId)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
            {
                return string.Empty;
            }
            if (fieldId.StartsWith("_"))
            {
                return ReplaceFirst(fieldId, "_", "@").ToLower();
            }
            var fieldItem = Database.GetItem(fieldId);
            return fieldItem != null ? fieldItem.Name : string.Empty;
        }

        /// <summary>
        /// Replaces the first.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="search">The search.</param>
        /// <param name="replace">The replace.</param>
        /// <returns></returns>
        internal string ReplaceFirst(string text, string search, string replace)
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
        internal static MediaCreatorOptions CreateMediaCreatorOptions(Item item)
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
        internal Item FindItem(string matchingColumnValue, Item targetFieldItem)
        {
            if (string.IsNullOrEmpty(matchingColumnValue))
                return null;

            if (!string.IsNullOrEmpty(SitecoreTarget.TemplateId))
            {
                var template = ItemUri.Parse(SitecoreTarget.TemplateId);
                return Database.SelectSingleItem(string.Format("fast://sitecore/content//*[@{0}='{1}' and @@templateid='{2}']", FastQueryUtility.EscapeDashes(targetFieldItem.Name), matchingColumnValue, template.ItemID));
            }
            else
            {
                return Database.SelectSingleItem(string.Format("fast://sitecore/content//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(targetFieldItem.Name), matchingColumnValue));
            }
        }
        internal virtual string GetItemName(ImportDataItem values, string defaultValue)
        {
            var name = defaultValue;
            if (_mapping != null && _mapping.FieldMappings != null && _mapping.FieldMappings.Any())
            {
                var fieldNameforItemName = _mapping.FieldMappings.FirstOrDefault().SourceFields;
                name = (string)values.Fields.FirstOrDefault(i => i.Key == fieldNameforItemName).Value;
            }
            return name;
        }


        /// <summary>
        /// Determines whether [is allowed system field] [the specified field name].
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        ///   <c>true</c> if [is allowed system field] [the specified field name]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAllowedSystemField(string fieldName)
        {
            var allowedSystemFields = GetAllowedSystemFields();
            if (allowedSystemFields.Contains(fieldName))
                return true;
            return false;
        }

        /// <summary>
        /// Gets the allowed system fields.
        /// </summary>
        /// <returns></returns>
        internal static List<string> GetAllowedSystemFields()
        {
            return new List<string>() {
                "__Created",
                "__Updated",
                "__Updated by",
                "__Created by",
                "__Lock",
                "__Display name",
                "__Valid from",
                "__Valid to",
                "__Hide version",
                "__Publish",
                "__Unpublish",
                "__Publishing groups",
                "__Never publish"
            };
        }
    }
}