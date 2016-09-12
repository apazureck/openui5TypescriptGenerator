using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public abstract class Ui5Complex : Ui5Symbol, ITyping
    {
        public List<Ui5Method> methods { get; set; } = new List<Ui5Method>();
        public List<Ui5Property> properties { get; set; } = new List<Ui5Property>();
        public abstract string SerializeTypescript();
        public override string name
        {
            get
            {
                return base.name;
            }

            set
            {
                List<string> parts = value.Split('.').ToList();
                base.name = parts.Last();
                parts.Remove(parts.Last());
                @namespace = parts.Count > 0 ? parts.Aggregate((a, b) => a + "." + b) : "";
            }
        }

        public string extends { get; set; }

        public string @namespace
        {
            get; private set;
        }

        public bool @static { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Visibility visibility { get; set; }
    }
}
