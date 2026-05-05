namespace Komponent.IO
{
    internal class ValueStorage
    {
        private readonly string? _scope;
        private readonly IDictionary<string, object?> _values;

        public ValueStorage()
        {
            _values = new Dictionary<string, object?>();
        }

        private ValueStorage(IDictionary<string, object?> store, string scope)
        {
            _scope = scope;
            _values = store;
        }

        public bool Exists(string fieldName)
        {
            return _values.ContainsKey(GetValueName(fieldName));
        }

        public object? Get(string fieldName)
        {
            return _values[GetValueName(fieldName)];
        }

        public void Set(string fieldName, object? value)
        {
            _values[GetValueName(fieldName)] = value;
        }

        public ValueStorage CreateScope(string? fieldName)
        {
            return new ValueStorage(_values, GetValueName(fieldName));
        }

        private string GetValueName(string? fieldName)
        {
            if (fieldName == null)
                return _scope ?? string.Empty;

            // Shortcut: Return normal concatenated string, if no back references exist
            if (!fieldName.Contains(".."))
            {
                var nestedName = fieldName;
                if (!string.IsNullOrEmpty(_scope))
                    nestedName = _scope + "." + fieldName;

                return nestedName;
            }

            // Remove optional starting dot, which would reference current scope
            if (fieldName.StartsWith('.'))
                fieldName = fieldName[1..];

            // Otherwise resolve back references
            var validParts = new List<string>();

            string[] nestedNameParts = string.IsNullOrEmpty(_scope) ? [] : _scope.Split('.');
            foreach (string part in nestedNameParts.Concat(fieldName.Split('.')))
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
