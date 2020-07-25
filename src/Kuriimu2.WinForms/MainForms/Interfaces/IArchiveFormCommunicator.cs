using System;
using System.Threading.Tasks;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace Kuriimu2.WinForms.MainForms.Interfaces
{
    public interface IArchiveFormCommunicator : IFormCommunicator
    {
        Task<bool> Open(ArchiveFileInfo file);
        Task<bool> Open(ArchiveFileInfo file, Guid pluginId);
        Task<bool> Close(ArchiveFileInfo file);

        void Rename(ArchiveFileInfo file, UPath renamedPath);
    }
}
