using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport
{
    public struct Templates
    {
        public struct ImportedItem
        {
            public struct Fields
            {
                public static readonly string OriginObject_FieldName = "Origin Object Id";
                public static readonly string OriginParentObject_FieldName = "Origin Parent Object Id";
                public static readonly string OriginBodyText_FieldName = "Body";
                public static readonly string OriginShortDescription_FieldName = "Short Description";
                public static readonly string OriginPath_FieldName = "Origin Path";
            }
        }

    }
}