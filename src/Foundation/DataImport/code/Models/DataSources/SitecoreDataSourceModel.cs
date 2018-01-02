namespace XC.Foundation.DataImport.Models.DataSources
{
    public class SitecoreDataSourceModel : DataSourceModel
    {
        public string DatabaseName { get; set; }
        public string ItemPath { get; set; }
        public string TemplateId { get; set; }
        public string FullPath { get; set; }
        public bool MigrateAllFields { get; set; }
        public bool IncludeDescendants { get; set; }
    }
}