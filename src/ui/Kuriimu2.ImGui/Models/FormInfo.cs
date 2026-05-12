using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Contract.Progress;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Progress;
using Serilog;
using System;

namespace Kuriimu2.ImGui.Models
{
    internal class FormInfo<TState> where TState : IFilePluginState
    {
        public IFileState FileState { get; }

        public TState PluginState => (TState)FileState.PluginState;

        public IFormCommunicator FormCommunicator { get; }

        public IProgressContext Progress { get; }

        public ProgressBarOutput ProgressOutput { get; }

        public ILogger Logger { get; }

        public bool CanSave => FileState.PluginState.CanSave;

        public FormInfo(IFileState fileState, IFormCommunicator formCommunicator, IProgressContext progress, ProgressBarOutput progressOutput, ILogger logger)
        {
            if (fileState.PluginState is not TState)
                throw new InvalidOperationException($"The given plugin state is not of type {typeof(TState).Name}");

            FileState = fileState;
            FormCommunicator = formCommunicator;
            Progress = progress;
            ProgressOutput = progressOutput;
            Logger = logger;
        }
    }

    internal class ArchiveFormInfo(
        IFileState fileState,
        IArchiveFormCommunicator formCommunicator,
        IProgressContext progress,
        ProgressBarOutput progressOutput,
        ILogger logger)
        : FormInfo<IArchiveFilePluginState>(fileState, formCommunicator, progress, progressOutput, logger)
    {
        public new IArchiveFormCommunicator FormCommunicator => (IArchiveFormCommunicator)base.FormCommunicator;

        public bool CanReplaceFiles => PluginState.CanReplaceFiles;
        public bool CanRenameFiles => PluginState.CanRenameFiles;
        public bool CanDeleteFiles => PluginState.CanDeleteFiles;
        public bool CanAddFiles => PluginState.CanAddFiles;
    }
}
