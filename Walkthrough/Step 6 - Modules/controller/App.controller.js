// Hint: Due to the openUI Syntax it is not possible to extend the classes right now. Therefore, declare your class and use the extend syntax shown below.
sap.ui.define("", [
    "sap/ui/core/mvc/Controller",
    "sap/m/MessageToast"
], function (Controller) {
    "use strict";
    return Controller.extend("sap.ui.demo.wt.controller.App", {
        onShowHello: function () {
            sap.m.MessageToast.show("Hello World");
        }
    });
});
//# sourceMappingURL=App.controller.js.map