namespace XC.Foundation.DataImport.Models.DataSources
{
    public class SitecoreDataSourceModel : DataSourceModel
    {
        public string DatabaseName { get; set; }
        public string Path { get; set; }
        public string Template { get; set; }
        public string FullPath { get; set; }
    }
}