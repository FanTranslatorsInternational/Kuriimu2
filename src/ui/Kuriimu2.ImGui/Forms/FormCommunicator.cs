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
    internal class FormCommunicator(IFileState fileState, IMainForm mainForm) : IArchiveFormCommunicator
    {
        #region Non-blocking Methods

        // All methods here leave execution on the thread they are currently ran on
        // Those methods are meant to execute for a longer amount of time
        // The methods called have to invoke execution to the main thread as soon as UI related tasks have to be done, like updating controls

        public Task<bool> Save(bool saveAs)
        {
            return mainForm.SaveFile(fileState, saveAs);
        }

        public async Task<bool> Open(IArchiveFile file)
        {
            return await Open(file, Guid.Empty);
        }

        public async Task<bool> Open(IArchiveFile file, Guid pluginId)
        {
            return await mainForm.OpenFile(fileState, file, pluginId);
        }

        public async Task<bool> Close(IArchiveFile file)
        {
            return await mainForm.CloseFile(fileState, file);
        }

        #endregion

        #region Blocking Methods

        // All methods here resume execution to the main thread.
        // Those methods are expected to be short-lived and block the UI for an insignificant amount of time

        public void Update(bool updateParents, bool updateChildren)
        {
            mainForm.Update(fileState, updateParents, updateChildren);
        }

        public void Rename(IArchiveFile file, UPath renamedPath)
        {
            mainForm.RenameFile(fileState, file, renamedPath);
        }

        public void ReportStatus(StatusKind status, LocalizedString message)
        {
            mainForm.ReportStatus(status, message);
        }

        #endregion
    }
}
