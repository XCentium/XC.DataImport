using Sitecore.Data;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models.Entities;
using XC.Foundation.DataImport.Pipelines.SourceProcessing;
using XC.Foundation.DataImport.Utilities;

namespace Aha.Project.DataImport.Scripts.Source
{
    public class WebReferenceXmlParsing
    {
        public void Process(SourceProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Source Processing WebReferenceXmlParsing started ##################");

            if (string.IsNullOrWhiteSpace((string)args.Content))
            {
                DataImportLogger.Log.Info("Source Processing WebReferenceXmlParsing: content is empty");
                return;
            }

            using (new SecurityDisabler())
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml((string)args.Content);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("wcm", "http://www.stellent.com/wcm-data/ns/8.0.0");

                var idFieldMapping = args.FieldMappings.FirstOrDefault(f => f.IsId)?.SourceFields;

                foreach (XmlNode item in xmlDoc.DocumentElement.SelectNodes("//*[@name='group1']/*"))
                {
                    var childNodes = item.ChildNodes.Cast<XmlNode>();
                    var node = childNodes.FirstOrDefault(i => i.Attributes.Cast<XmlAttribute>().Any(a => a.Value == idFieldMapping))?.InnerText;
                    if (!string.IsNullOrEmpty(node))
                    {
                        var itemId = node.StringToID();
                        args.Items2Import.Add(new ImportDataItem { ItemId = itemId });
                        DataImportLogger.Log.Info("#################Source Processing WebReferenceXmlParsing item ##################" + itemId);

                        foreach (var field in args.FieldMappings)
                        {
                            var sourceField = field.SourceFields;
                            var sourceValue = string.Empty;

                            var xmlNode = childNodes.FirstOrDefault(i => i.Attributes.Cast<XmlAttribute>().Any(a => a.Value == sourceField));
                            if (xmlNode != null)
                            {
                                sourceValue = xmlNode.InnerText;
                            }
                            var existingDataItem = args.Items2Import.FirstOrDefault(i => i.ItemId == itemId);
                            if (existingDataItem != null && existingDataItem.Fields.ContainsKey(field.TargetFields))
                            {
                                existingDataItem.Fields[field.TargetFields] = sourceValue;
                            }
                            else
                            {
                                existingDataItem.Fields.Add(field.TargetFields, sourceValue);
                            }
                            DataImportLogger.Log.Info(string.Format("#################Source Processing item {0} field {1} ##################", itemId, field.TargetFields));
                        }
                    }
                }
            }

            DataImportLogger.Log.Info("#################Source Processing WebReferenceXmlParsing ended ##################");
        }
    }
}