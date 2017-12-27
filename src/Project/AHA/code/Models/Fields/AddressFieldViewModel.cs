using Sitecore.ExperienceForms.Mvc.Models.Fields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;
using Sitecore;

namespace Aha.Project.DataImport.Models.Fields
{
    public class AddressFieldViewModel : FieldViewModel
    {
        public string Title { get; set; }

        protected override void InitItemProperties(Item item)
        {
            base.InitItemProperties(item);

            Title = StringUtil.GetString(item.Fields["Title"]);
        }

        protected override void UpdateItemFields(Item item)
        {
            base.UpdateItemFields(item);

            item.Fields["Title"]?.SetValue(Title, true);
        }
    }
}