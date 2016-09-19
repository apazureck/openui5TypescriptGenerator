sap.ui.getCore().attachInit(function () {
    sap.ui.xmlview("", {
        viewName: "sap.ui.demo.wt.view.App"
    }).placeAt("content");
});