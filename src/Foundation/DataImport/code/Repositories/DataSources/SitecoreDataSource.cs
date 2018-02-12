using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data;
using XC.Foundation.DataImport.Models;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Diagnostics;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using XC.Foundation.DataImport.Disablers;
using XC.Foundation.DataImport.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch;
using Sitecore.Data.Fields;
using XC.Foundation.DataImport.Models.Entities;
using XC.Foundation.DataImport.Repositories.Repositories;

namespace XC.Foundation.DataImport.Repositories.DataSources
{
    public class SitecoreDataSource : IDataSource
    {
        SitecoreDataSourceModel _model;

        public SitecoreDataSource() : this(new SitecoreDataSourceModel())
        {
        }

        public SitecoreDataSource(SitecoreDataSourceModel model)
        {
            _model = model;
        }

        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        public object GetSource(Action<string, string> statusMethod, string statusFilepath)
        {
            if (_model == null)
            {
                DataImportLogger.Log.Info("XC.DataImport - SitecoreDataSourceModel: datasource model is null");
                statusMethod("XC.DataImport - SitecoreDataSourceModel: datasource model is null", statusFilepath);
                return null;
            }
            try
            {
                var items = new List<ImportDataItem>();

                if (string.IsNullOrEmpty(_model.DatabaseName) || string.IsNullOrEmpty(_model.ItemPath) || string.IsNullOrEmpty(_model.TemplateId))
                {
                    DataImportLogger.Log.Info("XC.DataImport - SitecoreDataSourceModel: DatabaseName, _model.Path or _model.Template  is null");
                    statusMethod("XC.DataImport - SitecoreDataSourceModel: DatabaseName, _model.Path or _model.Template is null", statusFilepath);
                    return null;
                }

                if (Database != null)
                {
                    var items2import = GetSourceItemsForImport();
                    if (items2import == null || !items2import.Any())
                    {
                        DataImportLogger.Log.Info("XC.DataImport - SitecoreDataSourceModel: Nothing to import");
                        statusMethod("XC.DataImport - SitecoreDataSourceModel: Nothing to import", statusFilepath);
                        return null;
                    }

                    return items2import.Select(i => GetImportDataItem(i, statusMethod, statusFilepath)).ToList();
                }

                return items;
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} ({1}) (SitecoreDataSourceModel)</span>", ex.Message, ex.StackTrace), statusFilepath);
            }
            return null;
        }

        /// <summary>
        /// Gets the import data items.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        private ImportDataItem GetImportDataItem(Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            var dataItem = new ImportDataItem
            {
                ItemId = item.ID,
                Name = item.Name,
                TemplateId = item.TemplateID.ToString(),
                Fields = GetFields(item, statusMethod, statusFilepath),
                Children = _model.IncludeDescendants ? GetChildrenImportDataItems(item, statusMethod, statusFilepath).ToList() : new List<ImportDataItem>()
            };

            foreach (var languageVersion in item.Languages)
            {
                var languageItem = Database.GetItem(item.ID, languageVersion);
                if (languageItem != null)
                {
                    dataItem.LanguageVersions.Add(new ImportDataItemVersion
                    {
                        Language = languageVersion,
                        Fields = GetFields(item, statusMethod, statusFilepath),
                    });
                };
            }

            //foreach (var verNumber in item.Versions.GetVersionNumbers().OrderBy(v => v.Number))
            //{
            //    var versionItem = item.Versions[verNumber];
            //    dataItem.Versions.Add(new ImportDataItemVersion
            //    {
            //        Version = verNumber,
            //        Fields = GetFields(versionItem, statusMethod, statusFilepath),
            //    });
            //}

