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
    public class ResolveMediaReference
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing ResolveMediaReference started ##################");

            if (args.SourceValue == null)
            {
                DataImportLogger.Log.Info("ResolveMediaReference Field Processing: no SourceValue");
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
                DataImportLogger.Log.Info("ResolveMediaReference Field Processing: no folderWithAssets");
                return;
            }
            var rootDirectory = IOExtensions.GetRootFolder((string)args.SourceValue);
            if (rootDirectory == null)
            {
                DataImportLogger.Log.Info("ResolveMediaReference Field Processing: no rootDirecotry");
                return;
            }
            var referencedFolder = Directory.GetDirectories(folderWithAssets, rootDirectory, SearchOption.AllDirectories)?.FirstOrDefault();
            if (referencedFolder == null)
            {
                DataImportLogger.Log.Info("ResolveMediaReference Field Processing: no referencedFolder");
                return;
            }

            var file = Directory.GetFiles(referencedFolder.Replace(rootDirectory, string.Empty), (string)args.SourceValue, SearchOption.AllDirectories)?.FirstOrDefault();
            if (file == null)
            {
                DataImportLogger.Log.Info("ResolveMediaReference Field Processing: no file");
                return;
            }
            try
            {
                var imageFile = File.ReadAllBytes(file);

                if (imageFile != null)
                {
                    args.SourceValue = (byte[])imageFile;
                    return;
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Info("#################Field Processing ResolveMediaReference error ##################: " + ex.Message);
                throw new FieldProcessingException(ex.Message, ex);
            }
            DataImportLogger.Log.Info("#################Field Processing ResolveMediaReference ended ##################");
        }
    }
}