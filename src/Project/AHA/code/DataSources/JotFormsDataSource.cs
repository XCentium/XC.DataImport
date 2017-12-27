using Aha.Project.DataImport.JotForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using XC.Foundation.DataImport;
using XC.Foundation.DataImport.Diagnostics;
using XC.Foundation.DataImport.Models.Entities;
using XC.Foundation.DataImport.Utilities;

namespace Aha.Project.DataImport.DataSources
{
    public class JotFormsDataSource : XC.Foundation.DataImport.Repositories.DataSources.IDataSource
    {
        private JotFormsDataSourceModel _model;

        public JotFormsDataSource(JotFormsDataSourceModel model)
        {
            _model = model;
        }

        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <param name="statusMethod">The status method.</param>
        /// <param name="statusFilepath">The status filepath.</param>
        /// <returns></returns>
        public object GetSource(Action<string, string> statusMethod, string statusFilepath)
        {
            if (_model == null)
            {
                DataImportLogger.Log.Info("XC.DataImport - JotFormsDataSource: datasource model is null");
                statusMethod("XC.DataImport - JotFormsDataSource: datasource model is null", statusFilepath);
                return null;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(_model.ApiKey) && _model.FormIds != null && !string.IsNullOrWhiteSpace(_model.FormIds))
                {
                    var forms = new Dictionary<ID, Dictionary<string, object>>();
                    var api = new JotFormClient(_model.ApiKey);
                    foreach (var formEntry in _model.FormIds.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var formIds = formEntry.Split('\t');
                        if (formIds.Length == 2)
                        {
                            var formId = formIds[1];
                            var id = long.Parse(formId);
                            var formContent = api.getFormQuestions(id);
                            var form = api.getForm(id);
                            var formProperties = api.getFormProperties(id);
                            if (formContent != null && (string)formContent["responseCode"] == "200" && form != null && (string)form["responseCode"] == "200" && formProperties != null && (string)formProperties["responseCode"] == "200")
                            {
                                var itemId = formId?.StringToID();

                                var formDicModel = new Dictionary<string, object>();
                                forms.Add(itemId, formDicModel);

                                var formModel = new SitecoreFormModel
                                {
                                    ItemId = itemId.ToString()
                                };

                                formDicModel.Add(formIds[0], formModel);
                                formModel.Properties.Add("ucm_id", formIds[0]);

                                var formProps = formProperties[JotForm.Properties.Content]?.Children<JProperty>();
                                if (formProps != null)
                                {
                                    ProcessFormProperties(formModel, formProps);
                                }

                                var formInfo = form[JotForm.Properties.Content]?.Children<JProperty>();
                                if (formInfo != null)
                                {
                                    foreach (var question in formInfo)
                                    {
                                        formModel.Properties.Add(question.Name, (string)question.Value);
                                    }
                                }

                                var formQuestions = formContent[JotForm.Properties.Content]?.Children<JProperty>();
                                if (formQuestions != null)
                                {
                                    foreach (var question in formQuestions)
                                    {
                                        var questionObject = question.Value as JObject;
                                        var questionType = (string)questionObject[JotForm.QuestionProperty.FieldType];
                                        if (IsComplexType(questionType))
                                        {
                                            CreateComplexField(question, formModel);
                                        }
                                        else
                                        {
                                            var modelWapper = CreateModel(questionObject, formId);
                                            formModel.Fields.Add((string)questionObject[JotForm.QuestionProperty.Name], modelWapper);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return forms;
                }
            }
            catch (Exception ex)
            {
                DataImportLogger.Log.Error(ex.Message, ex);
                statusMethod(string.Format(" <span style=\"color:red\">[FAILURE] {0} ({1}) (JotFormsDataSource)</span>", ex.Message, ex.StackTrace), statusFilepath);
            }
            return null;
        }

        /// <summary>
        /// Processes the form properties.
        /// </summary>
        /// <param name="formModel">The form model.</param>
        /// <param name="formProps">The form props.</param>
        private static void ProcessFormProperties(SitecoreFormModel formModel, JEnumerable<JProperty>? formProps)
        {
            //var thankYouProp = formProps.Value.FirstOrDefault(i => i.Name == JotForm.Properties.ThankText) as JProperty;
            //if (thankYouProp != null)
            //{
            //    formModel.SubmitActions.Add(JotForm.EmailAction.ThankText, (string)thankYouProp.Value);
            //}
            var emailsProperty = formProps.Value.FirstOrDefault(i => i.Name == JotForm.Properties.Emails) as JProperty;
            if (emailsProperty != null)
            {
                var emailActions = emailsProperty.Value?.Children<JObject>().Cast<JObject>();
                if (emailActions != null)
                {
                    for (var i = 1; i <= emailActions.Count(); i++)
                    {
                        var emailAction = emailActions.ElementAt(i-1);
                        var name = "Email " + i;

                        var emailProperties = new Dictionary<string, object>();
                        emailProperties.Add(XC.Foundation.DataImport.Templates.Fields.SubmitActionParameters, 
                            JsonConvert.SerializeObject(new {
                                body = (string)emailAction.Properties().FirstOrDefault(a => a.Name == JotForm.Properties.Body).Value,
                                from = (string)emailAction.Properties().FirstOrDefault(a => a.Name == JotForm.Properties.From).Value,
                                replyTo = (string)emailAction.Properties().FirstOrDefault(a => a.Name == JotForm.Properties.ReplyTo).Value,
                                subject = (string)emailAction.Properties().FirstOrDefault(a => a.Name == JotForm.Properties.Subject).Value,
                                to = (string)emailAction.Properties().FirstOrDefault(a => a.Name == JotForm.Properties.To).Value
                            })
                        );
                        emailProperties.Add(XC.Foundation.DataImport.Templates.Fields.SubmitAction, JotForm.EmailAction.SendEmailActionType);

                        var model = new SitecoreFieldModel
                        {
                            ItemId = string.Concat(formModel.ItemId, emailAction.Path).StringToID().ToString(),
                            Name = name,
                            Properties = emailProperties,
                            FieldType = SitecoreFieldModel.FormFieldType.Action
                        };
                        formModel.SubmitActions.Add(name, model);
                    }
                }
            }
        }

        /// <summary>
        /// Creates the complex field.
        /// </summary>
        /// <param name="question">The question.</param>
        /// <param name="formWrapper">The form wrapper.</param>
        private void CreateComplexField(JProperty question, ISitecoreFieldModel formWrapper)
        {
            if (question == null)
            {
                return;
            }
            var properties = new Dictionary<string, object>();
            var name = (string)question.Value[JotForm.QuestionProperty.Name];
            var fieldId = string.Concat(formWrapper.ItemId, name, (string)question.Value[JotForm.QuestionProperty.FieldType], (string)question.Value[JotForm.QuestionProperty.QuestionId]).StringToID().ToString();
            var originFieldType = (string)question.Value[JotForm.QuestionProperty.FieldType];

            var model = new SitecoreFieldModel
            {
                FieldType = SitecoreFieldModel.FormFieldType.Section,
                ItemId = fieldId,
                Name = name,
                Properties = properties
            };

            properties.Add(Sitecore.ExperienceForms.Mvc.Constants.FieldNames.Title, (string)question.Value[JotForm.QuestionProperty.Title]);
            properties.Add(Sitecore.ExperienceForms.Mvc.Constants.FieldNames.Required, (string)question.Value[JotForm.QuestionProperty.Required]);
            properties.Add(XC.Foundation.DataImport.Templates.Fields.SortOrder, (string)question.Value[JotForm.QuestionProperty.Order]);

            formWrapper.Fields.Add(name, model);

            if (originFieldType == JotForm.QuestionType.FullName)
            {
                CreateComplexFieldModel(model, (string)question.Value[JotForm.QuestionProperty.FieldType], (string)question.Value[JotForm.QuestionProperty.QuestionId], JotForm.Question.FirstName, question, 1);
                CreateComplexFieldModel(model, (string)question.Value[JotForm.QuestionProperty.FieldType], (string)question.Value[JotForm.QuestionProperty.QuestionId], JotForm.Question.LastName, question, 2);
            }
            else if (originFieldType == JotForm.QuestionType.Address)
            {
                CreateComplexFieldModel(model, (string)question.Value[JotForm.QuestionProperty.FieldType], (string)question.Value[JotForm.QuestionProperty.QuestionId], JotForm.Question.Address1, question, 1);
                CreateComplexFieldModel(model, (string)question.Value[JotForm.QuestionProperty.FieldType], (string)question.Value[JotForm.QuestionProperty.QuestionId], JotForm.Question.Address2, question, 2);
                CreateComplexFieldModel(model, (string)question.Value[JotForm.QuestionProperty.FieldType], (string)question.Value[JotForm.QuestionProperty.QuestionId], JotForm.Question.City, question, 3);
                CreateComplexFieldModel(model, (string)question.Value[JotForm.QuestionProperty.FieldType], (string)question.Value[JotForm.QuestionProperty.QuestionId], JotForm.Question.State, question, 4);
                CreateComplexFieldModel(model, (string)question.Value[JotForm.QuestionProperty.FieldType], (string)question.Value[JotForm.QuestionProperty.QuestionId], JotForm.Question.Zip, question, 5);
                CreateComplexFieldModel(model, (string)question.Value[JotForm.QuestionProperty.FieldType], (string)question.Value[JotForm.QuestionProperty.QuestionId], JotForm.Question.Country, question, 6);
            }
        }

        /// <summary>
        /// Creates the complex field model.
        /// </summary>
        /// <param name="formWrapper">The form wrapper.</param>
        /// <param name="fieldType">Type of the field.</param>
        /// <param name="questionId">The question identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="question">The question.</param>
        /// <param name="sortOrder">The sort order.</param>
        private void CreateComplexFieldModel(ISitecoreFieldModel formWrapper, string fieldType, string questionId, string name, JProperty question, int sortOrder)
        {
            var properties = new Dictionary<string, object>();
            var fieldId = string.Concat(formWrapper.ItemId, name, fieldType, questionId).StringToID().ToString();

            properties.Add(Sitecore.ExperienceForms.Mvc.Constants.FieldNames.Title, name);
            properties.Add(Sitecore.ExperienceForms.Mvc.Constants.FieldNames.Required, ((question.Value.Children().FirstOrDefault(i => ((JProperty)i).Name == JotForm.QuestionProperty.Required) as JProperty).Value as JValue)?.Value);
            properties.Add(XC.Foundation.DataImport.Templates.Fields.SortOrder, sortOrder.ToString());

            var model = new SitecoreFieldModel
            {
                FieldType = SitecoreFieldModel.FormFieldType.SingleLineText,
                ItemId = fieldId,
                Name = name,
                Properties = properties
            };

            formWrapper.Fields.Add(name, model);
        }

        /// <summary>
        /// Determines whether [is complex type] [the specified question type].
        /// </summary>
        /// <param name="questionType">Type of the question.</param>
        /// <returns>
        ///   <c>true</c> if [is complex type] [the specified question type]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsComplexType(string questionType)
        {
            return questionType == JotForm.QuestionType.FullName || questionType == JotForm.QuestionType.Address;
        }

        /// <summary>
        /// Creates the model.
        /// </summary>
        /// <param name="question">The question.</param>
        /// <param name="formId">The form identifier.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private SitecoreFieldModel CreateModel(JObject question, string formId, string name = "")
        {
            if (question == null)
            {
                return null;
            }
            SitecoreFieldModel model = null;
            var fieldName = !string.IsNullOrWhiteSpace(name) ? name : (string)question[JotForm.QuestionProperty.Name];

            var fieldId = string.Concat(formId, fieldName, (string)question[JotForm.QuestionProperty.FieldType], (string)question[JotForm.QuestionProperty.QuestionId]).StringToID().ToString();

            model = new SitecoreFieldModel
            {
                ItemId = fieldId,
                Name = fieldName,
                Properties = question.Properties().Where(i => !string.IsNullOrWhiteSpace(GetPropertyKey(i))).ToDictionary(i => GetPropertyKey(i), i => (object)i.Value.ToString())
            };

            switch ((string)question[JotForm.QuestionProperty.FieldType])
            {
                case JotForm.QuestionType.TextBox:
                case JotForm.QuestionType.Phone:
                    model.FieldType = SitecoreFieldModel.FormFieldType.SingleLineText;
                    break;
                case JotForm.QuestionType.Text:
                    model.FieldType = SitecoreFieldModel.FormFieldType.Text;
                    break;
                case JotForm.QuestionType.TextArea:
                    model.FieldType = SitecoreFieldModel.FormFieldType.TextArea;
                    break;
                case JotForm.QuestionType.Button:
                    model.FieldType = SitecoreFieldModel.FormFieldType.Button;
                    break;
                case JotForm.QuestionType.Checkbox:
                    model.FieldType = SitecoreFieldModel.FormFieldType.Checkbox;
                    break;
                case JotForm.QuestionType.Email:
                    model.FieldType = SitecoreFieldModel.FormFieldType.Email;
                    break;
                case JotForm.QuestionType.Dropdown:
                    model.FieldType = SitecoreFieldModel.FormFieldType.Dropdown;
                    if (fieldName.ToLowerInvariant().Contains("country"))
                    {
                        model.Properties.Add(Templates.Fields.Datasource, Items.ItemIds.CountriesLookup);
                    } else if (fieldName.ToLowerInvariant().Contains("state"))
                    {
                        model.Properties.Add(Templates.Fields.Datasource, JotForms.ConfigSettings.StatesLookup);
                    }
                    break;
                case JotForm.QuestionType.Radio:
                    model.FieldType = SitecoreFieldModel.FormFieldType.Radio;
                    break;
                default:
                    break;
            }

            if(question[JotForm.QuestionProperty.Options] != null)
            {
                var optionValues = (question[JotForm.QuestionProperty.Options] as JValue).Value as string;
                model.Properties.Add(Templates.Fields.Options, optionValues.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
            }

            return model;
        }

        /// <summary>
        /// Gets the property key.
        /// </summary>
        /// <param name="prop">The property.</param>
        /// <returns></returns>
        private static string GetPropertyKey(JProperty prop)
        {
            switch (prop.Name)
            {
                case JotForm.QuestionProperty.Name:
                    return Sitecore.ExperienceForms.Mvc.Constants.FieldNames.ItemName;
                case JotForm.QuestionProperty.Title:
                    return Sitecore.ExperienceForms.Mvc.Constants.FieldNames.Title;
                case JotForm.QuestionProperty.Text:
                    var controlType = prop.Parent.Values<JProperty>().FirstOrDefault(i => i.Name == JotForm.QuestionProperty.FieldType);
                    if (controlType != null && (string)controlType.Value == JotForm.QuestionType.Text)
                    {
                        return Sitecore.ExperienceForms.Mvc.Constants.FieldNames.Text;
                    }
                    return Sitecore.ExperienceForms.Mvc.Constants.FieldNames.Title;
                case JotForm.QuestionProperty.Rows:
                    return Sitecore.ExperienceForms.Mvc.Constants.FieldNames.Rows;
                case JotForm.QuestionProperty.Required:
                    return Sitecore.ExperienceForms.Mvc.Constants.FieldNames.Required;
                case JotForm.QuestionProperty.Order:
                    return XC.Foundation.DataImport.Templates.Fields.SortOrder;
                default:
                    return null;
            }
        }
    }
}