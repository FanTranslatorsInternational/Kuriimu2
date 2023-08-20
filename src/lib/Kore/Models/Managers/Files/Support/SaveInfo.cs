using Kontract.Interfaces.Managers.Dialogs;
using Kontract.Interfaces.Progress;
using Serilog;

namespace Kore.Models.Managers.Files.Support
{
    class SaveInfo
    {
        public IProgressContext Progress { get; set; }

        public IDialogManager DialogManager { get; set; }

        public ILogger Logger { get; set; }
    }
}
