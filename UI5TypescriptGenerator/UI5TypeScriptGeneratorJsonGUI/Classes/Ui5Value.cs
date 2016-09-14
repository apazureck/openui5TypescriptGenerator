using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Value
    {
        private string _type;

        public string type
        {
            get { return _type; }
            set { _type = globalValues.ConvertToValidTypeIfKnown(value); }
        }

        public string description { get; set; }

        public string GetRelativeTypeDef(Ui5Complex owner)
        {
            return GetRelativeTypeDef(owner, type);
        }

        public static string GetRelativeTypeDef(Ui5Complex owner, string type)
        {
            if (owner == null)
                return type;
            string[] values = type.Split('|');
            List<string> retvalues = new List<string>(type.Length);
            foreach (string value in values)
                retvalues.Add(_CheckRelativeTypeDef(owner, value));
            // Split to remove empty entries (if type should not be used in typedef
            try
            {
                return retvalues.Distinct().Aggregate((a, b) => a + "|" + b).Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Aggregate((a, b) => a + "|" + b);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string _CheckRelativeTypeDef(Ui5Complex owner, string type)
        {
            if (!string.IsNullOrWhiteSpace(owner.@namespace))
            {
                // Check, if the path starts with the namespace the owning class, namespace, enum, etc. is in.
                if (type.StartsWith(owner.@namespace))
                    return type.Replace(owner.@namespace + ".", "");
                else
                {
                    string relpath = CheckAndCreateImport(owner, type);
                    if (relpath!=null)
                        return relpath;
                    else
                        return type;
                }
            }
            return type;
        }

        private static string CheckAndCreateImport(Ui5Complex owner, string type)
        {
            // Check if type is a base type and return if so
            if (globalValues.IsBaseType(type))
                return null;
            // Convert the type, if the user wanted to replace it
            if (globalValues.TranslationDictionary.ContainsKey(type))
                type = globalValues.ConvertToValidTypeIfKnown(type);
            // check for any equal parts
            string equals = getequalpart(type, owner.@namespace);
            if (equals != null)
                return type;

            // Now we know definetly it is an absolute path, which may collide with a relative path
            // so lets check this:

            string startofpath = owner.@namespace.Split('.')[0];

            // check the parent namespaces for any relative starts
            List<Ui5Namespace> namespacesinpath = new List<Ui5Namespace>();
            if (owner is Ui5Namespace)
                namespacesinpath.Add(owner as Ui5Namespace);
            Ui5Namespace curns = owner.parentNamespace;
                
            while(curns!=null)
            {
                namespacesinpath.Add(curns);
                curns = curns.parentNamespace;
            }

            // Check if any namespace on the list has the name of the start of the type
            // if nothing is found an import is not needed
            if (namespacesinpath.FirstOrDefault(x => x.name == startofpath) == null)
                return null;
                
            // we found a relative namespace which has the same name as the absolute start of the namespace

            Ui5Namespace ns = FindBaseNamespace(owner);

            ns.Imports[startofpath] = startofpath + "import";
            return Regex.Replace(type, "^" + startofpath + @"\.?", ns.Imports[startofpath]);
        }

        private static string AddImports(string equals, Ui5Namespace ns, string replacement)
        {
            foreach(string key in ns.Imports.Keys)
            {
                string commonbasenamespace = getequalpart(equals, key);
                if (commonbasenamespace != null)
                    return ns.Imports[key] + equals.Replace(commonbasenamespace, "");
            }
            ns.Imports[equals] = replacement;
            return replacement;
        }

        private static Ui5Namespace FindBaseNamespace(Ui5Complex owner)
        {
            if (owner.parentNamespace != null)
                return FindBaseNamespace(owner.parentNamespace);
            else
                return owner as Ui5Namespace;
        }

        private static string getequalpart(string type, string owner)
        {
            string[] atype = type.Split('.');
            string[] aowner = owner.Split('.');

            var minimumLength = Math.Min(atype.Length, aowner.Length);

            int matchindex = 0;
            while (matchindex < minimumLength && atype[matchindex] == aowner[matchindex])
                matchindex++;
            
            return matchindex > 0 ? atype.Take(matchindex).Aggregate((a, b) => a + "." + b) : null;
        }
    }
}