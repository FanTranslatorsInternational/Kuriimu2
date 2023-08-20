using System.Threading.Tasks;
using Kontract.Models.Managers.Dialogs;

namespace Kontract.Interfaces.Managers.Dialogs
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
        Task<bool> ShowDialog(DialogField[] fields);
    }
}
