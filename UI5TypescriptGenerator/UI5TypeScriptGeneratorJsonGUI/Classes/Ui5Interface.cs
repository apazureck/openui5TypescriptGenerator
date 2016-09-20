using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Interface : Ui5Complex
    {
        public List<Ui5Event> events { get; set; }

        override public string SerializeTypescript()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"interface {name} {(extends != null ? $"extends {extends} " : "")}{"{"}");

            AppendProperties(sb, alloptional: true);

            AppendMethods(sb);

            AppendEvents(sb);

            sb.AppendLine("}");
            return sb.ToString();
        }

        private void AppendEvents(StringBuilder sb)
        {
            if (events != null)
                foreach (Ui5Event e in events)
                    if(e.IncludedInVersion())
                    {
                        e.DeserializeParameters();
                        sb.AppendLine(e.SerializeTypescript()+";", 1);
                    }
        }
    }
}
