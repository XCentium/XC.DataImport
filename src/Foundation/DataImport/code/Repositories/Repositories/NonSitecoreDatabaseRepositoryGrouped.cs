using XC.Foundation.DataImport.Disablers;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Proxies;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using XC.DataImport.Repositories.History;
using XC.Foundation.DataImport.Models;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Utilities;

namespace XC.DataImport.Repositories.Repositories
{
    public class NonSitecoreDatabaseRepositoryGrouped : INonSitecoreDatabaseRepository 
    {
        private readonly string _databaseName;
        private readonly INonSitecoreMappingModel _mapping;
        private const string _prefix = "XC.DataImport_";

        public bool DetailedLogging { get; protected set; }
        private bool? _incrementalUpdate { get; set; }

        public DateTime IncrementalUpdateLastRun { get; set; }
        public List<Item> ItemsToProcess { get; set; }
        public Dictionary<ID, Dictionary<ID, string>> UpdatedItems;
        public DateTime? _lastUpdated { get; set; }

        public NonSitecoreDatabaseRepositoryGrouped(string databaseName, INonSitecoreMappingModel mapping, ref Dictionary<ID, Dictionary<ID, string>> updatedItems, bool detailedLogging, DateTime? lastUpdated, bool? incrementalUpdate = null)
        {
            _databaseName = databaseName;
            _mapping = mapping;
            DetailedLogging = detailedLogging;
            _incrementalUpdate = incrementalUpdate;
            UpdatedItems = updatedItems;
            _lastUpdated = lastUpdated;
        }

        /// <summary>
        /// Migrates the items.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parentItem"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public Item MigrateItem(DataRow row, Item parentItem, DateTime importStartDate, int index, Action<string, string> statusMethod, string statusFilepath)
        {
            if (row == null || _mapping == null || parentItem == null)
            {
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] Not Imported or Updated: either item, _mapping, parentItem is null or item is not a content item. ({0})</span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                return null;
            }
            try
            {
                using (new ItemFilteringDisabler())
                {
                    var idFieldFromMapping = _mapping.FieldMapping.FirstOrDefault(f => f.IsId);
                    if (idFieldFromMapping == null && !_mapping.MergeWithExistingItems)
                    {
                        return null;
                    }
                    var sitecoreId = ID.NewID;

                    if (_mapping.MergeWithExistingItems)
                    {
                        return UpdateExistingItem(row, importStartDate, statusMethod, statusFilepath);
                    }
                    else
                        return ProcessSeparateItem(row, parentItem, importStartDate, index, statusMethod, statusFilepath, idFieldFromMapping, ref sitecoreId);
                }

            }
            catch (Exception ex)
            {
                statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> {1} ({2})</span>", parentItem.Paths.Path, ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return null;
        }

        /// <summary>
        /// Processes the separate item.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="parentItem">The parent item.</param>
        /// <param name="importStartDate">The import start date.</param>
        /// <param name="index">The index.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <param name="idFieldFromMapping">The identifier field from mapping.</param>
        /// <param name="sitecoreId">The sitecore identifier.</param>
        /// <returns></returns>
        private Item ProcessSeparateItem(DataRow row, Item parentItem, DateTime importStartDate, int index, Action<string, string> statusMethod, string statusFilepath, NonScFieldMapping idFieldFromMapping, ref ID sitecoreId)
        {
            var id = row[idFieldFromMapping.SourceFields].ToString();
            if (!string.IsNullOrEmpty(id))
            {
                sitecoreId = StringToID(_mapping.Name + id);
            }

            using (new SecurityDisabler())
            {
                try
                {
                    //using (new EventDisabler())
                    //{
                    var existingItem = Database.GetItem(sitecoreId);
                    if (existingItem == null)
                    {
                        var itemName = _mapping.Name + " " + index;
                        var itemNameMapping = _mapping.FieldMapping.FirstOrDefault();
                        if (itemNameMapping != null)
                        {
                            itemName = row[itemNameMapping.SourceFields] != DBNull.Value ? row[itemNameMapping.SourceFields].ToString() : "";
                        }
                        if (itemName.All(Char.IsDigit))
                        {
                            // if name is plural convert it to singular
                            itemName = string.Concat(_mapping.Name.TrimEnd('s'), " ", itemName);
                        }
                        if (!string.IsNullOrEmpty(itemName) && !string.IsNullOrEmpty(_mapping.Templates.Target))
                        {
                            var templateId = ID.Parse(_mapping.Templates.Target);
                            var newItem = ItemManager.CreateItem(ItemUtil.ProposeValidItemName(itemName), parentItem, templateId, sitecoreId);
                            if (newItem == null) return null;

                            if (ItemsToProcess != null)
                                ItemsToProcess.Add(newItem);

                            if (!UpdatedItems.ContainsKey(newItem.ID) || UpdatedItems[newItem.ID] == null)
                            {
                                UpdatedItems[newItem.ID] = new Dictionary<ID, string>();
                            }

                            if (DetailedLogging)
                                statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Created </strong></span>", newItem.Paths.Path), statusFilepath);

                            UpdateFields(row, newItem, statusMethod, statusFilepath);
                            existingItem = newItem;
                        }
                        else
                        {
                            statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> Tamplates.Target or itemName are not defined. ({1})</span>", parentItem.Paths.Path, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                        }
                    }
                    else
                    {
                        if (DetailedLogging)
                            statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] Updating Fields on {0} </strong></span>", existingItem.Paths.Path), statusFilepath);

                        if (!UpdatedItems.ContainsKey(existingItem.ID) || UpdatedItems[existingItem.ID] == null)
                        {
                            UpdatedItems[existingItem.ID] = new Dictionary<ID, string>();
                        }

                        UpdateExistingFields(row, existingItem, statusMethod, statusFilepath);
                    }

                    DataImportLogger.Log.Info("XC.DataImport - item processed: Target path - " + existingItem.Paths.FullPath);
                    HistoryLogging.ItemMigrated(row, existingItem, importStartDate, "nonsitecore-" + _mapping.Name);

                    return existingItem;
                }
                catch (Exception ex)
                {
                    statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0}</strong> {1} ({2})</span>", parentItem.Paths.Path, ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                    DataImportLogger.Log.Error(ex.Message, ex);
                }
            }
            //}
            return null;
        }

