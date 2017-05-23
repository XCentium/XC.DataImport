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
                this.Messages = [];
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
            performPostRequest: function (serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess) {
                "use strict";

                var self = this;
                this.IsBusy = true;

                if (this.QueryParameters) {
                    serverRequestUrl += (serverRequestUrl.match(/\?/) ? '&' : '?') + this.QueryParameters;
                }

                var ajaxOptions = {
                    method : 'POST',
                    dataType: 'json',
                    contentType: "application/x-www-form-urlencoded; charset=UTF-8",
                    headers: {},
                    data: this.getRequestDataString(providerItemProperties),
                    url: serverRequestUrl,
                    success: function (data) {
                        self.baseSuccessHandler(data, serverRequestOnSuccess);
                    },
                    error: function (response) {
                        self.IsBusy = false;
                        self.handleError({ name: "Error", message: "Server returned" + ": " + response.status + " (" + response.statusText + ")", response: response });
                    }
                };

                var token = speak.utils.security.antiForgery.getAntiForgeryToken();
                ajaxOptions.headers[token.headerKey] = token.value;

                $.ajax(ajaxOptions);
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
                    "pageIndex": this.PageSize === 0 ? "" : this.PageIndex, // if Page.Size==0 then PageIndex has no meaning
                    "mapping": speak.utils.url.parameterByName("mapping")
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
                if (jsonData.messages) {
                    this.Messages = jsonData.messages;
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

