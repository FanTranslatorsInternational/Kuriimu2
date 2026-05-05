using Komponent.Contract.Enums;

namespace Komponent.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TypeChoiceAttribute(string fieldName, TypeChoiceComparer comp, ulong value, Type injectionType)
        : Attribute
    {
        public string FieldName { get; } = fieldName;
        public TypeChoiceComparer Comparer { get; } = comp;
        public ulong Value { get; } = value;
        public Type InjectionType { get; } = injectionType;
    }
}
