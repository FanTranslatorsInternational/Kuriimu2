namespace Kuriimu2.WinForms.MainForms.Models
{
    internal class FormatWrapper
    {
        public object Value { get; }
        public string Name { get; }

        public FormatWrapper(object value, string name)
        {
            Value = value;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
