// Hint: Due to the openUI Syntax it is not possible to extend the classes right now. Therefore, declare your class and use the extend syntax shown below.

declare namespace sap.ui.demo.wt.controller {
     class App extends sap.ui.core.mvc.Controller {
        constructor();
        onShowHello();
    }
}

sap.ui.define("",[
    "sap/ui/core/mvc/Controller",
    "sap/m/MessageToast"
], function (Controller: sap.ui.core.mvc.Controller) {
    "use strict";
    return Controller.extend("sap.ui.demo.wt.controller.App", {
        onShowHello: function () {
            sap.m.MessageToast.show("Hello World");
        }
    });
});