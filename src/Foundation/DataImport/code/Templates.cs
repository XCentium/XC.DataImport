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