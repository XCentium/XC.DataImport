using Sitecore.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;
using XC.Foundation.DataImport.Utilities;

namespace Aha.Project.DataImport.Scripts.Fields
{
    public class ResolveWeblayoutReference
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing ResolveWeblayoutReference started ##################");

            if (args.SourceValue == null)
            {
                DataImportLogger.Log.Info("ResolveWeblayoutReference Field Processing: no SourceValue");
                return;
            }

            var folderWithAssets = Settings.GetSetting("Aha.DataImport.AssetFolder");
            if (string.IsNullOrEmpty(folderWithAssets))
            {
                DataImportLogger.Log.Info("ResolveWeblayoutReference Field Processing: no folderWithAssets");
                return;
            }
            var rootDirectory = IOExtensions.GetRootFolder((string)args.SourceValue);
            if (rootDirectory == null)
            {
                DataImportLogger.Log.Info("ResolveWeblayoutReference Field Processing: no rootDirecotry");
                return;
            }
            var referencedFolder = Directory.GetDirectories(folderWithAssets, rootDirectory, SearchOption.AllDirectories)?.FirstOrDefault();
            if (referencedFolder == null)
            {
                DataImportLogger.Log.Info("ResolveWeblayoutReference Field Processing: no referencedFolder");
                return;
            }

            var file = Directory.GetFiles(referencedFolder.Replace(rootDirectory,string.Empty), (string)args.SourceValue, SearchOption.AllDirectories)?.FirstOrDefault();
            if(file == null)
            {
                DataImportLogger.Log.Info("ResolveWeblayoutReference Field Processing: no file");
                return;
            }
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                var articleContent = xmlDoc.DocumentElement.SelectSingleNode("//*[@name='articleWYSIWYG']")?.InnerText;
                if (articleContent != null)
                {
                    args.SourceValue = articleContent;
                    return;
                }
                var subHomePageContent = xmlDoc.DocumentElement.SelectSingleNode("//*[@name='subHomeContent']")?.InnerText;
                if (subHomePageContent != null)
                {
                    args.SourceValue = subHomePageContent;
                    return;
                }

            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Info("#################Field Processing ResolveWeblayoutReference error ##################: " + ex.Message);
            }
            DataImportLogger.Log.Info("#################Field Processing ResolveWeblayoutReference ended ##################");
        }
    }
}