using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XC.Foundation.DataImport.Models.Entities
{
    public class SitecoreFieldModel : ISitecoreFieldModel
    {
        public string Name { get; set; }
        public string ItemId { get; set; }
        public Dictionary<string,object> Properties { get; set; }
        public Dictionary<string, object> Fields { get; set; }
        public FormFieldType FieldType { get; set; }
        public string SortOrder { get; set; }

        public SitecoreFieldModel()
        {
            Properties = new Dictionary<string, object>();
            Fields = new Dictionary<string, object>();
        }

        public enum FormFieldType
        {
            SingleLineText,
            TextArea,
            Dropdown,
            Radio,
            Checkbox,
            Button,
            Text,
            Email,
            Phone,
            Section,
            Action
        }

        public struct PropertyNames
        {
            public const string Name = "name";
            public const string Text = "text";
            public const string Required = "required";
            public const string Order = "order";
            public const string ReadOnly = "readonly";
            public const string Special = "special";
            public const string Cols = "cols";
            public const string Rows = "rows";
            public const string Options = "options";
            public const string Size = "size";
            public const string QuestionId = "qid";
            public const string FieldType = "type";
            public const string Title = "title";
        }
    }
}