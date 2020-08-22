using Kontract.Interfaces.Managers;
using Kontract.Models.Dialog;

namespace Kore.Managers
{
    public class DefaultDialogManager : IDialogManager
    {
        public void ShowDialog(params DialogField[] fields)
        {
            // Set results of fields to default value
            foreach (var field in fields)
                field.Result = field.DefaultValue;
        }
    }
}
