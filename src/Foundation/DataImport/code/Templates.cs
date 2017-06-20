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
                public static readonly string OriginObjectId = "Origin Object Id";
                public static readonly string OriginParentObjectId = "Origin Parent Object Id";
                public static readonly string OriginBodyTextId = "Body";
            }
        }

    }
}