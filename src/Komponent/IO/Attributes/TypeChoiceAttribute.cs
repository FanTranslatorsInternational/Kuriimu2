using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komponent.IO.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TypeChoiceAttribute : Attribute
    {
        public string FieldName { get; }
        public TypeChoiceComparer Comparer { get; }
        public object Value { get; }
        public Type InjectionType { get; }

        public TypeChoiceAttribute(string fieldName, TypeChoiceComparer comp, object value, Type injectionType)
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
