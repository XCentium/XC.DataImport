define(["sitecore"], function (Sitecore) {
    var RunImport = Sitecore.Definitions.App.extend({
      initialized: function () {
          var qs = _sc.Helpers.url.getQueryParameters(window.document.location.href);
          if (qs["mapping"]) {
              this.MappingPathHidden.set("text", qs["mapping"]);
              this.set("mapping", qs["mapping"]);
          }
      },
      editMapping: function () {
          var navigateUrl = this.EditButton.get("navigateUrl");
          if (navigateUrl) {
              navigateUrl += "&mapping=" + this.get("mapping")
          }
          window.open(navigateUrl);
      },
      startImport: function () {
          var taskId = "XC.DataImport";
          var self = this;
          this.ImportDatasource.viewModel.startImport(taskId, this.Frame1);
          this.setRefresh(this.ImportResults, this.Frame1);
      },
      setRefresh: function (elem, frame) {
          var self = this;
          if (frame.viewModel.$el[0].contentWindow.location.href != frame.SourceUrl) {              
              self.refreshInterval = window.setInterval(function () {
                  self.refreshStatus(elem, frame.viewModel.$el[0].contentWindow.location.href);
              }, 5000);
          }
          
      },
      refreshStatus: function (elem, url) {
          if (url) {
              var self = this;
              var ajaxOptions = {
                  method: 'GET',
                  contentType: "application/x-www-form-urlencoded; charset=UTF-8",
                  headers: {},
                  url: url,
                  success: function (data) {
                      elem.viewModel.$el[0].innerHTML = data;
                      elem.viewModel.$el[0].scrollTop = elem.viewModel.$el[0].scrollHeight;
                      if (data.indexOf("DONE") >= 0) {
                          window.clearInterval(self.refreshInterval);
                      }
                  },
                  error: function (response) {
                  }
              };
              var token = _sc.Helpers.antiForgery.getAntiForgeryToken();
              ajaxOptions.headers[token.headerKey] = token.value;
              $.ajax(ajaxOptions);
          }
      }
  });

  return RunImport;
});