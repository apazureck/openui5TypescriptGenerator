using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Member : Ui5Base, ITyping
    {
        public bool @static { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Visibility visibility { get; set; }
        public Ui5Complex owner { get; set; }
    }
}