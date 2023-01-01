using System;

namespace Komponent.IO.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConditionAttribute : Attribute
    {
        public string FieldName { get; }
        public ConditionComparer Comparer { get; }
        public ulong Value { get; }

        public ConditionAttribute(string fieldName, ConditionComparer comp, ulong value)
        {
            FieldName = fieldName;
            Comparer = comp;
            Value = value;
        }
    }

    public enum ConditionComparer
    {
        Equal,
        Smaller,
        Greater,
        GEqual,
        SEqual
    }
}
