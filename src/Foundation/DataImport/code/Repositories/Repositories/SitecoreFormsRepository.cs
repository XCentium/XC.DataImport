using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Models.Mappings;
using XC.Foundation.DataImport.Disablers;
using XC.Foundation.DataImport.Diagnostics;
using Sitecore.SecurityModel;
using XC.DataImport.Repositories.History;
using Sitecore.Data.Managers;
using System.Collections.ObjectModel;
using Sitecore.ExperienceForms.Client.Models.Builder;
using XC.Foundation.DataImport.Models;
using Sitecore.ExperienceForms.Mvc.Models;
using Newtonsoft.Json;
using XC.Foundation.DataImport.Models.Entities;
using Sitecore.Data.Fields;
using Sitecore;
using System.IO;
using Sitecore.Install.Files;

namespace XC.Foundation.DataImport.Repositories.Repositories
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="XC.Foundation.DataImport.Repositories.Repositories.BaseTargetRepository" />
    /// <seealso cref="XC.Foundation.DataImport.Models.DataSources.ITargetRepository" />
    public class SitecoreFormsRepository : BaseTargetRepository, ITargetRepository
    {
        public SitecoreFormsRepository(ImportMappingModel mapping) : base(mapping)
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
        /// Gets the field value.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        internal override string GetFieldValue(Dictionary<string, object> values, ScFieldMapping field)
        {
            if (values.ContainsKey(field.SourceFields))
            {
                return values[field.SourceFields].ToString();
            }
            var fieldName = field.SourceFields;
            return values.ContainsKey(fieldName) ? values[fieldName].ToString() : string.Empty;
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
            var fieldId = ConvertToId(_mapping.MergeColumnFieldMatch.Source);
            var matchingColumnValue = values.ContainsKey(fieldId.ToString()) ? values[ConvertToId(_mapping.MergeColumnFieldMatch.Source).ToString()] as string : null;

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
        /// Updates the fields.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        internal override void UpdateFields(Dictionary<string, object> values, Item item, Action<string, string> statusMethod, string statusFilepath)
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
        /// Creates the form model wrapper.
        /// </summary>
        /// <param name="formName">Name of the form.</param>
        /// <param name="item">The item.</param>
        /// <param name="parentItem">The parent item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        private ModelWrapper<string> CreateFormModelWrapper(string formName, Item item, Item parentItem, Action<string, string> statusMethod, string statusFilepath)
        {
            var model = new FormViewModel();
            model.InitializeModel(item);
            model.Title = formName;
            model.Name = formName;

            RenderingSettings renderingSettings = null;
            var settingsItem = Database.GetItem(ID.Parse(model.FieldTypeItemId), Sitecore.Context.Language);
            if (settingsItem != null)
                renderingSettings = new RenderingSettings(settingsItem);

            var viewModelWrapper = new ModelWrapper<string>
            {
                Model = JsonConvert.SerializeObject(model),
                ParentId = parentItem?.ID.ToString(),
                RenderingSettings = renderingSettings
            };
            return viewModelWrapper;
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
                    var itemName = GetItemName(values, _mapping.Name + " " + index);

                    var formModel = values.FirstOrDefault().Value as SitecoreFormModel;
                    var formPropertyFields = formModel?.Properties;
                    var formFieldProperties = formModel?.Fields;

                    var formData = new Collection<ModelWrapper<string>>();
                    var existingItem = Database.GetItem(itemId);
                    if (existingItem == null)
                    {
                        var itemNameMapping = _mapping.FieldMappings.FirstOrDefault();
                        if (itemNameMapping != null)
                        {
                            var processedValue = RunFieldProcessingScripts(itemName, itemNameMapping.ProcessingScripts);
                            if (processedValue is string && !string.IsNullOrEmpty((string)processedValue))
                            {
                                itemName = (string)processedValue;
                            }
                        }

                        if (!string.IsNullOrEmpty(itemName) && !string.IsNullOrEmpty(SitecoreTarget.TemplateId))
                        {
                            var template = ItemUri.Parse(SitecoreTarget.TemplateId);
                            var templateId = template.ItemID;

                            // creating form item
                            var newForm = ItemManager.CreateItem(ItemUtil.ProposeValidItemName(itemName), parentItem, templateId, itemId);
                            if (newForm == null) return null;
                            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created </strong></span>", newForm.Paths.Path), statusFilepath);


                            // creating form page item
                            var formPage = ItemManager.CreateItem("Page", newForm, ID.Parse(Templates.FormTemplates.Page));
                            if (formPage == null) return null;
                            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created </strong></span>", formPage.Paths.Path), statusFilepath);

                            UpdateFields(formPropertyFields, newForm, statusMethod, statusFilepath);

                            UpdateChildFields(formFieldProperties, formPage, statusMethod, statusFilepath);
                            UpdateSubmitActions(formModel, formPage, statusMethod, statusFilepath);
                            existingItem = newForm;
                        }
                        else
                        {
                            statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> Templates.Target or itemName are not defined. ({1})</span>", parentItem.Paths.Path, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                        }
                    }
                    else
                    {
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] Updating Fields on {0} </strong></span>", existingItem.Paths.Path), statusFilepath);
                        var formPage = existingItem.Children.FirstOrDefault(i => i.TemplateID == ID.Parse(Templates.FormTemplates.Page));

                        UpdateFields(formPropertyFields, existingItem, statusMethod, statusFilepath);
                        UpdateChildFields(formFieldProperties, formPage, statusMethod, statusFilepath);
                        UpdateSubmitActions(formModel, formPage, statusMethod, statusFilepath);
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

        /// <summary>
        /// Updates the submit actions.
        /// </summary>
        /// <param name="formModel">The form model.</param>
        /// <param name="formPage">The form page.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void UpdateSubmitActions(SitecoreFormModel formModel, Item formPage, Action<string, string> statusMethod, string statusFilepath)
        {
            if (formModel == null || formPage == null || formModel.SubmitActions == null || !formModel.SubmitActions.Any())
            {
                statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> UpdateSubmitActions: SubmitActions either null or empty. ({1})</span>", formPage.Paths.Path, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
            }
            var submitButtonItem = formPage.Axes.GetDescendants().FirstOrDefault(i => i.TemplateID.ToString() == Templates.FormTemplates.Button && i[Templates.Fields.FieldType] == Items.ItemIds.SubmitFieldTypeItemId);
            if (submitButtonItem != null)
            {
                var definitionFolder = EnsureDefinitionFolder(submitButtonItem);
                foreach (var actionKey in formModel.SubmitActions.Keys)
                {
                    if (formModel.SubmitActions[actionKey] is SitecoreFieldModel action)
                    {
                        var actionItem = Database.GetItem(action.ItemId);
                        if (actionItem == null)
                        {
                            actionItem = ItemManager.CreateItem(ItemUtil.ProposeValidItemName(action.Name), definitionFolder, GetFieldTemplateId(action), ID.Parse(action.ItemId));
                            if (actionItem == null) return;

                            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created </strong></span>", actionItem.Paths.Path), statusFilepath);
                            UpdateFieldProperties(actionItem, action, statusMethod, statusFilepath);
                        }
                        else
                        {
                            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Updating </strong></span>", actionItem.Paths.Path), statusFilepath);
                            UpdateFieldProperties(actionItem, action, statusMethod, statusFilepath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ensures the definition folder.
        /// </summary>
        /// <param name="submitButtonItem">The submit button item.</param>
        /// <returns></returns>
        private static Item EnsureDefinitionFolder(Item submitButtonItem)
        {
            var submitDefinitionItem = submitButtonItem.GetChildren().FirstOrDefault(i => i.TemplateID.ToString() == Templates.FormTemplates.Folder);
            if (submitDefinitionItem != null)
            {
                return submitDefinitionItem;
            }
            return ItemManager.CreateItem(Templates.Fields.SubmitActionFolder, submitButtonItem, ID.Parse(Templates.FormTemplates.SubmitActionFolder));
        }

        /// <summary>
        /// Updates the child fields.
        /// </summary>
        /// <param name="fields">The fields.</param>
        /// <param name="formPageItem">The form page item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void UpdateChildFields(Dictionary<string, object> fields, Item formPageItem, Action<string, string> statusMethod, string statusFilepath)
        {
            if (fields == null || !fields.Any())
            {
                statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> UpdateChildFields: fields either null or empty. ({1})</span>", formPageItem.Paths.Path, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
            }

            foreach (var fieldName in fields.Keys)
            {
                if (fields[fieldName] is SitecoreFieldModel field)
                {
                    var fieldId = field.ItemId;
                    var fieldItem = Database.GetItem(field.ItemId);
                    if (fieldItem == null)
                    {
                        // creating field item
                        fieldItem = ItemManager.CreateItem(ItemUtil.ProposeValidItemName(fieldName), formPageItem, GetFieldTemplateId(field), ID.Parse(field.ItemId));
                        if (fieldItem == null) return;

                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created </strong></span>", fieldItem.Paths.Path), statusFilepath);

                        UpdateFieldProperties(fieldItem, field, statusMethod, statusFilepath);
                    }
                    else
                    {
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Updated </strong></span>", fieldItem.Paths.Path), statusFilepath);

                        UpdateFieldProperties(fieldItem, field, statusMethod, statusFilepath);
                    }

                    if (field.Fields != null && field.Fields.Any())
                    {
                        UpdateChildFields(field.Fields, fieldItem, statusMethod, statusFilepath);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the field template identifier.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        private ID GetFieldTemplateId(SitecoreFieldModel field)
        {
            switch (field.FieldType)
            {
                case SitecoreFieldModel.FormFieldType.SingleLineText:
                    return ID.Parse(Templates.FormTemplates.Input);
                case SitecoreFieldModel.FormFieldType.Button:
                    return ID.Parse(Templates.FormTemplates.Button);
                case SitecoreFieldModel.FormFieldType.Text:
                    return ID.Parse(Templates.FormTemplates.Text);
                case SitecoreFieldModel.FormFieldType.TextArea:
                    return ID.Parse(Templates.FormTemplates.TextArea);
                case SitecoreFieldModel.FormFieldType.Checkbox:
                    return ID.Parse(Templates.FormTemplates.Checkbox);
                case SitecoreFieldModel.FormFieldType.Dropdown:
                    return ID.Parse(Templates.FormTemplates.Dropdown);
                case SitecoreFieldModel.FormFieldType.Email:
                    return ID.Parse(Templates.FormTemplates.Email);
                case SitecoreFieldModel.FormFieldType.Radio:
                    return ID.Parse(Templates.FormTemplates.Input);
                case SitecoreFieldModel.FormFieldType.Section:
                    return ID.Parse(Templates.FormTemplates.Section);
                case SitecoreFieldModel.FormFieldType.Action:
                    return ID.Parse(Templates.FormTemplates.SubmitActionDefinition);
                default:
                    return ID.Null;
            }
        }

        /// <summary>
        /// Updates the field properties.
        /// </summary>
        /// <param name="fieldItem">The field item.</param>
        /// <param name="field">The field.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void UpdateFieldProperties(Item fieldItem, SitecoreFieldModel field, Action<string, string> statusMethod, string statusFilepath)
        {
            try
            {
                foreach (var propKey in field.Properties.Keys)
                {
                    if (propKey == Templates.Fields.Options)
                    {
                        ProcessOptions(fieldItem, field.Properties[propKey] as string[], statusMethod, statusFilepath);
                    }
                    else
                    {
                        var targetField = fieldItem.Fields[propKey];
                        if (targetField != null)
                        {
                            if (targetField.TypeKey.Contains("date"))
                            {
                                ProcessDateField((string)field.Properties[propKey], fieldItem, targetField, statusMethod, statusFilepath);
                            }
                            else if (targetField.TypeKey.Contains("tristate") || targetField.TypeKey.Contains("checkbox"))
                            {
                                ProcessTristateCheckboxField((string)field.Properties[propKey], fieldItem, targetField, statusMethod, statusFilepath);
                            }
                            else
                            {
                                ProcessGenericField((string)(field.Properties[propKey]), fieldItem, targetField, statusMethod, statusFilepath);
                            }
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

        /// <summary>
        /// Processes the options.
        /// </summary>
        /// <param name="fieldItem">The field item.</param>
        /// <param name="options">The options.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void ProcessOptions(Item fieldItem, string[] options, Action<string, string> statusMethod, string statusFilepath)
        {
            if(options == null || fieldItem == null)
            {
                statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] </strong> ProcessOptions: options or fieldItem either null or empty. ({0})</span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
            }
            var optionsPath = string.Concat(fieldItem.Paths.FullPath, "/Settings/Datasource");
            var datasourceFolderItem = Database.CreateItemPath(optionsPath);
            if(datasourceFolderItem != null)
            {
                foreach(var option in options)
                {
                    var optionPath = string.Concat(fieldItem.Paths.FullPath, option);
                    var optionItem = Database.GetItem(PathUtils.UnifyPathSeparators(optionPath));
                    if (optionItem != null)
                    {
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Updating </strong></span>", optionItem.Paths.Path), statusFilepath);
                        UpdateOptionItem(optionItem, option, statusMethod, statusFilepath);
                    }
                    else
                    {
                        optionItem = datasourceFolderItem.Add(ItemUtil.ProposeValidItemName(option), new TemplateID(ID.Parse(Templates.FormTemplates.ExtendedListItem)));
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created </strong></span>", optionItem.Paths.Path), statusFilepath);
                        UpdateOptionItem(optionItem, option, statusMethod, statusFilepath);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the option item.
        /// </summary>
        /// <param name="optionItem">The option item.</param>
        /// <param name="value">The value.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void UpdateOptionItem(Item optionItem, string value, Action<string, string> statusMethod, string statusFilepath)
        {
            if(optionItem == null || string.IsNullOrEmpty(value))
            {
                statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] </strong> UpdateOptionItem: optionItem or value either null or empty. ({0})</span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
            }
            using(new EditContext(optionItem))
            {
                optionItem["Value"] = value;
                optionItem["__Display name"] = value;
            }
            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Updated </strong></span>", optionItem.Paths.Path), statusFilepath);
        }

        /// <summary>
        /// Processes the tristate checkbox field.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="fieldItem">The field item.</param>
        /// <param name="targetField">The target field.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void ProcessTristateCheckboxField(string value, Item fieldItem, Field targetField, Action<string, string> statusMethod, string statusFilepath)
        {
            if (targetField.TypeKey.Contains("tristate"))
            {
                var finalValue = !string.IsNullOrWhiteSpace(value) ? value.ToLowerInvariant() == "yes" || value.ToLowerInvariant() == "true" || value.ToLowerInvariant() == "1" ? "1" : "0" : string.Empty;

                var tristateField = (ValueLookupField)targetField;
                if (finalValue != tristateField.Value)
                {
                    using (new EditContext(fieldItem))
                    {
                        tristateField.Value = finalValue;
                    }
                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" on {3}  </span>", targetField.DisplayName, targetField.Value, fieldItem.ID, fieldItem.Paths.FullPath), statusFilepath);
                    DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\"  on {3} </span>", targetField.DisplayName, targetField.Value, fieldItem.ID, fieldItem.Paths.FullPath));
                }
            }
            else if (targetField.TypeKey.Contains("checkbox"))
            {
                var processedValue = value.ToLowerInvariant() == "yes" || value.ToLowerInvariant() == "true" || value.ToLowerInvariant() == "1" ? true : false;
                var finalValue = processedValue ? "1" : "0";

                if (targetField.Value != finalValue)
                {
                    var checkboxField = (CheckboxField)targetField;
                    using (new EditContext(fieldItem))
                    {
                        checkboxField.Value = finalValue;
                    }

                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\"  on {3} </span>", targetField.DisplayName, targetField.Value, fieldItem.ID, fieldItem.Paths.FullPath), statusFilepath);
                    DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\"  on {3} </span>", targetField.DisplayName, targetField.Value, fieldItem.ID, fieldItem.Paths.FullPath));
                }
            }
        }

        /// <summary>
        /// Processes the generic field.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="fieldItem">The field item.</param>
        /// <param name="targetField">The target field.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void ProcessGenericField(string value, Item fieldItem, Field targetField, Action<string, string> statusMethod, string statusFilepath)
        {
            if (fieldItem == null || targetField == null)
            {
                return;
            }
            using (new EditContext(fieldItem))
            {
                targetField.Value = value;
            }
            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" on {3} </span>", targetField.DisplayName, targetField.Value, fieldItem.ID, fieldItem.Paths.FullPath), statusFilepath);
            DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" on {3} </span>", targetField.DisplayName, targetField.Value, fieldItem.ID, fieldItem.Paths.FullPath));
        }

        /// <summary>
        /// Processes the date field.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="fieldItem">The field item.</param>
        /// <param name="targetField">The target field.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        private void ProcessDateField(string value, Item fieldItem, Field targetField, Action<string, string> statusMethod, string statusFilepath)
        {
            if (fieldItem == null || targetField == null)
            {
                return;
            }
            using (new EditContext(fieldItem))
            {
                var fieldValue = value;

                if (string.IsNullOrEmpty(fieldValue))
                    return;

                DateTime columnValue;
                DateTime.TryParse(fieldValue, out columnValue);

                var dateField = (DateField)targetField;
                dateField.Value = DateUtil.ToIsoDate(columnValue);
            }
            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" on {3}</span>", targetField.DisplayName, targetField.Value, fieldItem.ID, fieldItem.Paths.FullPath), statusFilepath);
            DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" on {3}</span>", targetField.DisplayName, targetField.Value, fieldItem.ID, fieldItem.Paths.FullPath));
        }

        /// <summary>
        /// Gets the name of the item.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        internal override string GetItemName(Dictionary<string, object> values, string defaultValue)
        {
            var name = defaultValue;
            if (_mapping != null && _mapping.FieldMappings != null && _mapping.FieldMappings.Any())
            {
                var fieldNameforItemName = _mapping.FieldMappings.FirstOrDefault().SourceFields;
                name = (string)values.FirstOrDefault(i => i.Key == fieldNameforItemName).Value;
                if (name == null && values.FirstOrDefault().Value is ISitecoreFieldModel)
                {
                    var model = values.FirstOrDefault().Value as ISitecoreFieldModel;
                    name = (string)model?.Properties?.FirstOrDefault(i => i.Key == fieldNameforItemName).Value;
                }
            }
            return name == null ? defaultValue : name;
        }
    }
}