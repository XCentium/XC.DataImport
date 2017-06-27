define(["sitecore"], function (Sitecore) {
  var CreatenewMapping = Sitecore.Definitions.App.extend({
      initialized: function () {
          var qs = _sc.Helpers.url.getQueryParameters(window.document.location.href);
          if (qs["mapping"]) {
              this.MappingPathHidden.set("text", qs["mapping"]);
              this.set("mapping", qs["mapping"]);
          }
      },
      runImport: function () {
          var navigateUrl = this.RunImportButton.get("navigateUrl");
          if (navigateUrl) {
              navigateUrl += "?mapping=" + this.get("mapping");
          }
          window.location.href = navigateUrl;
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
          mapping["MergeColumnFieldMatch"]["Target"] = this.TargetMergeField.get("selectedValue");
          
          mapping["MergeWithExistingItems"] = this.MergeWithExistingItems.get("isChecked");

          mapping["IncrementalUpdate"] = this.IncrementalUpdateCheckbox.get("isChecked");
          mapping["IncrementalUpdateSourceColumn"] = this.SourceDateColumn.get("text");

          var scriptPaths = [];
          _.each(this.ScriptList.get("items"), function (i) {
              scriptPaths.push(i["itemId"]);
          });

          mapping["PostImportScripts"] = scriptPaths;

          var serverRequestParameters = null,
            serverRequestOnSuccess = null,
            serverRequestUrl = this.MappingSaveDatasource.get("dataUrl");

          var providerItemProperties = {
              "mapping": JSON.stringify(mapping)
          };

          this.MappingSaveDatasource.viewModel.performPostRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
      }
  });

  return CreatenewMapping;
});