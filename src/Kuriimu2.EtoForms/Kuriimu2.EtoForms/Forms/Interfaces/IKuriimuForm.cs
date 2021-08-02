namespace Kuriimu2.EtoForms.Forms.Interfaces
{
    public interface IKuriimuForm
    {
        void UpdateForm();

        bool HasRunningOperations();

        void CancelOperations();
    }
}
