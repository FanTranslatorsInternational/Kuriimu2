using System;
using System.Threading.Tasks;
using ImGui.Forms.Localization;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.FileSystem;

namespace Kuriimu2.ImGui.Interfaces
{
    interface IMainForm
    {
        Task<bool> OpenFile(IFileState fileState, IArchiveFileInfo file, Guid pluginId);
        Task<bool> SaveFile(IFileState fileState, bool saveAs);
        Task<bool> CloseFile(IFileState fileState, IArchiveFileInfo file);
        void RenameFile(IFileState fileState, IArchiveFileInfo file, UPath newPath);

        void Update(IFileState fileState, bool updateParents, bool updateChildren);

        void ReportStatus(bool isSuccessful, LocalizedString message);
        void ClearStatus();
    }
}
