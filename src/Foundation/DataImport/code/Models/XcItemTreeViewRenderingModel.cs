using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Speak.Components.Models;
using Sitecore.Speak.Components.Models.ListsAndGrids.ItemTreeViews;
using Sitecore.Web.UI;

namespace XC.Foundation.DataImport.Models
{
    public class XcItemTreeViewRenderingModel : ItemTreeViewRenderingModel
    {
        public override void Initialize(Sitecore.Mvc.Presentation.Rendering rendering)
        {
            base.Initialize(rendering);
            Requires.Clear();
            Requires.Script("client", "Applications/XCDataImport/TreeView/ItemTreeView.js");

            Presenter = "jQueryPresenter";
            //if (!string.IsNullOrEmpty(RootItem))
            //    CurrentRootItem = Database.GetItem(RootItem);
            //if (CurrentRootItem == null)
            //    CurrentRootItem = ClientHost.Databases.ContentDatabase.GetRootItem();
            //Properties["RootItem"] = CurrentRootItem.DisplayName + "," + CurrentRootItem.Database.Name + "," + CurrentRootItem.ID + "," + Images.GetThemedImageSource(!string.IsNullOrEmpty(CurrentRootItem.Appearance.Icon) ? CurrentRootItem.Appearance.Icon : "Applications/16x16/documents.png", ImageDimension.id16x16);
            //Properties["Database"] = Database != null ? Database.Name : ClientHost.Databases.ContentDatabase.Name;
        }
    }
}
