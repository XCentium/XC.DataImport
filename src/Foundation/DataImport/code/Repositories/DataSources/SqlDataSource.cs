using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Models.Entities;
using XC.Foundation.DataImport.Utilities;

namespace XC.Foundation.DataImport.Repositories.DataSources
{
    public class SqlDataSource : IDataSource
    {
        SqlDataSourceModel _model;

        public SqlDataSource():this(new SqlDataSourceModel())
        {
        }

        public SqlDataSource(SqlDataSourceModel model)
        {
            _model = model;
        }

        public object GetSource(Action<string, string> statusMethod, string statusFilepath)
        {
            if (_model == null || string.IsNullOrWhiteSpace(_model.ConnectionString) || string.IsNullOrWhiteSpace(_model.SqlStatement))
            {
                DataImportLogger.Log.Info("XC.DataImport - SqlDataSource: datasource model is null");
                statusMethod("XC.DataImport - SqlDataSource: datasource model is null", statusFilepath);
                return null;
            }
            try
            {
                try
                {
                    var startDate = DateTime.Now;                   

                    using (SqlConnection connection = new SqlConnection(_model.ConnectionString))
                    {
                        var commandText = _model.SqlStatement;
                        DataImportLogger.Log.Info("XC.DataImport - SQL statement: " + commandText);

                        using (SqlCommand command = new SqlCommand(commandText, connection))
                        {
                            connection.Open();
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandTimeout = 0;

                            var dataSet = new DataSet();
                            using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                            {
                                dataAdapter.SelectCommand = command;
                                dataAdapter.Fill(dataSet);
                                if (dataSet.Tables != null && dataSet.Tables.Count > 0)
                                {
                                    HttpRuntime.Cache[commandText] = dataSet.Tables[0];
                                    return ConvertToImportDataItems(dataSet.Tables[0]);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] Error occured during data retrieval from the source system: {0} ({1})</span>", ex.Message ), statusFilepath);
                    DataImportLogger.Log.Error(ex.Message, ex);
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} ({1}) (FileDataSource)</span>", ex.Message, ex.StackTrace), statusFilepath);
            }
            return null;
        }

        /// <summary>
        /// Converts to import data items.
        /// </summary>
        /// <param name="dataTable">The data table.</param>
        /// <returns></returns>
        private List<ImportDataItem> ConvertToImportDataItems(DataTable dataTable)
        {
            if(dataTable == null || dataTable.Rows.Count == 0)
            {
                return null;
            }
            var dataItems = new List<ImportDataItem>();
            foreach (DataRow row in dataTable.Rows)
            {
                var itemId = row.GetHashCode().ToString().StringToID();
                var dataItem = new ImportDataItem
                {
                    ItemId = itemId
                };
                dataItems.Add(dataItem);
                foreach(DataColumn column in dataTable.Columns)
                {
                    dataItem.Fields.Add(column.ColumnName, row[column.ColumnName]);
                }
            }
            return dataItems;
        }
    }
}