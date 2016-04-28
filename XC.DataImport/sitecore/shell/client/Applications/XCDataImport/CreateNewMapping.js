define(["sitecore"], function (Sitecore) {
  var CreatenewMapping = Sitecore.Definitions.App.extend({
      initialized: function () {
          this.DatabaseDatasource.loadData();
      },

      saveMapping: function () {
          "use strict";

          var mapping = {};
          mapping["Name"] = this.MappingName.Value;

          mapping["Databases"] = {};
          mapping["Databases"]["Source"] = this.SourceDatabases.SelectedValue;
          mapping["Databases"]["Target"] = this.TargetDatabases.SelectedValue;

          mapping["Templates"] = {};
          mapping["Templates"]["Source"] = this.SourceTemplates.SelectedValue;
          mapping["Templates"]["Target"] = this.TargetTemplates.SelectedValue;

          mapping["Paths"] = {};
          mapping["Paths"]["Source"] = this.SourcePath.SelectedItemPath;
          mapping["Paths"]["Target"] = this.TargetPath.SelectedItemPath;

          mapping["MigrateAllFields"] = this.MigrateAllFields.IsChecked;
          mapping["FieldMapping"] = this.FieldGridView.getFormData();


          var serverRequestParameters = null,
            serverRequestOnSuccess = null,
            serverRequestUrl = this.MappingDatasource.ServiceUrl;

          var providerItemProperties = {
              "mapping": mapping
          };

          this.MappingDatasource.performRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
      },
      populateMapping: function () {
          "use strict";

          if (this.DatabaseDatasource.DynamicData) {
              this.MappingName.Value = this.DatabaseDatasource.DynamicData.Name;
          }
      }

  });

  return CreatenewMapping;
});