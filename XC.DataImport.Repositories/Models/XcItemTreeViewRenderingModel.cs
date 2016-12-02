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
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace XC.DataImport.Repositories.Models
{
  public class XcItemTreeViewRenderingModel : ItemTreeViewRenderingModel
  {
    public override void Initialize(Sitecore.Mvc.Presentation.Rendering rendering)
    {
      base.Initialize(rendering);
      Requires.Clear();
      Requires.Script("client", "Applications/XCDataImport/TreeView/ItemTreeView.js");
      Presenter = "jQueryPresenter";

      Database database = this.GetDatabase();
      Item rootItem = this.GetRootItem(database);

      Properties["RootItem"] = rootItem.DisplayName + "," + rootItem.Database.Name + "," + rootItem.ID + "," + Images.GetThemedImageSource(!string.IsNullOrEmpty(rootItem.Appearance.Icon) ? rootItem.Appearance.Icon : "Applications/16x16/documents.png", ImageDimension.id16x16);
      Properties["Database"] = Database != null ? Database.Name : ClientHost.Databases.ContentDatabase.Name;
    }

    private Database GetDatabase()
    {
      string stringNonEmpty = this.GetStringNonEmpty("Database", "$context_contentdatabase");
      switch (stringNonEmpty.ToLower())
      {
        case "$context_database":
          return ClientHost.Databases.Database;
        case "$context_contentdatabase":
          return ClientHost.Databases.ContentDatabase;
        default:
          return Database.GetDatabase(stringNonEmpty);
      }
    }

    private Item GetRootItem(Database database)
    {
      Assert.ArgumentNotNull((object)database, "database");
      Item obj = (Item)null;
      string path = this.GetString("StaticData", "");
      if (!string.IsNullOrEmpty(path))
        obj = database.GetItem(path);
      return obj ?? database.GetRootItem();
    }
  }
}
