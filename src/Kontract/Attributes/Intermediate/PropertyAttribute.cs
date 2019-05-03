using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Attributes.Intermediate
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PropertyAttribute : Attribute
    {
        public string PropertyName { get; }
        public Type PropertyType { get; }
        public object DefaultValue { get; }

        public PropertyAttribute(string propertyName, Type propertyType, object defValue)
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
            DefaultValue = defValue;
        }
    }
}
