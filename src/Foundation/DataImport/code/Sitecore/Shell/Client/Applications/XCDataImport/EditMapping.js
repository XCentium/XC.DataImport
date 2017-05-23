define(["sitecore"], function (Sitecore) {
  var CreatenewMapping = Sitecore.Definitions.App.extend({
      initialized: function () {
          this.DatabaseDatasource.loadData();
          this.on("app:loaded", function () {
              this.MappingViewDatasource.loadMapping();
          }, this);
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
          mapping["MigrateDescendants"] = this.MigrateDescendants.IsChecked;


          var serverRequestParameters = null,
            serverRequestOnSuccess = null,
            serverRequestUrl = this.MappingSaveDatasource.ServiceUrl;

          var providerItemProperties = {
              "mapping": mapping
          };

          this.MappingSaveDatasource.performPostRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
      }
  });

  return CreatenewMapping;
});