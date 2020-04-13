using System.Collections.Generic;

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
                return string.Empty;

            var nestedName = fieldName;
            if (!string.IsNullOrEmpty(_nestedName))
                nestedName = _nestedName + "." + fieldName;

            return nestedName;
        }
    }
}