        /// <summary>
        /// Updates the existing item.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="importStartDate">The import start date.</param>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        private Item UpdateExistingItem(DataRow row, DateTime importStartDate, Action<string, string> statusMethod, string statusFilepath)
        {
            var matchingColumnValue = row[_mapping.MergeColumnFieldMatch.Source] as string;
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
                    if (DetailedLogging)
                    {
                        statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} not found ({1})</strong></span>", matchingColumnValue, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                    }
                    return null;
                }

                if (existingItem != null)
                {
                    if (!UpdatedItems.ContainsKey(existingItem.ID) || UpdatedItems[existingItem.ID] == null)
                    {
                        UpdatedItems[existingItem.ID] = new Dictionary<ID, string>();
                    }
 
                    if (DetailedLogging)
                    {
                        statusMethod(string.Format(" <span style=\"color:green\"><strong>[SUCCESS] {0} Updated </strong></span>", existingItem.Paths.Path), statusFilepath);
                    }

                    UpdateExistingFields(row, existingItem, statusMethod, statusFilepath);

                    DataImportLogger.Log.Info("XC.DataImport - item processed: Target path - " + existingItem.Paths.FullPath);
                    HistoryLogging.ItemMigrated(row, existingItem, importStartDate, "nonsitecore-" + _mapping.Name);
                }
            }
            return existingItem;
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

            if (ItemsToProcess == null)
            {
                if (!string.IsNullOrEmpty(_mapping.Paths.Target))
                {
                    return Database.SelectSingleItem(string.Format("fast:/{2}//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(targetFieldItem.Name), matchingColumnValue, FastQueryUtility.EscapeDashes(_mapping.Paths.Target)));
                }
                else if (!string.IsNullOrEmpty(_mapping.Templates.Target))
                {
                    var templateId = ID.Parse(_mapping.Templates.Target);
                    return Database.SelectSingleItem(string.Format("fast://sitecore/content//*[@{0}='{1}' and @@templateid='{2}']", FastQueryUtility.EscapeDashes(targetFieldItem.Name), matchingColumnValue, _mapping.Templates.Target));
                }
                else
                {
                    return Database.SelectSingleItem(string.Format("fast://sitecore/content//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(targetFieldItem.Name), matchingColumnValue));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(_mapping.Templates.Target))
                {
                    var templateId = ID.Parse(_mapping.Templates.Target);
                    return ItemsToProcess.FirstOrDefault(i => i.TemplateID == templateId && i[targetFieldItem.Name] == matchingColumnValue);
                }
                else
                {
                    return ItemsToProcess.FirstOrDefault(i => i[targetFieldItem.Name] == matchingColumnValue);
                }
            }
        }

        /// <summary>
        /// Gets the source items for import.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public DataTable GetDataSet(Action<string, string> statusMethod, string statusFilename, Tuple<string, string, string> filter = null)
        {
            if (_mapping == null || _mapping.Databases == null || string.IsNullOrEmpty(_mapping.Databases.Source))
            {
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] Not Imported or Updated: either  _mapping, _mapping.Databases, _mapping.Databases.Source is null or item is not a content item ({0})</span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilename);
                return null;
            }
            var dataTable = new DataTable();

            try
            {
                var startDate = DateTime.Now;
                using (SqlConnection connection = new SqlConnection(_mapping.Databases.Source))
                {
                    var commandText = _mapping.Templates.Source.Command;
                    commandText = AdjustCommandForIncrementalUpdate(commandText);
                    if (filter != null)
                    {
                        commandText = AddCodeFilter(commandText, filter);
                    }
                    DataImportLogger.Log.Info("XC.DataImport - SQL statement: " + commandText);

                    dataTable = HttpRuntime.Cache[commandText] as DataTable;
                    if (dataTable != null)
                    {
                        return dataTable;
                    }

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        connection.Open();

                        if (_mapping.Templates.Source.IsStoredProc)
                        {
                            command.CommandType = System.Data.CommandType.StoredProcedure;
                        }
                        else
                        {
                            command.CommandType = System.Data.CommandType.Text;
                        }
                        command.CommandTimeout = 0;

                        var dataSet = new DataSet();
                        using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                        {
                            dataAdapter.SelectCommand = command;
                            dataAdapter.Fill(dataSet);
                            if (dataSet.Tables != null && dataSet.Tables.Count > 0)
                                HttpRuntime.Cache[commandText] = dataSet.Tables[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] Error occured during data retrieval from the source system: {0} ({1})</span>", ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilename);
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return dataTable;
        }

        /// <summary>
        /// Gets the source items for import count.
        /// </summary>
        /// <returns></returns>
        public int GetSourceItemsForImportCount(Action<string, string> statusMethod, string statusFilename, Tuple<string, string, string> filter = null)
        {
            int rowCount = 0;

            if (_mapping == null || _mapping.Databases == null || string.IsNullOrEmpty(_mapping.Databases.Source))
            {
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] Not Imported or Updated: either  _mapping, _mapping.Databases, _mapping.Databases.Source is null or item is not a content item. ({0})</span>", _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilename);
                return rowCount;
            }
            try
            {
                var startDate = DateTime.Now;
                using (SqlConnection connection = new SqlConnection(_mapping.Databases.Source))
                {
                    var commandText = _mapping.Templates.Source.Command;
                    commandText = AdjustCommandForIncrementalUpdate(commandText);
                    if (filter != null)
                    {
                        commandText = AddCodeFilter(commandText, filter);
                    }
                    DataImportLogger.Log.Info("XC.DataImport - SQL statement: " + commandText);

                    var dataSet = GetDataSet(statusMethod, statusFilename, filter);
                    if (dataSet != null)
                    {
                        return dataSet.Rows.Count;
                    }

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        if (_mapping.Templates.Source.IsStoredProc)
                        {
                            command.CommandType = System.Data.CommandType.StoredProcedure;
                        }
                        else
                        {
                            command.CommandType = System.Data.CommandType.Text;
                        }
                        connection.Open();
                        command.CommandTimeout = 0;

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rowCount++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] Error occured during data retrieval from the source system: {0} ({1})</span>", ex.Message, _mapping.Templates.Source.Command), statusFilename);
                DataImportLogger.Log.Error(ex.Message, ex);
            }
            return rowCount;
        }

        /// <summary>
        /// Adjusts the command for incremental update.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <returns></returns>
        private string AdjustCommandForIncrementalUpdate(string commandText)
        {
            var lastSyncDate = _lastUpdated.HasValue ? _lastUpdated.Value.ToString() : HistoryLogging.NonSitecoreGetLatestRunDateString(_mapping);
            if ((_mapping.IncrementalUpdate || (IncrementalUpdate && !string.IsNullOrEmpty(_mapping.IncrementalUpdateSourceColumn))) && !string.IsNullOrWhiteSpace(lastSyncDate))
            {
                commandText = commandText.Contains("WHERE") ? string.Format(" {0} AND {1} >= '{2}' ", commandText, _mapping.IncrementalUpdateSourceColumn, lastSyncDate) : string.Format("{0} WHERE {1} >= '{2}' ", commandText, _mapping.IncrementalUpdateSourceColumn, lastSyncDate);
            }
            return commandText;
        }

        /// <summary>
        /// Adds the code filter.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        private string AddCodeFilter(string commandText, Tuple<string, string, string> filter)
        {
            if (filter == null)
                return commandText;
            return commandText = commandText.IndexOf("WHERE", StringComparison.CurrentCultureIgnoreCase) != -1 ? string.Format(" {0} AND [{1}] = '{2}' ", commandText, filter.Item1, filter.Item3) : string.Format(" {0} WHERE [{1}] = '{2}'  ", commandText, filter.Item1, filter.Item3);
        }

        /// <summary>
        /// Updates the existing fields.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="item">The existing item.</param>
        private void UpdateExistingFields(DataRow row, Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            //using (new EditContext(item, false, true))
            //{
                lock (_mapping.FieldMapping)
                foreach (var field in _mapping.FieldMapping)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(field.TargetFields) && item.Fields[field.TargetFields] != null && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                        {
                            var targetField = item.Fields[field.TargetFields];
                            if (item.Fields[field.TargetFields].TypeKey.Contains("multilist") && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                            {
                                var matchingColumnValue = row[field.SourceFields] != DBNull.Value ? row[field.SourceFields].ToString() : null;
                                if (!string.IsNullOrEmpty(matchingColumnValue))
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
                                        matchingItem = Database.SelectSingleItem(string.Format("fast:/{3}//*[@{0}=\"{1}\" and @@templateid='{2}']", FastQueryUtility.EscapeDashes(referenceFieldName), matchingColumnValue, field.ReferenceItemsTemplate, FastQueryUtility.EscapeDashes(startPath)));
                                        if (matchingItem == null)
                                        {
                                            matchingItem = Database.SelectSingleItem(string.Format("fast:/{2}//*[@{0}='%{1}%']", FastQueryUtility.EscapeDashes(referenceFieldName), matchingColumnValue, FastQueryUtility.EscapeDashes(startPath)));
                                        }
                                        if (DetailedLogging)
                                        {
                                            statusMethod(string.Format(" --- <span style=\"color:blue\">[INFO][{2}] Looking for field \"{0}\" match \"{1}\" under \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath), statusFilepath);
                                        }
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
                                            if (DetailedLogging)
                                            {
                                                statusMethod(string.Format(" --- <span style=\"color:blue\">[INFO][{2}] Looking for field \"{0}\" match \"{1}\"  under \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath), statusFilepath);
                                            }
                                            DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:blue\">[INFO][{2}]  Looking for field \"{0}\" match \"{1}\"  under \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath));
                                        }
                                        else
                                        {
                                            if (DetailedLogging)
                                            {
                                                statusMethod(string.Format(" --- <span style=\"color:red\">[FAILURE][{2}] referenceFieldName is empty: \"{0}\"  \"{1}\"   \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath), statusFilepath);
                                            }
                                            DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:red\">[FAILURE][{2}] referenceFieldName is empty:  \"{0}\"  \"{1}\"   \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath));
                                        }
                                    }
                                    if (matchingItem != null && multilistField != null)
                                    {

                                        var previousValue = UpdatedItems.ContainsKey(item.ID) && UpdatedItems[item.ID].ContainsKey(multilistField.InnerField.ID) ? UpdatedItems[item.ID][multilistField.InnerField.ID] : null;
                                        if (!string.IsNullOrWhiteSpace(previousValue))
                                        {
                                            var arrayOfValues = previousValue.Split('|').ToList();
                                            arrayOfValues.Add(matchingItem.ID.ToString());
                                            UpdatedItems[item.ID][multilistField.InnerField.ID] = string.Join("|", arrayOfValues);
                                        }
                                        else
                                        {
                                            UpdatedItems[item.ID][multilistField.InnerField.ID] = matchingItem.ID.ToString();
                                        }
                                        var newValue = UpdatedItems.ContainsKey(item.ID) && UpdatedItems[item.ID].ContainsKey(multilistField.InnerField.ID) ? UpdatedItems[item.ID][multilistField.InnerField.ID] : null;
                                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, newValue, item.ID));
                                    }
                                    else
                                    {
                                        if (DetailedLogging)
                                        {
                                            statusMethod(string.Format(" --- <span style=\"color:red\">[FAILURE] Field \"{0}\" NOT Updated, because matching reference item was not found. ({1})</span>", targetField.DisplayName, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                                        }
                                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:red\">[FAILURE] Field \"{0}\" NOT Updated, because matching reference item was not found. ({1})</span>", targetField.DisplayName, _mapping != null ? _mapping.Name : "Unknown mapping"));
                                    }
                                }
                            }
                            else if ((item.Fields[field.TargetFields].TypeKey.Contains("droplink") || item.Fields[field.TargetFields].TypeKey.Contains("reference")) && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                            {
                                var matchingColumnValue = row[field.SourceFields] != DBNull.Value ? row[field.SourceFields].ToString() : null;
                                if (!string.IsNullOrEmpty(matchingColumnValue))
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
                                        matchingItem = Database.SelectSingleItem(string.Format("fast:/{3}//*[@{0}='{1}' and @@templateid='{2}']", FastQueryUtility.EscapeDashes(referenceFieldName), matchingColumnValue, field.ReferenceItemsTemplate, FastQueryUtility.EscapeDashes(startPath)));
                                        if (matchingItem == null)
                                        {
                                            matchingItem = Database.SelectSingleItem(string.Format("fast:/{2}//*[@{0}='%{1}%']", FastQueryUtility.EscapeDashes(referenceFieldName), matchingColumnValue, FastQueryUtility.EscapeDashes(startPath)));
                                        }
                                        if (DetailedLogging)
                                        {
                                            statusMethod(string.Format(" --- <span style=\"color:blue\">[INFO][{2}] Looking for field \"{0}\" match \"{1}\" under \"{3}\" </span>", referenceFieldName, matchingColumnValue, item.ID, startPath), statusFilepath);
                                        }
                                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:blue\">[INFO][{2}]  Looking for field \"{0}\" match \"{1}\" under \"{3}\"  </span>", referenceFieldName, matchingColumnValue, item.ID, startPath));
                                    }

                                    if (matchingItem != null && referenceField != null)
                                    {
                                        UpdatedItems[item.ID][referenceField.InnerField.ID] = matchingItem.ID.ToString();
                                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, matchingItem.ID.ToString(), item.ID));
                                    }
                                    else
                                    {
                                        if (DetailedLogging)
                                        {
                                            statusMethod(string.Format(" --- <span style=\"color:red\">[FAILURE] Field \"{0}\" NOT Updated, because matching reference item was not found. ({1})</span>", targetField.DisplayName, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                                        }
                                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:red\">[FAILURE] Field \"{0}\" NOT Updated, because matching reference item was not found. ({1})</span>", targetField.DisplayName, _mapping != null ? _mapping.Name : "Unknown mapping"));
                                    }
                                }
                            }
                            else if ((item.Fields[field.TargetFields].TypeKey.Contains("tristate") || item.Fields[field.TargetFields].TypeKey.Contains("checkbox")) && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                            {
                                var matchingColumnValue = row[field.SourceFields] != DBNull.Value ? row[field.SourceFields].ToString() : null;
                                if (!string.IsNullOrEmpty(matchingColumnValue))
                                {
                                    if (item.Fields[field.TargetFields].TypeKey.Contains("tristate"))
                                    {
                                        var fieldValue = "";
                                        if (matchingColumnValue == "true")
                                        {
                                            fieldValue = "1";
                                        }
                                        else if (matchingColumnValue == "false")
                                        {
                                            fieldValue = "0";
                                        }

                                        var tristateField = (ValueLookupField)item.Fields[field.TargetFields];
                                        UpdatedItems[item.ID][tristateField.InnerField.ID] = fieldValue;
                                        if (DetailedLogging)
                                        {
                                            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, fieldValue, item.ID), statusFilepath);
                                        }
                                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, fieldValue, item.ID));
                                    }
                                    else if (item.Fields[field.TargetFields].TypeKey.Contains("checkbox"))
                                    {
                                        var boolValue = false;
                                        var fieldValue = item.Fields[field.TargetFields].ContainsStandardValue ? item.Fields[field.TargetFields].Value : "0";
                                        if (bool.TryParse(matchingColumnValue, out boolValue))
                                        {
                                            fieldValue = boolValue ? "1" : "0";
                                        }
                                        if (item.Fields[field.TargetFields].Value != fieldValue)
                                        {
                                            var checkboxField = (CheckboxField)item.Fields[field.TargetFields];
                                            UpdatedItems[item.ID][checkboxField.InnerField.ID] = fieldValue;
                                        }
                                        if (DetailedLogging)
                                        {
                                            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, fieldValue, item.ID), statusFilepath);
                                        }
                                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, fieldValue, item.ID));
                                    }
                                }
                            }
                            else if (item.Fields[field.TargetFields].TypeKey.Contains("general link") && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                            {
                                var matchingColumnValue = row[field.SourceFields] != DBNull.Value ? row[field.SourceFields].ToString() : null;
                                if (!string.IsNullOrEmpty(matchingColumnValue))
                                {
                                    var linkField = (LinkField)item.Fields[field.TargetFields];
                                    if (!string.IsNullOrEmpty(matchingColumnValue))
                                    {
                                        if (!matchingColumnValue.Contains("://"))
                                        {
                                            matchingColumnValue = "http://" + matchingColumnValue;
                                        }
                                        UpdatedItems[item.ID][linkField.InnerField.ID] = matchingColumnValue;

                                        if (DetailedLogging)
                                        {
                                            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, matchingColumnValue, item.ID), statusFilepath);
                                        }
                                        DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, matchingColumnValue, item.ID));
                                    }
                                }
                            }
                            else if (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite)
                            {
                                if (!UpdatedItems.ContainsKey(item.ID))
                                {
                                    UpdatedItems[item.ID] = new Dictionary<ID, string>();
                                }
                                UpdatedItems[item.ID][ID.Parse(field.TargetFields)] = row[field.SourceFields].ToString();

                                if (DetailedLogging)
                                {
                                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, row[field.SourceFields].ToString(), item.ID), statusFilepath);
                                }
                                DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, row[field.SourceFields].ToString(), item.ID));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0} ({1})</strong></span>", ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                        DataImportLogger.Log.Error(ex.Message, ex);
                    }
                }
            //}
        }

        /// <summary>
        /// Updates the fields.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="item">The new item.</param>
        private void UpdateFields(DataRow row, Item item, Action<string, string> statusMethod, string statusFilepath)
        {
            //using (new EditContext(item, false, true))
            //{
                lock (_mapping.FieldMapping)
                foreach (var field in _mapping.FieldMapping)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(field.TargetFields) && item.Fields[field.TargetFields] != null)
                        {
                            var targetField = item.Fields[field.TargetFields];
                            if (row[field.SourceFields] is bool)
                            {
                                var val = (bool)row[field.SourceFields];
                                UpdatedItems[item.ID][ID.Parse(field.TargetFields)] = val ? "1" : "0";
                                if (DetailedLogging)
                                {
                                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, item.Fields[field.TargetFields].Value, item.ID), statusFilepath);
                                }
                            }
                            else if (item.Fields[field.TargetFields].TypeKey.Contains("multilist"))
                            {
                                var matchingColumnValue = row[field.SourceFields] as string;
                                if (!string.IsNullOrEmpty(matchingColumnValue))
                                {
                                    var multilistField = (MultilistField)item.Fields[field.TargetFields];
                                    var startPath = "sitecore";
                                    var fieldSource = item.Database.GetItem(GetFieldSource(multilistField.InnerField.Source));
                                    if (fieldSource != null)
                                    {
                                        startPath = fieldSource.Paths.FullPath;
                                    }

                                    if (!string.IsNullOrWhiteSpace(field.ReferenceItemsTemplate))
                                    {
                                        var matchingItem = Database.SelectSingleItem(string.Format("fast://{3}//*[@{0}='{1}' and @@templateid='{2}']", FastQueryUtility.EscapeDashes(ResolveFieldName(field.ReferenceItemsField)), matchingColumnValue, field.ReferenceItemsTemplate, startPath));
                                        if (matchingItem != null && multilistField != null)
                                        {
                                            var previousValue = UpdatedItems.ContainsKey(item.ID) && UpdatedItems[item.ID].ContainsKey(multilistField.InnerField.ID) ? UpdatedItems[item.ID][multilistField.InnerField.ID] : null;
                                            if (!string.IsNullOrWhiteSpace(previousValue))
                                            {
                                                var arrayOfValues = previousValue.Split('|').ToList();
                                                arrayOfValues.Add(matchingItem.ID.ToString());
                                                UpdatedItems[item.ID][multilistField.InnerField.ID] = string.Join("|", arrayOfValues);
                                            }
                                            else
                                            {
                                                UpdatedItems[item.ID][multilistField.InnerField.ID] = matchingItem.ID.ToString();
                                            }
                                            if (DetailedLogging)
                                            {
                                                var newValue = UpdatedItems.ContainsKey(item.ID) && UpdatedItems[item.ID].ContainsKey(multilistField.InnerField.ID) ? UpdatedItems[item.ID][multilistField.InnerField.ID] : null;
                                                statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\".</span>", targetField.DisplayName, newValue), statusFilepath);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var matchingItem = Database.SelectSingleItem(string.Format("fast://{2}//*[@{0}='{1}']", FastQueryUtility.EscapeDashes(ResolveFieldName(field.ReferenceItemsField)), matchingColumnValue, startPath));
                                        if (matchingItem != null && multilistField != null)
                                        {
                                            var previousValue = UpdatedItems.ContainsKey(item.ID) && UpdatedItems[item.ID].ContainsKey(multilistField.InnerField.ID) ? UpdatedItems[item.ID][multilistField.InnerField.ID] : null;
                                            if (!string.IsNullOrWhiteSpace(previousValue))
                                            {
                                                var arrayOfValues = previousValue.Split('|').ToList();
                                                arrayOfValues.Add(matchingItem.ID.ToString());
                                                UpdatedItems[item.ID][multilistField.InnerField.ID] = string.Join("|", arrayOfValues);
                                            }
                                            else
                                            {
                                                UpdatedItems[item.ID][multilistField.InnerField.ID] = matchingItem.ID.ToString();
                                            }
                                            if (DetailedLogging)
                                            {
                                                var newValue = UpdatedItems.ContainsKey(item.ID) && UpdatedItems[item.ID].ContainsKey(multilistField.InnerField.ID) ? UpdatedItems[item.ID][multilistField.InnerField.ID] : null;
                                                statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\"</span>", targetField.DisplayName, newValue, item.ID), statusFilepath);
                                            }
                                        }
                                    }
                                }
                            }
                            else if ((item.Fields[field.TargetFields].TypeKey.Contains("tristate") || item.Fields[field.TargetFields].TypeKey.Contains("checkbox")) && (string.IsNullOrWhiteSpace(item[field.TargetFields]) || field.Overwrite))
                            {
                                var matchingColumnValue = row[field.SourceFields] != DBNull.Value ? row[field.SourceFields].ToString() : null;
                                if (!string.IsNullOrEmpty(matchingColumnValue))
                                {
                                    if (item.Fields[field.TargetFields].TypeKey.Contains("tristate"))
                                    {
                                        var fieldValue = "";
                                        if (matchingColumnValue == "true")
                                        {
                                            fieldValue = "1";
                                        }
                                        else if (matchingColumnValue == "false")
                                        {
                                            fieldValue = "0";
                                        }
                                        var tristateField = (ValueLookupField)item.Fields[field.TargetFields];
                                        if (fieldValue != tristateField.Value)
                                        {
                                            UpdatedItems[item.ID][tristateField.InnerField.ID] = fieldValue;

                                            if (DetailedLogging)
                                            {
                                                statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, fieldValue, item.ID), statusFilepath);
                                            }
                                            DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, fieldValue, item.ID));
                                        }
                                    }
                                    else if (item.Fields[field.TargetFields].TypeKey.Contains("checkbox"))
                                    {
                                        var boolValue = false;
                                        var fieldValue = item.Fields[field.TargetFields].ContainsStandardValue ? item.Fields[field.TargetFields].Value : "0";
                                        if (bool.TryParse(matchingColumnValue, out boolValue))
                                        {
                                            fieldValue = boolValue ? "1" : "0";
                                        }
                                        if (item.Fields[field.TargetFields].Value != fieldValue)
                                        {
                                            var checkboxField = (CheckboxField)item.Fields[field.TargetFields];
                                            UpdatedItems[item.ID][checkboxField.InnerField.ID] = fieldValue;
                                            if (DetailedLogging)
                                            {
                                                statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, fieldValue, item.ID), statusFilepath);
                                            }
                                            DataImportLogger.Log.Info(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\" </span>", targetField.DisplayName, fieldValue, item.ID));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                UpdatedItems[item.ID][ID.Parse(field.TargetFields)] = row[field.SourceFields].ToString();

                                if (DetailedLogging)
                                {
                                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{2}] Field \"{0}\" Updated to \"{1}\"</span>", targetField.DisplayName, row[field.SourceFields].ToString(), item.ID), statusFilepath);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        statusMethod(string.Format("<span style=\"color:red\"><strong>[FAILURE] {0} ({1})</strong</span>", ex.Message, _mapping != null ? _mapping.Name : "Unknown mapping"), statusFilepath);
                        DataImportLogger.Log.Error(ex.Message, ex);
                    }
                }
            //}
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

        /// <summary>
        /// Strings to identifier.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public ID StringToID(string value)
        {
            Assert.ArgumentNotNull((object)value, "value");
            return new ID(new Guid(MD5.Create().ComputeHash(Encoding.Default.GetBytes(_prefix + value))));
        }
        public void ClearMultilistFieldValues(Action<string, string> statusMethod, string statusFilepath, Item item)
        {
            if (_mapping == null || !_mapping.MergeWithExistingItems) return;

            if (item == null)
                return;

            using (new SecurityDisabler())
            {
                lock (_mapping.FieldMapping)
                {
                    foreach (var field in _mapping.FieldMapping.ToList())
                    {
                        if (!string.IsNullOrEmpty(field.TargetFields))
                        {
                            var itemField = item.Fields[field.TargetFields];
                            if (itemField != null && itemField.TypeKey.Contains("multilist"))
                            {
                                using (new EditContext(item, false, true))
                                {
                                    item.Fields[field.TargetFields].Value = string.Empty;
                                }
                                if (DetailedLogging)
                                {
                                    statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{1}] Field \"{0}\" cleared for {2}.</span>", item.Fields[field.TargetFields].Name, item.ID, item.Paths.FullPath), statusFilepath);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears the multilist field values.
        /// </summary>
        public void ClearMultilistFieldValues(Action<string, string> statusMethod, string statusFilepath, DataTable dataSet, Tuple<string, string, string> filter = null)
        {
            if (_mapping == null || !_mapping.MergeWithExistingItems) return;

            if (ItemsToProcess == null)
                return;

            using (new SecurityDisabler())
            {
                IEnumerable<Item> items = null;
                if (filter != null)
                {
                    items = !string.IsNullOrEmpty(filter.Item2) && !string.IsNullOrEmpty(filter.Item3) ? ItemsToProcess.Where(i => i[filter.Item2] == filter.Item3) : ItemsToProcess;
                }
                var sourceCodes = GetSourceCode(dataSet);
                lock (items)
                {
                    foreach (var item in items.ToList())
                    {
                        lock (_mapping.FieldMapping)
                        {
                            foreach (var field in _mapping.FieldMapping.ToList())
                            {
                                if (!string.IsNullOrEmpty(field.TargetFields))
                                {
                                    var itemField = item.Fields[field.TargetFields];
                                    if (itemField != null && itemField.TypeKey.Contains("multilist"))
                                    {
                                        using (new EditContext(item, false, true))
                                        {
                                            item.Fields[field.TargetFields].Value = string.Empty;
                                        }
                                        if (DetailedLogging)
                                        {
                                            statusMethod(string.Format(" --- <span style=\"color:green\">[SUCCESS][{1}] Field \"{0}\" cleared.</span>", item.Fields[field.TargetFields].Name, item.ID), statusFilepath);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the source code.
        /// </summary>
        /// <param name="dataSet">The data set.</param>
        /// <returns></returns>
        private IEnumerable<string> GetSourceCode(DataTable dataSet)
        {
            var sourceCodes = new List<string>();
            if (dataSet != null)
            {
                for (var i = 0; i < dataSet.Rows.Count; i++)
                {
                    for (var j = 0; j < dataSet.Rows[i].Table.Columns.Count; j++)
                    {
                        var cell = dataSet.Rows[i][dataSet.Rows[i].Table.Columns[j]] as string;
                        if (!string.IsNullOrEmpty(cell) && !sourceCodes.Contains(cell))
                        {
                            sourceCodes.Add(cell.ToLowerInvariant());
                        }
                    }
                }
            }
            return sourceCodes;
        }

        /// <summary>
        /// Retrieves the items to process.
        /// </summary>
        /// <param name="code">The code.</param>
        public List<Item> RetrieveItemsToProcess(Tuple<string, string, string> filter = null)
        {
            if (ItemsToProcess == null)
            {
                Item[] items = null;
                var query = string.Empty;

                if (filter != null)
                {
                    if (!string.IsNullOrEmpty(filter.Item3) && !string.IsNullOrEmpty(filter.Item2) && !string.IsNullOrEmpty(_mapping.Templates.Target))
                    {
                        query = string.Format("fast://sitecore//*[@@templateid='{0}' and @{1}='{2}']", _mapping.Templates.Target, FastQueryUtility.EscapeDashes(filter.Item2), filter.Item3);
                    }
                    else if (!string.IsNullOrEmpty(_mapping.Templates.Target))
                    {
                        query = string.Format("fast://sitecore//*[@@templateid='{0}']", _mapping.Templates.Target);
                    }
                    else if (!string.IsNullOrEmpty(filter.Item3) && !string.IsNullOrEmpty(filter.Item2))
                    {
                        query = string.Format("fast://sitecore//*[@{1}='{2}']", _mapping.Templates.Target, FastQueryUtility.EscapeDashes(filter.Item2), filter.Item3);
                    }
                    else if (!string.IsNullOrEmpty(_mapping.Paths.Target))
                    {
                        query = string.Format("fast:/{0}//*", FastQueryUtility.EscapeDashes(_mapping.Paths.Target));
                    }
                }
                //else
                //{
                //    if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(_mapping.Templates.Target))
                //    {
                //        query = string.Format("fast://sitecore//*[@@templateid='{0}' and @{1}='{2}']", _mapping.Templates.Target, EscapeDashes(CoreProductInfo.CatalogNumberFieldName), code);
                //    }
                //    else if (!string.IsNullOrEmpty(_mapping.Templates.Target))
                //    {
                //        query = string.Format("fast://sitecore//*[@@templateid='{0}']", _mapping.Templates.Target);
                //    }
                //    else if (!string.IsNullOrEmpty(code))
                //    {
                //        query = string.Format("fast://sitecore//*[@{1}='{2}']", _mapping.Templates.Target, EscapeDashes(CoreProductInfo.CatalogNumberFieldName), code);
                //    }
                //    else if (!string.IsNullOrEmpty(_mapping.Paths.Target))
                //    {
                //        query = string.Format("fast:/{0}//*", EscapeDashes(_mapping.Paths.Target));
                //    }
                //}
                var cache = HttpRuntime.Cache;
                var cachedItems = cache.Get(query) as Item[];
                if (cachedItems != null)
                {
                    ItemsToProcess = cachedItems.ToList();
                }
                else
                {
                    items = Database.SelectItems(query);
                    if (items != null)
                    {
                        ItemsToProcess = items.ToList();
                        cache.Insert(query, items, null, DateTime.Now.AddDays(3), System.Web.Caching.Cache.NoSlidingExpiration);
                    }
                }
            }
            return ItemsToProcess;
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
        /// Gets a value indicating whether [incremental update].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [incremental update]; otherwise, <c>false</c>.
        /// </value>
        public bool IncrementalUpdate
        {
            get
            {
                var setting = Settings.GetBoolSetting("Mds.IncrementalUpdateEnabled", true);
                if (HttpContext.Current != null)
                    return setting && !string.IsNullOrEmpty(HttpContext.Current.Request.QueryString.Get("incremental"));
                if (_incrementalUpdate.HasValue)
                    return _incrementalUpdate.Value;
                return setting;
            }
        }
    }
}
