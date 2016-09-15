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

        public string SerializeTypescript(bool @explicit = false, bool createstatic = false, bool alloptional = false)
        {
            StringBuilder sb = new StringBuilder();
            string safeproptype = Ui5Value.GetRelativeTypeDef(owner, propertytype);
            if (description != null)
                sb.AppendComment(description + (defaultValue!=null? Environment.NewLine + "@default " + defaultValue : ""));
            sb.Append(!@explicit ? visibility.GetDescription() : "");
            sb.Append(@explicit ? (@static && createstatic ? "static " : "var ") : "");
            sb.Append(base.name);
            sb.Append((alloptional ? "?" : "") + ": ");
            sb.Append(safeproptype);
            sb.AppendLine(CreateDefaultValueSafely(safeproptype) + ";");
            return sb.ToString();
        }

        private string CreateDefaultValueSafely(string safeproptype)
        {
            return "";
            //switch (safeproptype)
            //{
            //    case "string":
            //        return "\"" + defaultValue + "\"";
            //    case null:
            //        return "";
            //    default:
            //        return "; // original default: " + defaultValue;
            //}
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
