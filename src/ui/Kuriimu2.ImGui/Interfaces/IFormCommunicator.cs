using System;
using System.Threading.Tasks;
using ImGui.Forms.Localization;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Plugin.File.Archive;
using Kuriimu2.ImGui.Models;

namespace Kuriimu2.ImGui.Interfaces
{
    internal interface IFormCommunicator
    {
        Task<bool> Save(bool saveAs);
        void Update(bool updateParents, bool updateChildren);
        void ReportStatus(StatusKind status, LocalizedString message);
    }

    internal interface IArchiveFormCommunicator : IFormCommunicator
    {
        Task<bool> Open(IArchiveFile file);
        Task<bool> Open(IArchiveFile file, Guid pluginId);
        Task<bool> Close(IArchiveFile file);

        void Rename(IArchiveFile file, UPath renamedPath);
    }
}
