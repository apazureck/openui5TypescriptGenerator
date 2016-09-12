using System;
using System.Collections.Generic;

namespace UI5TypeScriptGeneratorJsonGUI
{
    internal class SymbolEqualityComparer : IEqualityComparer<Ui5Symbol>
    {
        public bool Equals(Ui5Symbol x, Ui5Symbol y)
        {
            return x.module == y.module && x.name == y.name;
        }

        public int GetHashCode(Ui5Symbol obj)
        {
            return obj.GetHashCode();
        }
    }
}