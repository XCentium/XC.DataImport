using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using XC.Foundation.DataImport.Models;
using XC.DataImport.Repositories.History;
using System.Data;
using XC.Foundation.DataImport;
using Sitecore.Resources.Media;
using Sitecore.Pipelines;
using Sitecore.Data.Proxies;
using XC.Foundation.DataImport.Disablers;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Utilities;
using System.Web;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;

namespace XC.DataImport.Repositories.Repositories
{
    public class SitecoreDatabaseRepository : ISitecoreDatabaseRepository
    {
        private readonly string _databaseName;
        private readonly IMappingModel _mapping;

        public SitecoreDatabaseRepository(string databaseName, IMappingModel mapping)
        {
            _databaseName = databaseName;
            _mapping = mapping as IMappingModel;
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Item> GetSourceItemsForImport()
        {
            try
            {
                if (Database != null)
                {
                    return Settings.GetBoolSetting("XC.DataImport.UseIndex", false) ? GetFromIndex().OrderBy(i => i.Paths.FullPath) : GetFromDatabase().OrderBy(i => i.Paths.FullPath);
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return null;
        }

        /// <summary>
        /// Gets from database.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Item> GetFromDatabase()
        {
            if (_mapping == null) return null;
            var query = string.Empty;

            using (new ItemFilteringDisabler())
            {
                if (string.IsNullOrEmpty(_mapping.Paths.Source) && !string.IsNullOrEmpty(_mapping.Templates.Source))
                {
                    var template = ItemUri.Parse(_mapping.Templates.Source);
                    query = string.Format("fast:/sitecore//*[@@templateid='{0}']",
                            template.ItemID);
                }
                else if (!string.IsNullOrEmpty(_mapping.Paths.Source) && string.IsNullOrEmpty(_mapping.Templates.Source))
                {
                    var startItem = Database.GetItem(_mapping.Paths.Source);
                    if (startItem != null)
                    {
                        query = string.Format("fast:{0}//*",
                                FastQueryUtility.EscapeDashes(startItem.Paths.Path));
                    }
                }
                else if (!string.IsNullOrEmpty(_mapping.Templates.Source))
                {
                    var template = ItemUri.Parse(_mapping.Templates.Source);
                    var startItem = Database.GetItem(_mapping.Paths.Source);
                    if (startItem != null)
                    {
                        query = string.Format("fast:{1}//*[@@templateid='{0}']", template.ItemID,
                            FastQueryUtility.EscapeDashes(startItem.Paths.Path));
                    }
                    else
                    {
                        query = string.Format("fast:/sitecore//*[@@templateid='{0}']",
                        template.ItemID);
                    }
                }
                if (!string.IsNullOrEmpty(query))
                {
                    return
                        Database.SelectItems(query);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets from index.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Item> GetFromIndex()
        {
            var rootItem = Database.GetRootItem();
            using (var context = GetIndexContext(rootItem).CreateSearchContext())
            {
                var query = context.GetQueryable<SearchResultItem>();
                if (_mapping.Templates != null && !string.IsNullOrEmpty(_mapping.Templates.Source))
                {
                    var template = ItemUri.Parse(_mapping.Templates.Source);
                    query = query.Where(i => i.TemplateId == template.ItemID);
                }
                if (_mapping.Paths != null && !string.IsNullOrEmpty(_mapping.Paths.Source))
                {
                    query = query.Where(i => i.Path.StartsWith(_mapping.Paths.Source));
                }
                return query.Select(i => i.GetItem()).ToList();
            }
        }

        /// <summary>
        /// Migrates the items.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parentItem"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public Item MigrateItem(Item item, Item parentItem, DateTime importStartDate, Action<string, string> statusMethod, string statusFilename)
        {
            if (item == null || _mapping == null || parentItem == null)
            {
                statusMethod(" <span style=\"color:red\">[FAILURE] Not Imported or Updated: either item, _mapping, parentItem is null or item is not a content item</span>", statusFilename);
                return null;
            }
            if (!item.Paths.FullPath.StartsWith("/sitecore/"))
            {
                statusMethod(" <span style=\"color:red\">[FAILURE] Source item is an orphan.</span>", statusFilename);
                return null;
            }
            try
            {
                var template = ItemUri.Parse(_mapping.Templates.Target);
                var templateId = new TemplateID(template.ItemID);

                using (new ProxyDisabler(true))
                using (new ItemFilteringDisabler())
                {
                    var existingItem = parentItem.Database.GetItem(item.ID);

                    // there is a bug somewhere in Sitecore data access where item that doesn't exist in target DB comes up as existing
                    if (existingItem != null && !existingItem.Paths.FullPath.Contains(parentItem.Paths.FullPath))
                    {
                        existingItem = null;
                    }

                    if (existingItem == null)
                    {
                        existingItem = ItemManager.CreateItem(ItemUtil.ProposeValidItemName(item.Name), parentItem, templateId, item.ID, SecurityCheck.Disable);
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created </strong></span>", existingItem.Paths.Path), statusFilename);
                        if (item.Versions.Count > 1)
                        {
                            foreach (var verNumber in item.Versions.GetVersionNumbers().OrderBy(v => v.Number))
                            {
                                var version = existingItem.Versions[verNumber] != null ? existingItem.Versions[verNumber] : existingItem.Versions.AddVersion();
                                ProcessExistingItem(item.Versions[verNumber], statusMethod, statusFilename, version);
                            }
                        }
                        else
                        {
                            ProcessExistingItem(item.Versions.GetLatestVersion(), statusMethod, statusFilename, existingItem);
                        }
                    }
                    else if (item.Paths.IsMediaItem && !item.TemplateName.ToLowerInvariant().Contains("folder"))
                    {
                        UpdateExistingFields(item.Versions.GetLatestVersion(), existingItem, statusMethod, statusFilename);
                        //AttachMediaStream(item.Versions.GetLatestVersion(), existingItem, statusMethod, statusFilename);
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] Updating Fields on {0} </strong></span>", existingItem.Paths.Path), statusFilename);
                    }
                    else
                    {
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] Updating Fields on {0} </strong></span>", existingItem.Paths.Path), statusFilename);
                        if (item.Versions.Count > 1)
                        {
                            foreach (var verNumber in item.Versions.GetVersionNumbers())
                            {
                                var version = existingItem.Versions[verNumber] != null ? existingItem.Versions[verNumber] : existingItem.Versions.AddVersion();
                                UpdateFields(item.Versions[verNumber], version, statusMethod, statusFilename);
                            }
                        }
                        else
                        {
                            UpdateFields(item.Versions.GetLatestVersion(), existingItem, statusMethod, statusFilename);
                        }
                    }

                    DataImportLogger.Log.Info("XC.DataImport - item processed: Source Path - " + item.Paths.Path + "; Target path - " + existingItem.Paths.FullPath);
                    HistoryLogging.ItemMigrated(item, existingItem, importStartDate, _mapping.Name);

                    if (existingItem != null && item.HasChildren && _mapping.MigrateDescendants)
                    {
                        MigrateChildren(item, existingItem, importStartDate, statusMethod, statusFilename);
                    }

                    return existingItem;

                }
            }
            catch (Exception ex)
            {
                statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE]{1}: {0}</strong></span>", item.Paths.Path, item.ID), statusFilename);
                statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE]{1}: {0}</strong></span>", ex.Message, item.ID), statusFilename);
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return null;
        }

        /// <summary>
        /// Processes the existing item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilename">The status filename.</param>
        /// <param name="existingItem">The existing item.</param>
        private void ProcessExistingItem(Item item, Action<string, string> statusMethod, string statusFilename, Item existingItem)
        {
            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Updating version: {1} </strong></span>", existingItem.Paths.Path, existingItem.Version), statusFilename);

            UpdateFields(item, existingItem, statusMethod, statusFilename);

            if (item.Paths.IsMediaItem)
            {
                AttachMediaStream(item, existingItem, statusMethod, statusFilename);
            }
        }

        /// <summary>
        /// Attaches the media stream.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="existingItem">The existing item.</param>
        private void AttachMediaStream(Item item, Item existingItem, Action<string, string> statusMethod, string statusFilename)
        {
            if (item == null || existingItem == null)
            {
                statusMethod("<span style=\"color:red\"><strong>[FAILURE] item or existingItem is null</strong></span>", statusFilename);
                return;
            }

            if (existingItem.TemplateID == Sitecore.TemplateIDs.MediaFolder)
                return;

            var options = CreateMediaCreatorOptions(existingItem);
            var creator = new Sitecore.Resources.Media.MediaCreator();
            var sourceMediaItem = new MediaItem(item);
            var sourceMediaStream = sourceMediaItem.GetMediaStream();

            if (sourceMediaStream == null)
            {
                sourceMediaStream = Sitecore.Reflection.Nexus.DataApi.GetBlobStream(Guid.Parse(item.Fields["Blob"].Value), item.Database);
            }

            if (sourceMediaStream != null)
            {
                DetachMedia(existingItem);
                var updatedItem = creator.AttachStreamToMediaItem(sourceMediaStream, existingItem.Paths.FullPath, sourceMediaItem.Name + "." + sourceMediaItem.Extension, options);
                statusMethod("<span style=\"color:green\"><strong>[SUCCESS] Updating Attached Media </strong></span>", statusFilename);
            }
            else
            {
                statusMethod("<span style=\"color:red\"><strong>[FAILURE] Stream is null</strong></span>", statusFilename);
            }
        }

        private void DetachMedia(MediaItem mediaItem)
        {
            MediaUri mediaUri = MediaUri.Parse(mediaItem);
            Media media = MediaManager.GetMedia(mediaUri);
            if (media != null)
            {
                media.ReleaseStream();
            }
        }

        protected virtual MediaStream GetStreamFromPipeline(MediaOptions options, MediaData mediaData)
        {
            Assert.IsNotNull((object)options, "options");
            try
            {
                GetMediaStreamPipelineArgs streamPipelineArgs = new GetMediaStreamPipelineArgs(mediaData, options);
                CorePipeline.Run("getMediaStream", (PipelineArgs)streamPipelineArgs);
                return streamPipelineArgs.OutputStream;
            }
            catch (Exception ex)
            {
                Log.Error("Could not run the 'getMediaStream' pipeline for '" + mediaData.MediaItem.InnerItem.Paths.Path + "'. Original media data will be used.", ex, typeof(SitecoreDatabaseRepository));
            }
            return mediaData.GetStream();
        }

        /// <summary>
        /// Creates the media creator options.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parentItem">The parent item.</param>
        /// <returns></returns>
        private static Sitecore.Resources.Media.MediaCreatorOptions CreateMediaCreatorOptions(Item item)
        {
            var options = new Sitecore.Resources.Media.MediaCreatorOptions
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
        /// Updates the existing fields.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="item">The existing item.</param>
        /// <summary>
        /// Updates the fields.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="item">The new item.</param>
        private void UpdateFields(Item sourceItem, Item targetItem, Action<string, string> statusMethod, string statusFilename)
        {
            lock (_mapping.FieldMapping)
            {
                if (_mapping.MigrateAllFields)
                {
                    foreach (var field in sourceItem.Template.Fields.Where(f => !f.Name.Contains("__") || IsAllowedSystemField(f.Name)))
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(field.Name))
                            {
                                if (field.Name != "Blob" && field.Name != "__Icon" && targetItem.Fields[field.Name] != null && (_mapping.FieldMapping.All(f => f.SourceFields != field.ID.ToString()) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)))
                                {
                                    ProcessField(sourceItem, targetItem, new Field(field.ID, sourceItem));
                                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{1}] Field \"{0}\" Updated to {2}</span>", field.Name, targetItem.ID, targetItem[field.Name]), statusFilename);
                                }
                                else
                                {
                                    statusMethod(string.Format(" --- <span style=\"color:orange\">[SKIPPED][{1}] Field \"{0}\" skipped</span>", field.Name, targetItem.ID), statusFilename);
                                }
                            }
                            else
                            {
                                statusMethod(string.Format(" --- <span style=\"color:red\">[SKIPPED][{1}] Field \"{0}\" has empty name</span>", field.ID, targetItem.ID), statusFilename);
                            }
                        }
                        catch (Exception ex)
                        {
                            statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0} ({1})</strong</span>", ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilename);
                            DataImportLogger.Log.Error(ex.Message, ex);
                        }
                    }
                }
                else
                {
                    foreach (var field in _mapping.FieldMapping)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(field.TargetFields))
                            {
                                var sitecoreField = targetItem.Fields[field.TargetFields];
                                if (sitecoreField.Name != "Blob" && sitecoreField.Name != "__Icon" && targetItem.Fields[sitecoreField.Name] != null && !field.Exclude)
                                {
                                    ProcessField(sourceItem, targetItem, sitecoreField);
                                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{1}] Field \"{0}\" Updated to {2}</span>", sitecoreField.Name, targetItem.ID, targetItem[sitecoreField.Name]), statusFilename);
                                }
                                else
                                {
                                    statusMethod(string.Format(" --- <span style=\"color:orange\">[SKIPPED][{1}] Field \"{0}\" skipped</span>", sitecoreField.Name, targetItem.ID), statusFilename);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0} ({1})</strong</span>", ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilename);
                            DataImportLogger.Log.Error(ex.Message, ex);
                        }
                    }
                    ProcessSystemFields(sourceItem, targetItem, statusMethod, statusFilename);
                }
            }
            //}
        }

        /// <summary>
        /// Processes the system fields.
        /// </summary>
        /// <param name="sourceItem">The source item.</param>
        /// <param name="targetItem">The target item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilename">The status filename.</param>
        private void ProcessSystemFields(Item sourceItem, Item targetItem, Action<string, string> statusMethod, string statusFilename)
        {
            foreach(var fieldName in GetAllowedSystemFields())
            {
                using (new EditContext(targetItem, false, true))
                {
                    targetItem.Fields[fieldName].Value = sourceItem[fieldName];
                }
            }
        }

        /// <summary>
        /// Gets the processing scripts.
        /// </summary>
        /// <param name="fieldId">The field identifier.</param>
        /// <returns></returns>
        private IEnumerable<string> GetProcessingScripts(ID fieldId)
        {
            if (_mapping.FieldMapping != null)
            {
                var matchingField = _mapping.FieldMapping.FirstOrDefault(f => f.SourceFields == fieldId.ToString() && !f.Exclude);
                if (matchingField != null)
                {
                    return matchingField.ProcessingScripts;
                }
            }
            return null;
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
            return pipelineArgs.Result;
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
        /// Updates the existing fields.
        /// </summary>
        /// <param name="sourceItem">The item.</param>
        /// <param name="targetItem">The existing item.</param>
        private void UpdateExistingFields(Item sourceItem, Item targetItem, Action<string, string> statusMethod, string statusFilename)
        {
            if (sourceItem == null || targetItem == null)
                return;

            if (_mapping.MigrateAllFields) {
                foreach (var field in sourceItem.Template.Fields.Where(f => !f.Name.Contains("__") || IsAllowedSystemField(f.Name)))
                {
                    if (!string.IsNullOrWhiteSpace(field.Name))
                    {
                        if (field.Name != "Blob" && targetItem.Fields[field.ID] != null && sourceItem[field.ID].Contains("xml") && field.Name != "__Renderings" && (_mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && f.Overwrite) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)))
                        {
                            using (new EditContext(targetItem, false, true))
                            {
                                targetItem.Fields[field.Name].Value = string.Empty;
                            }
                            statusMethod(string.Format(" --- <span style=\"color:blue\">[INFO][{1}] Field \"{0}\" Cleared </span>", field.Name, sourceItem.ID), statusFilename);
                        }
                        else if (field.Name != "Blob" && targetItem.Fields[field.Name] != null && (_mapping.FieldMapping.All(f => f.SourceFields != field.ID.ToString()) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && f.Overwrite) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)))
                        {
                            ProcessField(sourceItem, targetItem, new Field(field.ID, sourceItem));
                            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{3}] Field \"{0}\" Updated to {4}. Reasons ( Target Field Exists: {1}; Included in Mapping: {2})</span>", field.Name, targetItem.Fields[field.Name] != null, (_mapping.FieldMapping.All(f => f.SourceFields != field.ID.ToString()) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && f.Overwrite) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)), sourceItem.ID, sourceItem[field.ID]), statusFilename);
                        }
                        else
                        {
                            statusMethod(string.Format(" --- <span style=\"color:orange\">[SKIPPED][{3}] Field \"{0}\" Skipped. Reasons ( Target Field Exists: {1}; Included in Mapping: {2})</span>", field.Name, targetItem.Fields[field.Name] != null, (_mapping.FieldMapping.All(f => f.SourceFields != field.ID.ToString()) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && f.Overwrite) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)), sourceItem.ID), statusFilename);
                        }
                    }
                    else
                    {
                        statusMethod(string.Format(" --- <span style=\"color:red\">[SKIPPED][{1}] Field \"{0}\" has empty name</span>", field.ID, sourceItem.ID), statusFilename);
                    }
                }
            }
            else
            {
                foreach (var field in _mapping.FieldMapping)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(field.TargetFields))
                        {
                            var sitecoreField = targetItem.Fields[field.TargetFields];
                            if (sitecoreField.Name != "Blob" && sitecoreField.Name != "__Icon" && targetItem.Fields[sitecoreField.Name] != null && !field.Exclude)
                            {
                                ProcessField(sourceItem, targetItem, sitecoreField);
                                statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{1}] Field \"{0}\" Updated to {2}</span>", sitecoreField.Name, targetItem.ID, targetItem[sitecoreField.Name]), statusFilename);
                            }
                            else
                            {
                                statusMethod(string.Format(" --- <span style=\"color:orange\">[SKIPPED][{1}] Field \"{0}\" skipped</span>", sitecoreField.Name, targetItem.ID), statusFilename);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0} ({1})</strong</span>", ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilename);
                        DataImportLogger.Log.Error(ex.Message, ex);
                    }
                }
            }
        }

        /// <summary>
        /// Processes the field.
        /// </summary>
        /// <param name="sourceItem">The source item.</param>
        /// <param name="targetItem">The target item.</param>
        /// <param name="field">The field.</param>
        private void ProcessField(Item sourceItem, Item targetItem, Field field)
        {
            var valueToImport = sourceItem[field.Name];
            var processingScripts = GetProcessingScripts(field.ID);
            var processedValue = (string)RunFieldProcessingScripts(valueToImport, processingScripts);

            using (new EditContext(targetItem, false, true))
            {
                targetItem.Fields[field.Name].Value = processedValue;
            }
        }

        /// <summary>
        /// Determines whether [is allowed system field] [the specified field name].
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        ///   <c>true</c> if [is allowed system field] [the specified field name]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsAllowedSystemField(string fieldName)
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
        private List<string> GetAllowedSystemFields()
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
        /// <summary>
        /// Migrates the children.
        /// </summary>
        /// <param name="childItem">The item.</param>
        /// <param name="existingItem">The existing item.</param>
        private void MigrateChildren(Item childItem, Item parentItem, DateTime importStartDate, Action<string, string> statusMethod, string statusFilename)
        {
            if (parentItem != null && childItem.HasChildren && childItem.Children.Any(x => x.TemplateID != ID.Parse(ConfigSettings.ContentFolder)))
            {
                statusMethod("<span style=\"color:green\"><strong>[INFO] Attempting child import</strong></span>", statusFilename);

                foreach (Item child in childItem.Children.OrderBy(i => i.Paths.FullPath))
                {
                    var existingItem = Database.SelectSingleItem(string.Format("fast:/sitecore//*[@@id='{0}']", child.ID));

                    //using (new CacheWriteDisabler())
                    //using (new DisableCachePopulationSwitcher())
                    //using (new ProxyDisabler())
                    //using (new DatabaseCacheDisabler())
                    //using (new SyncOperationContext())
                    {
                        if (existingItem == null)
                        {
                            var newItem = ItemManager.CreateItem(ItemUtil.ProposeValidItemName(child.Name), parentItem, child.TemplateID, child.ID, SecurityCheck.Disable);
                            if (newItem == null) return;

                            if (child.Versions.Count > 1 && _mapping.MigrateAllVersions)
                            {
                                foreach (var verNumber in child.Versions.GetVersionNumbers().OrderBy(v => v.Number))
                                {
                                    var version = existingItem.Versions[verNumber] != null ? existingItem.Versions[verNumber] : existingItem.Versions.AddVersion();
                                    UpdateFields(child.Versions[verNumber], newItem, statusMethod, statusFilename);
                                }
                            }
                            else
                            {
                                UpdateFields(child.Versions.GetLatestVersion(), newItem, statusMethod, statusFilename);
                                existingItem = newItem;
                            }

                        }
                        else
                        {
                            if (child.Versions.Count > 1)
                            {
                                foreach (var verNumber in child.Versions.GetVersionNumbers())
                                {
                                    var version = existingItem.Versions[verNumber] != null ? existingItem.Versions[verNumber] : existingItem.Versions.AddVersion();
                                    UpdateFields(child.Versions[verNumber], existingItem, statusMethod, statusFilename);
                                }
                            }
                            else
                            {
                                UpdateFields(child.Versions.GetLatestVersion(), existingItem, statusMethod, statusFilename);
                            }
                        }

                        DataImportLogger.Log.Info("XC.DataImport - item processed: Source Path - " + child.Paths.Path + "; Target path - " + existingItem.Paths.FullPath);
                        HistoryLogging.ItemMigrated(child, existingItem, importStartDate, _mapping.Name);
                        statusMethod(string.Format("<span style=\"color:green\"><strong>[SUCCESS] {0} was imported</strong></span>", existingItem.Paths.Path), statusFilename);
                    }

                    if (existingItem != null && child.HasChildren && _mapping.MigrateDescendants)
                    {
                        MigrateChildren(child, existingItem, importStartDate, statusMethod, statusFilename);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the index context.
        /// </summary>
        /// <param name="rootItem">The root item.</param>
        /// <returns></returns>
        private ISearchIndex GetIndexContext(Item rootItem)
        {
            return Database.GetRootItem().ID != rootItem.ID ? ContentSearchManager.GetIndex(new SitecoreIndexableItem(rootItem)) : ContentSearchManager.GetIndex(string.Format("sitecore_{0}_index", Database.Name));
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public Database Database
        {
            get
            {
                return !string.IsNullOrEmpty(_databaseName) ? Factory.GetDatabase(_databaseName) : null;
            }
        }

    }
}
