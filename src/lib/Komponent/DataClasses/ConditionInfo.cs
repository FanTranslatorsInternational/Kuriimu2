using Komponent.Contract.Enums;

namespace Komponent.DataClasses
{
    public class ConditionInfo(string fieldName, ConditionComparer comp, ulong value)
    {
        public string FieldName { get; } = fieldName;
        public ConditionComparer Comparer { get; } = comp;
        public ulong Value { get; } = value;
    }
}
