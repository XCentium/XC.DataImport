using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport.Models.Entities
{
    public class SitecoreFormModel: SitecoreFieldModel, ISitecoreFieldModel
    {
        public Dictionary<string, object> SubmitActions { get; set; }

        public SitecoreFormModel()
        {
            SubmitActions = new Dictionary<string, object>();
        }
    }
}