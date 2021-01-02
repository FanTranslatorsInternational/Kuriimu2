using System;
using Kontract;

namespace Kuriimu2.EtoForms.Forms.Models
{
    public class ExtensionTypeParameter
    {
        public string Name { get; }

        public Type ParameterType { get; }

        public bool IsFile { get; }

        public object Value { get; set; }

        public bool HasDefaultValue { get; }

        public ExtensionTypeParameter(string name, Type type, object defaultValue) :
            this(name, type)
        {
            ContractAssertions.IsNotNull(defaultValue, nameof(defaultValue));

            Value = defaultValue;
            HasDefaultValue = true;
        }

        public ExtensionTypeParameter(string name, Type type)
        {
            ContractAssertions.IsNotNull(name, nameof(name));
            ContractAssertions.IsNotNull(type, nameof(type));

            Name = name;
            ParameterType = type;
        }

        public ExtensionTypeParameter(string name)
        {
            ContractAssertions.IsNotNull(name, nameof(name));

            Name = name;
            IsFile = true;
        }
    }
}
