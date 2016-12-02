using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.DataImport.Repositories.Cache
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
