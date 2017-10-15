using Sitecore.Data;

namespace XC.Foundation.DataImport
{
    public struct Templates
    {
        public struct ImportedItem
        {
            public static readonly ID ID = new ID("{3459622E-66CD-4878-8811-1C2F0EC17570}");

            public struct Fields
            {
                public static readonly string OriginObject_FieldName = "Origin Object Id";
                public static readonly string OriginParentObject_FieldName = "Origin Parent Object Id";
                public static readonly string OriginPath_FieldName = "Origin Path";
            }
        }

        public struct HasPageContent
        {
            public struct Fields
            {
                public static readonly string Body_FieldName = "Body";
                public static readonly string Summary_FieldName = "Summary";
            }
        }
    }
}