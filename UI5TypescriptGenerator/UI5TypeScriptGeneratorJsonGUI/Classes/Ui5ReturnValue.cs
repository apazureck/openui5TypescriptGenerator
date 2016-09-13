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

        public string GetRelativeTypeDef(string absolutepath)
        {
            return GetRelativeTypeDef(absolutepath, type);
        }

        public static string GetRelativeTypeDef(string absolutepath, string type)
        {
            if (!string.IsNullOrWhiteSpace(absolutepath) && type.StartsWith(absolutepath))
                return type.Replace(absolutepath + ".", "");
            else
                return type;
        }
    }
}