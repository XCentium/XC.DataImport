using Sitecore.Data;
using Sitecore.FakeDb;
using Sitecore.Foundation.Testing.Attributes;
using Xunit;

namespace XC.Foundation.DataImport.Tests.Repositories
{
    public class DatasourceRepositoryTests
    {
        [Theory, AutoDbData]
        public void CreateItemDatasourceTest(Db db)
        {
            var datasourceId = ID.NewID;
            // create a fake site context
            Sitecore.FakeDb.Sites.FakeSiteContext fakeSite = CreateFakeTreeStructure(db, datasourceId);

            // switch the context site
            using (new Sitecore.Sites.SiteContextSwitcher(fakeSite))
            {
                
            }
        }

        private static Sitecore.FakeDb.Sites.FakeSiteContext CreateFakeTreeStructure(Db db, ID datasourceId)
        {
            var fakeSite = new Sitecore.FakeDb.Sites.FakeSiteContext(
                         new Sitecore.Collections.StringDictionary
                           {
                    { "name", "website" },
                    { "database", "master" },
                    { "rootPath", "/sitecore/content/test" },
                    { "startItem", "/home" },
                    { "datasourceRootItem", datasourceId.ToString() }
                           });

            //var nonChildDatasourceTemplate = new DbTemplate("NonChildDatasourceSupport", NonChildDatasourceSupport.ID) { NonChildDatasourceSupport.Fields.DatasourceFolderFieldName };
            //db.Add(nonChildDatasourceTemplate);

            //var datasourceFolderTemplate = new DbTemplate("DatasourceFolder", DatasourceFolder.ID);
            //db.Add(datasourceFolderTemplate);

            //var datasourceSubfolderTemplate = new DbTemplate("DatasourceSubfolder", DatasourceSubfolder.ID);
            //db.Add(datasourceSubfolderTemplate);

            //var landingPage = new DbItem("Test", ID.NewID, NonChildDatasourceSupport.ID) {
            //    new DbItem("Home", ID.NewID, NonChildDatasourceSupport.ID)
            //    {
            //        new DbItem("Landing", ID.NewID, NonChildDatasourceSupport.ID)
            //        {
            //            new DbItem("Article", ID.NewID, NonChildDatasourceSupport.ID)
            //        }
            //    },
            //    new DbItem("Datasources", datasourceId)
            //};

            //db.Add(landingPage);
            return fakeSite;
        }
    }
}