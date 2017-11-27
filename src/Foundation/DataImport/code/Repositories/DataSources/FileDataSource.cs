using Sitecore.Data;
using Sitecore.Pipelines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Pipelines.SourceProcessing;

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

        public Dictionary<ID,Dictionary<ID, object>> GetSourceItemsForImport(IEnumerable<string> sourceProcessingScripts, ScFieldMapping[] fieldMappings, Action<string, string> statusMethod, string statusFilepath)
        {
            if (_model == null)
            {
                DataImportLogger.Log.Info("XC.DataImport - FileDataSource: datasource model is null");
                statusMethod("XC.DataImport - FileDataSource: datasource model is null", statusFilepath);
                return null;
            }
            if (fieldMappings == null)
            {
                DataImportLogger.Log.Info("XC.DataImport - FileDataSource: fieldMappings is null");
                statusMethod("XC.DataImport - FileDataSource: fieldMappings is null", statusFilepath);
                return null;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(_model.FilePath) && File.Exists(_model.FilePath))
                {
                    var fileContent = File.ReadAllText(_model.FilePath);

                    return ProcessSource(fileContent, sourceProcessingScripts, fieldMappings, statusMethod, statusFilepath);
                }
            }
            catch(Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} ({1}) (FileDataSource)</span>", ex.Message, ex.StackTrace), statusFilepath);
            }
            return null;
        }

        private Dictionary<ID, Dictionary<ID, object>> ProcessSource(string fileContent, IEnumerable<string> sourceProcessingScripts, ScFieldMapping[] fieldMappings, Action<string, string> statusMethod, string statusFilepath)
        {
            return RunSourceProcessingScripts(fileContent, fieldMappings, sourceProcessingScripts);
        }

        private Dictionary<ID, Dictionary<ID, object>> RunSourceProcessingScripts(string fileContent, ScFieldMapping[] fieldMappings, IEnumerable<string> sourceProcessingScripts)
        {
            var pipelineArgs = new SourceProcessingPipelineArgs(fileContent, fieldMappings, sourceProcessingScripts);
            CorePipeline.Run("xc.dataimport.sourceprocessing", pipelineArgs);
            return pipelineArgs.Items2Import;
        }
    }
}