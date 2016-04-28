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

namespace XC.DataImport.Repositories.Databases
{
    public class SitecoreDatabaseRepository : ISitecoreDatabaseRepository
    {
        private readonly string _databaseName;
        private readonly IMappingModel _mapping;

        public SitecoreDatabaseRepository(string databaseName, IMappingModel mapping)
        {
            _databaseName = databaseName;
            _mapping = mapping;
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
                    return Settings.GetBoolSetting("XC.DataImport.UseIndex", false) ? GetFromIndex() : GetFromDatabase();
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

            if (string.IsNullOrEmpty(_mapping.Paths.Source))
                return
                    Database.SelectItems(string.Format("fast:/sitecore//*[@@templateid='{0}']",
                        _mapping.Templates.Source));

            var startItem = Database.GetItem(_mapping.Paths.Source);
            if (startItem != null)
            {
                var query = string.Format("fast:{1}//*[@@templateid='{0}']", _mapping.Templates.Source,
                    startItem.Paths.Path);
                return
                    Database.SelectItems(query);
            }

            return
                Database.SelectItems(string.Format("fast:/sitecore//*[@@templateid='{0}']",
                    _mapping.Templates.Source));
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
        public void MigrateItem(Item item, Item parentItem)
        {
            if (item == null || _mapping == null || parentItem == null || !item.Paths.IsContentItem) return;
            try
            {
                var templateId = new TemplateID(ID.Parse(_mapping.Templates.Target));

                using (new SecurityDisabler())
                {
                    var existingItem = Database.SelectSingleItem(string.Format("fast:/sitecore//*[@@id='{0}']", item.ID));
                    if (existingItem == null)
                    {
                        var newItem = ItemManager.CreateItem(item.Name, parentItem, item.TemplateID, item.ID, SecurityCheck.Disable);
                        if (newItem == null) return;

                        using (new EditContext(newItem))
                        {
                            foreach (Field field in item.Fields)
                            {
                                if (newItem.Fields[field.Name] != null && (_mapping.FieldMapping.All(f => f.SourceFields != field.ID.ToString()) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)))
                                {
                                    newItem.Fields[field.Name].Value = item[field.Name];
                                }
                            }
                        }
                        existingItem = newItem;
                    }
                    else
                    {
                        using (new EditContext(existingItem))
                        {
                            foreach (Field field in item.Fields)
                            {
                                if (existingItem.Fields[field.Name] != null && (_mapping.FieldMapping.All(f => f.SourceFields != field.ID.ToString()) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && f.Overwrite) || _mapping.FieldMapping.Any(f => f.SourceFields == field.ID.ToString() && !f.Exclude)))
                                {
                                    existingItem.Fields[field.Name].Value = item[field.Name];
                                }
                            }
                        }
                    }

                    DataImportLogger.Log.Info("XC.DataImport - item processed: Source Path - " + item.Paths.Path + "; Target path - " + existingItem.Paths.FullPath);
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
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
