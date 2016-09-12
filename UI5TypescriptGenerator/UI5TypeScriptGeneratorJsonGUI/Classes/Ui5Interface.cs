using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Interface : Ui5Complex
    {
        override public string SerializeTypescript()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"interface {name} {(extends != null ? "extends " + extends : "")}{"{"}");

            foreach (Ui5Property property in properties)
                if (property.IncludedInVersion())
                    sb.AppendLine(property.SerializeTypescript());

            foreach (Ui5Method method in methods)
                if(method.IncludedInVersion())
                    sb.AppendLine(method.SerializeTypescriptMethodStubs().Aggregate((a, b) => a + ";" + Environment.NewLine + b) + ";", 1);
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
