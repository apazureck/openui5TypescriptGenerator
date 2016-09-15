using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{

    public class Ui5Namespace : Ui5Complex
    {
        public override string SerializeTypescript()
        {
            StringBuilder sb = new StringBuilder();

            if (parentNamespace == null)
                foreach (var entry in Imports)
                    sb.AppendLine($"import {entry.Value} = {entry.Key};");

            if(Imports.Count>0)
                sb.AppendLine();

            if (description != null)
                sb.AppendComment(description);

            // Check if namespace has a parent and then create namespace declaration.
            // This is used for file splitting, e.g. splitting in sap.m.ts, sap.ui.d.ts, etc.
            if (parentNamespace == null)
                sb.AppendLine("declare namespace " + fullname + " {");
            else
                sb.AppendLine("namespace " + name + " {");

            // using for to make the collection changeable during execution
            for (int i = 0; i < Content.Count; i++) {
                sb.AppendLine();
                sb.AppendLine(Content[i].SerializeTypescript(), 1);
            }

            AppendProperties(sb, @explicit: true, checkstatic: false);

            AppendMethods(sb, true, false);

            additionaltypedefs.ForEach(x => sb.AppendLine(x, 1));

            sb.AppendLine("}");
            return sb.ToString();
        }

        public Dictionary<string, string> Imports { get; set; } = new Dictionary<string, string>();

        internal void AppendAdditionalTypedef(string typedef)
        {
            additionaltypedefs.Add(typedef);
        }

        private List<string> additionaltypedefs = new List<string>();
    }
}
