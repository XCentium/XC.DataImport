using Sitecore.Data;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines.SourceProcessing;
using XC.Foundation.DataImport.Utilities;

namespace Aha.Project.DataImport.Scripts.Source
{
    public class XmlWordPress
    {
        public void Process(SourceProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Source Processing started ##################");

            if (string.IsNullOrWhiteSpace((string)args.Content))
            {
                DataImportLogger.Log.Info("Source Processing: content is empty");
            }

            using (new SecurityDisabler())
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml((string)args.Content);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("wp", "http://wordpress.org/export/1.2/");
                nsmgr.AddNamespace("content", "http://purl.org/rss/1.0/modules/content/");
                nsmgr.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
                nsmgr.AddNamespace("excerpt", "http://wordpress.org/export/1.2/excerpt/");
                nsmgr.AddNamespace("wfw", "http://wellformedweb.org/CommentAPI/");

                var idFieldMapping = args.FieldMappings.FirstOrDefault(f => f.IsId)?.SourceFields;

                foreach (XmlNode item in xmlDoc.DocumentElement.SelectNodes("//*[local-name()='item']"))
                {
                    var itemId = item.SelectSingleNode(idFieldMapping, nsmgr)?.InnerText?.StringToID();
                    args.Items2Import.Add(itemId, new Dictionary<string, object>());

                    DataImportLogger.Log.Info("#################Source Processing item ##################" + itemId);

                    foreach (var field in args.FieldMappings)
                    {
                        var sourceField = field.SourceFields;
                        var sourceValue = string.Empty;

                        var attr = item.Attributes[sourceField];
                        if (attr != null)
                        {
                            sourceValue = attr.Value;
                        }
                        else
                        {
                            sourceValue = item.SelectSingleNode(sourceField, nsmgr)?.InnerText;
                        }
                        if (args.Items2Import.ContainsKey(itemId))
                        {
                            args.Items2Import[itemId][field.TargetFields] = sourceValue;
                        }
                        else
                        {
                            args.Items2Import[itemId].Add(field.TargetFields, sourceValue);
                        }
                        DataImportLogger.Log.Info(string.Format("#################Source Processing item {0} field {1} ##################", itemId, field.TargetFields));
                    }
                }
            }

            DataImportLogger.Log.Info("#################Source Processing ended ##################");
        }

    }
}