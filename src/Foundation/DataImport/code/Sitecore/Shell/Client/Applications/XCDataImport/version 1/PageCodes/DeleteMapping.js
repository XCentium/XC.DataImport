define(["sitecore"], function (Sitecore) {
    var DeleteMapping = Sitecore.Definitions.App.extend({
      initialized: function () {
          var qs = _sc.Helpers.url.getQueryParameters(window.document.location.href);
          if (qs["mapping"]) {
              this.MappingPathHidden.set("text", qs["mapping"]);
              this.set("mapping", qs["mapping"]);
          }
      },
      deleteMapping: function () {

          var serverRequestParameters = null,
            serverRequestOnSuccess = null,
            serverRequestUrl = this.DeleteMappingDatasource.get("dataUrl");

          var providerItemProperties = {
              "mapping": this.get("mapping")
          };

          this.DeleteMappingDatasource.viewModel.performPostRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
      }      
  });

    return DeleteMapping;
});