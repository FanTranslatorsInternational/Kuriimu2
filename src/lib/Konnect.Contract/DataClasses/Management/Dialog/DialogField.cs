using Konnect.Contract.Enums.Management.Dialog;

namespace Konnect.Contract.DataClasses.Management.Dialog;

/// <summary>
/// The class representing one field on a dialog.
/// </summary>
public class DialogField
{
    /// <summary>
    /// The type of the field.
    /// </summary>
    public required DialogFieldType Type { get; init; }

    /// <summary>
    /// The label of the input.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The options available to choose from. Only important for <see cref="DialogFieldType.DropDown"/>.
    /// </summary>
    public required string[] Options { get; init; }

    /// <summary>
    /// The default value for this input.
    /// </summary>
    public required string DefaultValue { get; init; }

    /// <summary>
    /// The final value from the dialog.
    /// </summary>
    public string? Result { get; set; }
}