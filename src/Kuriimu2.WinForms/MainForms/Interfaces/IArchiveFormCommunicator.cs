using System;
using System.Threading.Tasks;
using Kontract.Models.Archive;

namespace Kuriimu2.WinForms.MainForms.Interfaces
{
    public interface IArchiveFormCommunicator : IFormCommunicator
    {
        Task<bool> Open(ArchiveFileInfo file);
        Task<bool> Open(ArchiveFileInfo file, Guid pluginId);
        Task<bool> Close(ArchiveFileInfo file);
    }
}
