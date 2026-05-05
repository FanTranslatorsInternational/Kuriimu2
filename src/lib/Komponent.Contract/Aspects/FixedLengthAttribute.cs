using Komponent.Contract.Enums;

namespace Komponent.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FixedLengthAttribute(int length) : Attribute
    {
        public int Length { get; } = length;
        public StringEncoding StringEncoding { get; set; } = StringEncoding.Ascii;
    }
}
