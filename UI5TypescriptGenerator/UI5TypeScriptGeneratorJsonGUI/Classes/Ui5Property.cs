using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Property : Ui5Member
    {
        public string defaultValue { get; set; }

        public string group { get; set; }

        public List<string> methods { get; set; }

        private string _type;

        [JsonProperty("type")]
        public string propertytype
        {
            get { return _type; }
            set { _type = globalValues.ConvertToValidTypeIfKnown(value); }
        }

        public string SerializeTypescript(bool @explicit = false)
        {
            StringBuilder sb = new StringBuilder();
            if (description != null)
                sb.AppendComment(description);
            sb.AppendLine($"{visibility.GetDescription()}{(@explicit ? " var" : "")} {name}: {Ui5Value.GetRelativeTypeDef(propertytype, absolutepath)}{(defaultValue != null ? " = " + defaultValue : "")};");
            return sb.ToString();
        }

        public string CreateDescription()
        {
            StringBuilder csb = new StringBuilder();
            csb.AppendLine(description);
            if (deprecated != null)
                csb.AppendLine("@deprecated " + (since != null ? "since version " + since + ":" : "") + deprecated.text);
            return csb.ToString();
        }
    }
}
