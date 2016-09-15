using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            sb.AppendLine($"class {name} {(extends != null ? "extends " + Ui5Value.GetRelativeTypeDef(this, extends) : "")} {"{"}");

            string metadata = null;

            if (ui5Metadata != null)
                metadata = CreateMetaDataStrings();

            if (constructor.visibility == Visibility.Public && constructor.IncludedInVersion())
            {
                sb.AppendLine(constructor.SerializeTypescriptMethodStubs().Aggregate((a, b) => a + ";" + Environment.NewLine + b) + ";", 1);
                // Create overload with any (to be extendable and get all intellisense experience.
                if (Metadata!=null)
                {
                    constructor.parameters.First(x => x.name == "mSettings").type = "any";
                    constructor.description += Environment.NewLine + "@note Any overloads to support not documented metadata";
                    sb.AppendLine(constructor.SerializeTypescriptMethodStubs().Aggregate((a, b) => a + ";" + Environment.NewLine + b) + ";", 1);
                }
            }

            AppendProperties(sb);

            AppendMethods(sb);

            sb.AppendLine("}");

            if(metadata != null)
                sb.AppendLine(metadata, 1);

            return sb.ToString();
        }

        private string CreateMetaDataStrings()
        {
            StringBuilder sb = new StringBuilder();

            return sb.ToString();
        }

        public void CreateMetadata(string suffix = "Metadata")
        {
            if (ui5Metadata == null)
                return;

            // Create Interface for initialization
            if (ui5Metadata.properties == null)
                return;

            // update constructor props
            var param = constructor.parameters.FirstOrDefault(x => x.name == "mSettings");

            if (param != null)
            {
                param.type = fullname + suffix;
                if(Metadata == null)
                {
                    Metadata = new Ui5Interface
                    {
                        properties = ui5Metadata.properties,
                        name = param.type,
                        extends = (extends != null ? extends + suffix : null)
                    };
                    parentNamespace?.Content.Add(Metadata);
                    Metadata.parentNamespace = parentNamespace;
                }
            }
        }

        public void ConnectMetadata(IEnumerable<Ui5Interface> allInterfaces)
        {
            if (Metadata == null)
                return;

            if(Metadata!=null)
            {
                Ui5Interface baseinterface = allInterfaces.FirstOrDefault(x => x.fullname == Metadata.extends);
                if (baseinterface == null)
                    Metadata.extends = null;
            }
        }

        public Ui5Interface Metadata
        {
            get; set;
        }

        override protected string DebuggerDisplay => "Ui5Class: " + name + " (" + @namespace + "." + name + ")";
    }
}
