using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XC.Foundation.DataImport.Models.DataSources;
using XC.Foundation.DataImport.Models.Entities;
using XC.Foundation.DataImport.Models.Mappings;

namespace XC.Foundation.DataImport.Repositories.Repositories
{
    public class BaseTargetRepository
    {
        internal ImportMappingModel _mapping;

        public BaseTargetRepository(ImportMappingModel mapping)
        {
            _mapping = mapping;
        }

        public IDataSourceModel Target
        {
            get
            {
                return ConvertToTargetRepository(_mapping.Target, _mapping.TargetType);
            }
        }

        internal TargetSitecoreDataSourceModel ConvertToTargetRepository(dynamic target, SourceType targetType)
        {
            return JsonConvert.DeserializeObject(target.ToString(), Type.GetType(targetType.ModelType));
        }
    }
}