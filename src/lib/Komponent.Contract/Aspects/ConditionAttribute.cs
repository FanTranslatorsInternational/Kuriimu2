using Komponent.Contract.Enums;

namespace Komponent.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConditionAttribute(string fieldName, ConditionComparer comp, ulong value) : Attribute
    {
        public string FieldName { get; } = fieldName;
        public ConditionComparer Comparer { get; } = comp;
        public ulong Value { get; } = value;
    }
}
