using Sitecore.Data;
using Sitecore.Services.Core.Model;

namespace XC.DataImport.Repositories.Models
{
    public class TemplateEntity 
    {
        public string Name { get; set; }
        public string Id { get; set; }

        public string Path { get; set; }

        public string Database { get; set; }
    }
}