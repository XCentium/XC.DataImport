using Sitecore.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Exceptions;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;
using XC.Foundation.DataImport.Repositories.Migration;
using XC.Foundation.DataImport.Utilities;

namespace Aha.Project.DataImport.Scripts.Fields
{
    public class ResolveWeblayoutReference
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing ResolveWeblayoutReference started ##################");

            if (args.SourceValue == null || args.Mapping == null)
            {
                DataImportLogger.Log.Info("ResolveWeblayoutReference Field Processing: no SourceValue");
                return;
            }

            FileDataSourceModel sourceModel = ImportManager.ConvertToDatasourceModel(args.Mapping.Source, args.Mapping.SourceType);
            if (sourceModel == null)
            {
                DataImportLogger.Log.Info("ResolveWeblayoutReference Field Processing: no source model");
                return;
            }
            var folderWithAssets = IOExtensions.GetFolderForFile(sourceModel.FilePath);
            if (string.IsNullOrEmpty(folderWithAssets))
            {
                DataImportLogger.Log.Info("ResolveWeblayoutReference Field Processing: no folderWithAssets");
                return;
            }

            var file = Path.Combine(folderWithAssets, (string)args.SourceValue);
            if (file == null || !File.Exists(file))
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
                throw new FieldProcessingException(ex.Message, ex);
            }
            DataImportLogger.Log.Info("#################Field Processing ResolveWeblayoutReference ended ##################");
        }
    }
}