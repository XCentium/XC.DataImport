using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.IO;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models;
using XC.Foundation.DataImport.Models.DataSources;

namespace XC.Foundation.DataImport.Repositories.DataSources
{
    public class FileDataSource : IDataSource
    {
        FileDataSourceModel _model;

        public FileDataSource():this(new FileDataSourceModel())
        {
        }

        public FileDataSource(FileDataSourceModel model)
        {
            _model = model;
        }

        public object GetSource(Action<string, string> statusMethod, string statusFilepath)
        {
            if (_model == null)
            {
                DataImportLogger.Log.Info("XC.DataImport - FileDataSource: datasource model is null");
                statusMethod("XC.DataImport - FileDataSource: datasource model is null", statusFilepath);
                return null;
            }
            try
            {
                if (!string.IsNullOrWhiteSpace(_model.FilePath) && File.Exists(_model.FilePath))
                {
                    return File.ReadAllText(_model.FilePath);                    
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} ({1}) (FileDataSource)</span>", ex.Message, ex.StackTrace), statusFilepath);
            }
            return null;
        }
    }
}