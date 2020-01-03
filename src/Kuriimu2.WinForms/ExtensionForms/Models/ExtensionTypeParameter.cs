using System;
using Kontract;

namespace Kuriimu2.WinForms.ExtensionForms.Models
{
    public class ExtensionTypeParameter
    {
        public string Name { get; }

        public Type ParameterType { get; }

        public bool IsFile { get; }

        public object Value { get; set; }

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
