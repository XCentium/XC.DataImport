define(["sitecore"], function (_sc) {
    _sc.Factories.createBaseComponent({
        name: "FieldGridView",
        selector: "script[type='text/x-sitecore-fieldgridview']",
        sortingChanging: false,
        attributes: [
        ],
        initialize: function () {
            this._super();
            //this.model.on("change:serverSorting", this.formatListSorting, this);
            //this.model.on("change:listSorting", this.formatServerSorting, this);
            //this.model.on("change:dataUrl", this.getData, this);
        }
    });
});