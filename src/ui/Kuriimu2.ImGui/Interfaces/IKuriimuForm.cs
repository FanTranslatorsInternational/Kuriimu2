using ImGui.Forms.Models;

namespace Kuriimu2.ImGui.Interfaces
{
    interface IKuriimuForm
    {
        void UpdateForm();

        bool HasRunningOperations();

        void CancelOperations();
    }
}
