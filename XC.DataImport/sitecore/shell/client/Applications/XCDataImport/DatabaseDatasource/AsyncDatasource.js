(function (speak) {

    require.config({
        paths: {
            baseDataSource: "/sitecore/shell/client/Business Component Library/version 2/Layouts/Renderings/Data/BaseDataSources/BaseDataSource"
        }
    });

    speak.component(["baseDataSource"], function (baseDataSource) {
        return speak.extend(baseDataSource, {
            name: "AsyncDatasource",

            initialized: function () {
                this.DynamicData = [];
                this.Progress = 0;
            },
            startImport: function (taskId) {
                "use strict";

                var serverRequestParameters = null,
                  serverRequestOnSuccess = null,
                  serverRequestUrl = this.ServiceUrl + "/" + this.StartMethod;

                var providerItemProperties = {
                    "mapping": speak.utils.url.parameterByName("mapping"),
                    "taskId": taskId
                };

                this.performRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
            },
            getStatus: function (taskId) {
                "use strict";

                var serverRequestParameters = null,
                  serverRequestOnSuccess = null,
                  serverRequestUrl = this.ServiceUrl + "/" + this.StatusCheckMethod;

                var providerItemProperties = {
                    "taskId": taskId
                };

                this.performRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
            },
            successHandler: function (jsonData) {
                this.Progress = jsonData.percentage;
                this.Log = jsonData.items;
            }
        });
    }, "AsyncDatasource");
})(Sitecore.Speak);
