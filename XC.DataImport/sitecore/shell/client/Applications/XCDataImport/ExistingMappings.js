define(["sitecore"], function (Sitecore) {
  var ExistingMapping = Sitecore.Definitions.App.extend({
      initialized: function () {
          this.MappingDatasource.loadData();
      }

  });

  return ExistingMapping;
});