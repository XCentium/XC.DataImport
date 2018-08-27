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
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Models.Entities;

namespace XC.Foundation.DataImport.Pipelines.SourceProcessing
{
    public class SourceProcessingPipelineArgs : PipelineArgs
    {
        public List<ImportDataItem> Items2Import;

        public SourceProcessingPipelineArgs(object content, ScFieldMapping[] fieldMappings, IEnumerable<string> sourceProcessingScripts, string mappingName)
        {
            SourceProcessingScripts = sourceProcessingScripts;
            Content = content;
            FieldMappings = fieldMappings;
            MappingName = mappingName;
            if (content is List<ImportDataItem>)
            {
                Items2Import = (List<ImportDataItem>)content;
            } 
            else
            {
                Items2Import = new List<ImportDataItem>();
            }
        }

        public IEnumerable<string> SourceProcessingScripts { get; }

        public object Content { get; }

        public ScFieldMapping[] FieldMappings { get; }

        public string MappingName { get; }
    }
}