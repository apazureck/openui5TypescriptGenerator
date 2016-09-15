using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Method : Ui5Member
    {
        public Ui5Method() { }
        public Ui5Method(Ui5Method x, Ui5Complex newowner)
        {
            deprecated = x.deprecated;
            description = x.description;
            name = x.name;
            owner = newowner;
            parameters = x.parameters.Select(y => y.Clone() as Ui5Parameter).ToList();
            returnValue = x.returnValue != null ? new Ui5Value(x.returnValue) : null;
            since = x.since;
            @static = x.@static;
            visibility = x.visibility;
        }

        public Ui5Value returnValue { get; set; }
        public List<Ui5Parameter> parameters { get; set; } = new List<Ui5Parameter>();
        public string[] SerializeTypescriptMethodStubs(bool @explicit = false, bool createstatic = false)
        {
            //return

            string[] stubs;

            var optionalnotoptionalgroups = parameters.GroupBy(x => x.optional, y => y).ToDictionary(x=>x.Key, y=>y.ToList());
            if (optionalnotoptionalgroups.ContainsKey(false))
            {
                if (optionalnotoptionalgroups.ContainsKey(true))
                    stubs = new string[] { CreateStub(optionalnotoptionalgroups[false].Concat(optionalnotoptionalgroups[true]), @explicit, createstatic) };
                else
                    stubs = new string[] { CreateStub(optionalnotoptionalgroups[false], @explicit, createstatic) };
            }
            else if (optionalnotoptionalgroups.ContainsKey(true))
                stubs = new string[] { CreateStub(optionalnotoptionalgroups[true], @explicit, createstatic) };
            else
                stubs = new string[] { CreateStub(parameters, @explicit, createstatic) };

            return stubs.Where(x => x != null).ToArray();
        }

        public string[] GetMethodDefinitions(bool @explicit = false, bool createstatic = false)
        {
            string lname = name;
            if (globalValues.SkipMethods.ContainsKey(name))
                lname = globalValues.SkipMethods[name];
            if (string.IsNullOrWhiteSpace(lname))
                return null;

            string[] stubs;

            var optionalnotoptionalgroups = parameters.GroupBy(x => x.optional, y => y).ToDictionary(x => x.Key, y => y.ToList());
            if (optionalnotoptionalgroups.ContainsKey(false))
            {
                if (optionalnotoptionalgroups.ContainsKey(true))
                    stubs = new string[] { CreateDefinition(optionalnotoptionalgroups[false].Concat(optionalnotoptionalgroups[true]), lname, @explicit, createstatic) };
                else
                    stubs = new string[] { CreateDefinition(optionalnotoptionalgroups[false], lname, @explicit, createstatic) };
            }
            else if (optionalnotoptionalgroups.ContainsKey(true))
                stubs = new string[] { CreateDefinition(optionalnotoptionalgroups[true], lname, @explicit, createstatic) };
            else
                stubs = new string[] { CreateDefinition(parameters, lname, @explicit, createstatic) };

            return stubs.Where(x => x != null).ToArray();
        }

        /// <summary>
        /// Returns null if skipped
        /// </summary>
        /// <param name="pars"></param>
        /// <param name="explicit"></param>
        /// <param name="createstatic"></param>
        /// <returns></returns>
        public virtual string CreateStub(IEnumerable<Ui5Parameter> pars, bool @explicit, bool createstatic)
        {
            string lname = name;
            if (globalValues.SkipMethods.ContainsKey(name))
                lname = globalValues.SkipMethods[name];
            if (string.IsNullOrWhiteSpace(lname))
                return null;
            StringBuilder sb = new StringBuilder();
            sb.AppendComment(CreateDescription(pars));
            sb.Append(CreateDefinition(pars, lname, @explicit, createstatic));
            return sb.ToString();
        }

        public string CreateDefinition(IEnumerable<Ui5Parameter> pars, string name, bool @explicit, bool createstatic, bool alwayspublic = true)
        {
            return $"{(@explicit ? (@static && createstatic ? "static function " : "function ") : (createstatic && @static ? "static " : "") + (alwayspublic ? "" : visibility.GetDescription()))}{name}(" + pars.Where(x=>!string.IsNullOrWhiteSpace(x.name)).Aggregate("", (a, b) => a + ", " + b.name + (b.optional ? "?" : "") + (string.IsNullOrWhiteSpace(b.type) ? "" : ": " + b.GetRelativeTypeDef(owner))).TrimStart(", ".ToCharArray()) + ")" + (returnValue != null && returnValue.type != null ? ": " + returnValue.GetRelativeTypeDef(owner) : "");
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
