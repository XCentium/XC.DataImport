using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Pipelines.PostProcessing;
using static Sitecore.Configuration.Settings;
using XC.Foundation.DataImport.Utilities;
using Sitecore.Data.Items;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore;
using Sitecore.Install.Files;

namespace XC.Project.DataImport.Scripts.PostImport
{
    public class MoveMediaAndReplaceWithReference
    {
        private const string MediaReferenceTemplateId = "{170EDED0-DB36-4FC8-98F8-EFF1D6CC65F5}";

        public void Process(ProcessingPipelineArgs args)
        {
            DataImportLogger.Log.Info("#################Post Processing MoveMediaAndReplaceWithReference started ##################");

            if (args.MigratedItems == null)
                DataImportLogger.Log.Info("Post Processing MoveMediaAndReplaceWithReference: no migrated items");

            using (new SecurityDisabler())
            {
                foreach (var item in args.MigratedItems)
                {
                    if(item.Paths.IsContentItem && (item.IsDerived(Sitecore.TemplateIDs.UnversionedImage) || item.IsDerived(Sitecore.TemplateIDs.UnversionedFile)))
                    {
                        var contentItemPath = item.Paths.FullPath;
                        var mediaLibraryPath = item.Paths.FullPath.Replace(Sitecore.Constants.ContentPath, Sitecore.Constants.MediaLibraryPath);
                        var mediaLibraryParentItem = item.Database.GetItem(mediaLibraryPath);
                        if(mediaLibraryParentItem == null)
                        {
                            mediaLibraryParentItem = CreateMediaPath(item.Database, mediaLibraryPath);
                        }
                        if (mediaLibraryParentItem == null)
                            return;

                        CreateMediaReferenceItem(item);
                        item.MoveTo(mediaLibraryParentItem);
                        DataImportLogger.Log.Info(string.Format("Post Processing MoveMediaAndReplaceWithReference: Moving item {0} to {1}", item.Name, item.Paths.FullPath));
                    }
                }
            }

            DataImportLogger.Log.Info("#################Post Processing ended ##################");
        }

        /// <summary>
        /// Creates the media reference item.
        /// </summary>
        /// <param name="item">The item.</param>
        private void CreateMediaReferenceItem(Item item)
        {
            if (item == null)
                return;
            try
            {
                using (new SecurityDisabler())
                {
                    var templateItem = item.Database.GetItem(MediaReferenceTemplateId);
                    if (templateItem == null)
                        return;

                    var mediaReferenceItem = item.Parent.Add(item.Name, new TemplateItem(templateItem));
                    if (mediaReferenceItem == null)
                        return;

                    using(new EditContext(mediaReferenceItem))
                    {
                        mediaReferenceItem["Content Reference"] = item.ID.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(string.Format("Post Processing MoveMediaAndReplaceWithReference: Error creating media reference item {0}. {1}", item.Paths.FullPath, ex.StackTrace));
            }
        }

        /// <summary>
        /// Creates the media path.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="mediaLibraryPath">The media library path.</param>
        private Item CreateMediaPath(Database database, string mediaLibraryPath)
        {
            if (string.IsNullOrEmpty(mediaLibraryPath))
                return null;
            try
            {
                using (new SecurityDisabler())
                {
                    var parentPath = mediaLibraryPath.Replace(StringUtil.GetLastPart(mediaLibraryPath, '/', ""),"");
                    var parentItem = database.GetItem(parentPath);
                    if (parentItem != null)
                        return parentItem;

                    var templateItem = database.GetItem(Sitecore.TemplateIDs.MediaFolder);
                    return database.CreateItemPath(parentPath, templateItem);
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(string.Format("Post Processing MoveMediaAndReplaceWithReference: Error creating path {0}. {1}", mediaLibraryPath, ex.StackTrace));
            }
            return null;
        }
    }
}