define(["sitecore"], function (Sitecore) {
  var CreatenewMapping = Sitecore.Definitions.App.extend({
      initialized: function () {
          //this.DatabaseDatasource.getData();
          //this.ConnectionsDatasource.getData();
          //if (_sc.utils.url.parameterByName("mapping")) {
          //    this.MappingViewDatasource.loadMapping();
          //}
      },

      saveMapping: function () {
          "use strict";

          var mapping = {};
          mapping["Name"] = this.MappingName.get("text");

          mapping["Databases"] = {};
          mapping["Databases"]["Source"] = this.SourceConnectionList.get("selectedValue");
          mapping["Databases"]["Target"] = this.TargetDatabaseList.get("selectedValue");

          mapping["Templates"] = {};
          mapping["Templates"]["Source"] = { "Command": this.SourceCommandText.get("text") };
          mapping["Templates"]["Target"] = this.TemplateList.get("selectedValue");

          mapping["Paths"] = {};
          mapping["Paths"]["Target"] = this.TargetPath.get("selectedItemPath");

          mapping["FieldMapping"] = this.FieldMappingList.get("items");

          mapping["MergeColumnFieldMatch"] = {};
          mapping["MergeColumnFieldMatch"]["Source"] = this.SourceMatchColumn.get("text");
          mapping["MergeColumnFieldMatch"]["Target"] = this.TargetMergeTemplate.get("selectedValue");
          
          mapping["MergeWithExistingItems"] = this.MergeWithExistingItems.get("checked");

          mapping["IncrementalUpdate"] = this.IncrementalUpdateCheckbox.get("checked");
          mapping["IncrementalUpdateSourceColumn"] = this.SourceDateColumn.get("text");


          var serverRequestParameters = null,
            serverRequestOnSuccess = null,
            serverRequestUrl = this.MappingSaveDatasource.get("dataUrl");

          var providerItemProperties = {
              "mapping": JSON.stringify(mapping)
          };

          this.MappingSaveDatasource.viewModel.performPostRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
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