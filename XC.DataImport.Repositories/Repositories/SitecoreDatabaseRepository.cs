using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Methods;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using XC.DataImport.Repositories.Models;
using XC.DataImport.Repositories.Diagnostics;
using XC.DataImport.Repositories.History;
using Sitecore.Buckets.Managers;
using System.Data;
using System.Security.Cryptography;
using Sitecore.IO;
using System.IO;
using Sitecore.Resources.Media;
using Sitecore.Pipelines;
using Sitecore.Text;
using Sitecore.Shell.Framework;
using Sitecore.Data.Engines;
using Sitecore.Caching;
using Sitecore.Data.Proxies;
using Sitecore.Data.Events;
using XC.DataImport.Repositories.Disablers;

namespace XC.DataImport.Repositories.Repositories
{
    public class SitecoreDatabaseRepository : BaseDatabaseRepository, ISitecoreDatabaseRepository
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
                    query = string.Format("fast:/sitecore//*[@@templateid='{0}']",
                            EscapeDashes(_mapping.Templates.Source));
                }
                else if (!string.IsNullOrEmpty(_mapping.Paths.Source) && string.IsNullOrEmpty(_mapping.Templates.Source))
                {
                    var startItem = Database.GetItem(_mapping.Paths.Source);
                    if (startItem != null)
                    {
                        query = string.Format("fast:{0}//*",
                                EscapeDashes(startItem.Paths.Path));
                    }
                }
                else
                {
                    var startItem = Database.GetItem(_mapping.Paths.Source);
                    if (startItem != null)
                    {
                        query = string.Format("fast:{1}//*[@@templateid='{0}']", _mapping.Templates.Source,
                            EscapeDashes(startItem.Paths.Path));
                    }
                    else
                    {
                        query = string.Format("fast:/sitecore//*[@@templateid='{0}']",
                        _mapping.Templates.Source);
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
        /// Escapes the dashes.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private string EscapeDashes(string path)
        {
            var newPath = new List<string>();
            var pathSegments = path.Split('/');
            foreach (var segment in pathSegments)
            {
                if (segment.Contains(" ") || segment.Contains("-"))
                {
                    newPath.Add(string.Format("#{0}#", segment));
                }
                else
                {
                    newPath.Add(segment);
                }
            }
            return string.Join("/", newPath);
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
                    query = query.Where(i => i.TemplateId == ID.Parse(_mapping.Templates.Source));
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
                var templateId = new TemplateID(ID.Parse(_mapping.Templates.Target));

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

                    MigrateChildren(item, existingItem, importStartDate, statusMethod, statusFilename);

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
        /// <param name="existingItem">The existing item.</param>
        private void UpdateExistingFields(Item item, Item existingItem, Action<string, string> statusMethod, string statusFilename)
        {
            if (item == null || existingItem == null)
                return;

            //using (new EditContext(existingItem, false, true))
            //{
                foreach (var field in item.Template.Fields.Where(f => !f.Name.Contains("__") || IsAllowedSystemField(f.Name)))
                {
                    if (!string.IsNullOrWhiteSpace(field.Name))
                    {
                        var mdsField = existingItem.Fields[field.Name] != null ? (CheckboxField)existingItem.Fields[field.Name].InnerItem.Fields["MDS Field"] : null;
                        if (mdsField != null && mdsField.Checked)
                        {
                            statusMethod(string.Format(" --- <span style=\"color:orange\">[SKIPPED][{1}] Field \"{0}\" Skipped. Reasons ( MDS Field: checked; )</span>", field.Name, item.ID), statusFilename);
                        }
                        else if (field.Name != "Blob" && existingItem.Fields[field.ID] != null && item[field.ID].Contains("xml") && field.Name != "__Renderings" && (_mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && f.Overwrite) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)))
                        {
                            using (new EditContext(existingItem, false, true))
                            {
                                existingItem.Fields[field.Name].Value = string.Empty;
                            }
                            statusMethod(string.Format(" --- <span style=\"color:blue\">[INFO][{1}] Field \"{0}\" Cleared </span>", field.Name, item.ID), statusFilename);
                        }
                        else if (field.Name != "Blob" && (mdsField == null || !mdsField.Checked) && existingItem.Fields[field.Name] != null && (_mapping.FieldMapping.All(f => f.SourceFields != field.ID.ToString()) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && f.Overwrite) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)))
                        {
                            using (new EditContext(existingItem, false, true))
                            {
                                existingItem.Fields[field.Name].Value = item[field.Name];
                            }
                            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{4}] Field \"{0}\" Updated to {5}. Reasons ( MDS Field: {1}; Target Field Exists: {2}; Included in Mapping: {3})</span>", field.Name, !(mdsField == null || !mdsField.Checked), existingItem.Fields[field.Name] != null, (_mapping.FieldMapping.All(f => f.SourceFields != field.ID.ToString()) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && f.Overwrite) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)), item.ID, item[field.ID]), statusFilename);
                        }
                        else
                        {
                            statusMethod(string.Format(" --- <span style=\"color:orange\">[SKIPPED][{4}] Field \"{0}\" Skipped. Reasons ( MDS Field: {1}; Target Field Exists: {2}; Included in Mapping: {3})</span>", field.Name, !(mdsField == null || !mdsField.Checked), existingItem.Fields[field.Name] != null, (_mapping.FieldMapping.All(f => f.SourceFields != field.ID.ToString()) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && f.Overwrite) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)), item.ID), statusFilename);
                        }
                    }
                    else
                    {
                        statusMethod(string.Format(" --- <span style=\"color:red\">[SKIPPED][{1}] Field \"{0}\" has empty name</span>", field.ID, item.ID), statusFilename);
                    }
                }

                // store origin template name
                if (existingItem.Fields["Old System Template Name"] != null)
                {
                    using (new EditContext(existingItem, false, true))
                    {
                        existingItem.Fields["Old System Template Name"].Value = item.TemplateName;
                    }
                }
            //}
        }

        /// <summary>
        /// Updates the fields.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="newItem">The new item.</param>
        private void UpdateFields(Item item, Item newItem, Action<string, string> statusMethod, string statusFilename)
        {
            if (item == null || newItem == null)
                return;

            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Updating version: {1} </strong></span>", newItem.Paths.Path, newItem.Version), statusFilename);

            //using (new EditContext(newItem, false, true))
            //{
                foreach (var field in item.Template.Fields.Where(f => !f.Name.Contains("__") || IsAllowedSystemField(f.Name)))
                {
                    if (!string.IsNullOrWhiteSpace(field.Name))
                    {
                        var mdsField = newItem.Fields[field.Name] != null ? (CheckboxField)newItem.Fields[field.Name].InnerItem.Fields["MDS Field"] : null;
                        if (mdsField != null && mdsField.Checked)
                        {
                            statusMethod(string.Format(" --- <span style=\"color:orange\">[SKIPPED][{1}] Field \"{0}\" skipped. MDS Field.</span>", field.Name, item.ID), statusFilename);
                        }
                        else if (field.Name != "Blob" && field.Name != "__Icon" && newItem.Fields[field.Name] != null && (_mapping.FieldMapping.All(f => f.SourceFields != field.ID.ToString()) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)))
                        {
                            using (new EditContext(newItem, false, true))
                            {
                                newItem.Fields[field.Name].Value = item[field.Name];
                            }
                            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{1}] Field \"{0}\" Updated to {2}</span>", field.Name, item.ID, item[field.Name]), statusFilename);
                        }
                        else
                        {
                            statusMethod(string.Format(" --- <span style=\"color:orange\">[SKIPPED][{1}] Field \"{0}\" skipped</span>", field.Name, item.ID), statusFilename);
                        }
                    }
                    else
                    {
                        statusMethod(string.Format(" --- <span style=\"color:red\">[SKIPPED][{1}] Field \"{0}\" has empty name</span>", field.ID, item.ID), statusFilename);
                    }
                }

                // store origin template name
                if (newItem.Fields["Old System Template Name"] != null)
                {
                    using (new EditContext(newItem, false, true))
                    {
                        newItem.Fields["Old System Template Name"].Value = item.TemplateName;
                    }
                }
            //}
        }
        private bool IsAllowedSystemField(string fieldName)
        {
            var allowedSystemFields = new List<string>() {
                "__Created",
                "__Updated",
                "__Updated by",
                "__Created by",
                "__Lock",
                "__Display Name",
                "__Valid from",
                "__Valid to",
                "__Hide version",
                "__Publish",
                "__Unpublish",
                "__Publishing groups",
                "__Never publish"
            };
            if (allowedSystemFields.Contains(fieldName))
                return true;
            return false;
        }
        /// <summary>
        /// Migrates the children.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="existingItem">The existing item.</param>
        private void MigrateChildren(Item item, Item parentItem, DateTime importStartDate, Action<string, string> statusMethod, string statusFilename)
        {
            if (parentItem != null && item.HasChildren)
            {
                statusMethod("<span style=\"color:green\">[INFO] Attempting child import</span>", statusFilename);

                foreach (Item child in item.Children.OrderBy(i=>i.Paths.FullPath))
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

                            if (child.Versions.Count > 1)
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
                        statusMethod(string.Format("<span style=\"color:green\">[SUCCESS] {0}</span>", existingItem.Paths.Path), statusFilename);
                    }

                    if (existingItem != null && child.HasChildren)
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
