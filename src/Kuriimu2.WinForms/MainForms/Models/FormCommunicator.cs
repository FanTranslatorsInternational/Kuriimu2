using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract;
using Kontract.Interfaces.Managers;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kuriimu2.WinForms.MainForms.Interfaces;

namespace Kuriimu2.WinForms.MainForms.Models
{
    class FormCommunicator : IArchiveFormCommunicator
    {
        private readonly IStateInfo _stateInfo;
        private readonly IMainForm _mainForm;
        private readonly Control _mainFormControl;

        public FormCommunicator(IStateInfo stateInfo, IMainForm mainForm)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(mainForm, nameof(mainForm));

            _stateInfo = stateInfo;
            _mainForm = mainForm;

            if (mainForm is Control mainFormControl)
                _mainFormControl = mainFormControl;
        }

        #region Non-blocking Methods

        // All methods here leave execution on the thread they are currently ran on
        // Those methods are meant to execute for a longer amount of time
        // The methods called have to invoke execution to the main thread as soon as UI related tasks have to be done, like updating controls

        public Task<bool> Save(bool saveAs)
        {
            return _mainForm.SaveFile(_stateInfo, saveAs);
            //if (_mainFormControl != null && _mainFormControl.InvokeRequired)
            //    return (Task<bool>)_mainFormControl.Invoke(new Func<IStateInfo, bool, Task<bool>>((s1, s2) => _mainForm.SaveFile(s1, s2)), _stateInfo, saveAs);

            //return _mainForm.SaveFile(_stateInfo, saveAs);
        }

        public Task<bool> Open(ArchiveFileInfo file)
        {
            return Open(file, Guid.Empty);
        }

        public Task<bool> Open(ArchiveFileInfo file, Guid pluginId)
        {
            return _mainForm.OpenFile(_stateInfo, file, pluginId);
            //if (_mainFormControl != null && _mainFormControl.InvokeRequired)
            //    return (Task<bool>)_mainFormControl.Invoke(new Func<IStateInfo, ArchiveFileInfo, Guid, Task<bool>>((s1, a1, g1) => _mainForm.OpenFile(s1, a1, g1)), _stateInfo, file, pluginId);

            //return _mainForm.OpenFile(_stateInfo, file, pluginId);
        }

        public Task<bool> Close(ArchiveFileInfo file)
        {
            return _mainForm.CloseFile(_stateInfo, file);
            //if (_mainFormControl != null && _mainFormControl.InvokeRequired)
            //    return (Task<bool>)_mainFormControl.Invoke(new Func<IStateInfo, ArchiveFileInfo, Task<bool>>((s1, a1) => _mainForm.CloseFile(s1, a1)), _stateInfo, file);

            //return _mainForm.CloseFile(_stateInfo, file);
        }

        #endregion

        #region Blocking Methods

        // All methods here resume execution to the main thread.
        // Those methods are expected to be short-lived and block the UI for an insignificant amount of time

        public void Update(bool updateParents, bool updateChildren)
        {
            InvokeAction(() => _mainForm.Update(_stateInfo, updateParents, updateChildren));
        }

        public void Rename(ArchiveFileInfo file, UPath renamedPath)
        {
            InvokeAction(() => _mainForm.RenameFile(_stateInfo, file, renamedPath));
        }

        public void ReportStatus(bool isSuccessful, string message)
        {
            InvokeAction(() => _mainForm.ReportStatus(isSuccessful, message));
        }

        #endregion

        private void InvokeAction(Action invokeAction)
        {
            if (_mainFormControl != null && _mainFormControl.InvokeRequired)
            {
                _mainFormControl.Invoke(invokeAction);
                return;
            }

            invokeAction();
        }
    }
}
