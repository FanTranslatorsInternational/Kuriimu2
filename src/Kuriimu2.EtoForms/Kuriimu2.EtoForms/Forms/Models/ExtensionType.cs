using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract;

namespace Kuriimu2.EtoForms.Forms.Models
{
    public class ExtensionType
    {
        public string Name { get; }

        public bool BuiltIn { get; }

        public IDictionary<string, ExtensionTypeParameter> Parameters { get; }

        public ExtensionType(string name, bool builtIn, params ExtensionTypeParameter[] parameters)
        {
            ContractAssertions.IsNotNull(name, nameof(name));
            ContractAssertions.IsNotNull(parameters, nameof(parameters));

            Name = name;
            BuiltIn = builtIn;

            Parameters = parameters.ToDictionary(x => x.Name, y => y);
        }

        public Stream GetStream(string name, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite)
        {
            ContractAssertions.IsNotNull(name, nameof(name));

            if (!Parameters.ContainsKey(name))
                throw new KeyNotFoundException(name);

            var parameter = Parameters[name];
            if (!parameter.IsFile)
                throw new InvalidOperationException($"Parameter '{name}' is a typed parameter.");

            return File.Open((string)parameter.Value, mode, access);
        }

        public TValue GetParameterValue<TValue>(string name)
        {
            ContractAssertions.IsNotNull(name, nameof(name));

            if (!Parameters.ContainsKey(name))
                throw new KeyNotFoundException(name);

            var parameter = Parameters[name];
            if (parameter.IsFile)
                throw new InvalidOperationException($"Parameter '{name}' is a file parameter.");

            if (typeof(TValue).IsEnum)
                return (TValue)Enum.Parse(typeof(TValue), (string)parameter.Value);

            return (TValue)Convert.ChangeType(parameter.Value, typeof(TValue));
        }

        public override string ToString()
        {
            return (BuiltIn ? "[Built-In] " : "") + Name;
        }
    }
}
