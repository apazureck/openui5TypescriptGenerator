using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public static class globalValues
    {
        public static Dictionary<string, string> TranslationDictionary { get; set; }
        public static Dictionary<string, int> UntouchedTypes { get; set; } = new Dictionary<string, int>();

        public static Dictionary<string, string> Typedefinitions { get; set; }
        public static string ConvertToValidTypeIfKnown(string inval)
        {
            string[] values = inval.Split('|');
            List<string> retvalues = new List<string>(values.Length);
            foreach (string value in values)
                if (IsBaseType(value))
                    retvalues.Add(value);
                else if (TranslationDictionary.ContainsKey(value))
                    retvalues.Add(TranslationDictionary[value]);
                else if (value.StartsWith("array("))
                    retvalues.Add(ConvertToValidTypeIfKnown(Regex.Match(value, @"array\((?<type>.*)\)").Groups["type"].Value));
                else
                {
                    if (UntouchedTypes.ContainsKey(value))
                        UntouchedTypes[value]++;
                    else
                        UntouchedTypes[value] = 1;
                    retvalues.Add(value);
                }
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

        internal static string InsertTypeForSimpleTypedef(string type)
        {
            if (Typedefinitions.ContainsKey(type))
                return Typedefinitions[type];
            else
                return "any";
        }

        public static Dictionary<string, string> Defaultvalues { set; get; } = new Dictionary<string, string>
        {
            { "any" , "" },
            { "number" , "" },
            { "enum" , "" },
            { "string" , "" },
            { "boolean" , "" },
            { "void", "" }
        };

        public static bool IsBaseType(string type)
        {
            return Defaultvalues.ContainsKey(type.Replace("[]", ""));
        }
    }
}
