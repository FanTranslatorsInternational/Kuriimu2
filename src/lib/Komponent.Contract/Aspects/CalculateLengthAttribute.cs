using Komponent.Contract.Enums;

namespace Komponent.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CalculateLengthAttribute(Type calculationType, string calculationMethod) : Attribute
    {
        public Type CalculationType { get; } = calculationType;
        public string CalculationMethodName { get; } = calculationMethod;
        public StringEncoding StringEncoding { get; set; } = StringEncoding.Ascii;
    }
}
