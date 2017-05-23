using Sitecore.Configuration;

namespace XC.Foundation.DataImport
{
    public static class ConfigSettings
    {
        public static string ContentFolder
        {
            get { return Settings.GetSetting("Neb.ContentFolder", "{C7F808C6-0FDD-46AC-A4CA-9298519FEA32}"); }
        }
    }
}