using System;
using System.Threading.Tasks;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace Kuriimu2.WinForms.MainForms.Interfaces
{
    public interface IArchiveFormCommunicator : IFormCommunicator
    {
        Task<bool> Open(IArchiveFileInfo file);
        Task<bool> Open(IArchiveFileInfo file, Guid pluginId);
        Task<bool> Close(IArchiveFileInfo file);

        void Rename(IArchiveFileInfo file, UPath renamedPath);
    }
}
