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

        public ArchiveFormInfo(IFileState fileState, IArchiveFormCommunicator formCommunicator, IProgressContext progress, ILogger logger) : base(fileState, formCommunicator, progress, logger)
        {
        }
        
        // TODO this cast smells, should IFileState/IPluginState be generified?
        // TODO can be inlined if generified
        public bool CanReplaceFiles => FileState.PluginState is IArchiveState {CanReplaceFiles: true};
        public bool CanRenameFiles => FileState.PluginState is IArchiveState {CanRenameFiles: true};
        //TODO also check for RemoveAll?
        public bool CanDeleteFiles => FileState.PluginState is IArchiveState {CanDeleteFiles: true};
        public bool CanAddFiles => FileState.PluginState is IArchiveState {CanAddFiles: true};
    }

    public class FormInfo
    {
        public IFileState FileState { get; }

        public IFormCommunicator FormCommunicator { get; }

        public IProgressContext Progress { get; }

        public ILogger Logger { get; }

        public FormInfo(IFileState fileState, IFormCommunicator formCommunicator, IProgressContext progress, ILogger logger)
        {
            FileState = fileState;
            FormCommunicator = formCommunicator;
            Progress = progress;
            Logger = logger;
        }
    }
}
