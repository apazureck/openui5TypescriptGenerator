using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{

    class Ui5Namespace : Ui5Complex
    {
        public override string SerializeTypescript()
        {
            StringBuilder sb = new StringBuilder();
            if (description != null)
                sb.AppendComment(description);

            // Check if namespace has a parent and then create namespace declaration.
            // This is used for file splitting, e.g. splitting in sap.m.ts, sap.ui.d.ts, etc.
            if (parentNamespace == null)
                sb.AppendLine("declare namespace " + fullname + " {");
            else
                sb.AppendLine("namespace " + name + " {");

            Content.ForEach(x =>
            {
                sb.AppendLine();
                sb.AppendLine(x.SerializeTypescript(), 1);
            });

            AppendProperties(sb, true);

            AppendMethods(sb, true);

            sb.AppendLine("}");
            return sb.ToString();
        }

        public List<Ui5Complex> Content = new List<Ui5Complex>();
        
        public Ui5Namespace parentNamespace { get; set; }
    }
}
