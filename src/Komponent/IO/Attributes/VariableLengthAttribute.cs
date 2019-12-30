using System;

namespace Komponent.IO.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class VariableLengthAttribute : Attribute
    {
        public string FieldName { get; }
        public StringEncoding StringEncoding = StringEncoding.ASCII;
        public int Offset;

        public VariableLengthAttribute(string fieldName)
        {
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
