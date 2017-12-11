namespace XC.Foundation.DataImport.Models.DataSources
{
    public class SitecoreQueryDataSourceModel : DataSourceModel
    {
        public string DatabaseName { get; set; }
        public string Query { get; set; }
    }
}