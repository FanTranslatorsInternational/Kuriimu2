using System;
using System.Threading.Tasks;
using ImGui.Forms.Localization;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Archive;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;

namespace Kuriimu2.ImGui.Forms
{
    class FormCommunicator : IArchiveFormCommunicator
    {
        private readonly IFileState _fileState;
        private readonly IMainForm _mainForm;

        public FormCommunicator(IFileState fileState, IMainForm mainForm)
        {
            _fileState = fileState;
            _mainForm = mainForm;
        }

        #region Non-blocking Methods

        // All methods here leave execution on the thread they are currently ran on
        // Those methods are meant to execute for a longer amount of time
        // The methods called have to invoke execution to the main thread as soon as UI related tasks have to be done, like updating controls

        public Task<bool> Save(bool saveAs)
        {
            return _mainForm.SaveFile(_fileState, saveAs);
        }

        public async Task<bool> Open(IArchiveFile file)
        {
            return await Open(file, Guid.Empty);
        }

        public async Task<bool> Open(IArchiveFile file, Guid pluginId)
        {
            return await _mainForm.OpenFile(_fileState, file, pluginId);
        }

        public async Task<bool> Close(IArchiveFile file)
        {
            return await _mainForm.CloseFile(_fileState, file);
        }

        #endregion

        #region Blocking Methods

        // All methods here resume execution to the main thread.
        // Those methods are expected to be short-lived and block the UI for an insignificant amount of time

        public void Update(bool updateParents, bool updateChildren)
        {
            _mainForm.Update(_fileState, updateParents, updateChildren);
        }

        public void Rename(IArchiveFile file, UPath renamedPath)
        {
            _mainForm.RenameFile(_fileState, file, renamedPath);
        }

        public void ReportStatus(StatusKind status, LocalizedString message)
        {
            _mainForm.ReportStatus(status, message);
        }

        #endregion
    }
}
