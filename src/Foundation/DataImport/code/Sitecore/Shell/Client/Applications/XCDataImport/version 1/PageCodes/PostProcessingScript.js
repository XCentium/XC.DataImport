define(["sitecore"], function (Sitecore) {
  var CreatenewMapping = Sitecore.Definitions.App.extend({
      initialized: function () {
          var qs = _sc.Helpers.url.getQueryParameters(window.document.location.href);
          if (qs["script"]) {
              this.ScriptLocationHidden.set("text", qs["script"]);
          }
      },
      saveScript: function () {
          "use strict";

          var postscript = {};
          postscript["Name"] = this.ScriptName.get("text");
          postscript["Type"] = this.ScriptTypeName.get("text");

          var serverRequestParameters = null,
            serverRequestOnSuccess = null,
            serverRequestUrl = this.SaveScriptDatasource.get("dataUrl");

          var providerItemProperties = {
              "script": JSON.stringify(postscript)
          };

          this.SaveScriptDatasource.viewModel.performPostRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
      }
  });

  return CreatenewMapping;
});