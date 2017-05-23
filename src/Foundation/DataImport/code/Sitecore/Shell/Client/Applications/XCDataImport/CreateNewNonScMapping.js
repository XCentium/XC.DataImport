define(["sitecore"], function (Sitecore) {
  var CreatenewMapping = Sitecore.Definitions.App.extend({
      initialized: function () {
          this.DatabaseDatasource.loadData();
          this.ConnectionsDatasource.loadData();
          if (_sc.utils.url.parameterByName("mapping")) {
              this.MappingViewDatasource.loadMapping();
          }
      },

      saveMapping: function () {
          "use strict";

          var mapping = {};
          mapping["Name"] = this.MappingName.Value;

          mapping["Databases"] = {};
          mapping["Databases"]["Source"] = this.SourceDatabases.SelectedValue;
          mapping["Databases"]["Target"] = this.TargetDatabases.SelectedValue;

          mapping["Templates"] = {};
          mapping["Templates"]["Source"] = { "Command": this.SourceCommandText.Value, "IsStoredProc" : this.IsStoredProc.IsChecked };
          mapping["Templates"]["Target"] = this.TargetTemplates.SelectedValue;

          mapping["Paths"] = {};
          mapping["Paths"]["Target"] = this.TargetPath.SelectedItemPath;

          mapping["FieldMapping"] = this.FieldGridView.getFormData();

          mapping["MergeColumnFieldMatch"] = {};
          mapping["MergeColumnFieldMatch"]["Source"] = this.SourceMatchColumn.Value;
          mapping["MergeColumnFieldMatch"]["Target"] = this.TargetMergeTemplate.SelectedValue;
          
          mapping["MergeWithExistingItems"] = this.MergeWithExistingItems.IsChecked;

          mapping["IncrementalUpdate"] = this.IsIncrementalUpdate.IsChecked;
          mapping["IncrementalUpdateSourceColumn"] = this.SourceDateColumn.Value;


          var serverRequestParameters = null,
            serverRequestOnSuccess = null,
            serverRequestUrl = this.MappingSaveDatasource.ServiceUrl;

          var providerItemProperties = {
              "mapping": mapping
          };

          this.MappingSaveDatasource.performPostRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
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