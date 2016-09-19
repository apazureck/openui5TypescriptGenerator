using System.ComponentModel;
using System.Runtime.Serialization;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public enum Visibility
    {
        [EnumMember(Value = "public")]
        [Description("")]
        Public,
        [Description("protected ")]
        [EnumMember(Value = "protected")]
        Protected,
        [Description("private ")]
        [EnumMember(Value = "private")]
        Private,
        [Description("private ")]
        [EnumMember(Value = "restricted")]
        Resticted,
        [Description("")]
        [EnumMember(Value = "hidden")]
        Hidden
    }
}