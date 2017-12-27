using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Models.Mappings;
using XC.Foundation.DataImport.Utilities;

namespace XC.Foundation.DataImport.Repositories.FileSystem
{
    public class MappingRepository : IMappingRepository
    {
        private IFileSystemRepository _fileSystemRepository;

        public MappingRepository() : base()
        {
            _fileSystemRepository = new FileSystemRepository();
        }

        public IMappingModel RetrieveMappingModel<T>(string id) where T : IMappingModel
        {
            if (typeof(T) == typeof(ImportMappingModel))
            {
                var filePath = _fileSystemRepository.FindMappingById(id);
                var mappingContent = System.IO.File.ReadAllText(filePath);
                return (ImportMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(ImportMappingModel));
            }
            else if (typeof(T) == typeof(ImportBatchMappingModel))
            {
                var filePath = _fileSystemRepository.FindBatchMappingById(id);
                var mappingContent = System.IO.File.ReadAllText(filePath);
                return (ImportBatchMappingModel)JsonConvert.DeserializeObject(mappingContent, typeof(ImportBatchMappingModel));
            }
            return null;
        }
    }
}