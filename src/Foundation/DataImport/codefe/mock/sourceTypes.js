module.exports = {
    'GET /sitecore/api/ssc/dataimport/mappings/-/getsourcetypes': function (req, res) {
      res.json(
        {"data":[{"Name":"FileDataSource","ModelType":"XC.Foundation.DataImport.Repositories.DataSources.FileDataSource","DataSourceType":"XC.Foundation.DataImport.Models.DataSources.FileDataSourceModel","Fields":[{"Name":"FilePath","InputType":"text","OptionsSource":null,"TriggerFields":null}]},{"Name":"SitecoreDataSource","ModelType":"XC.Foundation.DataImport.Repositories.DataSources.SitecoreDataSource","DataSourceType":"XC.Foundation.DataImport.Models.DataSources.SitecoreDataSourceModel","Fields":[{"Name":"DatabaseName","InputType":"select","OptionsSource":"/sitecore/api/ssc/dataimport/mappings/-/getdatabases","TriggerFields":"Template,Path"},{"Name":"Template","InputType":"select","OptionsSource":"/sitecore/api/ssc/dataimport/mappings/[DatabaseName]/gettemplates","TriggerFields":null},{"Name":"Path","InputType":"tree","OptionsSource":"/sitecore/api/ssc/dataimport/mappings/[DatabaseName]/getsitecoretree","TriggerFields":null}]},{"Name":"SitecoreQueryDataSource","ModelType":"XC.Foundation.DataImport.Repositories.DataSources.SitecoreQueryDataSource","DataSourceType":"XC.Foundation.DataImport.Models.DataSources.SitecoreQueryDataSourceModel","Fields":[{"Name":"DatabaseName","InputType":"select","OptionsSource":"/sitecore/api/ssc/dataimport/mappings/-/getdatabases","TriggerFields":null},{"Name":"Query","InputType":"textarea","OptionsSource":null,"TriggerFields":null}]},{"Name":"SqlDataSource","ModelType":"XC.Foundation.DataImport.Repositories.DataSources.SqlDataSource","DataSourceType":"XC.Foundation.DataImport.Models.DataSources.SqlDataSourceModel","Fields":[{"Name":"ConnectionStringName","InputType":"textarea","OptionsSource":"/sitecore/api/ssc/dataimport/mappings/-/GetConnectionStrings","TriggerFields":null},{"Name":"SqlStatement","InputType":"textarea","OptionsSource":null,"TriggerFields":null}]}],"messages":[]});
  }
}