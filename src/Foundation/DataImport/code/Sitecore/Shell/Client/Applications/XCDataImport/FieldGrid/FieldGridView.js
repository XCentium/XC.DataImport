(function (speak) {

    speak.component(["knockout"], function (ko) {

        var initializeBinding = function () {
            var formData = [];
            var rows = $(this.el).find("div[class*='row_']");
            if (rows) {
                for (var i = 0; i < rows.length; i++) {
                    var rowData = {};
                    var indexedNodes = $(rows[i]).find("*[data-sc-id*='_']");
                    for (var k = 0; k < indexedNodes.length; k++) {
                        if (indexedNodes[k]) {
                            var keyName = indexedNodes[k].attributes["data-sc-id"].value.split("_");
                            var key = keyName[0];
                            var fieldValue;
                            switch (indexedNodes[k].localName) {
                                case "label":
                                    var nestedInput = $(indexedNodes[k]).find("input").get(0);
                                    fieldValue = nestedInput.type === "checkbox" || nestedInput.type === "radio" ? nestedInput.checked : nestedInput.value;
                                    break;
                                case "input":
                                    fieldValue = $(indexedNodes[k]).val();
                                    break;
                                case "select":
                                    if (indexedNodes[k].selectedIndex > -1) {
                                        fieldValue = indexedNodes[k].selectedOptions[0].value;
                                    }
                                break;
                            }
                        }
                        rowData[key] = fieldValue;
                    }
                    formData[i] = rowData;
                }
            }
            return formData;

        },
        rebind = function (app, config, bindableData) {
            var cleanData = bindableData;
             speak.module("bindings").applyBindings(bindableData, config, app);
             this.setFormData(cleanData);
        },

        getBindingConfigProperties = function (bindableData) {
            return _.pick(bindableData, _.keys(this.Bindings));
        },
        getBindingConfiguration = function () {
            if (!this.BindingConfiguration) {
                return;
            }
            c = JSON.parse(this.BindingConfiguration);
        },
        processBinding = function () {
            var self = this;
            if (this.FormData.FieldMapping) {
                self.renderFormData();
            }
        },
        
        disableDefaultFormSubmitOnEnter = function (e) {
            if (e.keyCode === 13) {
                e.preventDefault();
            }
        };

        return {
            initialized: function () {
                initializeBinding.call(this);
                this.FormData.FieldMapping = [];
                this.Bindings = {};
                this.on("change:FormDataSaved", this.processBinding, this);
            },

            setFormData: function (properties) {
                _.extend(this.FormData.FieldMapping, properties);
            },

            getFormData: function () {
                return initializeBinding.call(this);
            },
            processBinding: function(){
                return processBinding.call(this);
            },

            clickHandler: function (e) {
                var invocation = this.Click;

                if (this.Click) {
                    var i = invocation.indexOf(":");
                    if (i <= 0) {
                        throw "Invocation is malformed (missing 'handler:')";
                    }

                    Speak.module("pipelines").get("Invoke").execute({
                        control: this,
                        app: this.app,
                        handler: invocation.substr(0, i),
                        target: invocation.substr(i + 1)
                    });
                }

                this.insertRow();
            },
            renderFormData: function () {
                if (this.FormData.FieldMapping) {
                    var self = this;
                    $(this.FormData.FieldMapping).each(function (i) {
                        var mappingRow = self.FormData.FieldMapping[i];
                        var row = $(self.el).find("div[class*='row_"+i+"']");
                        if (row && row.length > 0) {
                            self.populateRow(row, i, mappingRow);
                        } else {
                            row = self.insertRow(function () { self.populateRow(row, i, mappingRow); });                            
                        }
                    });
                }
            },
            populateRow: function (row, i, mappingRow) {
                var indexedNodes = $(row).find("*[data-sc-id*='_" + i + "']");
                for (var k = 0; k < indexedNodes.length; k++) {
                    if (indexedNodes[k]) {
                        var fieldName = $(indexedNodes[k]).attr("data-sc-id").split("_")[0];
                        if (fieldName) {
                            switch (indexedNodes[k].localName) {
                                case "label":
                                    if ($(indexedNodes[k]).attr("data-sc-id") === fieldName + "_" + i) {
                                        var nestedInput = $(indexedNodes[k]).find("input").get(0);
                                        nestedInput.checked = mappingRow[fieldName];
                                    }
                                    break;
                                case "input":
                                    $(indexedNodes[k]).val(mappingRow[fieldName]);
                                    break;
                                case "select":
                                    $(indexedNodes[k]).val(mappingRow[fieldName]);
                                    break;
                            }
                        }
                    }
                }
            },
            insertRow: function (callBack) {
                var firstRow = $(this.el).find("div[class*='row_']").last();
                var newRow = $(firstRow).clone(true);
                var indexedNodes = $(newRow).find("*[data-sc-id*='_']");
                var newIdx = 0;
                var bindings = getBindingConfiguration.call(this);
                for (var i = 0; i < indexedNodes.length ; i++) {
                    for (var a = 0; a < indexedNodes[i].attributes.length; a++) {
                        if (indexedNodes[i].attributes[a].nodeValue.indexOf("_") > -1) {
                            var attributeParts = indexedNodes[i].attributes[a].nodeValue.split("_");
                            var idx = parseInt(attributeParts[1]);
                            newIdx = (idx + 1);
                            indexedNodes[i].attributes[a].nodeValue = attributeParts[0] + "_" + newIdx;

                            var newRowClass = $(newRow).attr("class").replace("row_" + idx, "row_" + newIdx);
                            $(newRow).attr("class", newRowClass)

                            var id = $(indexedNodes[i]).attr("data-sc-id");
                            this[id] = _.extend(true, {}, this[attributeParts[0] + "_0"]);

                        }
                    }
                }

                newRow.insertAfter($(this.el).find("div[class*='row_']").last());
                _sc.app.insertMarkups(newRow, { el: this.el, parse: true }, callBack);

                return newRow;
            }
            
        };
    }, "FieldGridView");

})(Sitecore.Speak);