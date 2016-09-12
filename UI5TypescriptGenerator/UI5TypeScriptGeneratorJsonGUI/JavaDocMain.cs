using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class JavaDocMain
    {
        [JsonProperty(PropertyName = "$schema-ref")]
        public string schemaref { get; set; }
        public string version { get; set; }
        public string library { get; set; }

        public List<Ui5Symbol> symbols { get; set; }
    }
}
