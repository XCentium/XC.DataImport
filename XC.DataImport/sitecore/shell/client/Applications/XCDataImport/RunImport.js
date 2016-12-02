define(["sitecore"], function (Sitecore) {
  var RunImport = Sitecore.Definitions.App.extend({
      initialized: function () {
          this.MappingDatasource.loadMapping();
          this.refreshInterval;
      },
      startImport: function () {
          var taskId = "XC.DataImport";
          var self = this;
          this.ImportDatasource.startImport(taskId, this.Frame1);
          this.setRefresh(this.ImportResults, this.Frame1);
      },
      setRefresh: function (elem, frame) {
          var self = this;
          if (frame.el.contentWindow.location != frame.SourceUrl) {              
              self.refreshInterval = window.setInterval(function () {
                  self.refreshStatus(elem, frame.el.contentWindow.location);
              }, 5000);
          }
          
      },
      refreshStatus: function (elem, url) {
          var self = this;
          var ajaxOptions = {
              method: 'GET',
              contentType: "application/x-www-form-urlencoded; charset=UTF-8",
              headers: {},
              url: url,
              success: function (data) {
                  elem.el.innerHTML = data;
                  elem.el.scrollTop = elem.el.scrollHeight;
                  if (data.indexOf("DONE") >= 0) {
                      window.clearInterval(self.refreshInterval);
                  }
              },
              error: function (response) {
              }
          };
          var token = Sitecore.utils.security.antiForgery.getAntiForgeryToken();
          ajaxOptions.headers[token.headerKey] = token.value;
          $.ajax(ajaxOptions);
      }
  });

  return RunImport;
});