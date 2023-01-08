using System;
using System.Threading.Tasks;
using ImGui.Forms.Localization;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.FileSystem;
using Kuriimu2.ImGui.Models;

namespace Kuriimu2.ImGui.Interfaces
{
    interface IFormCommunicator
    {
        Task<bool> Save(bool saveAs);
        void Update(bool updateParents, bool updateChildren);
        void ReportStatus(StatusKind status, LocalizedString message);
    }

    interface IArchiveFormCommunicator : IFormCommunicator
    {
        Task<bool> Open(IArchiveFileInfo file);
        Task<bool> Open(IArchiveFileInfo file, Guid pluginId);
        Task<bool> Close(IArchiveFileInfo file);

        void Rename(IArchiveFileInfo file, UPath renamedPath);
    }
}
