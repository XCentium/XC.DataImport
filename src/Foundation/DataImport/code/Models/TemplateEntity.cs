using Sitecore.Data;
using Sitecore.Services.Core.Model;

namespace XC.Foundation.DataImport.Models
{
    public class TemplateEntity 
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Path { get; set; }
        public string Database { get; set; }
    }
}