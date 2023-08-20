using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Managers.Streams;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Progress;
using Kore.Implementation.Managers.Dialogs;
using Kore.Implementation.Managers.Files;
using Serilog;

namespace Kore.Models.Managers.Files.Support
{
    class LoadInfo
    {
        public IFileState ParentFileState { get; set; }

        public IStreamManager StreamManager { get; set; }

        public KoreFileManager KoreFileManager { get; set; }

        public IFilePlugin Plugin { get; set; }

        public IProgressContext Progress { get; set; }

        public PredefinedDialogManager DialogManager { get; set; }

        public bool AllowManualSelection { get; set; }

        public ILogger Logger { get; set; }
    }
}
