using Kontract.Models.Dialog;

namespace Kontract.Interfaces.Managers
{
    /// <summary>
    /// An interface defining methods to communicate with the User Interface.
    /// </summary>
    public interface IDialogManager
    {
        /// <summary>
        /// Shows a dialog on which the user can interact with the plugin.
        /// </summary>
        /// <param name="fields">The fields to show on the dialog.</param>
        void ShowDialog(DialogField[] fields);
    }
}
