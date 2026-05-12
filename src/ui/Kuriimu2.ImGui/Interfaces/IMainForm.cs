using System;
using System.Threading.Tasks;
using ImGui.Forms.Localization;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Archive;
using Kuriimu2.ImGui.Models;

namespace Kuriimu2.ImGui.Interfaces
{
    internal interface IMainForm
    {
        Task<bool> OpenFile(IFileState fileState, IArchiveFile file, Guid pluginId);
        Task<bool> SaveFile(IFileState fileState, bool saveAs);
        Task<bool> CloseFile(IFileState fileState, IArchiveFile file);
        void RenameFile(IFileState fileState, IArchiveFile file, UPath newPath);

        void Update(IFileState fileState, bool updateParents, bool updateChildren);

        void ReportStatus(StatusKind kind, LocalizedString message);
        void ClearStatus();
    }
}
