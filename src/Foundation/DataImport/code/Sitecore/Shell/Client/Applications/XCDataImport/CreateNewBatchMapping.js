define(["sitecore"], function (Sitecore) {
  var CreatenewMapping = Sitecore.Definitions.App.extend({
      initialized: function () {
          this.MappingDatasource.loadData();
          if (_sc.utils.url.parameterByName("mapping")) {
              this.MappingViewDatasource.loadMapping();
          }
      },

      saveBatchMapping: function () {
          "use strict";

          var mapping = {};
          mapping["Files"] = this.ListControl1.CheckedValues;
          mapping["Name"] = this.MappingName.Value;
          mapping["Description"] = this.MappingDescription.Value;

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