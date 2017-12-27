using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aha.Project.DataImport.JotForms
{
    public struct JotForm
    {
        public struct Question
        {
            public const string FirstName = "First Name";
            public const string LastName = "Last Name";
            public const string Address1 = "Street Address";
            public const string Address2 = "Street Address Line 2";
            public const string City = "City";
            public const string Zip = "Zip / Postal";
            public const string Country = "Country";
            public const string State = "State / Province";
        }
        public struct Properties
        {
            public const string Title = "title";
            public const string Content = "content";
            public const string Username = "username";
            public const string Created = "created_at";
            public const string Updated = "updated_at";
            public const string Questions = "questions";
            public const string ActiveRedirect = "activeRedirect";
            public const string ThankText = "thanktext";
            public const string Emails = "emails";
            public const string Body = "body";
            public const string From = "from";
            public const string ReplyTo = "replyTo";
            public const string Subject = "subject";
            public const string To = "to";
        }
        public struct QuestionType
        {
            public const string TextBox = "control_textbox";
            public const string FullName = "control_fullname";
            public const string TextArea = "control_textarea";
            public const string Dropdown = "control_dropdown";
            public const string Email = "control_email";
            public const string Phone = "control_phone";
            public const string Button = "control_button";
            public const string Text = "control_text";
            public const string Checkbox = "control_checkbox";
            public const string Radio = "control_radio";
            public const string Address = "control_address";
        }

        public struct QuestionProperty
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
            public const string Sublabels = "sublabels";
            public const string Prefix = "prefix";
            public const string Suffix = "suffix";
            public const string First = "first";
            public const string Last = "last";
            public const string Middle = "middle";
            public const string Address1 = "addr_line1";
            public const string Address2 = "addr_line2";
            public const string City = "city";
            public const string State = "state";
            public const string Postal = "postal";
            public const string Country = "country";
            public const string CcExpYear = "cc_exp_year";
            public const string CcExpMonth = "cc_exp_month";
            public const string CcCcv = "cc_ccv";
            public const string CcNumber = "cc_number";
            public const string CcLastName = "cc_lastName";
            public const string CcFirstName = "cc_firstName";
        }

        public struct EmailAction
        {
            public const string ThankText = "Thank you text";
            public const string Body = "Email Body";
            public const string From = "From";
            public const string ReplyTo = "Reply To";
            public const string Subject = "Subject";
            public const string To = "To";
            public const string SendEmailActionType = "{C2065F73-67EB-4AED-8B60-8ACC51B95158}";
        }
    }
}