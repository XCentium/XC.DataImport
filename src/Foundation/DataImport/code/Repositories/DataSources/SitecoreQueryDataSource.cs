using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models;
using XC.Foundation.DataImport.Models.DataSources;

namespace XC.Foundation.DataImport.Repositories.DataSources
{
    public class SitecoreQueryDataSource : IDataSource
    {
        SitecoreQueryDataSourceModel _model;

        public SitecoreQueryDataSource():this(new SitecoreQueryDataSourceModel())
        {
        }

        public SitecoreQueryDataSource(SitecoreQueryDataSourceModel model)
        {
            _model = model;
        }

        public object GetSource(Action<string, string> statusMethod, string statusFilepath)
        {
            if (_model == null)
            {
                DataImportLogger.Log.Info("XC.DataImport - SitecoreQueryDataSource: datasource model is null");
                statusMethod("XC.DataImport - SitecoreQueryDataSource: datasource model is null", statusFilepath);
                return null;
            }
            try
            {
                
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} ({1}) (SitecoreQueryDataSource)</span>", ex.Message, ex.StackTrace), statusFilepath);
            }
            return null;
        }
    }
}