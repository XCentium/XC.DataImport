﻿using Sitecore.Data;
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
            public static readonly ID ScMappingsCreateTemplateMapping = ID.Parse("{3B13CFDF-D81E-4479-A755-F1337ED06210}");
            public static readonly ID ScMappingsRunTemplateImport = ID.Parse("{4E4C8144-F78E-4439-9549-F01BC2282D76}");
            public static readonly ID ScMappingsDeleteTemplateImport = ID.Parse("{9F745642-FD57-4076-BBB6-A5316D0271D2}");
        }

        public struct NonSitecore
        {
            public static readonly ID NonScMappingsCreateTemplateMapping = ID.Parse("{217B963F-DCE3-4008-B4A7-F253679324CF}");
            public static readonly ID NonScMappingsRunTemplateImport = ID.Parse("{EDA349B3-B298-4994-86FD-3868CD572BCB}");
            public static readonly ID NonScMappingsDeleteTemplateImport = ID.Parse("{0B99D360-E15D-4B35-98A3-0A2C6C0083F8}");
        }

        public struct SitecoreBatch
        {
            public static readonly ID BatchMappingsCreateBatchMapping = ID.Parse("{8C76611D-AA83-429E-9861-1F4AEA2457FC}");
            public static readonly ID BatchMappingsRunBatchImport = ID.Parse("{E218E2FA-1911-47DB-8479-91851BAACEB6}");
        }
        public struct NonSitecoreBatch
        {
            public static readonly ID BatchNonScMappingsCreateBatchMapping = ID.Parse("{9F6CE075-50B0-4488-9F71-FE18463EB47F}");
            public static readonly ID BatchNonScMappingsRunBatchImport = ID.Parse("{58EE9324-03FC-4BAF-99B5-C83E94F8601A}");
        }

        public struct Scripts
        {
            public static readonly ID PostProcessingScriptCreate = ID.Parse("{84343A27-76A9-42B2-B21E-9FACECD7138C}");
            public static readonly ID PostProcessingScriptDelete = ID.Parse("{13D62B06-47B0-4E50-8324-D4A5A1D9FE31}");
            public static readonly ID FieldProcessingScriptCreate = ID.Parse("{96B0189C-0CA3-40C0-A151-49E1CF89EA07}");
            public static readonly ID FieldProcessingScriptDelete = ID.Parse("{BFF23631-1841-4D98-8D96-5D5A955B9B56}");
        }
    }
}