using System;
using System.Drawing;
using System.Threading.Tasks;
using Kontract.Interfaces.Managers;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace Kuriimu2.WinForms.MainForms.Interfaces
{
    interface IMainForm
    {
        Task<bool> OpenFile(IStateInfo stateInfo, ArchiveFileInfo file, Guid pluginId);
        Task<bool> SaveFile(IStateInfo stateInfo, bool saveAs);
        Task<bool> CloseFile(IStateInfo stateInfo, ArchiveFileInfo file);
        void RenameFile(IStateInfo stateInfo, ArchiveFileInfo file, UPath newPath);

        void Update(IStateInfo stateInfo, bool updateParents, bool updateChildren);

        void ReportStatus(bool isSuccessful, string message);
    }
}
