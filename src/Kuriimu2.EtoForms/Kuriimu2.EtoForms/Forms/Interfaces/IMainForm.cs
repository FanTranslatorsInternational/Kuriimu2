using System;
using System.Threading.Tasks;
using Kontract.Interfaces.Managers;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace Kuriimu2.EtoForms.Forms.Interfaces
{
    interface IMainForm
    {
        Task<bool> OpenFile(IFileState fileState, IArchiveFileInfo file, Guid pluginId);
        Task<bool> SaveFile(IFileState fileState, bool saveAs);
        Task<bool> CloseFile(IFileState fileState, IArchiveFileInfo file);
        void RenameFile(IFileState fileState, IArchiveFileInfo file, UPath newPath);

        void Update(IFileState fileState, bool updateParents, bool updateChildren);

        void ReportStatus(bool isSuccessful, string message);
    }
}
