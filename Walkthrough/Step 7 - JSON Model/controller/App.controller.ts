// Hint: Due to the openUI Syntax it is not possible to extend the classes right now. Therefore, declare your class and use the extend syntax shown below.

declare namespace sap.ui.demo.wt.controller {
     class App extends sap.ui.core.mvc.Controller {
        constructor();
        onShowHello();
    }
     interface recipient {
         name: string;
     }

     interface jsonmodel {
         recipient: recipient;
     }
}

sap.ui.define([
    "sap/ui/core/mvc/Controller",
    "sap/m/MessageToast",
    "sap/ui/model/json/JSONModel"
], function (Controller: sap.ui.core.mvc.Controller) {
    "use strict";
    return Controller.extend("sap.ui.demo.wt.controller.App", {
        onInit: function () {
            // set data model on view
            var oData: sap.ui.demo.wt.controller.jsonmodel = {
                recipient: {
                    name: "World"
                }
            };

            var oModel = new sap.ui.model.json.JSONModel(oData, false);
            this.getView().setModel(oModel);
        },
        onShowHello: function () {
            sap.m.MessageToast.show("Hello World");
        }
    });
});