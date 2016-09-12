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

            if (methods.Count == 0)
            {
                sb.AppendLine("type " + name + " = any");
                return sb.ToString();
            }

            sb.AppendLine("namespace " + name + " {");

            foreach (Ui5Method method in methods)
                if(method.IncludedInVersion())
                    sb.AppendLine(method.SerializeTypescriptMethodStubs(true).Aggregate((a, b) => a + ";" + Environment.NewLine + b) + ";", 1);

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
