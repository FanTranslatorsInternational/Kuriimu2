using System.Collections.Generic;
using System.Linq;
using Kontract;

namespace Kuriimu2.WinForms.ExtensionForms.Models
{
    public class ExtensionType
    {
        private readonly bool _builtIn;

        public string Name { get; }

        public IDictionary<string, ExtensionTypeParameter> Parameters { get; }

        public ExtensionType(string name, bool builtIn, params ExtensionTypeParameter[] parameters)
        {
            ContractAssertions.IsNotNull(name, nameof(name));
            ContractAssertions.IsNotNull(parameters, nameof(parameters));

            Name = name;
            _builtIn = builtIn;

            Parameters = parameters.ToDictionary(x => x.Name, y => y);
        }

        public TValue GetParameterValue<TValue>(string name)
        {
            ContractAssertions.IsNotNull(name,nameof(name));

            if (!Parameters.ContainsKey(name))
                return default;

            return (TValue) Parameters[name].Value;
        }

        public override string ToString()
        {
            if (_builtIn)
                return "[Built-In] " + Name;

            return Name;
        }
    }
}
