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

            globalValues.TranslationDictionary = CreateglobalDictionary();

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
                        globalValues.TranslationDictionary = CreateglobalDictionary();
                        Settings.TypeReplacements = globalValues.TranslationDictionary.OrderBy(x => x.Key).Select(x => x.Key + " : " + x.Value).Aggregate((a, b) => a.Trim() + Environment.NewLine + b).Trim();
                        Settings.Save();
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TypeReplacements)));
                    }
                    catch { }
                });
            }
        }

        private System.Collections.Generic.Dictionary<string, string> CreateglobalDictionary()
        {
            return TypeReplacements.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToDictionary(key => key.Split(':')[0].Trim(), val => val.Split(':')[1].Trim());
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
                .Concat(allsymbols[typeof(Ui5Namespace).Name].Cast<Ui5Complex>().Distinct(new ComplexEqualityComparer()))
                .OrderBy(x=> x.@namespace+"."+x.name).ToList();
                alldistinctcomplex = alldistinctcomplex.Where(x => x.IncludedInVersion()).ToList();

                Log("Found " + alldistinctcomplex.Count() + " classes.");

                Dictionary<string, List<Ui5Complex>> classesbynamespaces = alldistinctcomplex.GroupBy(x => x.@namespace.Split('.').Take(2).Aggregate((a,b) => a + "." + b), y => y).ToDictionary(key => key.Key, value => value.ToList());

                CreateTypeDefinitionFiles(classesbynamespaces);

                Log("Conversion successfully executed.");

                Output = globalValues.UntouchedTypes.Select(x => x.Key + "(" + x.Value.ToString() + ")").OrderBy(x => x).Aggregate((a, b) => a + Environment.NewLine + b);

            });
        }

        private void CreateTypeDefinitionFiles(Dictionary<string, List<Ui5Complex>> classesbynamespaces)
        {
            Log("Trying to convert classes");

            foreach(var entry in classesbynamespaces)
            {
                StringBuilder filecontent = new StringBuilder();
                var namespaces = entry.Value.GroupBy(x => x.@namespace, y => y).ToDictionary(key => key.Key, value => value.ToList());

                foreach(var @namespace in namespaces)
                {
                    Log("Creating content for " + @namespace.Key);
                    filecontent.AppendLine();
                    filecontent.AppendLine("declare namespace " + @namespace.Key + " {");
                    @namespace.Value.ForEach(x => filecontent.AppendLine(x.SerializeTypescript(), 1));
                    filecontent.AppendLine("}");
                }

                string filename = entry.Key + ".d.ts";
                File.WriteAllText(Path.Combine(OutputFolder, filename), filecontent.ToString());
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
                            CreateglobalDictionary();
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
