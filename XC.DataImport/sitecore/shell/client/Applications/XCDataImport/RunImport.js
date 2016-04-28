define(["sitecore"], function (Sitecore) {
  var RunImport = Sitecore.Definitions.App.extend({
      initialized: function () {
          this.MappingDatasource.loadMapping();
      },
      startImport: function () {
          var taskId = "XC.DataImport";
          var self = this;
          _sc.on("intervalCompleted:ImportProgress", function() {
              self.getStatus(taskId);
          });
          this.ImportDatasource.startImport(taskId);
      },
      getStatus: function (taskId) {
          if (this.ImportProgress.Percentage < 100) {
              this.ImportDatasource.getStatus(taskId);
              this.ImportProgress.Percentage = this.ImportDatasource.Progress;
              this.ImportLog.innerHtml = this.ImportDatasource.Log;
          }
          else {
              _sc.stopListening(this.ImportProgress, "intervalCompleted:ImportProgress");
          }
      }
  });

  return RunImport;
});