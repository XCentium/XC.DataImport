﻿require.config({
  paths: {
    baseDataprovider: "/sitecore/shell/client/Speak/Layouts/Renderings/Data/DataProviders/BaseDataprovider"
  }
});

define(["sitecore", "baseDataprovider"], function (_sc) {
  _sc.Factories.createBaseComponent({
    name: "ServiceDatasource",
    base: "BaseDataProvider",
    selector: "script[type='text/x-sitecore-servicedatasource']",
    sortingChanging: false,
    attributes: [
        { name: "dataUrl", value: "$el.data:sc-dataurl" },
        { name: "queryParameters", value: "$el.data:sc-queryparameters" },
        { name: "serverSorting", value: "$el.data:sc-serversorting" },
        { name: "listSorting", defaultValue: null},
        { name: "pageSize", value: "$el.data:sc-pagesize" },
        { name: "pageNumber", value: "$el.data:sc-pagenumber" },
        { name: "hasMoreData", defaultValue: 0 },
        { name: "totalRecordCount", defaultValue: -1 },
        { name: "hasData", defaultValue: false },
        { name: "hasNoData", defaultValue: false },
        { name: "isBusy", defaultValue: false }
    ],
    initialize: function () {
      this._super();
      this.model.on("change:serverSorting", this.formatListSorting, this);
      this.model.on("change:listSorting", this.formatServerSorting, this);
      this.model.on("change:dataUrl", this.getData, this);
      this.on("change:Parameters", this.getData, this);

      this.formatListSorting();
    },
    /// <summary>
    /// Format serverSorting to listSorting.
    /// </summary>
    formatListSorting: function () {

      if (this.sortingChanging) {
        return;
      }

      this.sortingChanging = true;
      
      var serverSorting = this.model.get("serverSorting");
      if (serverSorting) {

        var direction,
            field,
            listSorting = "",
            serverSortingLength = serverSorting.length;
        for (var i = 0; i < serverSortingLength; i++) {
          direction = serverSorting[i].direction.substring(0,1).toLowerCase();
          field = serverSorting[i].field;
          listSorting = listSorting + "|" + direction + field;
        }
        if (listSorting.length > 0) {
          listSorting = listSorting.substring(1, listSorting.length);
        }
        this.model.set("listSorting", listSorting);
      } else {
        this.model.set("listSorting", null);
      }
      
      this.sortingChanging = false;
    },
    
    /// <summary>
    /// Format listSorting to serverSorting.
    /// </summary>
    formatServerSorting: function () {
      if (this.sortingChanging) {
        return;
      }

      this.sortingChanging = true;
      
      var listSorting = this.model.get("listSorting");
      if (listSorting) {
        var direction,
            serverSorting = new Array(),
            listSortingArray = listSorting.split("|"),
            listSortingArrayLenght = listSortingArray.length;
        for (var i = 0; i < listSortingArrayLenght; i++) {
          direction = listSortingArray[i].substring(0, 1);
          direction = direction === "a" ? "Asc" : "Desc";
          field = listSortingArray[i].substring(1, listSortingArray[i].length);
          serverSorting.push({
            "direction": direction,
            "field": field
          });
        }
        this.model.set("serverSorting", serverSorting);
      } else {
        this.model.set("serverSorting", null);
      }
      
      this.sortingChanging = false;
    },
    
    getData: function (serverRequestOptions) {
        "use strict";

      var serverRequestParameters = null,
                        serverRequestOnSuccess = null,
                        serverRequestUrl = this.model.get("dataUrl");

      if (serverRequestOptions) {
          var serverRequestParameters = serverRequestOptions["parameters"],
            serverRequestOnSuccess = serverRequestOptions["onSuccess"],
            serverRequestUrl = serverRequestOptions["url"] ? serverRequestOptions["url"] : this.model.get("dataUrl");
      }

      var providerItemProperties = {
        "serverSorting": this.model.get("serverSorting"),
        "pageSize": this.model.get("pageSize"),
        "pageNumber": this.model.get("pageNumber")
      };

      this.performRequest(serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess);
    },
    getRequestDataString: function (providerRequestData, serverRequestParameters, queryParameters) {
        var requestString = "",
          queryParametersString = queryParameters || "";

        requestString = this.removeNotUsedParameters($.param(providerRequestData));

        if (serverRequestParameters) {
            var serverRequestParametersString = this.removeNotUsedParameters($.param(serverRequestParameters));
        
            if (serverRequestParametersString.substr(0, 1) !== "&") {
                serverRequestParametersString = "&" + serverRequestParametersString;
            }

            requestString = requestString + serverRequestParametersString;
        }
      
        if (queryParameters) {
            requestString = requestString + "&item=" + queryParametersString;
        }
      
        return (requestString);
    },
    performPostRequest: function (serverRequestUrl, providerItemProperties, serverRequestParameters, serverRequestOnSuccess) {
        "use strict";

        var self = this;
        this.model.set("isBusy", true);

        if (this.QueryParameters) {
            serverRequestUrl += (serverRequestUrl.match(/\?/) ? '&' : '?') + this.QueryParameters;
        }

        var ajaxOptions = {
            method: 'POST',
            dataType: "json",
            //contentType: "application/x-www-form-urlencoded; charset=UTF-8",
            headers: {},
            data: providerItemProperties,
            url: serverRequestUrl,
            success: function (data) {
                self.baseSuccessHandler(data, serverRequestOnSuccess);
            },
            error: function (response) {
                self.handleError({ name: "Error", message: self.$el.data("sc-serverreturn") + ": " + response.status + " (" + response.statusText + ")", response: response });
            }
        };

        var token = _sc.Helpers.antiForgery.getAntiForgeryToken();
        ajaxOptions.headers[token.headerKey] = token.value;

        $.ajax(ajaxOptions);
    },
    successHandler: function (jsonData) {            
      this.model.set("totalRecordCount", jsonData.data.length);
      this.model.set("items", jsonData.data);
      this.model.set("data", jsonData.data);
      this.model.set("messages", jsonData.messages);
    }
  });
});