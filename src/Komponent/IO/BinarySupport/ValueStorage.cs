using System;
using System.Collections.Generic;
using System.Linq;

namespace Komponent.IO.BinarySupport
{
    public class ValueStorage
    {
        private readonly string _nestedName;
        private readonly IDictionary<string, object> _values;

        public ValueStorage()
        {
            _values = new Dictionary<string, object>();
        }

        private ValueStorage(IDictionary<string, object> values, string nestedName)
        {
            _values = values;
            _nestedName = nestedName;
        }

        public bool Exists(string fieldName)
        {
            return _values.ContainsKey(GetValueName(fieldName));
        }

        public void Add(string fieldName, object value)
        {
            _values[GetValueName(fieldName)] = value;
        }

        public object Get(string fieldName)
        {
            return _values[GetValueName(fieldName)];
        }

        internal ValueStorage CreateScope(string fieldName)
        {
            return new ValueStorage(_values, GetValueName(fieldName));
        }

        private string GetValueName(string fieldName)
        {
            if (fieldName == null)
                return _nestedName ?? string.Empty;

            // Shortcut: Return normal concatenated string, if no back references exist
            if (!fieldName.Contains(".."))
            {
                var nestedName = fieldName;
                if (!string.IsNullOrEmpty(_nestedName))
                    nestedName = _nestedName + "." + fieldName;

                return nestedName;
            }

            // Remove optional starting dot, which would reference current scope
            if (fieldName.StartsWith("."))
                fieldName = fieldName.Substring(1, fieldName.Length - 1);

            // Otherwise resolve back references
            var validParts = new List<string>();

            var nestedNameParts = string.IsNullOrEmpty(_nestedName) ? Array.Empty<string>() : _nestedName.Split('.');
            foreach (var part in nestedNameParts.Concat(fieldName.Split('.')))
            {
                if (string.IsNullOrEmpty(part))
                {
                    if (validParts.Count <= 0)
                        throw new InvalidOperationException("Value is not deep enough.");

                    validParts.RemoveAt(validParts.Count - 1);
                    continue;
                }

                validParts.Add(part);
            }

            // And join all valid parts
            return string.Join(".", validParts);
        }
    }
}
