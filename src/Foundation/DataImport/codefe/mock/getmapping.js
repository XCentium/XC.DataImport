module.exports = {
    'GET /sitecore/api/ssc/dataimport/mappings/8e63916d-01f6-4b6e-a25c-d9e6fc244975/getmapping': function (req, res) {
      res.json({"data":
      {
        "Name": "Article Mapping",
        "Id": "8e63916d-01f6-4b6e-a25c-d9e6fc244975",
        "SourceType": { "Name":"FileDataSource", "ModelType":"XC.Foundation.DataImport.Models.DataSources.FileDataSourceModel,XC.Foundation.DataImport", "DataSourceType":"XC.Foundation.DataImport.Repositories.DataSources.FileDataSource,XC.Foundation.DataImport" },
        "Source": {
          "FilePath": "C:\\Users\\Julia.Gavrilova\\Downloads\\xcentium-samplefiles\\xcentium-testfiles\\XCentium-TestFiles-17-nov-10_11.57.39_295_01-17314115739-1.xml"
        },
        "SourceProcessingScripts": [
          "Aha.Project.DataImport.Scripts.Source.XmlFilterArticleList, Aha.Project.DataImport",
          "Aha.Project.DataImport.Scripts.Source.XmlFilterPageList, Aha.Project.DataImport"
        ],
        "PostImportScripts": [
          "Aha.Project.DataImport.Scripts.Source.XmlFilterArticleList, Aha.Project.DataImport",
          "Aha.Project.DataImport.Scripts.Source.XmlFilterPageList, Aha.Project.DataImport"
        ],
        "TargetType": { "Name":"SitecoreDataSource", "ModelType":"XC.Foundation.DataImport.Models.DataSources.FileDataSourceModel,XC.Foundation.DataImport", "DataSourceType":"XC.Foundation.DataImport.Repositories.DataSources.FileDataSource,XC.Foundation.DataImport" },
        "Target": { 
        "DatabaseName": "master",
        "TemplateId": "sitecore://master/{AD386352-ACAF-42CF-9DFC-0EB3700BAAA8}?lang=en&ver=0",
        "ItemPath": "/sitecore/content/Data Import/News",
        "FullPath": "/{11111111-1111-1111-1111-111111111111}/{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}/{1B86E247-1DEA-4865-968D-ADFBBD872B24}/{90002DEA-EE90-4313-91EB-6460A80DF553}"},
        "FieldMappings": [
          {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"dDocTitle","TargetFields":"{DFCC317E-E90C-4FEA-A8E3-0304E0A3986F}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"contentID","TargetFields":"{3FAFC95D-33FC-4EEA-986C-E71254B48DA5}","IsId":true,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"webViewableFile","TargetFields":"{B07A6D05-D5E5-440F-A9A0-C7C13CC76649}","IsId":false,"ProcessingScripts":["Aha.Project.DataImport.Scripts.Fields.ResolveWeblayoutReference,Aha.Project.DataImport","Aha.Project.DataImport.Scripts.Fields.UpdateReferences,Aha.Project.DataImport","Aha.Project.DataImport.Scripts.Fields.RemoveLegacyMarkup,Aha.Project.DataImport"]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"dReleaseDate","TargetFields":"{366882F0-B954-4F09-B881-0C229DDDE2E2}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"xSyndGroup","TargetFields":"{19FEFC37-6CDB-4621-8C98-3B281E23DF36}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"xDepartment","TargetFields":"{D30D674F-4347-423D-9AFD-64571E6A66A6}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"xKeywords","TargetFields":"{221B3ECB-7961-4310-8833-8D6258E3FB83}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"xComments","TargetFields":"{506FA8CB-7356-4BA6-B2F5-AA42250D39C1}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"xSyndGroup","TargetFields":"{ED0283A9-C8D7-4B26-B4E5-D49242471226}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"xNextReviewDate","TargetFields":"{54B31CA5-5E6D-4FE1-9897-0A2245033445}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"xModifyDate","TargetFields":"{25BED78C-4957-4165-998A-CA1B52F67497}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"dCreateDate","TargetFields":"{D9CF14B1-FA16-4BA6-9288-E8A174D4D522}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"xOriginalAuthor","TargetFields":"{5DD74568-4D4B-44C1-B513-0AF5F4CDA34F}","IsId":false,"ProcessingScripts":[]},
        {"ReferenceItemsTemplate":"","ReferenceItemsField":"","Exclude":false,"Overwrite":true,"SourceFields":"dDocAuthor","TargetFields":"{BADD9CF9-53E0-4D0C-BCC0-2D784C282F6A}","IsId":false,"ProcessingScripts":[]}
        ]
      },"messages":[]});
    }
  };
  