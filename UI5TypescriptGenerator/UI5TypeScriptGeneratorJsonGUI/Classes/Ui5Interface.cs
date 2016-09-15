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
            sb.AppendLine($"interface {name} {(extends != null ? $"extends {extends} " : "")}{"{"}");

            AppendProperties(sb, alloptional: true);

            AppendMethods(sb);

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
