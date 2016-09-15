using Newtonsoft.Json;
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
            globalValues.SkipMethods = CreateDictionaryFromConfigString(Settings.SkipMethods);

            restclient = new RestClient(Settings.ServiceAddress);

            if(string.IsNullOrWhiteSpace(Properties.Settings.Default.OutputFolder))
                OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), "output");

            DataContext = this;

        }

        private string postProcessing;

        public string PostProcessing
        {
            get { return Settings.PostProcessing; }
            set { Settings.PostProcessing = value; Settings.Save(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PostProcessing))); }
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

        public string SkipMethods
        {
            get { return Settings.SkipMethods; }
            set
            {
                Settings.SkipMethods = value.Trim();
                Task.Run(() =>
                {
                    try
                    {
                        // Split text and create dictionary
                        globalValues.SkipMethods = CreateDictionaryFromConfigString(Settings.SkipMethods);
                        Settings.SkipMethods = globalValues.SkipMethods.OrderBy(x => x.Key).Select(x => x.Key + " : " + x.Value).Aggregate((a, b) => a.Trim() + Environment.NewLine + b).Trim();
                        Settings.Save();
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SkipMethods)));
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
                        globalValues.Typedefinitions = CreateDictionaryFromConfigString(Settings.TypeDefinitions);
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

                var allnamespaces = allsymbols[typeof(Ui5Namespace).Name].Cast<Ui5Namespace>().Distinct(new ComplexEqualityComparer()).Where(x => x.IncludedInVersion()).ToList();
                var allrealnamespaces = allnamespaces.Where(x =>
                    {
                        return !(globalValues.Typedefinitions.ContainsKey(x.fullname) && globalValues.Typedefinitions[x.fullname] == "Ui5Enum");
                    }).ToList();

                var enums = allnamespaces.Where(x => globalValues.Typedefinitions.ContainsKey(x.fullname) && globalValues.Typedefinitions[x.fullname] == "Ui5Enum").Select(x=> new Ui5Enum(x)).ToList();

                var alldistinctcomplex = allsymbols[typeof(Ui5Class).Name].Cast<Ui5Complex>().Distinct(new ComplexEqualityComparer()).ToList();
                alldistinctcomplex = alldistinctcomplex.Concat(allsymbols[typeof(Ui5Interface).Name].Cast<Ui5Complex>().Distinct(new ComplexEqualityComparer())).ToList();
                alldistinctcomplex = alldistinctcomplex.Concat(allsymbols[typeof(Ui5Enum).Name].Cast<Ui5Complex>().Distinct(new ComplexEqualityComparer())).ToList();
                alldistinctcomplex = alldistinctcomplex.Concat(enums.Cast<Ui5Complex>()).ToList();
                alldistinctcomplex = alldistinctcomplex.OrderBy(x => x.fullname).ToList();
                var alldistinctcomplexlist = alldistinctcomplex.Where(x => x.IncludedInVersion()).ToList();

                foreach (Ui5Complex type in alldistinctcomplexlist.Concat(allrealnamespaces))
                    type.SetAbsolutePathsOnMembers();

                List<Ui5Complex> results = AddToNamespaces(allrealnamespaces.Cast<Ui5Namespace>(), alldistinctcomplexlist);

                foreach (Ui5Namespace result in results.OfType<Ui5Namespace>())
                    SetParents(result);

                CreateMetadata(results);

                AppendCustomTypeDefinitions(results);

                foreach (Ui5Namespace result in results.OfType<Ui5Namespace>())
                    SetParents(result);

                CreateOverloads(results);

                //MergeDuplicateNamespaces(results);

                Log("Found " + alldistinctcomplexlist.Count() + " classes.");
                Log("Creating " + results.Count + " files with declarations.");

                SortHierarchyAlphabetically(results);

                string[] createdfiles = CreateTypeDefinitionFiles(results);

                Log("Doing postprocessing.");

                PostProcess(createdfiles);

                Log("Conversion successfully executed.");

                Output = globalValues.UntouchedTypes.Select(x => x.Key + "(" + x.Value.ToString() + ")").OrderBy(x => x).Aggregate((a, b) => a + Environment.NewLine + b);
            });
        }

        private void SortHierarchyAlphabetically(IEnumerable<Ui5Complex> results)
        {
            foreach (Ui5Complex item in results)
                SortHierarchyAlphabetically(item.Content);

            results.OrderBy(x => x.name);
        }

        private void PostProcess(string[] createdfiles)
        {
            string[] requestedfiles = Regex.Matches(PostProcessing, "# file: (?<filename>.*)").Cast<Match>().Select(x => x.Groups["filename"].Value.Trim()).ToArray();

            foreach(string file in requestedfiles)
            {
                Dictionary<string, string> commands = GetCommands(PostProcessing, file);
                foreach(var command in commands)
                {
                    switch(command.Key)
                    {
                        case "ADD":
                            string[] args = command.Value.Split(',');
                            string newcontent;
                            switch (args[0].Trim())
                            {
                                case ("@start"):
                                    newcontent = args[1].Trim() + Environment.NewLine + File.ReadAllText(Path.Combine(OutputFolder, file));
                                    break;
                                default:
                                    newcontent = File.ReadAllText(Path.Combine(OutputFolder, file)) + Environment.NewLine + args[1].Trim();
                                    break;
                            }
                            File.WriteAllText(Path.Combine(OutputFolder, file), newcontent);
                            break;
                        case "REPLACE":
                            args = command.Value.Split(',');
                            string replacement = Regex.Replace(File.ReadAllText(Path.Combine(OutputFolder, file)), Regex.Escape(args[0].Trim()), args[1].Trim());
                            File.WriteAllText(Path.Combine(OutputFolder, file), replacement);
                            break;
                        case "REMOVE":
                            replacement = Regex.Replace(File.ReadAllText(Path.Combine(OutputFolder, file)), Regex.Escape(command.Value.Trim()), "");
                            File.WriteAllText(Path.Combine(OutputFolder, file), replacement);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private Dictionary<string, string> GetCommands(string postProcessing, string file)
        {
            string content = Regex.Match(PostProcessing, @"# file: "+ Regex.Escape(file)+@"(?<content>.*?)(#|\z)", RegexOptions.Singleline).Groups["content"].Value.Trim();
            return Regex.Matches(content, @"(?<command>\w*)\((?<arguments>.*?)\)").Cast<Match>().ToDictionary(x => x.Groups["command"].Value, y => y.Groups["arguments"].Value);
        }

        private void CreateOverloads(List<Ui5Complex> results)
        {
            var alltypes = results.Flatten(x => x.Content);
            foreach (Ui5Complex c in alltypes)
                c.CheckOverloads(alltypes);
        }

        private void MergeDuplicateNamespaces(List<Ui5Complex> results)
        {
            var allnamespaces = results.Flatten(x => x.Content).OfType<Ui5Namespace>().OrderBy(x=>x.name).ToList();
            var allgroupednamespaces = allnamespaces.GroupBy(x => x.fullname, y => y).ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
            var duplicates = allgroupednamespaces.Where(y => y.Value.Count() > 1).ToList();
        }

        private static void CreateMetadata(List<Ui5Complex> results)
        {
            var fullclasslist = results.Flatten(x => x.Content).OfType<Ui5Class>().ToList();
            foreach (Ui5Class c in fullclasslist)
                c.CreateMetadata();
            var fullinterfacelist = results.Flatten(x => x.Content).OfType<Ui5Interface>();
            foreach (Ui5Class c in fullclasslist)
                c.ConnectMetadata(fullinterfacelist);
        }

        private void AppendCustomTypeDefinitions(List<Ui5Complex> results)
        {
            foreach(var entry in globalValues.Typedefinitions.Where(x=>x.Value!=nameof(Ui5Enum)))
            {
                Ui5Complex toreplace = GetElementByPath(results, entry.Key);

                if(toreplace==null)
                {
                    // if not found try to get the namespace
                    string[] arr = entry.Key.Split('.');
                    Ui5Namespace @namespace = GetElementByPath(results, arr.Take(arr.Length - 1).Aggregate((a, b) => a + "." + b)) as Ui5Namespace;
                    if(toreplace==null)
                    {
                        Log("Could not find element to replace with Type '" + entry.Key + "'");
                        continue;
                    }
                    // Create a dummy (to reuse code below)
                    toreplace = new Ui5Class { parentNamespace = @namespace };
                }

                if (!entry.Value.StartsWith("{"))
                {
                    StringBuilder sb = new StringBuilder();
                    if (toreplace.description != null)
                        sb.AppendComment(toreplace.description);
                    sb.AppendLine($"type {toreplace.name} = {entry.Value};");
                    toreplace.parentNamespace?.AppendAdditionalTypedef(sb.ToString());
                    toreplace.parentNamespace?.Content.Remove(toreplace);
                }

            }
        }

        private Ui5Complex GetElementByPath(List<Ui5Complex> results, string key)
        {
            var a = results.Flatten(x => x.Content).OrderBy(x=>x.name).ToList();
            return results.Flatten(x => x.Content).FirstOrDefault(x => x.fullname == key);
        }

        private void SetParents(Ui5Namespace result)
        {
            foreach (Ui5Complex entry in result.Content)
                entry.parentNamespace = result;
            foreach (Ui5Namespace @namespace in result.Content.OfType<Ui5Namespace>())
                SetParents(@namespace);
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
                        name = entry.@namespace.Split('.').TakeWhile(x => x != address[0]).Aggregate((a, b) => a + "." + b)+"."+address[0]
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

        private string[] CreateTypeDefinitionFiles(List<Ui5Complex> namespaces)
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
                        namespc.Content.Cast<Ui5Namespace>().ToList().ForEach(x => {
                            x.parentNamespace = null;
                            x.Imports = namespc.Imports;
                            });
                        namespaces.Remove(namespc);
                    }

            List<string> files = new List<string>();
            foreach(var entry in namespaces)
            {
                string filename = entry.fullname + ".d.ts";
                File.WriteAllText(Path.Combine(OutputFolder, filename), entry.SerializeTypescript());
                Log("Put content to file " + filename);
                files.Add(filename);
            }

            return files.ToArray();
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

        private void OpenOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(OutputFolder);
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            LogEntries.Clear();
        }
    }
}
