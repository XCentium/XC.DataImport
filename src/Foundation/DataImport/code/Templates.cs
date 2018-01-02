using Sitecore.Data;
using Sitecore.Data.Items;

namespace XC.Foundation.DataImport
{
    public struct Templates
    {
        public struct DataImport
        {
            public const string ItemImporting = "_item_";
        }

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
                public const string Body_FieldName = "Body";
                public const string Summary_FieldName = "Summary";
            }
        }

        public struct FormTemplates
        {
            public const string Input = "{0908030B-4564-42EA-A6FA-C7A5A2D921A8}";
            public const string Checkbox = "{2F07293C-077F-456C-B715-FDB791ACB367}";
            public const string Text = "{FC18F915-EAC6-460A-8777-6E1376A9EA09}";
            public const string Email = "{886ADEC1-ABF8-40E1-9926-D9189C4E8E1B}";
            public const string TextArea = "{D8386D04-C1E3-4CD3-9227-9E9F86EF3C88}";
            public const string Dropdown = "{9121D435-48B8-4649-9D13-03D680474FAD}";
            public const string Button = "{94A46D66-B1B8-405D-AAE4-7B5A9CD61C5E}";
            public const string Page = "{CFEE7B51-8505-45CE-B843-9358F827DF87}";
            public const string List = "{5B672865-55D2-413E-B699-FDFC7E732CCF}";
            public const string Section = "{8CDDB194-F456-4A75-89B7-346F8F39F95C}";
            public const string SubmitActionFolder = "{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}";
            public const string SubmitActionDefinition = "{05FE45D4-B9C7-40DE-B767-7C5ABE7119F9}";
            public const string Folder = "{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}";
            public const string ExtendedListItem = "{B3BDFE59-6667-4432-B261-05D0E3F7FDF6}";
        }

        public struct FormFieldTypeTemplates
        {
            public const string Form = "{6ABEE1F2-4AB4-47F0-AD8B-BDB36F37F64C}";
            public const string FieldType = "{A60EDCAF-1285-46B5-8380-D790BB8C8708}";
        }

        public struct Fields
        {
            public const string SortOrder = "__Sortorder";
            public const string FieldType = "Field Type";
            public const string SubmitActionFolder = "SubmitActions";
            public const string SubmitAction = "Submit Action";
            public const string SubmitActionParameters = "Parameters";
            public const string Datasource = "Datasource";
            public const string IsDynamic = "Is Dynamic";
            public const string Options = "_Options_";
            public const string DisplayFieldName = "Display Field Name";
            public const string ValueFieldName = "Value Field Name";
            public const string DefaultSelection = "Default Selection";
            public const string ItemName = "__Item name";
            public const string ItemDisplayName = "__Display name";
        }

    }
}