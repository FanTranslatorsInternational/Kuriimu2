using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Managers.Streams;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Progress;
using Kore.Managers;
using Kore.Managers.Plugins;
using Serilog;

namespace Kore.Models.LoadInfo
{
    class LoadInfo
    {
        public IFileState ParentFileState { get; set; }

        public IStreamManager StreamManager { get; set; }

        public FileManager FileManager { get; set; }

        public IFilePlugin Plugin { get; set; }

        public IProgressContext Progress { get; set; }

        public InternalDialogManager DialogManager { get; set; }

        public bool AllowManualSelection { get; set; }

        public ILogger Logger { get; set; }
    }
}
