namespace XC.Foundation.DataImport.Models.DataSources
{
    public class SqlDataSourceModel : DataSourceModel
    {
        public string ConnectionString { get; set; }
        public string SqlStatement { get; set; }
    }
}