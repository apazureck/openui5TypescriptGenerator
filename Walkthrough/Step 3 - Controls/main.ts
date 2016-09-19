sap.ui.getCore().attachInit(function () {
    new sap.m.Text("", {
        text: "Hello World"
    }).placeAt("content");
});