namespace XC.Foundation.DataImport.Cache
{
    public class ItemCacheDependency : System.Web.Caching.CacheDependency
    {
        public ItemCacheDependency()
        {
        }

        public void ItemCacheDependency_OnPublishEnd(object sender, System.EventArgs eventArgs)
        {
        }

        protected override void DependencyDispose()
        {
        }
    }
}
