using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace UI5TypeScriptGeneratorJsonGUI
{
    [DebuggerDisplay("{DebuggerDisplay,ng}")]
    public class Ui5Symbol : Ui5Base
    {
        public string basename { get; set; }
        public string resource { get; set; }
        public string module { get; set; }
        protected virtual string DebuggerDisplay => $"{GetType().Name}: {name} ({module})";
    }
}