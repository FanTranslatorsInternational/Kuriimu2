using System;
using System.Threading.Tasks;
using ImGui.Forms.Localization;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.FileSystem;

namespace Kuriimu2.ImGui.Interfaces
{
    public interface IFormCommunicator
    {
        Task<bool> Save(bool saveAs);
        void Update(bool updateParents, bool updateChildren);
        void ReportStatus(bool isSuccessful, LocalizedString message);
    }

    public interface IArchiveFormCommunicator : IFormCommunicator
    {
        Task<bool> Open(IArchiveFileInfo file);
        Task<bool> Open(IArchiveFileInfo file, Guid pluginId);
        Task<bool> Close(IArchiveFileInfo file);

        void Rename(IArchiveFileInfo file, UPath renamedPath);
    }
}
