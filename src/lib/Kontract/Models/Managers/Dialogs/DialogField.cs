namespace Kontract.Models.Managers.Dialogs
{
    /// <summary>
    /// The class representing one field on a dialog.
    /// </summary>
    public class DialogField
    {
        /// <summary>
        /// The type of the field.
        /// </summary>
        public DialogFieldType Type { get; }

        /// <summary>
        /// The label of the input.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The options available to choose from. Only important for <see cref="DialogFieldType.DropDown"/>.
        /// </summary>
        public string[] Options { get; }

        /// <summary>
        /// The default value for this input.
        /// </summary>
        public string DefaultValue { get; }

        /// <summary>
        /// The final value from the dialog.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="DialogField"/>.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="text">The label of the input.</param>
        /// <param name="defaultValue">The default value for this input.</param>
        /// <param name="options">The options available to choose from.</param>
        public DialogField(DialogFieldType type, string text, string defaultValue, params string[] options)
        {
            ContractAssertions.IsNotNull(defaultValue, nameof(defaultValue));
            ContractAssertions.IsNotNull(options, nameof(options));
            ContractAssertions.IsElementContained(options, defaultValue, nameof(options), nameof(defaultValue));

            Type = type;
            Text = text;
            Options = options;
            DefaultValue = defaultValue;
        }
    }
}
