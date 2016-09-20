using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System.Linq;

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

        public string CreateDescription(IEnumerable<Ui5Parameter> pars)
        {
            StringBuilder csb = new StringBuilder();
            csb.AppendLine(description);
            foreach (Ui5Parameter par in pars)
                csb.AppendLine("@param " + par.name + " " + par.description + (par.optional ? "(optional)" : ""));
            if (deprecated != null)
                csb.AppendLine("@deprecated " + (since != null ? "since version " + since + ":" : "") + deprecated.text);
            return csb.ToString();
        }

        public string SerializeTypescript()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendComment(CreateDescription(Parameters));

            sb.Append(name);
            sb.Append("?: (");

            // append parameters
            sb.Append(Parameters.Where(x => !string.IsNullOrWhiteSpace(x.name)).Aggregate("", (a, b) =>
            {
                return a + ", " + b.name + (b.optional ? "?" : "") +
                (string.IsNullOrWhiteSpace(b.type) ? "" : ": " + b.GetRelativeTypeDef(owner));
            }).TrimStart(", ".ToCharArray()));
            sb.Append(") => void");
            return sb.ToString();
        }
    }
}