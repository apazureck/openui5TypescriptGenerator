using System.Linq;
using System.Text;

namespace UI5TypeScriptGeneratorJsonGUI
{
    internal class TypeScriptSerializer
    {
        private JavaDocMain jdm;
        private ILog log;

        public TypeScriptSerializer(JavaDocMain jdm, ILog log)
        {
            this.log = log;
            this.jdm = jdm;
        }

        public string GetClasses()
        {
            // Group by type
            var groups = jdm.symbols.GroupBy(x => x.GetType().Name, x => x).ToDictionary(x => x.Key, y => y.ToList());

            // Group all classes
            var classnamespaces = groups[nameof(Ui5Class)].Cast<Ui5Class>().GroupBy(x => x.@namespace, y => y).ToDictionary(x => x.Key, y => y.ToList());
            StringBuilder classes = new StringBuilder();
            foreach (var entry in classnamespaces)
            {
                log.Log($"Found {entry.Value.Count} class entries in {entry.Key}");
                classes.AppendLine("declare namespace " + entry.Key + " {");
                foreach(Ui5Class c in entry.Value)
                {
                    log.Log("\tCreating Outline for " + c.name);
                    classes.AppendLine(c.SerializeTypescript(), 1);
                }
                classes.AppendLine("}");
            }
            return classes.ToString();
        }
    }
}