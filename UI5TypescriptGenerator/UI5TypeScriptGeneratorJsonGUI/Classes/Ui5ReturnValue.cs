namespace UI5TypeScriptGeneratorJsonGUI
{
    public class Ui5Value
    {
        private string _type;

        public string type
        {
            get { return _type; }
            set { _type = globalValues.ConvertToValidTypeIfKnown(value); }
        }

        public string description { get; set; }
    }
}