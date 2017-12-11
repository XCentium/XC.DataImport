using HtmlAgilityPack;
using Sitecore.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Exceptions;
using XC.Foundation.DataImport.Pipelines.FieldProcessing;
using XC.Foundation.DataImport.Utilities;

namespace Aha.Project.DataImport.Scripts.Fields
{
    public class RenderedHtmlReference
    {
        public void Process(FieldProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Field Processing RenderedHtmlReference started ##################");

            if (args.SourceValue == null)
            {
                DataImportLogger.Log.Info("RenderedHtmlReference Field Processing: no SourceValue");
                return;
            }

            var folderWithAssets = Settings.GetSetting("Aha.DataImport.AssetFolder");
            if (string.IsNullOrEmpty(folderWithAssets))
            {
                DataImportLogger.Log.Info("RenderedHtmlReference Field Processing: no folderWithAssets");
                return;
            }
            var uid = (string)args.SourceValue;
            var htmlFile = IOExtensions.FindFile(folderWithAssets, uid, "*.html");
            if (string.IsNullOrWhiteSpace(htmlFile))
            {
                DataImportLogger.Log.Info("RenderedHtmlReference Field Processing: no htmlFile");
                return;
            }
            try
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.Load(htmlFile);

                var articleContent = htmlDoc.DocumentNode.SelectSingleNode("//*[@class='content']");
                if (articleContent != null)
                {
                    args.SourceValue = articleContent.InnerHtml;
                    return;
                }
                
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Info("#################Field Processing RenderedHtmlReference error ##################: " + ex.Message);
                throw new FieldProcessingException(ex.Message, ex);
            }
            DataImportLogger.Log.Info("#################Field Processing RenderedHtmlReference ended ##################");
        }
    }
}