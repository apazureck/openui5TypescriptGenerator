using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UI5TypescriptGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.Load(Path.Combine(Directory.GetCurrentDirectory(), "gen", "Button.html"));
            StringBuilder output = new StringBuilder();

            ClassDefinition tst = new ClassDefinition(doc);

            StringBuilder nscontent = new StringBuilder();
            nscontent.AppendLine("namespace " + tst.Namespace + " {");
            tst.CreateDefinitionString(nscontent, 1);
            nscontent.AppendLine("}");
            Console.Write(nscontent.ToString());
            Console.Read();

            //var node = doc.DocumentNode.Descendants("h1").First(x => x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("classTitle"));
            //classdef += node.Descendants("span").First(x => x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("package")).InnerText.TrimEnd(".".ToCharArray());
        }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ClassDefinition
    {
        /// <summary>
        /// HtmlDocument has to be loaded first!
        /// </summary>
        /// <param name="doc"></param>
        public ClassDefinition(HtmlDocument doc)
        {
            Source = doc;
            CreateTypeBasicInfo(doc, this); // Name, namespace and base class

            var detailsections = doc.DocumentNode.Descendants("div").Where(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("section") && x.Attributes["class"].Value.Contains("details") : false);

            Constructors = GetConstructors(detailsections, "Constructor Detail");
            Methods = GetMethodList(detailsections, "Method Detail");
            // Filter static methods
            Methods = Methods.Where(x => !x.Attributes.Contains("static")).ToList();
            Events = GetMethodList(detailsections, "Event Detail");
        }

        private static bool commentSection;
        private static bool definition;
        private static bool firstdefline;
        private static bool ismethod;
        public List<ConstructorDescription> Constructors { get; set; } = new List<ConstructorDescription>();
        public List<MethodDescription> Methods { get; set; }
        public List<MethodDescription> Events { get; set; }
        public List<InterfaceDefinition> UsedInterfaces { get; set; } = new List<InterfaceDefinition>();
        public string Description { get; set; }
        public string Name { get; set; }
        public string Baseclass { get; set; }
        public string Namespace { get; set; }
        private string DebuggerDisplay => "{Name = '" + Name + "' Namespace = '" + Namespace + "'}";
        public HtmlDocument Source { get; private set; }

        private static List<MethodDescription> GetMethodList(IEnumerable<HtmlNode> detailsections, string searchitem)
        {
            List<MethodDescription> mdlist = new List<MethodDescription>();

            // div with class section details has a div with sectionTitle class and the title is Constructor Detail
            var detailsection = detailsections.FirstOrDefault(x => x.Descendants("div")
            .FirstOrDefault(subnode => (subnode.Attributes["class"] != null ? subnode.Attributes["class"].Value.Contains("sectionTitle") : false)
            && subnode.InnerText.Contains(searchitem)) != null);

            if (detailsection != null)
            {
                var sectionItems = detailsection.Descendants("div").Where(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("sectionItem") : false);
                foreach (var sectionItem in sectionItems)
                {
                    try
                    {
                        MethodDescription md = CreateMethodDescription(sectionItem);
                        mdlist.Add(md);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return mdlist;
        }

        private List<ConstructorDescription> GetConstructors(IEnumerable<HtmlNode> detailsections, string searchitem)
        {
            List<ConstructorDescription> ctorlist = new List<ConstructorDescription>();

            // div with class section details has a div with sectionTitle class and the title is Constructor Detail
            var detailsection = detailsections.FirstOrDefault(x => x.Descendants("div")
            .FirstOrDefault(subnode => (subnode.Attributes["class"] != null ? subnode.Attributes["class"].Value.Contains("sectionTitle") : false)
            && subnode.InnerText.Contains(searchitem)) != null);

            if (detailsection != null)
            {
                var sectionItems = detailsection.Descendants("div").First(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("sectionItems") : false)
                    .Descendants("div").Where(x=> x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("sectionItem") : false);
                foreach (var sectionItem in sectionItems)
                {
                    ConstructorDescription ctor = new ConstructorDescription(CreateMethodDescription(sectionItem));
                    ctor.Name = "constructor";
                    try
                    {
                        var parameterlist = sectionItem.Descendants("ul").First().Descendants("li").First(x => x.Descendants("#text").FirstOrDefault(y => y.InnerText.Contains("Properties")) != null).Descendants("ul").FirstOrDefault().Descendants("li");

                        InterfaceDefinition id = new InterfaceDefinition();
                        id.Name = Name + "Metadata";
                        foreach(string paramcontent in parameterlist.Select(x => x.InnerText))
                        {
                            Property p = new Property();
                            Match m = Regex.Match(paramcontent, @"^(?<name>\w+)\s*:\s*(?<type>[\w\.]+)\s*(?:\(default:\s*(?<default>.*)\))*");
                            p.Name = m.Groups["name"].Value;
                            p.Type = m.Groups["type"].Value;
                            p.DefaultValue = m.Groups["default"].Value;
                            id.Properties.Add(p);
                        }
                        UsedInterfaces.Add(id);
                    }
                    catch (Exception)
                    {
                    }
                    ctorlist.Add(ctor);
                }
            }

            return ctorlist;
        }

        private static MethodDescription CreateMethodDescription(HtmlNode sectionItem)
        {
            MethodDescription md = new MethodDescription();
            try
            {
                md.Name = sectionItem.Descendants("div").First(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("itemTitle") : false).InnerText.Split("(".ToCharArray())[0].Trim();
            }
            catch (InvalidOperationException)
            {
                // maybe constructor handling here.
            }
            md.Attributes = sectionItem.Attributes["class"].Value.Split().ToList();
            var description = sectionItem.Descendants("div").FirstOrDefault(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("full-description") : false);
            md.Description = description.InnerHtml.Trim();
            var paramtables = sectionItem.Descendants("table").Where(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("methodItem") : false)
                .Select(x => new { category = x.ParentNode.ChildNodes[x.ParentNode.ChildNodes.IndexOf(x) - 2].InnerText.TrimEnd(":".ToCharArray()), table = x });

            foreach (var paramtable in paramtables)
            {
                if (paramtable.category == "Returns" || paramtable.category == "Parameters")
                    CreateParameters(md, paramtable.table);
            }

            return md;
        }

        private static void CreateParameters(MethodDescription md, HtmlNode paramtable)
        {
            foreach (var row in paramtable.Descendants("tr"))
            {
                Parameter p = new Parameter();
                // get type of method parameter without brackets
                p.Type = row.Descendants("td").First(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("methodItemType") : false).InnerText.TrimStart("{".ToCharArray()).TrimEnd("}".ToCharArray());
                try
                {
                    p.Name = row.Descendants("td").First(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("methodItemName") : false).InnerText.Trim();
                }
                catch (InvalidOperationException)
                {
                    // no name means it is a return value
                    md.ReturnType = p.Type;
                    continue;
                }
                p.Description = row.Descendants("td").First(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("methodItemDesc") : false).InnerText.Trim();
                md.Parameters.Add(p);
            }
        }

        private static void CreateTypeBasicInfo(HtmlDocument doc, ClassDefinition tst)
        {
            // Create Namespace and Class name
            var classdefnode = doc.DocumentNode.Descendants("title").First();
            string[] classpath = classdefnode.InnerText.Split("-".ToCharArray()).Last().Split(".".ToCharArray());
            tst.Namespace = classpath.Take(classpath.Length - 1).Aggregate((a, b) => a + "." + b).Trim();
            tst.Name = classpath.Last();

            // Get Base class (if existing)
            var extendnode = doc.DocumentNode.Descendants("div").FirstOrDefault(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("classRelation extends") : false);
            if (extendnode != null)
                tst.Baseclass = extendnode.Descendants("a").First(x => x.Attributes["title"] != null).Attributes["title"].Value;

            // Get Description
            var descriptionnode = doc.DocumentNode.Descendants("div").FirstOrDefault(x => x.Attributes["class"] != null ? x.Attributes["class"].Value.Contains("full-description") : false);
            if (descriptionnode != null)
                tst.Description = descriptionnode.InnerText;
        }
        
        #region Typescript class generation
        public void CreateDefinitionString(StringBuilder namespacecontent, int tablevel)
        {
            StringBuilder references = new StringBuilder();
            StringBuilder classcontent = new StringBuilder();

            CreateClassContent(classcontent);

            //CreateInterfaceDescriptions(classcontent, tablevel);
            namespacecontent.AppendLine("", tablevel);
            namespacecontent.AppendLine("class " + Name + (string.IsNullOrWhiteSpace(Baseclass) ? "" : " extends " + Baseclass) + " {", tablevel);
            namespacecontent.AppendLine(classcontent.ToString(), tablevel+1);
            namespacecontent.AppendLine("}", tablevel);
            CreateInterfaceDescriptions(namespacecontent, tablevel);

        }

        private void CreateInterfaceDescriptions(StringBuilder classcontent, int tablevel)
        {
            foreach(InterfaceDefinition idesc in UsedInterfaces)
            {
                classcontent.AppendLine();
                StringBuilder interfacecontent = new StringBuilder();
                interfacecontent.AppendLine("interface " + idesc.Name + "{");
                foreach (Property prop in idesc.Properties)
                {
                    if (prop.Description != null)
                        interfacecontent.AppendComment(prop.Description, 1);
                    interfacecontent.AppendLine($"{prop.Name}?: {prop.Type}", 1);
                }
                interfacecontent.AppendLine("}");
                classcontent.AppendLine(interfacecontent.ToString(), tablevel);
            }
        }

        private void CreateClassContent(StringBuilder classcontent)
        {
            // Constructor
            foreach (ConstructorDescription cd in Constructors)
            {
                classcontent.AppendLine();
                AppendMethod(classcontent, cd);
            }
            foreach (MethodDescription md in Methods)
            {
                classcontent.AppendLine();
                AppendMethod(classcontent, md);
            }
        }

        private static void AppendMethod(StringBuilder classcontent, MethodDescription cd)
        {
            foreach(string s in cd.CreateMethodStubs())
            {
                classcontent.AppendLine();
                classcontent.AppendLine(s + ";");
            }
        } 
        #endregion
    }

    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Append string as comment (fill in string without any comment prefixes.
        /// </summary>
        /// Result will be
        /// <example>
        /// <code>
        ///  /** <br/>
        ///  * Your Comment lines <br/>
        ///  */
        /// </code>
        /// </example>
        /// <param name="sb"></param>
        /// <param name="commenttext"></param>
        public static void AppendComment(this StringBuilder sb, string commenttext, int tablevel = 0)
        {
            sb.AppendLine("/**", tablevel);
            using (StringReader sr = new StringReader(commenttext))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    sb.AppendLine(" * " + line, tablevel);
            }
            sb.AppendLine(" */", tablevel);
        }

        /// <summary>
        /// Appens a new line with beginning tabs.
        /// </summary>
        /// <param name="sb">extension</param>
        /// <param name="value">string to append</param>
        /// <param name="tabs">tabs to put in front</param>
        public static void AppendLine(this StringBuilder sb, string value, int tabs)
        {
            using (StringReader sr = new StringReader(value))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    sb.AppendLine(new String('\t', tabs) + line);
            }
        }
    }

    public class InterfaceDefinition
    {
        public string Name { get; set; }
        public List<Property> Properties { get; set; } = new List<Property>();
        public string BaseInterface { get; set; }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ConstructorDescription : MethodDescription
    {
        public ConstructorDescription(MethodDescription methodDescription)
        {
            Attributes = methodDescription.Attributes;
            Description = methodDescription.Description;
            Name = methodDescription.Name;
            Parameters = methodDescription.Parameters;
            ReturnType = methodDescription.ReturnType;
        }

        private string DebuggerDisplay => $"Constructor: " + ToString();
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MethodDescription
    {
        private string name;

        public string Name
        {
            get { return name; }
            set
            {
                if(value!=null && Regex.IsMatch(value, @"\W"))
                    throw new ArgumentException("Not a valid Type name");
                name = value;
            }
        }

        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        private string returnType = "";

        public string ReturnType
        {
            get { return returnType; }
            set { returnType = GlobalTypeManager.ConvertToValidTypeIfKnown(value); }
        }

        public string Description { get; set; } = "";

        public List<string> Attributes { get; set; } = new List<string>();

        private string DebuggerDisplay => $"Method: " + ToString();

        public virtual string[] CreateMethodStubs()
        {
            int lastnotoptionalindex = Parameters.IndexOf(Parameters.LastOrDefault(x => !x.IsOptional));
            // Last optional parameter is first parameter -> ok
            if (lastnotoptionalindex <= 0)
                return new string[] { CreateDefaultStub() };

            // Otherwise you have to check if there are optional parameters before
            int firstoptionalindex = Parameters.IndexOf(Parameters.FirstOrDefault(x => x.IsOptional));

            // Only non optional indizes are before the first optional
            if (firstoptionalindex > -1 && firstoptionalindex < lastnotoptionalindex)
            {
                List<string> retlist = new List<string>();
                // Create overload without leading optional parameters
                List<Parameter> plit = Parameters.Select(p => p.Clone() as Parameter).ToList();
                retlist.Add(CreateStubWithParameters(plit.SkipWhile(x=> x.IsOptional)));
                // Create Overload with optional leading parameters as not optional parameters
                plit.TakeWhile(x => x.IsOptional).ToList().ForEach(x => x.Name = x.Name.Replace("?", ""));
                retlist.Add(CreateStubWithParameters(plit));
                return retlist.ToArray();
            }

            // Have to create overload.

            return new string[] { CreateDefaultStub() };
        }

        private string CreateStubWithParameters(IEnumerable<Parameter> parameters)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendComment(CreateTypescriptMethodDescription(parameters));
            sb.Append($"{Modifier} {Name}(" + parameters.Aggregate("", (a, b) => a + ", " + b.Name + ":" + b.Type).TrimStart(", ".ToCharArray()) + ")" + (string.IsNullOrWhiteSpace(ReturnType) ? "" : " : " + ReturnType));
            return sb.ToString();
        }

        private string CreateDefaultStub()
        {
            return CreateStubWithParameters(Parameters);
        }

        private string CreateTypescriptMethodDescription(IEnumerable<Parameter> parameters)
        {
            // Append Description:
            StringBuilder description = new StringBuilder();
            description.AppendLine(Description);
            foreach (Parameter p in parameters)
                description.AppendLine("@param " + p.Name + " " + p.Description);

            string s = description.ToString();
            return s;
        }

        public string Modifier
        {
            get
            {
                if (Attributes.Contains("protected"))
                    return "protected";
                return "";
            }
        }

        public override string ToString() => $"{Modifier} {Name}(" + Parameters.Aggregate("", (a, b) => a + ", " + b.Name + ":" + b.Type).TrimStart(", ".ToCharArray()) + ")" + (string.IsNullOrWhiteSpace(ReturnType) ? "" : " : " + ReturnType);
    }

    /// <summary>
    /// Manages all that has to do with types
    /// </summary>
    public static class GlobalTypeManager
    {
        private static Dictionary<string, string> TranslationDictionary = Properties.Settings.Default.TypeTranslations.Cast<string>().ToDictionary(key => key.Split()[0], val => val.Split()[1]);
        public static string ConvertToValidTypeIfKnown(string inval)
        {
            string[] values = inval.Split('|');
            List<string> retvalues = new List<string>(values.Length);
            foreach (string value in values)
                if (TranslationDictionary.ContainsKey(value))
                    retvalues.Add(TranslationDictionary[value]);
                else
                    retvalues.Add(value);
            return retvalues.Aggregate((a, b) => a + "|" + b);
        }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Parameter : ICloneable
    {
        private string type;

        public string Type
        {
            get { return type; }
            set
            {
                type = GlobalTypeManager.ConvertToValidTypeIfKnown(value);
            }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        private string DebuggerDisplay => $"Parameter '{Name}: {Type}'";
        public bool IsOptional => Name?.Contains('?') == true;

        public object Clone()
        {
            return new Parameter()
            {
                Description = Description,
                Name = Name,
                Type = Type
            };
        }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Property : Parameter
    {
        public string DefaultValue { get; set; }

        private string DebuggerDisplay => $"Property '{Name}: {Type}'";
    }
}
