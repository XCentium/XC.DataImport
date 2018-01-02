using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models;
using XC.Foundation.DataImport.Models.DataSources;

namespace XC.Foundation.DataImport.Repositories.DataSources
{
    public class WebDataSource : IDataSource
    {
        WebDataSourceModel _model;

        public WebDataSource():this(new WebDataSourceModel())
        {
        }

        public WebDataSource(WebDataSourceModel model)
        {
            _model = model;
        }

        public object GetSource(Action<string, string> statusMethod, string statusFilepath)
        {
            if (_model == null)
            {
                DataImportLogger.Log.Info("XC.DataImport - WebDataSource: datasource model is null");
                statusMethod("XC.DataImport - WebDataSource: datasource model is null", statusFilepath);
                return null;
            }
            try
            {
                if (!string.IsNullOrWhiteSpace(_model.Url))
                {
                    WebRequest req = WebRequest.Create(_model.Url);
                    req.Method = _model.Method.ToString();

                    WebHeaderCollection headers = new WebHeaderCollection();
                    headers.Add("apiKey:" + _model.ApiKey);
                    req.Headers = headers;

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        using (Stream responseStream = resp.GetResponseStream())
                        {
                            return new StreamReader(responseStream).ReadToEnd();
                        }                        
                    }
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