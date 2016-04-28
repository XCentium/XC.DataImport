using Sitecore.Services.Core.Model;

namespace XC.DataImport.Repositories.Models
{
    public class DatabaseEntity 
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string Id { get; set; }
    }
}