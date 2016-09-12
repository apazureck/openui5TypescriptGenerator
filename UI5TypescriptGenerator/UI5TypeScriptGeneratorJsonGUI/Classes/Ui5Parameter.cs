using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Parameter : Ui5Value, ICloneable
    {
        public string name { get; set; }
        public bool optional { get; set; }

        public object Clone()
        {
            return new Ui5Parameter
            {
                description = description,
                name = name,
                optional = optional,
                type = type
            };
        }

        public JObject parameterProperties { get; set; }
    }
}
