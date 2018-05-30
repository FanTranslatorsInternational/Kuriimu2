using System;

namespace Kontract.Attributes
{
    /// <inheritdoc />
    /// <summary>
    /// This attribute is used to prepare class members for display as form fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FormFieldAttribute : Attribute
    {
        /// <summary>
        /// The data type that the form will cast to and from.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The display name of the member on the form field label.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The minimum length of the string that is expected in the TextBox.
        /// </summary>
        public int MinLength { get; }

        /// <summary>
        /// The maximum length of the string that is expected in the TextBox.
        /// </summary>
        public int MaxLength { get; }

        /// <summary>
        /// Initializes a new FormFieldAttribute.
        /// </summary>
        /// <param name="type">The data type that the form will cast to and from.</param>
        /// <param name="displayName">The display name of the member on the form field label.</param>
        /// <param name="minLength">The minimum length of the string that is expected in the TextBox.</param>
        /// <param name="maxLength">The maximum length of the string that is expected in the TextBox.</param>
        public FormFieldAttribute(Type type, string displayName = "", int minLength = 0, int maxLength = 0)
        {
            Type = type;
            DisplayName = displayName;
            MinLength = minLength;
            MaxLength = maxLength;
        }
    }
}
