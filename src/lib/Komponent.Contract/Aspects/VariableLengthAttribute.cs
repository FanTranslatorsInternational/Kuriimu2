using Komponent.Contract.Enums;

namespace Komponent.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Field)]
    public class VariableLengthAttribute(string fieldName) : Attribute
    {
        public string FieldName { get; } = fieldName;
        public StringEncoding StringEncoding { get; set; } = StringEncoding.Ascii;
        public int Offset { get; set; }
    }
}
