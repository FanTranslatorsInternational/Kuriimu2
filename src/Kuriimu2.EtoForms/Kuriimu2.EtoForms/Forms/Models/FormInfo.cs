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

        public ArchiveFormInfo(IFileState fileState, IArchiveFormCommunicator formCommunicator, IProgressContext progress, ILogger logger) : base(fileState, formCommunicator, progress, logger)
        {
        }
        
        public bool CanReplaceFiles => FileState.PluginState is IReplaceFiles;
        public bool CanRenameFiles => FileState.PluginState is IRenameFiles;
        public bool CanDeleteFiles => FileState.PluginState is IRemoveFiles;
        public bool CanAddFiles => FileState.PluginState is IAddFiles;
    }

    public class FormInfo
    {
        public IFileState FileState { get; }

        public IFormCommunicator FormCommunicator { get; }

        public IProgressContext Progress { get; }

        public ILogger Logger { get; }
        
        public bool CanSave => FileState.PluginState is ISaveFiles;

        public FormInfo(IFileState fileState, IFormCommunicator formCommunicator, IProgressContext progress, ILogger logger)
        {
            FileState = fileState;
            FormCommunicator = formCommunicator;
            Progress = progress;
            Logger = logger;
        }
    }
}
