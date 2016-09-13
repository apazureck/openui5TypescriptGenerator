﻿using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using UI5TypeScriptGeneratorJsonGUI.Properties;

namespace UI5TypeScriptGeneratorJsonGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILog, INotifyPropertyChanged, IDataErrorInfo
    {
        public MainWindow()
        {
            //var apifiles = Directory.GetFiles(@"C:\Temp\test-resources", "*api.json", SearchOption.AllDirectories).Select(x=> x.Replace(@"C:\Temp\test-resources\", "")).Aggregate((a,b) => a+Environment.NewLine + $"sb.AppendLine(\"{b}\");").Replace("\\", "/");

            InitializeComponent();

            globalValues.TranslationDictionary = CreateDictionaryFromConfigString(Settings.TypeReplacements);
            globalValues.Typedefinitions = CreateDictionaryFromConfigString(Settings.TypeDefinitions);

            restclient = new RestClient(Settings.ServiceAddress);

            if(string.IsNullOrWhiteSpace(Properties.Settings.Default.OutputFolder))
                OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), "output");

            DataContext = this;

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        public string TypeReplacements
        {
            get { return Settings.TypeReplacements; }
            set
            {
                Settings.TypeReplacements = value.Trim();
                Task.Run(() => 
                {
                    try
                    {
                        // Split text and create dictionary
                        globalValues.TranslationDictionary = CreateDictionaryFromConfigString(Settings.TypeReplacements);
                        Settings.TypeReplacements = globalValues.TranslationDictionary.OrderBy(x => x.Key).Select(x => x.Key + " : " + x.Value).Aggregate((a, b) => a.Trim() + Environment.NewLine + b).Trim();
                        Settings.Save();
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TypeReplacements)));
                    }
                    catch { }
                });
            }
        }

        public string TypeDefinitions
        {
            get { return Settings.TypeDefinitions; }
            set
            {
                Settings.TypeDefinitions = value.Trim();
                Task.Run(() =>
                {
                    try
                    {
                        // Split text and create dictionary
                        globalValues.TranslationDictionary = CreateDictionaryFromConfigString(Settings.TypeDefinitions);
                        Settings.TypeDefinitions = globalValues.Typedefinitions.OrderBy(x => x.Key).Select(x => x.Key + " : " + x.Value).Aggregate((a, b) => a.Trim() + Environment.NewLine + b).Trim();
                        Settings.Save();
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TypeDefinitions)));
                    }
                    catch { }
                });
            }
        }

        /// <summary>
        /// Creates a dictionary with a json like definition structure 'key : value' (trimming whitespaces)
        /// </summary>
        /// <param name="configstring"></param>
        /// <returns></returns>
        private System.Collections.Generic.Dictionary<string, string> CreateDictionaryFromConfigString(string configstring)
        {
            return configstring.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToDictionary(key => key.Split(':')[0].Trim(), val => val.Split(':')[1].Trim());
        }

        public string RestAddress
        {
            get { return restclient.BaseUrl.ToString(); }
            set {
                restclient = new RestClient(value);
                Settings.ServiceAddress = value;
                Settings.TypeReplacements = value.Trim();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RestAddress)));
            }
        }

        public void Log(string entry)
        {
            Dispatcher.Invoke(() =>
            {
                if (LogEntries.Count > maxlog)
                    LogEntries.RemoveAt(0);
                LogEntries.Add(entry);
                log.ScrollIntoView(log.Items[log.Items.Count - 1]);
            });
        }

        public string RestEndpoints
        {
            get { return Settings.RestEndpoints; }
            set { Settings.RestEndpoints = value; Settings.Save(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RestEndpoints))); }
        }

        public int maxlog = 10000;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> LogEntries { get; set; } = new ObservableCollection<string>();

        private string GetCorrespondingType(string value)
        {
            switch(value)
            {
                case "namespace":
                    return "UI5TypeScriptGeneratorJsonGUI.Ui5Namespace, UI5TypeScriptGeneratorJsonGUI";
                case "class":
                    return "UI5TypeScriptGeneratorJsonGUI.Ui5Class, UI5TypeScriptGeneratorJsonGUI";
                case "interface":
                    return "UI5TypeScriptGeneratorJsonGUI.Ui5Interface, UI5TypeScriptGeneratorJsonGUI";
                case "enum":
                    return "UI5TypeScriptGeneratorJsonGUI.Ui5Enum, UI5TypeScriptGeneratorJsonGUI";
                default:
                    return "UI5TypeScriptGeneratorJsonGUI.Ui5Symbol, UI5TypeScriptGeneratorJsonGUI";
            }
        }

        private string version;

        public string Version
        {
            get { return Settings.Version; }
            set { Settings.Version = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Version))); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Log("Set up.");

            globalValues.UntouchedTypes.Clear();

            Task.Run(() =>
            {
                List<JavaDocMain> allDocs = GetSources();

                var allsymbols = allDocs.Select(x => x.symbols).Aggregate((a, b) => a.Union(b, new SymbolEqualityComparer()).ToList()).GroupBy(x => x.GetType().Name, y => y).ToDictionary(key => key.Key, value => value.ToList());

                var alldistinctcomplex = allsymbols[typeof(Ui5Class).Name].Cast<Ui5Complex>().Distinct(new ComplexEqualityComparer())
                .Concat(allsymbols[typeof(Ui5Interface).Name].Cast<Ui5Complex>().Distinct(new ComplexEqualityComparer()))
                .Concat(allsymbols[typeof(Ui5Enum).Name].Cast<Ui5Complex>().Distinct(new ComplexEqualityComparer()))
                .OrderBy(x=> x.@namespace+"."+x.name).ToList();
                alldistinctcomplex = alldistinctcomplex.Where(x => x.IncludedInVersion()).ToList();

                foreach (Ui5Complex type in alldistinctcomplex.Concat(allsymbols[typeof(Ui5Namespace).Name].Cast<Ui5Complex>().Distinct(new ComplexEqualityComparer())))
                    type.SetAbsolutePathsOnMembers();

                List<Ui5Complex> results = AddToNamespaces(allsymbols[typeof(Ui5Namespace).Name].Cast<Ui5Namespace>(), alldistinctcomplex);

                Log("Found " + alldistinctcomplex.Count() + " classes.");
                Log("Creating " + results.Count + " files with declarations.");

                CreateTypeDefinitionFiles(results);

                Log("Conversion successfully executed.");

                Output = globalValues.UntouchedTypes.Select(x => x.Key + "(" + x.Value.ToString() + ")").OrderBy(x => x).Aggregate((a, b) => a + Environment.NewLine + b);

            });
        }

        private List<Ui5Complex> AddToNamespaces(IEnumerable<Ui5Namespace> namespaces, List<Ui5Complex> alldistinctcomplex)
        {
            List<Ui5Complex> hierarchicalNamespacesList = CreateNamespaceHierarchy(namespaces);

            foreach(Ui5Complex entry in alldistinctcomplex)
                AppendOnNamespace(hierarchicalNamespacesList, entry);

            return hierarchicalNamespacesList;
        }

        private List<Ui5Complex> CreateNamespaceHierarchy(IEnumerable<Ui5Namespace> namespaces)
        {
            List<Ui5Namespace> innerlist = new List<Ui5Namespace>(namespaces);
            // cast to ui5complex for code reuse.
            List<Ui5Complex> result = CreateNamespaceHierarchy(namespaces, innerlist);

            List<Ui5Namespace> newnamespaces = new List<Ui5Namespace>();

            // filter the namespaces that have a namespace which is not explicitly defined
            foreach (Ui5Namespace entry in result.ToList())
                if (!string.IsNullOrWhiteSpace(entry.@namespace))
                {
                    AppendOnNamespace(result, entry);
                    result.Remove(entry);
                }
            return result;
        }

        private void AppendOnNamespace(ICollection<Ui5Complex> content, Ui5Complex entry, string[] address = null)
        {
            // Create the namespace elements, if no array is handed (for recursion)
            if (address == null)
                address = entry.@namespace.Split('.');

            // check address length (for stability)
            if (address.Length > 0)
            {
                // try to find the parent
                Ui5Namespace parent = content.OfType<Ui5Namespace>().FirstOrDefault(x => x.name == address[0]);
                if (parent == null)
                {
                    Ui5Namespace ns = new Ui5Namespace
                    {
                        name = entry.@namespace.Split('.').TakeWhile(x => x != address[0]).Aggregate((a, b) => a + "." + b)
                    };
                    content.Add(ns);
                    AppendOnNamespace(ns.Content, entry, address.Skip(1).ToArray());
                }
                else
                    AppendOnNamespace(parent.Content, entry, address.Skip(1).ToArray());
            }
            else
                content.Add(entry);
        }

        private static List<Ui5Complex> CreateNamespaceHierarchy(IEnumerable<Ui5Namespace> namespaces, IEnumerable<Ui5Namespace> innerlist)
        {
            // get the hierarchy per group join -> will filter for the full name of the parent namespace
            // e.g. sap.m
            // should get added to sap namespace
            var groupednamespaces = namespaces.GroupJoin(innerlist,
                x =>
                {
                    return x.fullname;
                }
                , y =>
                {
                    return y.@namespace;
                }, (outer, inner) => new
                {
                    parent = outer,
                    children = new List<Ui5Namespace>(inner)
                });

            List<Ui5Complex> result = new List<Ui5Complex>();

            // add entries to existing namespaces
            foreach (var entry in groupednamespaces)
            {
                entry.parent.Content.AddRange(entry.children);
                entry.children.ForEach(x => x.parentNamespace = entry.parent);
            }

            // in the bottom level just add namespaces without a parent
            foreach (Ui5Namespace entry in namespaces)
                if (entry.parentNamespace == null)
                    result.Add(entry);
            return result;
        }

        private void CreateTypeDefinitionFiles(List<Ui5Complex> namespaces)
        {
            Log("Staring conversion");

            // to get more smaller files check if children of namespace are all namespaces
            // if this is the case use these namespaces as base for the files
            if (namespaces.Count < 5)
                foreach(Ui5Namespace namespc in namespaces.ToList())
                    if(namespc.Content.TrueForAll(x=>x is Ui5Namespace))
                    {
                        namespaces.AddRange(namespc.Content);
                        // set parent to null to force file creation.
                        namespc.Content.Cast<Ui5Namespace>().ToList().ForEach(x => x.parentNamespace = null);
                        namespaces.Remove(namespc);
                    }

            foreach(var entry in namespaces)
            {
                string filename = entry.fullname + ".d.ts";
                File.WriteAllText(Path.Combine(OutputFolder, filename), entry.SerializeTypescript());
                Log("Put content to file " + filename);
            }
        }

        private List<JavaDocMain> GetSources()
        {
            List<JavaDocMain> allDocs = new List<JavaDocMain>();
            using (StringReader sr = new StringReader(RestEndpoints))
            {
                string address;
                while ((address = sr.ReadLine()) != null)
                {
                    Log("Sending Request to URL '" + restclient.BaseUrl.ToString() + "/" + address);
                    RestRequest rr = new RestRequest(address);
                    IRestResponse response = restclient.Get<JavaDocMain>(rr);
                    Log($"Received Response with length {response.Content.Length}: {response.ResponseStatus}");
                    string input = Regex.Replace(response.Content, "\"kind\": ", "\"$type\": ");
                    input = Regex.Replace(input, @"""\$type"": ""(?<typename>\w+)""", (Match m) => "\"$type\": \"" + GetCorrespondingType(m.Groups["typename"].Value) + "\"");
                    allDocs.Add(JsonConvert.DeserializeObject<JavaDocMain>(input, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    }));
                    Log($"Added {allDocs.Last().library} to output");
                }
            }

            return allDocs;
        }

        private Settings Settings => Properties.Settings.Default;

        public string OutputFolder
        {
            get { return Settings.OutputFolder; }
            set { Settings.OutputFolder = value; Settings.Save(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OutputFolder))); }
        }


        private string output;
        private RestClient restclient;

        public string Output
        {
            get { return output; }
            set { output = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Output))); }
        }

        public string Error
        {
            get
            {
                return "Not valid input for dictionary.";
            }
        }

        public string this[string columnName]
        {
            get
            {
                return Validate(columnName);
            }
        }

        private string Validate(string propertyName)
        {
            switch(propertyName)
            {
                case nameof(TypeReplacements):
                    {
                        try
                        {
                            CreateDictionaryFromConfigString(TypeReplacements);
                            return "";
                        }
                        catch
                        {
                            return "Not valid input for dictionary.";
                        }
                    }
                default:
                    return "";
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Process.Start(OutputFolder);
        }
    }
}
