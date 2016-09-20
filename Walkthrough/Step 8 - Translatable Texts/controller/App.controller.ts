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
    "sap/ui/model/json/JSONModel",
    "sap/ui/model/resource/ResourceModel"
], function (Controller: sap.ui.core.mvc.Controller) {
    "use strict";
    return Controller.extend("sap.ui.demo.wt.controller.App", {
        onInit: function () {
            // set data model on view
            var oData = {
                recipient: {
                    name: "World"
                }
            };
            var oModel = new sap.ui.model.json.JSONModel(oData, false);
            this.getView().setModel(oModel);
            // set i18n model on view
            var i18nModel = new sap.ui.model.resource.ResourceModel({
                bundleName: "sap.ui.demo.wt.i18n.i18n"
            });
            this.getView().setModel(i18nModel, "i18n");
        },
        onShowHello: function () {
            // read msg from i18n model
            let oResourceModel: sap.ui.model.resource.ResourceModel = this.getView().getModel("i18n");
            let oBundle = oResourceModel.getResourceBundle();
            let oRecipient: sap.ui.model.json.JSONModel = this.getView().getModel();
            let sRecipient = oRecipient.getProperty("/recipient/name");
            let sMsg = oBundle.getText("helloMsg", [sRecipient]);
            // show message
            sap.m.MessageToast.show(sMsg);
        }
    });
});