using Konnect.Contract.DataClasses.Management.Dialog;

namespace Konnect.Contract.Management.Dialog;

/// <summary>
/// An interface defining methods to communicate with the User Interface.
/// </summary>
public interface IDialogManager
{
    /// <summary>
    /// The options selected by <see cref="ShowDialog"/>.
    /// </summary>
    public IList<string> DialogOptions { get; }

    /// <summary>
    /// Shows a dialog on which the user can interact with the plugin.
    /// </summary>
    /// <param name="fields">The fields to show on the dialog.</param>
    /// <returns>The selected options from the dialog.</returns>
    Task<bool> ShowDialog(DialogField[] fields);
}