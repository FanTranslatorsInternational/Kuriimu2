using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Models.Archive;
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
        
        // TODO this cast smells, should IStateInfo/IPluginState be generified?
        public bool CanReplaceFiles => StateInfo.PluginState is IArchiveState {CanReplaceFiles: true};
        public bool CanRenameFiles => StateInfo.PluginState is IArchiveState {CanRenameFiles: true};
        //TODO also check for RemoveAll?
        public bool CanDeleteFiles => StateInfo.PluginState is IArchiveState {CanDeleteFiles: true};
        public bool CanAddFiles => StateInfo.PluginState is IArchiveState {CanAddFiles: true};
    }

    public class FormInfo
    {
        public IStateInfo StateInfo { get; }

        public IFormCommunicator FormCommunicator { get; }

        public IProgressContext Progress { get; }

        public ILogger Logger { get; }

        // TODO remove+inline?
        public bool CanSave => StateInfo.PluginState.CanSave;

        public FormInfo(IStateInfo stateInfo, IFormCommunicator formCommunicator, IProgressContext progress, ILogger logger)
        {
            StateInfo = stateInfo;
            FormCommunicator = formCommunicator;
            Progress = progress;
            Logger = logger;
        }
    }
}
