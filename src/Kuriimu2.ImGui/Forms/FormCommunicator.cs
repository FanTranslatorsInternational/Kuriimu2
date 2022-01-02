using System;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.Managers;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kuriimu2.ImGui.Interfaces;

namespace Kuriimu2.ImGui.Forms
{
    class FormCommunicator : IArchiveFormCommunicator
    {
        private readonly IFileState _fileState;
        private readonly IMainForm _mainForm;

        public FormCommunicator(IFileState fileState, IMainForm mainForm)
        {
            ContractAssertions.IsNotNull(fileState, nameof(fileState));
            ContractAssertions.IsNotNull(mainForm, nameof(mainForm));

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

        public Task<bool> Open(IArchiveFileInfo file)
        {
            return Open(file, Guid.Empty);
        }

        public Task<bool> Open(IArchiveFileInfo file, Guid pluginId)
        {
            return _mainForm.OpenFile(_fileState, file, pluginId);
        }

        public Task<bool> Close(IArchiveFileInfo file)
        {
            return _mainForm.CloseFile(_fileState, file);
        }

        #endregion

        #region Blocking Methods

        // All methods here resume execution to the main thread.
        // Those methods are expected to be short-lived and block the UI for an insignificant amount of time

        public void Update(bool updateParents, bool updateChildren)
        {
            _mainForm.Update(_fileState, updateParents, updateChildren);
        }

        public void Rename(IArchiveFileInfo file, UPath renamedPath)
        {
            _mainForm.RenameFile(_fileState, file, renamedPath);
        }

        public void ReportStatus(bool isSuccessful, string message)
        {
            _mainForm.ReportStatus(isSuccessful, message);
        }

        #endregion
    }
}
