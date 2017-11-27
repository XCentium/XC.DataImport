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

namespace XC.Foundation.DataImport.Pipelines.SourceProcessing
{
    public class SourceProcessingPipelineArgs : PipelineArgs
    {
        private IEnumerable<string> _sourceProcessingScripts;
        public Dictionary<ID, Dictionary<ID, object>> Items2Import;
        private string _content = string.Empty;
        private ScFieldMapping[] _fieldMappings;

        public SourceProcessingPipelineArgs(string content, ScFieldMapping[] fieldMappings, IEnumerable<string> sourceProcessingScripts)
        {
            _sourceProcessingScripts = sourceProcessingScripts;
            _content = content;
            _fieldMappings = fieldMappings;
            Items2Import = new Dictionary<ID, Dictionary<ID, object>>();
        }

        public IEnumerable<string> SourceProcessingScripts
        {
            get {
                return _sourceProcessingScripts;
            }
        }
        public string Content
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