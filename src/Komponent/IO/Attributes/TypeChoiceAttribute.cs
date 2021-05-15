using System;

namespace Komponent.IO.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TypeChoiceAttribute : Attribute
    {
        public string FieldName { get; }
        public TypeChoiceComparer Comparer { get; }
        public ulong Value { get; }
        public Type InjectionType { get; }

        public TypeChoiceAttribute(string fieldName, TypeChoiceComparer comp, ulong value, Type injectionType)
        {
            FieldName = fieldName;
            Comparer = comp;
            Value = value;
            InjectionType = injectionType;
        }
    }

    public enum TypeChoiceComparer
    {
        Equal,
        Smaller,
        Greater,
        GEqual,
        SEqual
    }
}
