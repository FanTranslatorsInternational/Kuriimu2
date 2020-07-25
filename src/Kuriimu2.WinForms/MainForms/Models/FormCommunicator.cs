using System;
using System.Drawing;
using System.Threading.Tasks;
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

        public Func<IStateInfo, ArchiveFileInfo, Guid, Task<bool>> OpenFileDelegate { get; set; }
        public Func<IStateInfo, bool, Task<bool>> SaveFileDelegate { get; set; }
        public Func<IStateInfo, ArchiveFileInfo, Task<bool>> CloseFileDelegate { get; set; }
        public Action<IStateInfo, ArchiveFileInfo, UPath> RenameFileDelegate { get; set; }

        public Action<IStateInfo, bool, bool> UpdateTabDelegate { get; set; }

        public Action<string, Color> ReportStatusDelegate { get; set; }

        public FormCommunicator(IStateInfo stateInfo)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));

            _stateInfo = stateInfo;
        }

        public Task<bool> Save(bool saveAs)
        {
            return SaveFileDelegate?.Invoke(_stateInfo, saveAs);
        }

        public void Update(bool updateParents, bool updateChildren)
        {
            UpdateTabDelegate?.Invoke(_stateInfo, updateParents, updateChildren);
        }

        public void ReportStatus(bool isSuccessful, string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var textColor = isSuccessful ? Color.Black : Color.DarkRed;
            ReportStatusDelegate?.Invoke(message, textColor);
        }

        public Task<bool> Open(ArchiveFileInfo file)
        {
            return Open(file, Guid.Empty);
        }

        public Task<bool> Open(ArchiveFileInfo file, Guid pluginId)
        {
            return OpenFileDelegate?.Invoke(_stateInfo, file, pluginId);
        }

        public Task<bool> Close(ArchiveFileInfo file)
        {
            return CloseFileDelegate?.Invoke(_stateInfo, file);
        }

        public void Rename(ArchiveFileInfo file, UPath renamedPath)
        {
            RenameFileDelegate?.Invoke(_stateInfo, file, renamedPath);
        }
    }
}
