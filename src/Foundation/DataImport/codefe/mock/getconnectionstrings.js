module.exports = {
    'GET /sitecore/api/ssc/dataimport/mappings/-/getconnectionstrings': function (req, res) {
      res.json({"data":[
        {"Name":"LocalSqlServer","Value":"data source=.\\SQLEXPRESS;Integrated Security=SSPI;AttachDBFilename=|DataDirectory|aspnetdb.mdf;User Instance=true","Id":"LocalSqlServer"},
        {"Name":"core","Value":"Data Source=JGAVRILOVA-XC;Initial Catalog=sxa_Core;User ID=coreuser;Password=Test12345","Id":"core"},
        {"Name":"master","Value":"Data Source=JGAVRILOVA-XC;Initial Catalog=sxa_Master;User ID=masteruser;Password=Test12345","Id":"master"},
        {"Name":"web","Value":"Data Source=JGAVRILOVA-XC;Initial Catalog=sxa_Web;User ID=webuser;Password=Test12345","Id":"web"},
        {"Name":"xconnect.collection","Value":"https://sxa.xconnect","Id":"xconnect.collection"},
        {"Name":"xconnect.collection.certificate","Value":"StoreName=My;StoreLocation=LocalMachine;FindType=FindByThumbprint;FindValue=43F0A6A5334A5115F72D5FBDD5A46D03230E31C6","Id":"xconnect.collection.certificate"},
        {"Name":"xdb.referencedata","Value":"Data Source=JGAVRILOVA-XC;Initial Catalog=sxa_ReferenceData;User ID=referencedatauser;Password=Test12345","Id":"xdb.referencedata"},
        {"Name":"xdb.referencedata.client","Value":"https://sxa.xconnect","Id":"xdb.referencedata.client"},
        {"Name":"xdb.referencedata.client.certificate","Value":"StoreName=My;StoreLocation=LocalMachine;FindType=FindByThumbprint;FindValue=43F0A6A5334A5115F72D5FBDD5A46D03230E31C6","Id":"xdb.referencedata.client.certificate"},
        {"Name":"xdb.processing.pools","Value":"Data Source=JGAVRILOVA-XC;Initial Catalog=sxa_Processing.Pools;User ID=poolsuser;Password=Test12345","Id":"xdb.processing.pools"},
        {"Name":"reporting","Value":"Data Source=JGAVRILOVA-XC;Initial Catalog=sxa_Reporting;User ID=reportinguser;Password=Test12345","Id":"reporting"},
        {"Name":"xdb.processing.tasks","Value":"Data Source=JGAVRILOVA-XC;Initial Catalog=sxa_Processing.Tasks;User ID=tasksuser;Password=Test12345","Id":"xdb.processing.tasks"},
        {"Name":"xdb.marketingautomation","Value":"Data Source=JGAVRILOVA-XC;Initial Catalog=sxa_MarketingAutomation;User ID=marketingautomationuser;Password=Test12345","Id":"xdb.marketingautomation"},
        {"Name":"xdb.marketingautomation.reporting.client","Value":"https://sxa.xconnect","Id":"xdb.marketingautomation.reporting.client"},
        {"Name":"xdb.marketingautomation.reporting.client.certificate","Value":"StoreName=My;StoreLocation=LocalMachine;FindType=FindByThumbprint;FindValue=43F0A6A5334A5115F72D5FBDD5A46D03230E31C6","Id":"xdb.marketingautomation.reporting.client.certificate"},
        {"Name":"xdb.marketingautomation.operations.client","Value":"https://sxa.xconnect","Id":"xdb.marketingautomation.operations.client"},
        {"Name":"xdb.marketingautomation.operations.client.certificate","Value":"StoreName=My;StoreLocation=LocalMachine;FindType=FindByThumbprint;FindValue=43F0A6A5334A5115F72D5FBDD5A46D03230E31C6","Id":"xdb.marketingautomation.operations.client.certificate"},
        {"Name":"experienceforms","Value":"Data Source=JGAVRILOVA-XC;Initial Catalog=sxa_ExperienceForms;User ID=formsuser;Password=Test12345","Id":"experienceforms"}],"messages":""});
    }
  };
  