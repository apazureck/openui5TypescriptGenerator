using System.Linq;
using System.Text;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Enum : Ui5Complex
    {
        public override string SerializeTypescript()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendComment(description);
            if(properties.Count==0)
            {
                sb.AppendLine("type " + name + " = any;");
                return sb.ToString();
            }
            sb.AppendLine("type " + name + " = ");
            int i = 1;
            foreach(Ui5Property prop in properties)
            {
                if (prop.description != null)
                    sb.AppendComment(description);
                sb.AppendLine("\"" + prop.name + "\"" + (i++ < properties.Count ? " |" : ";"));
            }
            return sb.ToString();
        }
    }
}
