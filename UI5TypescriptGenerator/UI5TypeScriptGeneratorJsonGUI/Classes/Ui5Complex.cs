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

        protected void AppendProperties(StringBuilder sb, bool @explicit = false, bool checkstatic = false)
        {
            foreach (Ui5Property property in properties)
                if (property.IncludedInVersion())
                    sb.AppendLine(property.SerializeTypescript(@explicit, checkstatic), 1);
        }

        protected void AppendMethods(StringBuilder sb, bool @explicit = false, bool createstatic = false)
        {
            foreach (Ui5Method method in methods)
                if (method.IncludedInVersion())
                    sb.AppendLine(method.SerializeTypescriptMethodStubs(@explicit, createstatic).Aggregate((a, b) => a + ";" + Environment.NewLine + b) + ";", 1);
        }

        public string fullname { get { return (string.IsNullOrWhiteSpace(@namespace) ? "" : @namespace + ".") + name; } }

        public string extends { get; set; }

        public void SetAbsolutePathsOnMembers()
        {
            IEnumerable<Ui5Member> members = methods.Cast<Ui5Member>().Concat(properties);
            foreach (Ui5Member member in members)
            {
                member.owner = this;
            }
        }

        public string @namespace
        {
            get; private set;
        }

        public bool @static { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Visibility visibility { get; set; }

        public Ui5Namespace parentNamespace { get; set; }

        public List<Ui5Complex> Content = new List<Ui5Complex>();

        override protected string DebuggerDisplay => $"{GetType().Name}: {name} ({fullname})";
    }
}
