namespace XC.Foundation.DataImport.Models.DataSources
{
    public class WebDataSourceModel : DataSourceModel
    {
        public string Url { get; set; }
        public Method Method { get; set; }
        public string ApiKey { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public enum Method { GET, POST }
}