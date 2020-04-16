using System;
using System.Collections.Generic;
using System.Linq;

namespace Kontract
{
    /// <summary>
    /// Contains methods to assert basic needs.
    /// </summary>
    public static class ContractAssertions
    {
        /// <summary>
        /// Asserts the value to not be <see langword="null"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="valueName">The value name.</param>
        public static void IsNotNull(object value, string valueName)
        {
            if (value == null)
                throw new ArgumentNullException($"The argument '{valueName}' is null.");
        }

        /// <summary>
        /// Asserts the element to be contained in <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the element.</typeparam>
        /// <param name="list">The list to assert against.</param>
        /// <param name="element">The element to assert in the list.</param>
        /// <param name="listName">The name of the list.</param>
        /// <param name="elementName">The name of the element.</param>
        public static void IsElementContained<T>(IEnumerable<T> list, T element, string listName, string elementName)
        {
            if (!list.Contains(element))
                throw new InvalidOperationException($"'{listName}' doesn't contain '{elementName}'.'");
        }

        /// <summary>
        /// Asserts the value to be in a range.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="valueName">The value name.</param>
        /// <param name="min">The inclusive minimum of the range.</param>
        /// <param name="max">The inclusive maximum of the range.</param>
        public static void IsInRange(int value, string valueName, int min, int max)
        {
            if (value < min || value > max)
                throw new InvalidOperationException($"The value '{valueName}' is not in the range of '{min}' and '{max}'.");
        }

        /// <summary>
        /// Asserts the value to be true.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="valueName">The value name.</param>
        public static void IsTrue(bool value, string valueName)
        {
            if(!value)
                throw new InvalidOperationException($"The value '{valueName}' is not true.");
        }
    }
}
