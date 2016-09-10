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
                    MethodDescription md = CreateMethodDescription(sectionItem);
                    mdlist.Add(md);
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
            md.Description = description.InnerText.Trim();
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
    }

    public class InterfaceDefinition
    {
        public string Name { get; internal set; }
        public List<Property> Properties { get; set; } = new List<Property>();
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ConstructorDescription : MethodDescription
    {
        private MethodDescription methodDescription;

        public ConstructorDescription(MethodDescription methodDescription)
        {
            Attributes = methodDescription.Attributes;
            Description = methodDescription.Description;
            Name = methodDescription.Name;
            Parameters = methodDescription.Parameters;
            ReturnType = methodDescription.ReturnType;
        }

        private string DebuggerDisplay => $"Method: " + ToString();
        public override string ToString()
        {
            return $"{ ReturnType} {Name}(" + Parameters.Aggregate("", (a, b) => a + ", " + b.Name + ":" + b.Type).TrimStart(", ".ToCharArray()) + ")";
        }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MethodDescription
    {
        public string Name { get; set; } = "";
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public string ReturnType { get; set; } = "";
        public string Description { get; set; } = "";

        public List<string> Attributes { get; set; } = new List<string>();
        private string DebuggerDisplay => $"Method: " + ToString();
        public override string ToString()
        {
            return $"{ ReturnType} {Name}(" + Parameters.Aggregate("", (a, b) => a + ", " + b.Name + ":" + b.Type).TrimStart(", ".ToCharArray()) + ")";
        }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Parameter
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        private string DebuggerDisplay => $"Parameter '{Name}: {Type}'";
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Property : Parameter
    {
        public string DefaultValue { get; set; }

        private string DebuggerDisplay => $"Property '{Name}: {Type}'";
    }
}
