using System;
using System.Threading.Tasks;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace Kuriimu2.EtoForms.Forms.Interfaces
{
    public interface IFormCommunicator
    {
        Task<bool> Save(bool saveAs);
        void Update(bool updateParents, bool updateChildren);
        void ReportStatus(bool isSuccessful, string message);
    }

    public interface IArchiveFormCommunicator : IFormCommunicator
    {
        Task<bool> Open(IArchiveFileInfo file);
        Task<bool> Open(IArchiveFileInfo file, Guid pluginId);
        Task<bool> Close(IArchiveFileInfo file);

        void Rename(IArchiveFileInfo file, UPath renamedPath);
    }
}
