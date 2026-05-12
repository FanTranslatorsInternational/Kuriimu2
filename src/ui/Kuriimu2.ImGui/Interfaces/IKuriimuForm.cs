namespace Kuriimu2.ImGui.Interfaces
{
    internal interface IKuriimuForm
    {
        void UpdateForm();

        bool HasRunningOperations();

        void CancelOperations();
    }
}
