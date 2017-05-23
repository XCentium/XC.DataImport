using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.Foundation.DataImport.Disablers
{
    public class ItemFilteringDisabler : IDisposable
    {
        private readonly bool _disableCondition = true;

        public ItemFilteringDisabler()
        {
            if (Sitecore.Context.Site != null)
            {
                Sitecore.Context.Site.DisableFiltering = true;
            }
        }

        public ItemFilteringDisabler(bool disableCondition)
        {
            _disableCondition = disableCondition;
            if (_disableCondition && Sitecore.Context.Site != null)
            {
                Sitecore.Context.Site.DisableFiltering = true;
            }
        }

        public void Dispose()
        {
            if (_disableCondition && Sitecore.Context.Site != null)
            {
                Sitecore.Context.Site.DisableFiltering = false;
            }
        }
    }
}
