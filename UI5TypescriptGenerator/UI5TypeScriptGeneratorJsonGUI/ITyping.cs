using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public interface ITyping
    {
        bool @static { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        Visibility visibility { get; set; }
    }
}