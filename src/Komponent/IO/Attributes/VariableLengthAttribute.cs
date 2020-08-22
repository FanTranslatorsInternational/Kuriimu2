using System;
using Kontract;

namespace Komponent.IO.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class VariableLengthAttribute : Attribute
    {
        public string FieldName { get; }
        public StringEncoding StringEncoding { get; set; } = StringEncoding.ASCII;
        public int Offset { get; set; }

        public VariableLengthAttribute(string fieldName)
        {
            ContractAssertions.IsNotNull(fieldName, nameof(fieldName));

            FieldName = fieldName;
        }
    }

    public enum StringEncoding : byte
    {
        ASCII,
        UTF7,
        UTF8,
        UTF16,
        Unicode,
        UTF32,
        SJIS
    }
}
