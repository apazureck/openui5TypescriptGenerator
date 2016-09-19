using System;
using System.Collections.Generic;

namespace UI5TypeScriptGeneratorJsonGUI
{
    internal class Ui5MethodEqualityComparer : IEqualityComparer<Ui5Method>
    {
        public bool Equals(Ui5Method x, Ui5Method y)
        {
            return x.CreateDefinition(x.parameters, x.name, true, false) == y.CreateDefinition(x.parameters, x.name, true, false);
        }

        public int GetHashCode(Ui5Method obj)
        {
            return obj.CreateDefinition(obj.parameters, obj.name, true, false).GetHashCode();
        }
    }
}