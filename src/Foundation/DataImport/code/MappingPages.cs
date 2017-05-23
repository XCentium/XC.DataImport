using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport
{
    public struct MappingPages
    {
        public struct Sitecore
        {
            public static readonly ID ScMappingsEditTemplateMapping = ID.Parse("{F683E74B-35C1-4C16-9168-8DD1A989640A}");
            public static readonly ID ScMappingsRunTemplateImport = ID.Parse("{9539E038-208E-4C03-8C9F-AFF72CF1DCD7}");
        }

        public struct NonSitecore
        {
            public static readonly ID NonScMappingsCreateTemplateMapping = ID.Parse("{DF2723B0-CAB8-4086-8E8B-73AEFB079164}");
            public static readonly ID NonScMappingsRunTemplateImport = ID.Parse("{A6E59199-D1D6-4DDD-AAE3-14B6B228B136}");
        }

        public struct SitecoreBatch
        {
            public static readonly ID BatchMappingsCreateBatchMapping = ID.Parse("{C7E0C875-E351-4359-932D-9806D377779A}");
            public static readonly ID BatchMappingsRunBatchImport = ID.Parse("{E218E2FA-1911-47DB-8479-91851BAACEB6}");
        }
        public struct NonSitecoreBatch
        {
            public static readonly ID BatchNonScMappingsCreateBatchMapping = ID.Parse("{9F6CE075-50B0-4488-9F71-FE18463EB47F}");
            public static readonly ID BatchNonScMappingsRunBatchImport = ID.Parse("{58EE9324-03FC-4BAF-99B5-C83E94F8601A}");
        }
    }
}