using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using Sitecore;
using Sitecore.Data.DataSources;
using Sitecore.Diagnostics;
using Sitecore.Mvc;

namespace XC.DataImport.Repositories
{
    public static class HtmlExtensions
    {
        public static HtmlString RenderView(this HtmlHelper htmlHelper, string renderingItemId, object parameters = null, ViewDataDictionary viewData = null)
        {
            Assert.ArgumentNotNull(htmlHelper, "htmlHelper");
            Assert.ArgumentNotNull(renderingItemId, "renderingItemId");
            ViewRendering viewRendering = ClientHost.Factory.GetViewRendering(new DataSourceItem(ClientHost.Items.GetItem(renderingItemId)));
            RouteValueDictionary routeValueDictionary = parameters != null ? new RouteValueDictionary(parameters) : new RouteValueDictionary();
            RenderPartialExtensions.RenderPartial(htmlHelper, viewRendering.GetView(), viewRendering.GetModel(routeValueDictionary), viewData);
            return new HtmlString(string.Empty);
        }
    }
}
