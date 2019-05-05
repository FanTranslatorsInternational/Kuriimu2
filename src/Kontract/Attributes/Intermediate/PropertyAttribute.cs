using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces.Intermediate;

namespace Kontract.Attributes.Intermediate
{
    /// <summary>
    /// An attribute used to describe a property injection.
    /// </summary>
    /// <remarks>Mainly used in <see cref="IColorEncodingAdapter"/> and <see cref="IImageSwizzleAdapter"/>.</remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PropertyAttribute : Attribute
    {
        /// <summary>
        /// The name of the property to inject.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// The <see cref="Type"/> of the property to inject.
        /// </summary>
        public Type PropertyType { get; }

        /// <summary>
        /// The default value to inject to the property.
        /// </summary>
        public object DefaultValue { get; }

        /// <summary>
        /// Creates a new instance of <see cref="PropertyAttribute"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="propertyType"><see cref="Type"/> of the property.</param>
        /// <param name="defValue">Default value.</param>
        public PropertyAttribute(string propertyName, Type propertyType, object defValue)
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
            DefaultValue = defValue;
        }
    }
}
