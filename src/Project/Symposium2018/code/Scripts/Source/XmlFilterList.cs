using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using Sitecore.SecurityModel;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models.Entities;
using XC.Foundation.DataImport.Pipelines.SourceProcessing;
using XC.Foundation.DataImport.Utilities;

namespace XC.Symposium2018.Website.Scripts.Source
{
    public class XmlFilterList
    {
        public void Process(SourceProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Source Processing started ##################");

            if (string.IsNullOrWhiteSpace((string)args.Content))
            {
                DataImportLogger.Log.Info("Source Processing: content is empty");
                return;
            }

            using (new SecurityDisabler())
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml((string)args.Content);

                var idFieldMapping = args.FieldMappings.FirstOrDefault(f => f.IsId)?.SourceFields;

                if (xmlDoc.DocumentElement?.ChildNodes != null)
                {
                    var idx = 0;
                    foreach (XmlNode item in xmlDoc.DocumentElement?.ChildNodes)
                    {
                        if (item.Attributes != null)
                        {
                            var valueForId = args.MappingName + (item.Attributes[idFieldMapping] != null && !string.IsNullOrWhiteSpace(item.Attributes[idFieldMapping].Value)? item.Attributes[idFieldMapping].Value : idx.ToString());
                            var itemId = valueForId.StringToID();
                            args.Items2Import.Add(new ImportDataItem { ItemId = itemId });
                            DataImportLogger.Log.Info("#################Source Processing item ##################" +
                                                      itemId);

                            foreach (var field in args.FieldMappings)
                            {
                                var sourceField = field.SourceFields;
                                var sourceValue = string.Empty;

                                if (item.Attributes != null)
                                {
                                    var attr = item.Attributes[sourceField];
                                    if (attr != null)
                                    {
                                        sourceValue = attr.Value;
                                    }
                                    else
                                    {
                                        sourceValue = item.SelectSingleNode(".//*[local-name()='" + sourceField + "']")
                                            ?.InnerText;
                                    }
                                }

                                if (field.TargetFields != null)
                                {
                                    var existingDataItem = args.Items2Import.FirstOrDefault(i => i.ItemId == itemId);
                                    if (existingDataItem != null &&
                                        existingDataItem.Fields.ContainsKey(field.TargetFields))
                                    {
                                        existingDataItem.Fields[field.TargetFields] = sourceValue;
                                    }
                                    else
                                    {
                                        existingDataItem?.Fields.Add(field.TargetFields, sourceValue);
                                    }
                                }

                                DataImportLogger.Log.Info(
                                    $"#################Source Processing item {itemId} field {field.TargetFields} ##################");
                            }
                        }

                        ++idx;
                    }
                }
            }

            DataImportLogger.Log.Info("#################Source Processing ended ##################");
        }
    }
}