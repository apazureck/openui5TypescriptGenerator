using System.Linq;
using System.Text;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Enum : Ui5Complex
    {

        public Ui5Enum() { }
        public Ui5Enum(Ui5Complex source)
        {
            basename = source.basename;
            deprecated = source.deprecated;
            description = source.description;
            extends = source.extends;
            methods = source.methods;
            module = source.module;
            name = source.fullname;
            parentNamespace = source.parentNamespace;
            properties = source.properties;
            resource = source.resource;
            since = source.since;
            @static = source.@static;
            visibility = source.visibility;
        }

        public override string SerializeTypescript()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"class {name} {(extends != null ? "extends " + Ui5Value.GetRelativeTypeDef(this, extends) : "")}{"{"}");

            AppendProperties(sb, true, true);

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
