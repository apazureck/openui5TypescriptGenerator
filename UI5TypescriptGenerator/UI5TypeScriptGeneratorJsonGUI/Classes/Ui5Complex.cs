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

        protected void AppendProperties(StringBuilder sb, bool @explicit = false, bool checkstatic = false, bool alloptional = false)
        {
            foreach (Ui5Property property in properties)
                if (property.IncludedInVersion())
                    sb.AppendLine(property.SerializeTypescript(@explicit, checkstatic, alloptional), 1);
        }

        protected void AppendMethods(StringBuilder sb, bool @explicit = false, bool createstatic = false)
        {
            foreach (Ui5Method method in methods)
                if (method.IncludedInVersion())
                {
                    string[] overloads = method.SerializeTypescriptMethodStubs(@explicit, createstatic);
                    if (overloads.Length>0)
                        sb.AppendLine(overloads.Aggregate((a, b) => a + ";" + Environment.NewLine + b) + ";", 1);
                }
        }

        /// <summary>
        /// Will check base class for any methods that are overridden and not type matching. The class will create overloads for those and add it to its method list.
        /// </summary>
        /// <param name="allcontent"></param>
        public void CheckOverloads(IEnumerable<Ui5Complex> allcontent, Ui5Complex requestor = null, Ui5Complex basetype = null)
        {
            if (requestor == null)
                requestor = this;

            if (basetype == null)
                basetype = this;

            Ui5Complex extender = allcontent.FirstOrDefault(x => x.fullname == basetype.extends);

            if (extender == null)
                return;

            if (!extender.Overloaded)
                extender.CheckOverloads(allcontent);

            if (extender.extends != null)
                extender.CheckOverloads(allcontent, requestor, extender);

            List<Ui5Method> appendmethods = new List<Ui5Method>();

            foreach (Ui5Method m in requestor.methods)
            {
                // bm = basemethod
                string[] mdefs = m.GetMethodDefinitions();
                if(mdefs==null)
                    continue;

                foreach (Ui5Method bm in extender.methods.Where(x => x.name == m.name))
                    foreach(string defbase in bm.GetMethodDefinitions())
                        foreach(string mdef in mdefs)
                            if(!mdef.Equals(defbase))
                                appendmethods.Add(bm);
            }

            requestor.methods.AddRange(appendmethods.Select(x =>
            {
                Ui5Method overload = new Ui5Method(x, requestor);
                overload.description += Environment.NewLine + "@note Overload from base type " + x.owner.fullname;
                return overload;
            }));
            requestor.methods = requestor.methods.OrderBy(x => x.name).ToList();
            requestor.Overloaded = true;
        }

        public bool Overloaded { get; set; }

        public string fullname { get { return (string.IsNullOrWhiteSpace(@namespace) ? "" : @namespace + ".") + name; } }

        public string extends { get; set; }

        public void SetAbsolutePathsOnMembers()
        {
            IEnumerable<Ui5Member> members = methods.Cast<Ui5Member>().Concat(properties);
            foreach (Ui5Member member in members)
                member.owner = this;
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
