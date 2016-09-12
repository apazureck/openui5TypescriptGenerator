using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Method : Ui5Member
    {
        public Ui5Value returnValue { get; set; }
        public List<Ui5Parameter> parameters { get; set; } = new List<Ui5Parameter>();

        public string[] SerializeTypescriptMethodStubs(bool @explicit = false)
        {               
            //return
            var optionalnotoptionalgroups = parameters.GroupBy(x => x.optional, y => y).ToDictionary(x=>x.Key, y=>y.ToList());
            if (optionalnotoptionalgroups.ContainsKey(false))
            {
                if (optionalnotoptionalgroups.ContainsKey(true))
                    return new string[] { CreateStub(optionalnotoptionalgroups[false].Concat(optionalnotoptionalgroups[true]), @explicit) };
                else
                    return new string[] { CreateStub(optionalnotoptionalgroups[false], @explicit) };
            }
            else if (optionalnotoptionalgroups.ContainsKey(true))
                return new string[] { CreateStub(optionalnotoptionalgroups[true], @explicit) };
            else
                return new string[] { CreateStub(parameters, @explicit) };
            //int lastmandatoryindex = parameters.IndexOf(parameters.LastOrDefault(x => !x.optional));
            //// Last optional parameter is first parameter -> ok
            //if (lastmandatoryindex <= 0)
            //    return new string[] { CreateStub(parameters) };

            //// Otherwise you have to check if there are optional parameters before
            //int firstoptionalindex = parameters.IndexOf(parameters.FirstOrDefault(x => x.optional));

            //// Only non optional indizes are before the first optional
            //if (firstoptionalindex > -1 && firstoptionalindex < lastmandatoryindex)
            //{
            //    List<string> retlist = new List<string>();
            //    // Create overload without leading optional parameters
            //    List<Ui5Parameter> plit = parameters.Select(p => p.Clone() as Ui5Parameter).ToList();
            //    retlist.Add(CreateStub(plit.SkipWhile(x => x.optional)));
            //    // Create Overload with optional leading parameters as not optional parameters
            //    plit.TakeWhile(x => x.optional).ToList().ForEach(x => x.optional = false);
            //    retlist.Add(CreateStub(plit));
            //    return retlist.ToArray();
            //}

            //return new string[] { CreateStub(parameters) };
        }

        public virtual string CreateStub(IEnumerable<Ui5Parameter> pars, bool @explicit)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendComment(CreateDescription(pars));
            sb.Append(CreateDefinition(pars, @explicit));
            return sb.ToString();
        }

        public string CreateDefinition(IEnumerable<Ui5Parameter> pars, bool @explicit)
        {
            return $"{(@explicit ? "function" : visibility.GetDescription())} {name}(" + pars.Aggregate("", (a, b) => a + ", " + b.name + (b.optional ? "?" : "") + (string.IsNullOrWhiteSpace(b.type) ? "" : ":" + b.type)).TrimStart(", ".ToCharArray()) + ")" + (returnValue != null && returnValue.type != null ? ": " + returnValue.type : "");
        }

        public string CreateDescription(IEnumerable<Ui5Parameter> pars)
        {
            StringBuilder csb = new StringBuilder();
            csb.AppendLine(description);
            foreach (Ui5Parameter par in pars)
                csb.AppendLine("@param " + par.name + " " + par.description);
            if (deprecated != null)
                csb.AppendLine("@deprecated " + (since!=null ? "since version " + since + ":" : "") + deprecated.text);
            if (returnValue != null)
                csb.AppendLine("@return " + returnValue.description);
            return csb.ToString();
        }
    }
}
