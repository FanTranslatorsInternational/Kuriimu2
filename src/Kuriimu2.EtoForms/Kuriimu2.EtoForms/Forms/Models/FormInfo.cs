using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Progress;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Serilog;

namespace Kuriimu2.EtoForms.Forms.Models
{
    public class ArchiveFormInfo : FormInfo
    {
        public new IArchiveFormCommunicator FormCommunicator => (IArchiveFormCommunicator)base.FormCommunicator;

        public ArchiveFormInfo(IStateInfo stateInfo, IArchiveFormCommunicator formCommunicator, IProgressContext progress, ILogger logger) : base(stateInfo, formCommunicator, progress, logger)
        {
        }
    }

    public class FormInfo
    {
        public IStateInfo StateInfo { get; }

        public IFormCommunicator FormCommunicator { get; }

        public IProgressContext Progress { get; }

        public ILogger Logger { get; }

        public bool CanSave => StateInfo.PluginState is ISaveFiles;

        public bool CanReplaceFiles => StateInfo.PluginState is IReplaceFiles;
        public bool CanRenameFiles => StateInfo.PluginState is IRenameFiles;
        public bool CanDeleteFiles => StateInfo.PluginState is IRemoveFiles;
        public bool CanAddFiles => StateInfo.PluginState is IAddFiles;

        public FormInfo(IStateInfo stateInfo, IFormCommunicator formCommunicator, IProgressContext progress, ILogger logger)
        {
            StateInfo = stateInfo;
            FormCommunicator = formCommunicator;
            Progress = progress;
            Logger = logger;
        }
    }
}
