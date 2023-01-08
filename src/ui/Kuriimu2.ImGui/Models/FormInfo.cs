﻿using System;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kuriimu2.ImGui.Interfaces;
using Serilog;

namespace Kuriimu2.ImGui.Models
{
    class FormInfo<TState> where TState : IPluginState
    {
        public IFileState FileState { get; }

        public TState PluginState => (TState)FileState.PluginState;

        public IFormCommunicator FormCommunicator { get; }

        public IProgressContext Progress { get; }

        public ILogger Logger { get; }

        public bool CanSave => FileState.PluginState.CanSave;

        public FormInfo(IFileState fileState, IFormCommunicator formCommunicator, IProgressContext progress, ILogger logger)
        {
            if(!(fileState.PluginState is TState))
                throw new InvalidOperationException($"The given plugin state is not of type {typeof(TState).Name}");

            FileState = fileState;
            FormCommunicator = formCommunicator;
            Progress = progress;
            Logger = logger;
        }
    }

    class ArchiveFormInfo : FormInfo<IArchiveState>
    {
        public new IArchiveFormCommunicator FormCommunicator => (IArchiveFormCommunicator)base.FormCommunicator;

        public ArchiveFormInfo(IFileState fileState, IArchiveFormCommunicator formCommunicator, IProgressContext progress, ILogger logger) : base(fileState, formCommunicator, progress, logger)
        {
        }

        public bool CanReplaceFiles => PluginState.CanReplaceFiles;
        public bool CanRenameFiles => PluginState.CanRenameFiles;
        public bool CanDeleteFiles => PluginState.CanDeleteFiles;
        public bool CanAddFiles => PluginState.CanAddFiles;
    }
}
