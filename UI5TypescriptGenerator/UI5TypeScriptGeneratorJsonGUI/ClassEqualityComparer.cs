using System;
using System.Collections.Generic;

namespace UI5TypeScriptGeneratorJsonGUI
{
    internal class ComplexEqualityComparer : IEqualityComparer<Ui5Complex>
    {
        public bool Equals(Ui5Complex x, Ui5Complex y)
        {
            return x.name == y.name && x.@namespace == y.@namespace;
        }

        public int GetHashCode(Ui5Complex obj)
        {
            return obj.GetHashCode();
        }
    }
}