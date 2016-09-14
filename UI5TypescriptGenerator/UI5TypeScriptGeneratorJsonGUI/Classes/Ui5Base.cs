using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Base
    {
        private string _name;

        public virtual string name
        {
            get { return _name; }
            set
            {
                _name = Regex.Replace(value, @"s/^[^a-zA-Z_]+|[^\w_]+", "");
            }
        }

        public string since { get; set; }
        public string description { get; set; }
        public bool IncludedInVersion()
        {
            if (since != null)
            {
                var wishedversion = Properties.Settings.Default.Version.Split('.').Select(x => int.Parse(x)).ToArray();
                var gotversion = since.Split('.').Take(wishedversion.Length).Select(x => int.Parse(x)).ToArray();
                for (int i = 0; i < wishedversion.Length && i < gotversion.Length; i++)
                    if (!(wishedversion[i] >= gotversion[i]))
                        return false;
            }

            if (deprecated != null && deprecated.since != null)
            {
                var wishedversion = Properties.Settings.Default.Version.Split('.').Select(x => int.Parse(x)).ToArray();
                var gotversion = deprecated.since.Split('.').Take(wishedversion.Length).Select(x => int.Parse(x)).ToArray();
                for (int i = 0; i < wishedversion.Length && i < gotversion.Length; i++)
                    if (!(wishedversion[i] < gotversion[i]))
                        return false;
            }
            return true;
        }
        public Ui5Deprecated deprecated { get; set; }
    }
}
