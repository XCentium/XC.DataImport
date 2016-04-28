(function (speak) {

    require.config({
        paths: {
            baseDataSource: "/sitecore/shell/client/Business Component Library/version 2/Layouts/Renderings/Data/BaseDataSources/BaseDataSource"
        }
    });

    speak.component(["baseDataSource"], function (baseDataSource) {

        return speak.extend(baseDataSource, {
            name: "DatabaseDatasource",

            initialized: function () {
                this.DynamicData = [];
                this.on("change:Parameters", this.reloadData, this);
            },

            reloadData: function (item) {
                "use strict";

                var serverRequestParameters = null,
                  serverRequestOnSuccess = null,
                  serverRequestUrl = this.ServiceUrl;

                var providerItemProperties = {
                    "item": item.Id,
                    "database": item.Database
                };

                this.performRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
            },
            loadMapping: function (item) {
                "use strict";

                var serverRequestParameters = null,
                  serverRequestOnSuccess = null,
                  serverRequestUrl = this.ServiceUrl;

                var providerItemProperties = {
                    "mapping": speak.utils.url.parameterByName("mapping")
                };

                this.performRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
            },
            
            loadData: function (serverRequestOptions) {
                "use strict";

                var serverRequestParameters = null,
                  serverRequestOnSuccess = null,
                  serverRequestUrl = this.ServiceUrl;

                if (serverRequestOptions) {
                    serverRequestParameters = serverRequestOptions["parameters"],
                    serverRequestOnSuccess = serverRequestOptions["onSuccess"],
                    serverRequestUrl = serverRequestOptions["url"] ? serverRequestOptions["url"] : this.ServiceUrl;
                }

                var providerItemProperties = {
                    "serverSorting": this.ServerSorting,
                    "pageSize": this.PageSize === 0 ? "" : this.PageSize,
                    "pageIndex": this.PageSize === 0 ? "" : this.PageIndex // if Page.Size==0 then PageIndex has no meaning
                };

                this.performRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
            },

            successHandler: function (jsonData) {

                this.HasMoreData = jsonData.hasMoreData;
                this.TotalRecordCount = jsonData.totalRecordCount;
                this.PageSize = jsonData.pageSize;
                this.PageIndex = jsonData.pageIndex;

                if (jsonData.sorting) {
                    this.ServerSorting = jsonData.sorting;
                }
                if (jsonData.data) {
                    this.DynamicData = jsonData.data;
                }
            },

            editMapping: function(path) {
                alert("editing");
            },
            runImport: function (path) {
                alert("importing");
            }
        });
    }, "DatabaseDatasource");
})(Sitecore.Speak);

