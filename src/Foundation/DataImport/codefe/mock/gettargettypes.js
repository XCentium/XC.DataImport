module.exports = {
    'GET /sitecore/api/ssc/dataimport/mappings/-/gettargettypes': function (req, res) {
      res.json(
        {"data":[{"Name":"SitecoreDataSource","ModelType":"XC.Foundation.DataImport.Models.DataSources.TargetSitecoreDataSourceModel","DataSourceType":"XC.Foundation.DataImport.Repositories.Repositories.SitecoreRepository","Fields":[{"Name":"DatabaseName","InputType":"select","OptionsSource":"/sitecore/api/ssc/dataimport/mappings/-/getdatabases"},{"Name":"Path","InputType":"tree","OptionsSource":null},{"Name":"Template","InputType":"select","OptionsSource":"/sitecore/api/ssc/dataimport/mappings/[DatabaseName]/gettemplates"}]},{"Name":"SitecoreFormsDataSource","ModelType":"XC.Foundation.DataImport.Models.DataSources.TargetSitecoreFormsDataSourceModel","DataSourceType":"XC.Foundation.DataImport.Repositories.Repositories.SitecoreRepository","Fields":[{"Name":"ConnectionStringName","InputType":"text","OptionsSource":null}]}],"messages":[]});
  }
}