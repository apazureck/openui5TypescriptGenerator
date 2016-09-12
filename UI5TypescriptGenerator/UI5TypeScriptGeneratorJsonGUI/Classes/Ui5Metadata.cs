using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Metadata
    {
        public string stereotype { get; set; }
        public List<Ui5Property> properties { get; set; }
        public List<Ui5Aggregation> aggregations { get; set; }
        public List<Ui5Event> events { get; set; }
    }
}
