using Sitecore.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aha.Project.DataImport.JotForms
{
    public static class ConfigSettings
    {
        public static string StatesLookup
        {
            get
            {
                return Settings.GetSetting("XC.DataImport.StatesDatasource");
            }
        }
    }
}