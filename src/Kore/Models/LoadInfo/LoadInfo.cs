using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Progress;
using Kore.Managers;
using Kore.Managers.Plugins;
using Serilog;

namespace Kore.Models.LoadInfo
{
    class LoadInfo
    {
        public IStateInfo ParentStateInfo { get; set; }

        public IStreamManager StreamManager { get; set; }

        public PluginManager PluginManager { get; set; }

        public IFilePlugin Plugin { get; set; }

        public IProgressContext Progress { get; set; }

        public InternalDialogManager DialogManager { get; set; }

        public bool AllowManualSelection { get; set; }

        public ILogger Logger { get; set; }
    }
}
