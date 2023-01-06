using System.Threading.Tasks;
using Kontract.Interfaces.Managers.Dialogs;
using Kontract.Models.Managers.Dialogs;

namespace Kore.Managers
{
    public class DefaultDialogManager : IDialogManager
    {
        public Task<bool> ShowDialog(params DialogField[] fields)
        {
            // Set results of fields to default value
            foreach (var field in fields)
                field.Result = field.DefaultValue;

            return Task.FromResult(true);
        }
    }
}