            return dataItem;
        }
        /// <summary>
        /// Gets the import data item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        //private ImportDataItem GetImportDataItem(Item item, Action<string, string> statusMethod, string statusFilepath)
        //{
        //    return new ImportDataItem
        //    {
        //        ItemId = item.ID,
        //        Name = item.Name,
        //        Fields = GetFields(item, statusMethod, statusFilepath),
        //        Children = _model.IncludeDescendants ? GetChildrenImportDataItems(item, statusMethod, statusFilepath).ToList() : new List<ImportDataItem>()
        //    };
        //}

        /// <summary>
        /// Gets the children import data items.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        private IEnumerable<ImportDataItem> GetChildrenImportDataItems(Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            foreach (Item childItem in item.Children)
            {
                yield return GetImportDataItem(childItem, statusMethod, statusFilepath);
            }
        }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        internal Dictionary<string, object> GetFields(Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            if (item == null)
            {
                DataImportLogger.Log.Info("XC.DataImport - GetFields: item is null");
                statusMethod("XC.DataImport - GetFields: item is null", statusFilepath);
                return null;
            }
            var fields = new Dictionary<string, object>();
            fields.Add(Templates.Fields.ItemName, item.Name);
            fields.Add(Templates.Fields.ItemDisplayName, item.Appearance.DisplayName);

            foreach (var field in item.Template.Fields.Where(f => !f.Name.Contains("__") || BaseTargetRepository.IsAllowedSystemField(f.Name)))
            {
                if (!string.IsNullOrWhiteSpace(field.Name))
                {
                    if (!fields.ContainsKey(field.Name))
                    {
                        fields.Add(field.Name, item[field.ID]);
                    }
                }
                else
                {
                    statusMethod(string.Format(" --- <span style=\"color:red\">[SKIPPED][{1}] Field \"{0}\" has empty name</span>", field.ID, item.ID), statusFilepath);
                }
            }
            return fields;
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        internal Database Database
        {
            get
            {
                if (string.IsNullOrEmpty(_model.DatabaseName))
                {
                    return null;
                }
                return Factory.GetDatabase(_model.DatabaseName);
            }
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
        internal virtual IEnumerable<Item> GetFromDatabase()
        {
            if (_model == null) return null;
            var query = string.Empty;

            using (new ItemFilteringDisabler())
            {
                if (string.IsNullOrEmpty(_model.ItemPath) && !string.IsNullOrEmpty(_model.TemplateId))
                {
                    var template = ItemUri.Parse(_model.TemplateId);
                    query = string.Format("fast:/sitecore//*[@@templateid='{0}']",
                            template.ItemID);
                }
                else if (!string.IsNullOrEmpty(_model.ItemPath) && string.IsNullOrEmpty(_model.TemplateId))
                {
                    var startItem = Database.GetItem(_model.ItemPath);
                    if (startItem != null)
                    {
                        query = string.Format("fast:{0}//*",
                                FastQueryUtility.EscapeDashes(startItem.Paths.Path));
                    }
                }
                else if (!string.IsNullOrEmpty(_model.TemplateId))
                {
                    var template = ItemUri.Parse(_model.TemplateId);
                    var startItem = Database.GetItem(_model.ItemPath);
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
        internal IEnumerable<Item> GetFromIndex()
        {
            var rootItem = Database.GetRootItem();
            using (var context = GetIndexContext(rootItem).CreateSearchContext())
            {
                var query = context.GetQueryable<SearchResultItem>();
                if (_model != null && !string.IsNullOrEmpty(_model.TemplateId))
                {
                    var template = ItemUri.Parse(_model.TemplateId);
                    query = query.Where(i => i.TemplateId == template.ItemID);
                }
                if (_model != null && !string.IsNullOrEmpty(_model.ItemPath))
                {
                    query = query.Where(i => i.Path.StartsWith(_model.ItemPath));
                }
                return query.Select(i => i.GetItem()).ToList();
            }
        }

        /// <summary>
        /// Gets the index context.
        /// </summary>
        /// <param name="rootItem">The root item.</param>
        /// <returns></returns>
        internal ISearchIndex GetIndexContext(Item rootItem)
        {
            return Database.GetRootItem().ID != rootItem.ID ? ContentSearchManager.GetIndex(new SitecoreIndexableItem(rootItem)) : ContentSearchManager.GetIndex(string.Format("sitecore_{0}_index", Database.Name));
        }

    }

}