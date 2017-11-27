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
    public class XmlFilterSubHomePageList
    {
        public void Process(SourceProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Source Processing started ##################");

            if (string.IsNullOrWhiteSpace(args.Content))
            {
                DataImportLogger.Log.Info("Source Processing: content is empty");
            }

            using (new SecurityDisabler())
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(args.Content);

                var idFieldMapping = args.FieldMappings.FirstOrDefault(f => f.IsId)?.SourceFields;

                foreach (XmlNode item in xmlDoc.DocumentElement.SelectNodes("//*[local-name()='ContentItem']"))
                {
                    if (item.SelectSingleNode(".//*[local-name()='dDocType']")?.InnerText == "SubHomePage")
                    {
                        var itemId = item.Attributes[idFieldMapping]?.Value?.StringToID();
                        args.Items2Import.Add(itemId, new Dictionary<ID, object>());
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
                                sourceValue = item.SelectSingleNode(".//*[local-name()='" + sourceField + "']")?.InnerText;
                            }
                            if (args.Items2Import.ContainsKey(itemId))
                            {
                                args.Items2Import[itemId][ID.Parse(field.TargetFields)] = sourceValue;
                            }
                            else
                            {
                                args.Items2Import[itemId].Add(ID.Parse(field.TargetFields), sourceValue);
                            }
                            DataImportLogger.Log.Info(string.Format("#################Source Processing item {0} field {1} ##################", itemId, field.TargetFields));
                        }
                    }
                }
            }

            DataImportLogger.Log.Info("#################Source Processing ended ##################");
        }
        
    }
}