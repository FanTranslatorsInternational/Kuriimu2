using System.Drawing;
using System.Threading.Tasks;

namespace Kuriimu2.WinForms.MainForms.Interfaces
{
    public interface IFormCommunicator
    {
        Task<bool> Save(bool saveAs);
        void Update(bool updateParents, bool updateChildren);
        void ReportStatus(bool isSuccessful, string message);

        // TODO: Report progress
    }
}
