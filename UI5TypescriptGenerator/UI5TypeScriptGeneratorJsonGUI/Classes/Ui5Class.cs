using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Class : Ui5Complex
    {
        [JsonProperty("ui5-metadata")]
        public Ui5Metadata ui5Metadata { get; set; }
        public Ui5Constructor constructor { get; set; }
        override public string SerializeTypescript()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"class {name} {(extends != null ? "extends " + extends : "")}{"{"}");
            if(constructor.visibility == Visibility.Public && constructor.IncludedInVersion())
                sb.AppendLine(constructor.SerializeTypescriptMethodStubs().Aggregate((a, b) => a + ";" + Environment.NewLine + b)+ ";", 1);
            foreach (Ui5Method method in methods)
                if(method.IncludedInVersion())
                    sb.AppendLine(method.SerializeTypescriptMethodStubs().Aggregate((a, b) => a + ";" + Environment.NewLine + b) + ";", 1);
            foreach (Ui5Property property in properties)
                if (property.IncludedInVersion())
                    sb.AppendLine(property.SerializeTypescript());
            sb.AppendLine("}");
            return sb.ToString();
        }

        private void CreateMethod(StringBuilder sb)
        {
            sb.AppendComment(description, 1);
        }

        override protected string DebuggerDisplay => "Ui5Class: " + name + " (" + @namespace + "." + name + ")";
    }
}
