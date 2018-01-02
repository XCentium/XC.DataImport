using Sitecore.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;
using XC.Foundation.DataImport.Models;
using System.IO;
using Newtonsoft.Json;
using Sitecore.Data;
using XC.Foundation.DataImport.Models.Entities;

namespace XC.Foundation.DataImport.Pipelines.SourceProcessing
{
    public class SourceProcessingPipelineArgs : PipelineArgs
    {
        private IEnumerable<string> _sourceProcessingScripts;
        public List<ImportDataItem> Items2Import;
        private object _content = string.Empty;
        private ScFieldMapping[] _fieldMappings;

        public SourceProcessingPipelineArgs(object content, ScFieldMapping[] fieldMappings, IEnumerable<string> sourceProcessingScripts)
        {
            _sourceProcessingScripts = sourceProcessingScripts;
            _content = content;
            _fieldMappings = fieldMappings;
            if (content is List<ImportDataItem>)
            {
                Items2Import = (List<ImportDataItem>)content;
            } 
            else
            {
                Items2Import = new List<ImportDataItem>();
            }
        }

        public IEnumerable<string> SourceProcessingScripts
        {
            get
            {
                return _sourceProcessingScripts;
            }
        }
        public object Content
        {
            get
            {
                return _content;
            }
        }

        public ScFieldMapping[] FieldMappings
        {
            get
            {
                return _fieldMappings;
            }
        }
    }
}