using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Event : Ui5Property
    {
        public bool enableEventBubbling { get; set; }

        [JsonProperty("parameters")]
        public dynamic parametersRaw { get; set; }

        public List<Ui5Parameter> Parameters { get; } = new List<Ui5Parameter>();

        internal void DeserializeParameters()
        {
            if (parametersRaw != null)
            {
                foreach (var parameter in parametersRaw)
                    Parameters.Add(JsonConvert.DeserializeObject<Ui5Parameter>(((JProperty)parameter).First.ToString()));
            }
        }

        public string SerializeTypescript()
        {
            return null;
        }
    }
}